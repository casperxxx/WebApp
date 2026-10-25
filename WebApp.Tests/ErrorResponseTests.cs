using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using WebApp.Application.Models;
using WebApp.Application.Services;
using WebApp.Infrastructure.DataAccess;

namespace WebApp.Tests;

public class ErrorResponseTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ErrorResponseTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetEvents_InvalidPage_ReturnsProblemJson()
    {
        var response = await _client.GetAsync("/events?page=0&pageSize=10");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(400, body.GetProperty("status").GetInt32());
        Assert.True(body.TryGetProperty("traceId", out _));
        Assert.True(body.TryGetProperty("type", out _));
    }

    [Fact]
    public async Task GetEvents_InvalidPageSize_ReturnsProblemJson()
    {
        var response = await _client.GetAsync("/events?page=1&pageSize=0");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task GetById_NotFound_ReturnsProblemJson()
    {
        var response = await _client.GetAsync($"/events/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(404, body.GetProperty("status").GetInt32());
        Assert.True(body.TryGetProperty("traceId", out _));
    }

    [Fact]
    public async Task Create_WithoutToken_ReturnsUnauthorized()
    {
        var json = """{"title":"x","startAt":"2027-07-10T10:00:00","endAt":"2027-07-10T12:00:00","totalSeats":1}""";
        var response = await _client.PostAsync(
            "/events",
            new StringContent(json, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_InvalidModel_AsAdmin_ReturnsProblemJson()
    {
        var admin = await _factory.CreateAuthenticatedClientAsync($"admin_{Guid.NewGuid():N}", role: "Admin");
        var json = """{"title":"","startAt":"2027-07-10T12:00:00","endAt":"2027-07-10T10:00:00"}""";
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await admin.PostAsync("/events", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(400, body.GetProperty("status").GetInt32());
        Assert.True(body.TryGetProperty("errors", out _));
        Assert.True(body.TryGetProperty("traceId", out _));
    }

    [Fact]
    public async Task Book_NoSeats_ReturnsConflictProblemJson()
    {
        var admin = await _factory.CreateAuthenticatedClientAsync($"admin_{Guid.NewGuid():N}", role: "Admin");
        var user = await _factory.CreateAuthenticatedClientAsync($"user_{Guid.NewGuid():N}", role: "User");

        var start = DateTime.UtcNow.AddDays(30);
        var end = start.AddHours(2);
        var createJson = $$"""
            {
              "title": "Концерт",
              "startAt": "{{start:O}}",
              "endAt": "{{end:O}}",
              "totalSeats": 1
            }
            """;
        var createResponse = await admin.PostAsync(
            "/events",
            new StringContent(createJson, Encoding.UTF8, "application/json"));
        createResponse.EnsureSuccessStatusCode();

        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var eventId = created.GetProperty("id").GetGuid();

        var firstBook = await user.PostAsync($"/events/{eventId}/book", null);
        Assert.Equal(HttpStatusCode.Accepted, firstBook.StatusCode);

        var secondBook = await user.PostAsync($"/events/{eventId}/book", null);

        Assert.Equal(HttpStatusCode.Conflict, secondBook.StatusCode);
        Assert.Equal("application/problem+json", secondBook.Content.Headers.ContentType?.MediaType);

        var body = await secondBook.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(409, body.GetProperty("status").GetInt32());
        Assert.True(body.TryGetProperty("traceId", out _));
    }
}

/// <summary>
/// WebApplicationFactory с InMemory БД и без фонового сервиса
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"ErrorResponseTests_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<AppDbContext>();

            var optionConfigurations = services
                .Where(d => d.ServiceType.IsGenericType
                    && d.ServiceType.GetGenericTypeDefinition() == typeof(IDbContextOptionsConfiguration<>))
                .ToList();

            foreach (var descriptor in optionConfigurations)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));

            var backgroundServices = services
                .Where(d => d.ImplementationType == typeof(BookingBackgroundService))
                .ToList();

            foreach (var descriptor in backgroundServices)
            {
                services.Remove(descriptor);
            }
        });
    }

    /// <summary>
    /// Регистрирует пользователя, логинится и возвращает клиент с Bearer-токеном
    /// </summary>
    public async Task<HttpClient> CreateAuthenticatedClientAsync(string login, string role)
    {
        var client = CreateClient();
        var register = new RegisterRequest
        {
            Login = login,
            Password = "password",
            Role = Enum.Parse<Domain.Entities.UserRole>(role)
        };
        var registerResponse = await client.PostAsJsonAsync("/auth/register", register);
        registerResponse.EnsureSuccessStatusCode();

        var loginResponse = await client.PostAsJsonAsync("/auth/login", new LoginRequest
        {
            Login = login,
            Password = "password"
        });
        loginResponse.EnsureSuccessStatusCode();

        var body = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", body!.Token);
        return client;
    }
}

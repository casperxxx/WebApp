using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace WebApp.Tests;

public class ErrorResponseTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ErrorResponseTests(WebApplicationFactory<Program> factory)
    {
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
    public async Task Create_InvalidModel_ReturnsProblemJson()
    {
        var json = """{"title":"","startAt":"2026-07-10T12:00:00","endAt":"2026-07-10T10:00:00"}""";
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/events", content);

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
        var createJson = """
            {
              "title": "Концерт",
              "startAt": "2026-08-10T10:00:00",
              "endAt": "2026-08-10T12:00:00",
              "totalSeats": 1
            }
            """;
        var createResponse = await _client.PostAsync(
            "/events",
            new StringContent(createJson, Encoding.UTF8, "application/json"));
        createResponse.EnsureSuccessStatusCode();

        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var eventId = created.GetProperty("id").GetGuid();

        var firstBook = await _client.PostAsync($"/events/{eventId}/book", null);
        Assert.Equal(HttpStatusCode.Accepted, firstBook.StatusCode);

        var secondBook = await _client.PostAsync($"/events/{eventId}/book", null);

        Assert.Equal(HttpStatusCode.Conflict, secondBook.StatusCode);
        Assert.Equal("application/problem+json", secondBook.Content.Headers.ContentType?.MediaType);

        var body = await secondBook.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(409, body.GetProperty("status").GetInt32());
        Assert.True(body.TryGetProperty("traceId", out _));
    }
}

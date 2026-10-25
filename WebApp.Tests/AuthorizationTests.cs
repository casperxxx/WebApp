using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace WebApp.Tests;

/// <summary>
/// API-тесты ролей: 401/403 и отмена чужой брони
/// </summary>
public class AuthorizationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AuthorizationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateEvent_AsUser_ReturnsForbidden()
    {
        var user = await _factory.CreateAuthenticatedClientAsync($"user_{Guid.NewGuid():N}", "User");
        var start = DateTime.UtcNow.AddDays(10);
        var json = $$"""
            {
              "title": "Запрещено",
              "startAt": "{{start:O}}",
              "endAt": "{{start.AddHours(1):O}}",
              "totalSeats": 5
            }
            """;

        var response = await user.PostAsync(
            "/events",
            new StringContent(json, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Book_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsync($"/events/{Guid.NewGuid()}/book", null);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Cancel_OtherUsersBooking_AsUser_ReturnsForbidden()
    {
        var admin = await _factory.CreateAuthenticatedClientAsync($"admin_{Guid.NewGuid():N}", "Admin");
        var owner = await _factory.CreateAuthenticatedClientAsync($"owner_{Guid.NewGuid():N}", "User");
        var other = await _factory.CreateAuthenticatedClientAsync($"other_{Guid.NewGuid():N}", "User");

        var start = DateTime.UtcNow.AddDays(20);
        var createJson = $$"""
            {
              "title": "Событие",
              "startAt": "{{start:O}}",
              "endAt": "{{start.AddHours(2):O}}",
              "totalSeats": 5
            }
            """;
        var createResponse = await admin.PostAsync(
            "/events",
            new StringContent(createJson, Encoding.UTF8, "application/json"));
        createResponse.EnsureSuccessStatusCode();
        var eventId = (await createResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();

        var bookResponse = await owner.PostAsync($"/events/{eventId}/book", null);
        bookResponse.EnsureSuccessStatusCode();
        var bookingId = (await bookResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();

        var cancelResponse = await other.DeleteAsync($"/bookings/{bookingId}");
        Assert.Equal(HttpStatusCode.Forbidden, cancelResponse.StatusCode);
    }

    [Fact]
    public async Task Cancel_OtherUsersBooking_AsAdmin_ReturnsNoContent()
    {
        var admin = await _factory.CreateAuthenticatedClientAsync($"admin2_{Guid.NewGuid():N}", "Admin");
        var owner = await _factory.CreateAuthenticatedClientAsync($"owner2_{Guid.NewGuid():N}", "User");

        var start = DateTime.UtcNow.AddDays(25);
        var createJson = $$"""
            {
              "title": "Событие AdminCancel",
              "startAt": "{{start:O}}",
              "endAt": "{{start.AddHours(2):O}}",
              "totalSeats": 5
            }
            """;
        var createResponse = await admin.PostAsync(
            "/events",
            new StringContent(createJson, Encoding.UTF8, "application/json"));
        createResponse.EnsureSuccessStatusCode();
        var eventId = (await createResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();

        var bookResponse = await owner.PostAsync($"/events/{eventId}/book", null);
        bookResponse.EnsureSuccessStatusCode();
        var bookingId = (await bookResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();

        var cancelResponse = await admin.DeleteAsync($"/bookings/{bookingId}");
        Assert.Equal(HttpStatusCode.NoContent, cancelResponse.StatusCode);
    }
}

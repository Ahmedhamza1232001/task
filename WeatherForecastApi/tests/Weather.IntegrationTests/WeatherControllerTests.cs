using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Weather.Application.DTOs;

namespace Weather.IntegrationTests;

public class WeatherControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public WeatherControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<string> GetAccessTokenAsync()
    {
        var email = $"weather_{Guid.NewGuid()}@example.com";
        var registerRequest = new RegisterRequest(email, $"user_{Guid.NewGuid():N}", "password123");

        var response = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();

        return authResponse!.AccessToken;
    }

    [Fact]
    public async Task GetWeather_WithValidTokenAndCity_ReturnsWeatherData()
    {
        // Arrange
        var accessToken = await GetAccessTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        // Act
        var response = await _client.GetAsync("/api/weather?city=London");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var weatherResponse = await response.Content.ReadFromJsonAsync<WeatherResponse>();
        weatherResponse.Should().NotBeNull();
        weatherResponse!.City.Should().Be("London");
        weatherResponse.Condition.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetWeather_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/weather?city=London");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetWeather_WithInvalidToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid_token");

        // Act
        var response = await client.GetAsync("/api/weather?city=London");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetWeather_WithoutCityParameter_ReturnsBadRequest()
    {
        // Arrange
        var accessToken = await GetAccessTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        // Act
        var response = await _client.GetAsync("/api/weather");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetWeather_WithEmptyCity_ReturnsBadRequest()
    {
        // Arrange
        var accessToken = await GetAccessTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        // Act
        var response = await _client.GetAsync("/api/weather?city=");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("London")]
    [InlineData("Paris")]
    [InlineData("Tokyo")]
    [InlineData("New York")]
    public async Task GetWeather_WithDifferentCities_ReturnsWeatherData(string city)
    {
        // Arrange
        var accessToken = await GetAccessTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        // Act
        var response = await _client.GetAsync($"/api/weather?city={Uri.EscapeDataString(city)}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var weatherResponse = await response.Content.ReadFromJsonAsync<WeatherResponse>();
        weatherResponse.Should().NotBeNull();
        weatherResponse!.Condition.Should().NotBeNullOrEmpty();
    }
}

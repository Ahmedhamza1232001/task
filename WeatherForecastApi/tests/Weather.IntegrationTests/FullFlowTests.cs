using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Weather.Application.DTOs;

namespace Weather.IntegrationTests;

public class FullFlowTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public FullFlowTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CompleteFlow_Register_Login_GetWeather_RefreshToken_GetWeather()
    {
        // Step 1: Register a new user
        var email = $"flow_{Guid.NewGuid()}@example.com";
        var username = $"user_{Guid.NewGuid():N}";
        var password = "password123";

        var registerRequest = new RegisterRequest(email, username, password);
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var registerAuthResponse = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();
        registerAuthResponse.Should().NotBeNull();
        registerAuthResponse!.AccessToken.Should().NotBeNullOrEmpty();

        // Step 2: Login with the registered user
        var loginRequest = new LoginRequest(email, password);
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var loginAuthResponse = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        loginAuthResponse.Should().NotBeNull();

        // Step 3: Access weather endpoint with the token
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", loginAuthResponse!.AccessToken);

        var weatherResponse1 = await _client.GetAsync("/api/weather?city=London");
        weatherResponse1.StatusCode.Should().Be(HttpStatusCode.OK);

        var weather1 = await weatherResponse1.Content.ReadFromJsonAsync<WeatherResponse>();
        weather1.Should().NotBeNull();
        weather1!.City.Should().Be("London");

        // Step 4: Refresh the token
        _client.DefaultRequestHeaders.Authorization = null;
        var refreshRequest = new RefreshTokenRequest(loginAuthResponse.RefreshToken);
        var refreshResponse = await _client.PostAsJsonAsync("/api/auth/refresh", refreshRequest);

        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var refreshAuthResponse = await refreshResponse.Content.ReadFromJsonAsync<AuthResponse>();
        refreshAuthResponse.Should().NotBeNull();
        refreshAuthResponse!.AccessToken.Should().NotBe(loginAuthResponse.AccessToken);
        refreshAuthResponse.RefreshToken.Should().NotBe(loginAuthResponse.RefreshToken);

        // Step 5: Access weather endpoint with the new token
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", refreshAuthResponse.AccessToken);

        var weatherResponse2 = await _client.GetAsync("/api/weather?city=Paris");
        weatherResponse2.StatusCode.Should().Be(HttpStatusCode.OK);

        var weather2 = await weatherResponse2.Content.ReadFromJsonAsync<WeatherResponse>();
        weather2.Should().NotBeNull();
        weather2!.City.Should().Be("Paris");

        // Step 6: Verify old refresh token no longer works
        _client.DefaultRequestHeaders.Authorization = null;
        var oldRefreshRequest = new RefreshTokenRequest(loginAuthResponse.RefreshToken);
        var oldRefreshResponse = await _client.PostAsJsonAsync("/api/auth/refresh", oldRefreshRequest);

        oldRefreshResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UnauthorizedAccess_WeatherEndpoint_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/weather?city=London");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task TokenRotation_OldRefreshTokenInvalidated()
    {
        // Arrange - Register user
        var email = $"rotation_{Guid.NewGuid()}@example.com";
        var registerRequest = new RegisterRequest(email, $"user_{Guid.NewGuid():N}", "password123");
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        var authResponse = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();

        // Act - Use refresh token
        var refreshRequest = new RefreshTokenRequest(authResponse!.RefreshToken);
        var firstRefreshResponse = await _client.PostAsJsonAsync("/api/auth/refresh", refreshRequest);
        firstRefreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Try to use the same refresh token again
        var secondRefreshResponse = await _client.PostAsJsonAsync("/api/auth/refresh", refreshRequest);

        // Assert - Old token should be invalid
        secondRefreshResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

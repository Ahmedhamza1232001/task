using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Weather.Application.DTOs;

namespace Weather.IntegrationTests;

public class AuthControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_WithValidData_ReturnsTokens()
    {
        // Arrange
        var request = new RegisterRequest(
            $"test_{Guid.NewGuid()}@example.com",
            $"user_{Guid.NewGuid():N}",
            "password123");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
        authResponse.Should().NotBeNull();
        authResponse!.AccessToken.Should().NotBeNullOrEmpty();
        authResponse.RefreshToken.Should().NotBeNullOrEmpty();
        authResponse.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsConflict()
    {
        // Arrange
        var email = $"duplicate_{Guid.NewGuid()}@example.com";
        var request1 = new RegisterRequest(email, $"user1_{Guid.NewGuid():N}", "password123");
        var request2 = new RegisterRequest(email, $"user2_{Guid.NewGuid():N}", "password123");

        // First registration
        await _client.PostAsJsonAsync("/api/auth/register", request1);

        // Act - Second registration with same email
        var response = await _client.PostAsJsonAsync("/api/auth/register", request2);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Register_WithInvalidData_ReturnsBadRequest()
    {
        // Arrange
        var request = new RegisterRequest("", "", "");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsTokens()
    {
        // Arrange
        var email = $"login_{Guid.NewGuid()}@example.com";
        var password = "password123";
        var registerRequest = new RegisterRequest(email, $"user_{Guid.NewGuid():N}", password);

        await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        var loginRequest = new LoginRequest(email, password);

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
        authResponse.Should().NotBeNull();
        authResponse!.AccessToken.Should().NotBeNullOrEmpty();
        authResponse.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var loginRequest = new LoginRequest("nonexistent@example.com", "wrongpassword");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_WithValidToken_ReturnsNewTokens()
    {
        // Arrange
        var email = $"refresh_{Guid.NewGuid()}@example.com";
        var registerRequest = new RegisterRequest(email, $"user_{Guid.NewGuid():N}", "password123");

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        var authResponse = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();

        var refreshRequest = new RefreshTokenRequest(authResponse!.RefreshToken);

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/refresh", refreshRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var newAuthResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
        newAuthResponse.Should().NotBeNull();
        newAuthResponse!.AccessToken.Should().NotBeNullOrEmpty();
        newAuthResponse.RefreshToken.Should().NotBeNullOrEmpty();
        // New tokens should be different from old ones
        newAuthResponse.AccessToken.Should().NotBe(authResponse.AccessToken);
        newAuthResponse.RefreshToken.Should().NotBe(authResponse.RefreshToken);
    }

    [Fact]
    public async Task Refresh_WithInvalidToken_ReturnsUnauthorized()
    {
        // Arrange
        var refreshRequest = new RefreshTokenRequest("invalid_refresh_token");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/refresh", refreshRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_WithUsedToken_ReturnsUnauthorized()
    {
        // Arrange
        var email = $"used_{Guid.NewGuid()}@example.com";
        var registerRequest = new RegisterRequest(email, $"user_{Guid.NewGuid():N}", "password123");

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        var authResponse = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();

        var refreshRequest = new RefreshTokenRequest(authResponse!.RefreshToken);

        // First refresh - should succeed
        await _client.PostAsJsonAsync("/api/auth/refresh", refreshRequest);

        // Act - Second refresh with same token - should fail
        var response = await _client.PostAsJsonAsync("/api/auth/refresh", refreshRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

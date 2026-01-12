using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Weather.Domain.Entities;
using Weather.Infrastructure.Configuration;
using Weather.Infrastructure.Services;

namespace Weather.UnitTests.Infrastructure;

public class TokenServiceTests
{
    private readonly JwtTokenService _tokenService;
    private readonly JwtSettings _jwtSettings;

    public TokenServiceTests()
    {
        _jwtSettings = new JwtSettings
        {
            SecretKey = "ThisIsAVerySecureSecretKeyForJwtTokenGenerationMinimum32Characters!",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            AccessTokenExpirationMinutes = 15,
            RefreshTokenExpirationDays = 7
        };

        _tokenService = new JwtTokenService(Options.Create(_jwtSettings));
    }

    [Fact]
    public void GenerateAccessToken_ReturnsValidJwtToken()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            Username = "testuser"
        };

        // Act
        var token = _tokenService.GenerateAccessToken(user);

        // Assert
        token.Should().NotBeNullOrEmpty();

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        jwtToken.Issuer.Should().Be(_jwtSettings.Issuer);
        jwtToken.Audiences.Should().Contain(_jwtSettings.Audience);
    }

    [Fact]
    public void GenerateAccessToken_ContainsCorrectClaims()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            Username = "testuser"
        };

        // Act
        var token = _tokenService.GenerateAccessToken(user);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == userId.ToString());
        jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == user.Email);
        jwtToken.Claims.Should().Contain(c => c.Type == ClaimTypes.Name && c.Value == user.Username);
        jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Jti);
    }

    [Fact]
    public void GenerateAccessToken_HasCorrectExpiration()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            Username = "testuser"
        };

        // Act
        var beforeGeneration = DateTime.UtcNow;
        var token = _tokenService.GenerateAccessToken(user);
        var afterGeneration = DateTime.UtcNow;

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        var expectedEarliestExpiration = beforeGeneration.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);
        var expectedLatestExpiration = afterGeneration.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);

        jwtToken.ValidTo.Should().BeOnOrAfter(expectedEarliestExpiration.AddSeconds(-1));
        jwtToken.ValidTo.Should().BeOnOrBefore(expectedLatestExpiration.AddSeconds(1));
    }

    [Fact]
    public void GenerateRefreshToken_ReturnsUniqueTokens()
    {
        // Act
        var token1 = _tokenService.GenerateRefreshToken();
        var token2 = _tokenService.GenerateRefreshToken();

        // Assert
        token1.Should().NotBeNullOrEmpty();
        token2.Should().NotBeNullOrEmpty();
        token1.Should().NotBe(token2);
    }

    [Fact]
    public void GenerateRefreshToken_ReturnsBase64String()
    {
        // Act
        var token = _tokenService.GenerateRefreshToken();

        // Assert
        var action = () => Convert.FromBase64String(token);
        action.Should().NotThrow();
    }

    [Fact]
    public void GetAccessTokenExpiration_ReturnsCorrectTime()
    {
        // Act
        var beforeCall = DateTime.UtcNow;
        var expiration = _tokenService.GetAccessTokenExpiration();
        var afterCall = DateTime.UtcNow;

        // Assert
        var expectedEarliest = beforeCall.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);
        var expectedLatest = afterCall.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);

        expiration.Should().BeOnOrAfter(expectedEarliest.AddSeconds(-1));
        expiration.Should().BeOnOrBefore(expectedLatest.AddSeconds(1));
    }

    [Fact]
    public void GetRefreshTokenExpiration_ReturnsCorrectTime()
    {
        // Act
        var beforeCall = DateTime.UtcNow;
        var expiration = _tokenService.GetRefreshTokenExpiration();
        var afterCall = DateTime.UtcNow;

        // Assert
        var expectedEarliest = beforeCall.AddDays(_jwtSettings.RefreshTokenExpirationDays);
        var expectedLatest = afterCall.AddDays(_jwtSettings.RefreshTokenExpirationDays);

        expiration.Should().BeOnOrAfter(expectedEarliest.AddSeconds(-1));
        expiration.Should().BeOnOrBefore(expectedLatest.AddSeconds(1));
    }
}

using FluentAssertions;
using Moq;
using Weather.Application.DTOs;
using Weather.Application.Exceptions;
using Weather.Application.Interfaces;
using Weather.Application.Services;
using Weather.Domain.Entities;

namespace Weather.UnitTests.Services;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _refreshTokenRepositoryMock = new Mock<IRefreshTokenRepository>();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _tokenServiceMock = new Mock<ITokenService>();

        _authService = new AuthService(
            _userRepositoryMock.Object,
            _refreshTokenRepositoryMock.Object,
            _passwordHasherMock.Object,
            _tokenServiceMock.Object);
    }

    [Fact]
    public async Task RegisterAsync_WithValidRequest_ReturnsAuthResponse()
    {
        // Arrange
        var request = new RegisterRequest("test@example.com", "testuser", "password123");

        _userRepositoryMock.Setup(x => x.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _userRepositoryMock.Setup(x => x.ExistsByUsernameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _userRepositoryMock.Setup(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User u, CancellationToken _) => u);
        _passwordHasherMock.Setup(x => x.Hash(It.IsAny<string>()))
            .Returns("hashedPassword");
        _tokenServiceMock.Setup(x => x.GenerateAccessToken(It.IsAny<User>()))
            .Returns("access_token");
        _tokenServiceMock.Setup(x => x.GenerateRefreshToken())
            .Returns("refresh_token");
        _tokenServiceMock.Setup(x => x.GetAccessTokenExpiration())
            .Returns(DateTime.UtcNow.AddMinutes(15));
        _tokenServiceMock.Setup(x => x.GetRefreshTokenExpiration())
            .Returns(DateTime.UtcNow.AddDays(7));
        _refreshTokenRepositoryMock.Setup(x => x.CreateAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken rt, CancellationToken _) => rt);

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be("access_token");
        result.RefreshToken.Should().Be("refresh_token");
    }

    [Fact]
    public async Task RegisterAsync_WithExistingEmail_ThrowsConflictException()
    {
        // Arrange
        var request = new RegisterRequest("existing@example.com", "testuser", "password123");

        _userRepositoryMock.Setup(x => x.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act & Assert
        await _authService.Invoking(s => s.RegisterAsync(request))
            .Should().ThrowAsync<ConflictException>()
            .WithMessage("A user with this email already exists.");
    }

    [Fact]
    public async Task RegisterAsync_WithExistingUsername_ThrowsConflictException()
    {
        // Arrange
        var request = new RegisterRequest("test@example.com", "existinguser", "password123");

        _userRepositoryMock.Setup(x => x.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _userRepositoryMock.Setup(x => x.ExistsByUsernameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act & Assert
        await _authService.Invoking(s => s.RegisterAsync(request))
            .Should().ThrowAsync<ConflictException>()
            .WithMessage("A user with this username already exists.");
    }

    [Theory]
    [InlineData("", "username", "password123", "Email is required.")]
    [InlineData("email@test.com", "", "password123", "Username is required.")]
    [InlineData("email@test.com", "username", "", "Password is required.")]
    [InlineData("email@test.com", "username", "12345", "Password must be at least 6 characters long.")]
    public async Task RegisterAsync_WithInvalidRequest_ThrowsValidationException(
        string email, string username, string password, string expectedMessage)
    {
        // Arrange
        var request = new RegisterRequest(email, username, password);

        // Act & Assert
        await _authService.Invoking(s => s.RegisterAsync(request))
            .Should().ThrowAsync<ValidationException>()
            .WithMessage(expectedMessage);
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsAuthResponse()
    {
        // Arrange
        var request = new LoginRequest("test@example.com", "password123");
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            Username = "testuser",
            PasswordHash = "hashedPassword"
        };

        _userRepositoryMock.Setup(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasherMock.Setup(x => x.Verify(request.Password, user.PasswordHash))
            .Returns(true);
        _tokenServiceMock.Setup(x => x.GenerateAccessToken(user))
            .Returns("access_token");
        _tokenServiceMock.Setup(x => x.GenerateRefreshToken())
            .Returns("refresh_token");
        _tokenServiceMock.Setup(x => x.GetAccessTokenExpiration())
            .Returns(DateTime.UtcNow.AddMinutes(15));
        _tokenServiceMock.Setup(x => x.GetRefreshTokenExpiration())
            .Returns(DateTime.UtcNow.AddDays(7));
        _refreshTokenRepositoryMock.Setup(x => x.CreateAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken rt, CancellationToken _) => rt);

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be("access_token");
        result.RefreshToken.Should().Be("refresh_token");
    }

    [Fact]
    public async Task LoginAsync_WithInvalidEmail_ThrowsUnauthorizedException()
    {
        // Arrange
        var request = new LoginRequest("nonexistent@example.com", "password123");

        _userRepositoryMock.Setup(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act & Assert
        await _authService.Invoking(s => s.LoginAsync(request))
            .Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("Invalid email or password.");
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ThrowsUnauthorizedException()
    {
        // Arrange
        var request = new LoginRequest("test@example.com", "wrongpassword");
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            PasswordHash = "hashedPassword"
        };

        _userRepositoryMock.Setup(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasherMock.Setup(x => x.Verify(request.Password, user.PasswordHash))
            .Returns(false);

        // Act & Assert
        await _authService.Invoking(s => s.LoginAsync(request))
            .Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("Invalid email or password.");
    }

    [Fact]
    public async Task RefreshTokenAsync_WithValidToken_ReturnsNewAuthResponse()
    {
        // Arrange
        var request = new RefreshTokenRequest("valid_refresh_token");
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            Username = "testuser"
        };
        var storedToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = "valid_refresh_token",
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            IsRevoked = false
        };

        _refreshTokenRepositoryMock.Setup(x => x.GetByTokenAsync(request.RefreshToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(storedToken);
        _userRepositoryMock.Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _tokenServiceMock.Setup(x => x.GenerateAccessToken(user))
            .Returns("new_access_token");
        _tokenServiceMock.Setup(x => x.GenerateRefreshToken())
            .Returns("new_refresh_token");
        _tokenServiceMock.Setup(x => x.GetAccessTokenExpiration())
            .Returns(DateTime.UtcNow.AddMinutes(15));
        _tokenServiceMock.Setup(x => x.GetRefreshTokenExpiration())
            .Returns(DateTime.UtcNow.AddDays(7));
        _refreshTokenRepositoryMock.Setup(x => x.CreateAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken rt, CancellationToken _) => rt);

        // Act
        var result = await _authService.RefreshTokenAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be("new_access_token");
        result.RefreshToken.Should().Be("new_refresh_token");
        _refreshTokenRepositoryMock.Verify(x => x.RevokeAsync(storedToken, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RefreshTokenAsync_WithExpiredToken_ThrowsUnauthorizedException()
    {
        // Arrange
        var request = new RefreshTokenRequest("expired_refresh_token");
        var storedToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = "expired_refresh_token",
            ExpiresAt = DateTime.UtcNow.AddDays(-1),
            IsRevoked = false
        };

        _refreshTokenRepositoryMock.Setup(x => x.GetByTokenAsync(request.RefreshToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(storedToken);

        // Act & Assert
        await _authService.Invoking(s => s.RefreshTokenAsync(request))
            .Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("Refresh token has expired.");
    }

    [Fact]
    public async Task RefreshTokenAsync_WithRevokedToken_ThrowsUnauthorizedException()
    {
        // Arrange
        var request = new RefreshTokenRequest("revoked_refresh_token");
        var storedToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = "revoked_refresh_token",
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            IsRevoked = true
        };

        _refreshTokenRepositoryMock.Setup(x => x.GetByTokenAsync(request.RefreshToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(storedToken);

        // Act & Assert
        await _authService.Invoking(s => s.RefreshTokenAsync(request))
            .Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("Refresh token has been revoked.");
    }
}

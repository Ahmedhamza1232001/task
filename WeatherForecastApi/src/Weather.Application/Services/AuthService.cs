using Weather.Application.DTOs;
using Weather.Application.Exceptions;
using Weather.Application.Interfaces;
using Weather.Domain.Entities;

namespace Weather.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;

    public AuthService(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordHasher passwordHasher,
        ITokenService tokenService)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            throw new ValidationException("Email is required.");

        if (string.IsNullOrWhiteSpace(request.Username))
            throw new ValidationException("Username is required.");

        if (string.IsNullOrWhiteSpace(request.Password))
            throw new ValidationException("Password is required.");

        if (request.Password.Length < 6)
            throw new ValidationException("Password must be at least 6 characters long.");

        if (await _userRepository.ExistsByEmailAsync(request.Email, cancellationToken))
            throw new ConflictException("A user with this email already exists.");

        if (await _userRepository.ExistsByUsernameAsync(request.Username, cancellationToken))
            throw new ConflictException("A user with this username already exists.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email.ToLowerInvariant(),
            Username = request.Username,
            PasswordHash = _passwordHasher.Hash(request.Password),
            CreatedAt = DateTime.UtcNow
        };

        await _userRepository.CreateAsync(user, cancellationToken);

        return await GenerateAuthResponseAsync(user, cancellationToken);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            throw new ValidationException("Email is required.");

        if (string.IsNullOrWhiteSpace(request.Password))
            throw new ValidationException("Password is required.");

        var user = await _userRepository.GetByEmailAsync(request.Email.ToLowerInvariant(), cancellationToken);

        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedException("Invalid email or password.");

        return await GenerateAuthResponseAsync(user, cancellationToken);
    }

    public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            throw new ValidationException("Refresh token is required.");

        var storedToken = await _refreshTokenRepository.GetByTokenAsync(request.RefreshToken, cancellationToken);

        if (storedToken is null)
            throw new UnauthorizedException("Invalid refresh token.");

        if (storedToken.IsRevoked)
            throw new UnauthorizedException("Refresh token has been revoked.");

        if (storedToken.ExpiresAt < DateTime.UtcNow)
            throw new UnauthorizedException("Refresh token has expired.");

        await _refreshTokenRepository.RevokeAsync(storedToken, cancellationToken);

        var user = await _userRepository.GetByIdAsync(storedToken.UserId, cancellationToken);

        if (user is null)
            throw new UnauthorizedException("User not found.");

        return await GenerateAuthResponseAsync(user, cancellationToken);
    }

    private async Task<AuthResponse> GenerateAuthResponseAsync(User user, CancellationToken cancellationToken)
    {
        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshTokenValue = _tokenService.GenerateRefreshToken();
        var accessTokenExpiration = _tokenService.GetAccessTokenExpiration();

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = refreshTokenValue,
            UserId = user.Id,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = _tokenService.GetRefreshTokenExpiration(),
            IsRevoked = false
        };

        await _refreshTokenRepository.CreateAsync(refreshToken, cancellationToken);

        return new AuthResponse(accessToken, refreshTokenValue, accessTokenExpiration);
    }
}

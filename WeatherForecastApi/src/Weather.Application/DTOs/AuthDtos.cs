namespace Weather.Application.DTOs;

public record RegisterRequest(string Email, string Username, string Password);

public record LoginRequest(string Email, string Password);

public record AuthResponse(string AccessToken, string RefreshToken, DateTime ExpiresAt);

public record RefreshTokenRequest(string RefreshToken);

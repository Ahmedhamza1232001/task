# Weather Forecast API

A production-ready ASP.NET Core Web API for weather forecasting with JWT authentication, refresh token rotation, and caching.

## Project Overview

This API provides weather data for various cities with secure authentication. Key features include:

- **JWT Authentication** with refresh token rotation
- **Weather data** from a mocked service (configurable for real API integration)
- **In-memory caching** for performance optimization
- **Clean Architecture** for maintainability and testability
- **Comprehensive testing** with unit and integration tests

## Architecture

The solution follows **Clean Architecture** principles with four layers:

```
WeatherForecastApi/
├── src/
│   ├── Weather.Api/           # Presentation Layer - Controllers, Middleware
│   ├── Weather.Application/   # Application Layer - Services, DTOs, Interfaces
│   ├── Weather.Domain/        # Domain Layer - Entities
│   └── Weather.Infrastructure/# Infrastructure Layer - EF Core, Auth, Caching
├── tests/
│   ├── Weather.UnitTests/     # Unit tests
│   └── Weather.IntegrationTests/ # Integration tests
├── Dockerfile
└── README.md
```

### Layer Responsibilities

| Layer | Responsibility |
|-------|----------------|
| **Domain** | Core business entities (User, RefreshToken, WeatherData) |
| **Application** | Business logic, DTOs, service interfaces |
| **Infrastructure** | Data access, external services, authentication |
| **API** | HTTP endpoints, middleware, DI configuration |

### Dependency Flow

```
API → Application ← Infrastructure
         ↓
       Domain
```

## Authentication Flow

```
┌─────────────────────────────────────────────────────────────────┐
│                     AUTHENTICATION FLOW                          │
└─────────────────────────────────────────────────────────────────┘

1. REGISTRATION
   ┌────────┐    POST /api/auth/register     ┌────────┐
   │ Client │ ─────────────────────────────► │  API   │
   └────────┘    {email, username, password} └────────┘
                                                  │
                                                  ▼
                                          ┌──────────────┐
                                          │ Hash Password│
                                          │ Store User   │
                                          │ Generate JWT │
                                          │ + Refresh    │
                                          └──────────────┘
                                                  │
   ┌────────┐    {accessToken, refreshToken}     │
   │ Client │ ◄──────────────────────────────────┘
   └────────┘

2. LOGIN
   ┌────────┐    POST /api/auth/login        ┌────────┐
   │ Client │ ─────────────────────────────► │  API   │
   └────────┘    {email, password}           └────────┘
                                                  │
                                                  ▼
                                          ┌──────────────┐
                                          │Verify Password│
                                          │ Generate JWT │
                                          │ + Refresh    │
                                          └──────────────┘
                                                  │
   ┌────────┐    {accessToken, refreshToken}     │
   │ Client │ ◄──────────────────────────────────┘
   └────────┘

3. ACCESSING PROTECTED RESOURCES
   ┌────────┐  GET /api/weather?city=London  ┌────────┐
   │ Client │ ─────────────────────────────► │  API   │
   └────────┘  Authorization: Bearer {token} └────────┘
                                                  │
                                                  ▼
                                          ┌──────────────┐
                                          │ Validate JWT │
                                          │ Return Data  │
                                          └──────────────┘
                                                  │
   ┌────────┐    {city, temperature, ...}        │
   │ Client │ ◄──────────────────────────────────┘
   └────────┘

4. TOKEN REFRESH (Rotation)
   ┌────────┐    POST /api/auth/refresh      ┌────────┐
   │ Client │ ─────────────────────────────► │  API   │
   └────────┘    {refreshToken}              └────────┘
                                                  │
                                                  ▼
                                          ┌──────────────┐
                                          │Validate Token│
                                          │ Revoke Old   │
                                          │ Issue New    │
                                          └──────────────┘
                                                  │
   ┌────────┐  {NEW accessToken, refreshToken}   │
   │ Client │ ◄──────────────────────────────────┘
   └────────┘
```

## API Endpoints

| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| POST | `/api/auth/register` | Register new user | No |
| POST | `/api/auth/login` | Login and get tokens | No |
| POST | `/api/auth/refresh` | Refresh access token | No |
| GET | `/api/weather?city={city}` | Get weather for city | Yes |
| GET | `/health` | Health check endpoint | No |

## How to Run Locally

### Prerequisites

- .NET 9.0 SDK or later
- (Optional) Docker for containerized deployment

### Running the Application

1. **Clone the repository**
   ```bash
   cd WeatherForecastApi
   ```

2. **Restore dependencies**
   ```bash
   dotnet restore
   ```

3. **Run the application**
   ```bash
   dotnet run --project src/Weather.Api
   ```

4. **Access Swagger UI**

   Open your browser and navigate to: `https://localhost:5001` or `http://localhost:5000`

### Environment Configuration

The application uses `appsettings.json` for configuration:

```json
{
  "Jwt": {
    "SecretKey": "your-secret-key-min-32-chars",
    "Issuer": "WeatherForecastApi",
    "Audience": "WeatherForecastApiUsers",
    "AccessTokenExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 7
  }
}
```

## How to Run Tests

### Run All Tests

```bash
dotnet test
```

### Run with Verbosity

```bash
dotnet test --verbosity normal
```

### Run Specific Test Project

```bash
# Unit tests only
dotnet test tests/Weather.UnitTests

# Integration tests only
dotnet test tests/Weather.IntegrationTests
```

### Test Coverage

The test suite includes:

- **45 Unit Tests** covering:
  - Authentication service logic
  - Password hashing
  - JWT token generation
  - Weather service behavior
  - Cache hit/miss scenarios

- **20 Integration Tests** covering:
  - Complete registration flow
  - Login with valid/invalid credentials
  - Token refresh and rotation
  - Protected endpoint access
  - Full end-to-end scenarios

## How to Run with Docker

### Build the Image

```bash
docker build -t weather-api .
```

### Run the Container

```bash
docker run -p 8080:8080 weather-api
```

### Access the API

- API Base URL: `http://localhost:8080`
- Health Check: `http://localhost:8080/health`

### Docker Compose (Optional)

Create a `docker-compose.yml`:

```yaml
version: '3.8'
services:
  weather-api:
    build: .
    ports:
      - "8080:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - Jwt__SecretKey=YourProductionSecretKeyHere123!
```

Run with:
```bash
docker-compose up
```

## Performance & Caching

### Caching Strategy

The API implements **in-memory caching** for weather data:

- **Cache Key Format**: `weather:{city}` (case-insensitive)
- **Cache Duration**: 5 minutes
- **Cache Provider**: `IMemoryCache`

### How Caching Works

```
┌─────────┐     GET /weather?city=London     ┌─────────────┐
│ Client  │ ────────────────────────────────►│   API       │
└─────────┘                                  └──────┬──────┘
                                                    │
                                                    ▼
                                            ┌──────────────┐
                                            │ Check Cache  │
                                            └──────┬───────┘
                                                   │
                              ┌────────────────────┴────────────────────┐
                              │                                         │
                        Cache HIT                                  Cache MISS
                              │                                         │
                              ▼                                         ▼
                    ┌──────────────┐                          ┌──────────────┐
                    │Return Cached │                          │ Call Weather │
                    │    Data      │                          │   Service    │
                    └──────────────┘                          └──────┬───────┘
                                                                     │
                                                                     ▼
                                                            ┌──────────────┐
                                                            │ Store in     │
                                                            │ Cache (5min) │
                                                            └──────────────┘
```

### Benefits

1. **Reduced latency** for repeated requests
2. **Lower load** on weather data source
3. **Consistent responses** within cache window

## Security Notes

### Password Security

- Passwords are hashed using **BCrypt** with automatic salt generation
- Plain text passwords are never stored

### JWT Security

- Access tokens expire after **15 minutes**
- Refresh tokens expire after **7 days**
- Refresh tokens are **rotated** on each use (one-time use)
- Used refresh tokens are **revoked** immediately

### Best Practices Implemented

1. **No secrets in code** - Configuration via environment variables
2. **Token rotation** - Prevents refresh token theft attacks
3. **Short-lived access tokens** - Limits exposure window
4. **Server-side refresh token storage** - Enables revocation
5. **Centralized error handling** - Prevents information leakage
6. **Input validation** - Prevents injection attacks

### Production Recommendations

- Use HTTPS in production
- Store JWT secret key securely (e.g., Azure Key Vault, AWS Secrets Manager)
- Consider rate limiting for authentication endpoints
- Implement logging and monitoring
- Replace InMemory database with a production database (PostgreSQL, SQL Server)

## Project Structure Details

```
src/
├── Weather.Api/
│   ├── Controllers/
│   │   ├── AuthController.cs      # Authentication endpoints
│   │   └── WeatherController.cs   # Weather data endpoint
│   ├── Middleware/
│   │   └── ExceptionHandlingMiddleware.cs
│   ├── Program.cs                 # Application entry point
│   └── appsettings.json           # Configuration
│
├── Weather.Application/
│   ├── DTOs/
│   │   ├── AuthDtos.cs           # Auth request/response DTOs
│   │   └── WeatherDtos.cs        # Weather response DTO
│   ├── Exceptions/               # Custom exceptions
│   ├── Interfaces/               # Service contracts
│   └── Services/
│       └── AuthService.cs        # Authentication logic
│
├── Weather.Domain/
│   └── Entities/
│       ├── User.cs
│       ├── RefreshToken.cs
│       └── WeatherData.cs
│
└── Weather.Infrastructure/
    ├── Configuration/
    │   └── JwtSettings.cs
    ├── Data/
    │   └── ApplicationDbContext.cs
    ├── Repositories/
    │   ├── UserRepository.cs
    │   └── RefreshTokenRepository.cs
    ├── Services/
    │   ├── BcryptPasswordHasher.cs
    │   ├── JwtTokenService.cs
    │   ├── MockWeatherService.cs
    │   └── CachedWeatherService.cs
    └── DependencyInjection.cs
```

## Technology Stack

| Component | Technology |
|-----------|------------|
| Framework | ASP.NET Core |
| ORM | Entity Framework Core (InMemory) |
| Authentication | JWT Bearer |
| Password Hashing | BCrypt.Net |
| Caching | IMemoryCache |
| Testing | xUnit, Moq, FluentAssertions |
| Documentation | Swagger/OpenAPI |
| Containerization | Docker |

## License

This project is for interview/demonstration purposes.

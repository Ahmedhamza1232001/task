# Weather Forecast API

A production-style ASP.NET Core Web API demonstrating Clean Architecture, JWT authentication with refresh token rotation, and best practices for building maintainable backend services.

## Project Description

This API provides weather forecast data for cities worldwide with secure user authentication. It serves as a demonstration of enterprise-level architecture patterns and security implementations.

### Key Features

- JWT-based authentication with refresh token rotation
- Clean Architecture with separation of concerns
- In-memory data persistence using EF Core
- Request caching for improved performance
- Comprehensive test coverage
- Docker support for containerized deployment
- OpenAPI documentation with Swagger UI

### Technologies

- ASP.NET Core (.NET 9)
- Entity Framework Core (InMemory Provider)
- BCrypt.NET for password hashing
- xUnit, Moq, FluentAssertions for testing
- Swagger/OpenAPI for documentation
- Docker for containerization

## Architecture Overview

The solution follows Clean Architecture principles, organizing code into four distinct layers with clear dependency boundaries.

```
WeatherForecastApi/
├── src/
│   ├── Weather.Api/              # Presentation layer
│   │   ├── Controllers/          # HTTP endpoints
│   │   ├── Middleware/           # Exception handling
│   │   └── Program.cs            # Application entry point
│   │
│   ├── Weather.Application/      # Application layer
│   │   ├── DTOs/                 # Data transfer objects
│   │   ├── Exceptions/           # Custom exceptions
│   │   ├── Interfaces/           # Service contracts
│   │   └── Services/             # Business logic
│   │
│   ├── Weather.Domain/           # Domain layer
│   │   └── Entities/             # Core business entities
│   │
│   └── Weather.Infrastructure/   # Infrastructure layer
│       ├── Configuration/        # Settings classes
│       ├── Data/                 # EF Core DbContext
│       ├── Repositories/         # Data access
│       └── Services/             # External service implementations
│
├── tests/
│   ├── Weather.UnitTests/        # Unit tests
│   └── Weather.IntegrationTests/ # Integration tests
│
├── Dockerfile
├── .gitignore
└── README.md
```

### Layer Responsibilities

| Layer | Purpose |
|-------|---------|
| Domain | Contains core entities (User, RefreshToken, WeatherData) with no external dependencies |
| Application | Defines interfaces, DTOs, and orchestrates business logic |
| Infrastructure | Implements data access, authentication, caching, and external services |
| API | Handles HTTP requests, routing, and middleware configuration |

## Prerequisites

- .NET 9.0 SDK or later
- Docker (optional, for containerized deployment)
- Git

## Running the Application

### Run Locally

```bash
# Clone and navigate to project
cd WeatherForecastApi

# Restore dependencies
dotnet restore

# Run the application
dotnet run --project src/Weather.Api

# Application will start on https://localhost:5001 and http://localhost:5000
```

### Run with Docker

```bash
# Build the Docker image
docker build -t weather-api .

# Run the container
docker run -p 8080:8080 weather-api

# Access the API at http://localhost:8080
```

## Authentication Flow

The API implements JWT authentication with refresh token rotation for enhanced security.

### Flow Overview

1. **Registration**: User submits email, username, and password. Server returns access token and refresh token.

2. **Login**: User submits credentials. Server validates and returns new token pair.

3. **Accessing Resources**: Client includes access token in Authorization header. Server validates token and processes request.

4. **Token Refresh**: When access token expires, client sends refresh token. Server validates, revokes old refresh token, and issues new token pair.

### Token Specifications

| Token Type | Expiration | Storage |
|------------|------------|---------|
| Access Token | 15 minutes | Client-side (memory/secure storage) |
| Refresh Token | 7 days | Server-side (database) |

### Security Features

- Refresh tokens are single-use and rotated on each refresh
- Used refresh tokens are immediately revoked
- Passwords are hashed using BCrypt with automatic salting

## API Endpoints

### Authentication Endpoints

#### Register a New User

```bash
curl -X POST http://localhost:8080/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "user@example.com",
    "username": "johndoe",
    "password": "SecurePass123"
  }'
```

Response:
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIs...",
  "refreshToken": "dGhpcyBpcyBhIHJlZnJlc2g...",
  "expiresAt": "2024-01-15T10:30:00Z"
}
```

#### Login

```bash
curl -X POST http://localhost:8080/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "user@example.com",
    "password": "SecurePass123"
  }'
```

#### Refresh Access Token

```bash
curl -X POST http://localhost:8080/api/auth/refresh \
  -H "Content-Type: application/json" \
  -d '{
    "refreshToken": "dGhpcyBpcyBhIHJlZnJlc2g..."
  }'
```

### Weather Endpoint

#### Get Weather Data (Requires Authentication)

```bash
curl -X GET "http://localhost:8080/api/weather?city=London" \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIs..."
```

Response:
```json
{
  "city": "London",
  "temperatureCelsius": 15.5,
  "condition": "Cloudy",
  "date": "2024-01-15T10:00:00Z"
}
```

### Endpoint Summary

| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| POST | `/api/auth/register` | Create new user account | No |
| POST | `/api/auth/login` | Authenticate and get tokens | No |
| POST | `/api/auth/refresh` | Refresh access token | No |
| GET | `/api/weather` | Get weather for city | Yes |
| GET | `/health` | Health check endpoint | No |

## Swagger

Swagger UI is available in development mode for API exploration and testing.

### Access Swagger

Navigate to: `http://localhost:8080` (or `https://localhost:5001` when running locally)

### Authorize in Swagger

1. Click the **Authorize** button in the top right
2. Enter your JWT token in the format: `Bearer <your-token>`
3. Click **Authorize** to apply
4. Execute authenticated requests

## Testing

The project includes comprehensive test coverage with both unit and integration tests.

### Unit Tests (45 tests)

Located in `tests/Weather.UnitTests/`, covering:

- Authentication service logic (registration, login, token refresh)
- Password hashing functionality
- JWT token generation and validation
- Weather service behavior
- Cache hit/miss scenarios

### Integration Tests (20 tests)

Located in `tests/Weather.IntegrationTests/`, covering:

- Complete authentication flows
- Protected endpoint access control
- Token refresh and rotation
- Error handling scenarios
- End-to-end user journeys

### Run Tests

```bash
# Run all tests
dotnet test

# Run with detailed output
dotnet test --verbosity normal

# Run specific test project
dotnet test tests/Weather.UnitTests
dotnet test tests/Weather.IntegrationTests
```

## Security Notes

### Password Storage

- Passwords are never stored in plain text
- BCrypt hashing with automatic salt generation
- Hash verification performed server-side only

### Token Security

- Access tokens are short-lived (15 minutes) to limit exposure
- Refresh tokens are rotated on each use, preventing replay attacks
- Revoked tokens are tracked server-side
- JWT includes user ID and email claims

### Recommendations for Production

- Store JWT secret key in secure vault (Azure Key Vault, AWS Secrets Manager)
- Enable HTTPS only
- Implement rate limiting on authentication endpoints
- Add request logging and monitoring
- Consider token blacklisting for logout functionality

## Assumptions and Limitations

### Current Implementation

- **In-Memory Database**: All data is lost when the application restarts. Suitable for demonstration purposes only.

- **Mocked Weather Provider**: Weather data is generated from predefined ranges per city. No external API calls are made.

- **Single Instance**: No distributed caching or session management. Not suitable for load-balanced deployments without modification.

- **No Email Verification**: User registration does not require email confirmation.

### Not Production-Ready

- No persistent data storage
- No SSL certificate configuration
- No rate limiting
- No logging to external systems
- Hardcoded JWT secret in configuration

## Future Improvements

### Data Persistence
- Replace InMemory provider with PostgreSQL or SQL Server
- Implement database migrations
- Add connection pooling and retry policies

### External Integration
- Integrate real weather API (OpenWeatherMap, WeatherAPI)
- Add circuit breaker pattern for external calls
- Implement response caching with Redis

### Security Enhancements
- Add rate limiting middleware
- Implement account lockout after failed attempts
- Add two-factor authentication
- Implement token revocation on password change

### Infrastructure
- Add structured logging (Serilog, Application Insights)
- Implement health checks for dependencies
- Add CI/CD pipeline configuration
- Create Kubernetes deployment manifests

### Performance
- Add distributed caching with Redis
- Implement response compression
- Add API versioning
- Optimize database queries with indexing

## License

This project is intended for interview and demonstration purposes.

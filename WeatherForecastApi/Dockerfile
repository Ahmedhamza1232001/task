# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY WeatherForecastApi.sln .
COPY src/Weather.Api/Weather.Api.csproj src/Weather.Api/
COPY src/Weather.Application/Weather.Application.csproj src/Weather.Application/
COPY src/Weather.Domain/Weather.Domain.csproj src/Weather.Domain/
COPY src/Weather.Infrastructure/Weather.Infrastructure.csproj src/Weather.Infrastructure/


RUN dotnet restore src/Weather.Api/Weather.Api.csproj
# Copy all source code
COPY src/ src/

# Build and publish
RUN dotnet publish src/Weather.Api/Weather.Api.csproj -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Create non-root user for security
RUN adduser --disabled-password --gecos "" appuser

# Copy published files
COPY --from=build /app/publish .

# Change ownership to non-root user
RUN chown -R appuser:appuser /app

# Switch to non-root user
USER appuser

# Expose port
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Start the application
ENTRYPOINT ["dotnet", "Weather.Api.dll"]

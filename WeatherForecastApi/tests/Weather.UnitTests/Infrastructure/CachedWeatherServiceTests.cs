using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Weather.Application.DTOs;
using Weather.Application.Interfaces;
using Weather.Infrastructure.Services;

namespace Weather.UnitTests.Infrastructure;

public class CachedWeatherServiceTests
{
    private readonly Mock<IWeatherService> _innerServiceMock;
    private readonly IMemoryCache _cache;
    private readonly Mock<ILogger<CachedWeatherService>> _loggerMock;
    private readonly CachedWeatherService _cachedWeatherService;

    public CachedWeatherServiceTests()
    {
        _innerServiceMock = new Mock<IWeatherService>();
        _cache = new MemoryCache(new MemoryCacheOptions());
        _loggerMock = new Mock<ILogger<CachedWeatherService>>();

        _cachedWeatherService = new CachedWeatherService(
            _innerServiceMock.Object,
            _cache,
            _loggerMock.Object);
    }

    [Fact]
    public async Task GetWeatherAsync_CacheMiss_CallsInnerService()
    {
        // Arrange
        var city = "London";
        var weatherResponse = new WeatherResponse("London", 15.5, "Sunny", DateTime.UtcNow);

        _innerServiceMock.Setup(x => x.GetWeatherAsync(city, It.IsAny<CancellationToken>()))
            .ReturnsAsync(weatherResponse);

        // Act
        var result = await _cachedWeatherService.GetWeatherAsync(city);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(weatherResponse);
        _innerServiceMock.Verify(x => x.GetWeatherAsync(city, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetWeatherAsync_CacheHit_DoesNotCallInnerService()
    {
        // Arrange
        var city = "London";
        var weatherResponse = new WeatherResponse("London", 15.5, "Sunny", DateTime.UtcNow);

        _innerServiceMock.Setup(x => x.GetWeatherAsync(city, It.IsAny<CancellationToken>()))
            .ReturnsAsync(weatherResponse);

        // First call - cache miss
        await _cachedWeatherService.GetWeatherAsync(city);

        // Reset mock to verify second call
        _innerServiceMock.Invocations.Clear();

        // Act - second call - should be cache hit
        var result = await _cachedWeatherService.GetWeatherAsync(city);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(weatherResponse);
        _innerServiceMock.Verify(x => x.GetWeatherAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetWeatherAsync_DifferentCities_CallsInnerServiceForEach()
    {
        // Arrange
        var londonResponse = new WeatherResponse("London", 15.5, "Sunny", DateTime.UtcNow);
        var parisResponse = new WeatherResponse("Paris", 18.0, "Cloudy", DateTime.UtcNow);

        _innerServiceMock.Setup(x => x.GetWeatherAsync("London", It.IsAny<CancellationToken>()))
            .ReturnsAsync(londonResponse);
        _innerServiceMock.Setup(x => x.GetWeatherAsync("Paris", It.IsAny<CancellationToken>()))
            .ReturnsAsync(parisResponse);

        // Act
        var londonResult = await _cachedWeatherService.GetWeatherAsync("London");
        var parisResult = await _cachedWeatherService.GetWeatherAsync("Paris");

        // Assert
        londonResult.Should().Be(londonResponse);
        parisResult.Should().Be(parisResponse);
        _innerServiceMock.Verify(x => x.GetWeatherAsync("London", It.IsAny<CancellationToken>()), Times.Once);
        _innerServiceMock.Verify(x => x.GetWeatherAsync("Paris", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetWeatherAsync_CaseInsensitiveCacheKey_ReturnsCachedResult()
    {
        // Arrange
        var weatherResponse = new WeatherResponse("London", 15.5, "Sunny", DateTime.UtcNow);

        _innerServiceMock.Setup(x => x.GetWeatherAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(weatherResponse);

        // Act - First call with "London"
        await _cachedWeatherService.GetWeatherAsync("London");

        // Reset mock
        _innerServiceMock.Invocations.Clear();

        // Second call with "LONDON" - should hit cache
        var result = await _cachedWeatherService.GetWeatherAsync("LONDON");

        // Assert
        result.Should().NotBeNull();
        _innerServiceMock.Verify(x => x.GetWeatherAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task GetWeatherAsync_WithInvalidCity_ReturnsNull(string? city)
    {
        // Act
        var result = await _cachedWeatherService.GetWeatherAsync(city!);

        // Assert
        result.Should().BeNull();
        _innerServiceMock.Verify(x => x.GetWeatherAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetWeatherAsync_InnerServiceReturnsNull_DoesNotCache()
    {
        // Arrange
        var city = "UnknownCity";

        _innerServiceMock.Setup(x => x.GetWeatherAsync(city, It.IsAny<CancellationToken>()))
            .ReturnsAsync((WeatherResponse?)null);

        // Act
        var result1 = await _cachedWeatherService.GetWeatherAsync(city);
        var result2 = await _cachedWeatherService.GetWeatherAsync(city);

        // Assert
        result1.Should().BeNull();
        result2.Should().BeNull();
        // Inner service should be called twice since null results are not cached
        _innerServiceMock.Verify(x => x.GetWeatherAsync(city, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }
}

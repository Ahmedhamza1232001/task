using FluentAssertions;
using Weather.Infrastructure.Services;

namespace Weather.UnitTests.Services;

public class WeatherServiceTests
{
    private readonly MockWeatherService _weatherService;

    public WeatherServiceTests()
    {
        _weatherService = new MockWeatherService();
    }

    [Fact]
    public async Task GetWeatherAsync_WithValidCity_ReturnsWeatherData()
    {
        // Arrange
        var city = "London";

        // Act
        var result = await _weatherService.GetWeatherAsync(city);

        // Assert
        result.Should().NotBeNull();
        result!.City.Should().Be("London");
        result.Condition.Should().NotBeNullOrEmpty();
        result.Date.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task GetWeatherAsync_WithDifferentCities_ReturnsDifferentTemperatures()
    {
        // Arrange & Act
        var londonWeather = await _weatherService.GetWeatherAsync("London");
        var dubaiWeather = await _weatherService.GetWeatherAsync("Dubai");

        // Assert
        londonWeather.Should().NotBeNull();
        dubaiWeather.Should().NotBeNull();

        // Dubai typically has higher temperatures than London
        dubaiWeather!.TemperatureCelsius.Should().BeGreaterThan(londonWeather!.TemperatureCelsius);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task GetWeatherAsync_WithInvalidCity_ReturnsNull(string? city)
    {
        // Act
        var result = await _weatherService.GetWeatherAsync(city!);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetWeatherAsync_NormalizesCity_ReturnsProperlyCasedCity()
    {
        // Arrange & Act
        var result = await _weatherService.GetWeatherAsync("LONDON");

        // Assert
        result.Should().NotBeNull();
        result!.City.Should().Be("London");
    }

    [Fact]
    public async Task GetWeatherAsync_WithUnknownCity_ReturnsDefaultTemperatureRange()
    {
        // Arrange
        var unknownCity = "UnknownCity123";

        // Act
        var result = await _weatherService.GetWeatherAsync(unknownCity);

        // Assert
        result.Should().NotBeNull();
        result!.TemperatureCelsius.Should().BeInRange(10, 25);
    }

    [Fact]
    public async Task GetWeatherAsync_SameCity_ReturnsDeterministicResults()
    {
        // Arrange
        var city = "Tokyo";

        // Act
        var result1 = await _weatherService.GetWeatherAsync(city);
        var result2 = await _weatherService.GetWeatherAsync(city);

        // Assert
        result1.Should().NotBeNull();
        result2.Should().NotBeNull();
        result1!.TemperatureCelsius.Should().Be(result2!.TemperatureCelsius);
        result1.Condition.Should().Be(result2.Condition);
    }
}

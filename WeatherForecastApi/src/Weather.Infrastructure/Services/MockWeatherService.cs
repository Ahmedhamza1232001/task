using Weather.Application.DTOs;
using Weather.Application.Interfaces;

namespace Weather.Infrastructure.Services;

public class MockWeatherService : IWeatherService
{
    private static readonly string[] Conditions =
    {
        "Sunny", "Cloudy", "Rainy", "Partly Cloudy", "Stormy", "Snowy", "Foggy", "Windy"
    };

    private static readonly Dictionary<string, (double MinTemp, double MaxTemp)> CityTemperatureRanges = new(StringComparer.OrdinalIgnoreCase)
    {
        { "London", (5, 20) },
        { "New York", (0, 30) },
        { "Tokyo", (5, 35) },
        { "Sydney", (15, 35) },
        { "Paris", (5, 25) },
        { "Dubai", (20, 45) },
        { "Moscow", (-20, 25) },
        { "Cairo", (15, 40) },
        { "Mumbai", (20, 35) },
        { "Berlin", (0, 25) }
    };

    public Task<WeatherResponse?> GetWeatherAsync(string city, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(city))
        {
            return Task.FromResult<WeatherResponse?>(null);
        }

        var normalizedCity = NormalizeCity(city);
        var temperature = GenerateTemperature(normalizedCity);
        var condition = GenerateCondition(normalizedCity);

        var response = new WeatherResponse(
            City: normalizedCity,
            TemperatureCelsius: temperature,
            Condition: condition,
            Date: DateTime.UtcNow
        );

        return Task.FromResult<WeatherResponse?>(response);
    }

    private static string NormalizeCity(string city)
    {
        var trimmed = city.Trim();
        if (string.IsNullOrEmpty(trimmed))
            return trimmed;

        return char.ToUpper(trimmed[0]) + trimmed[1..].ToLower();
    }

    private static double GenerateTemperature(string city)
    {
        var (minTemp, maxTemp) = CityTemperatureRanges.TryGetValue(city, out var range)
            ? range
            : (10, 25);

        var seed = city.GetHashCode() + DateTime.UtcNow.DayOfYear;
        var random = new Random(seed);
        var temperature = minTemp + random.NextDouble() * (maxTemp - minTemp);

        return Math.Round(temperature, 1);
    }

    private static string GenerateCondition(string city)
    {
        var seed = city.GetHashCode() + DateTime.UtcNow.DayOfYear;
        var random = new Random(seed);
        return Conditions[random.Next(Conditions.Length)];
    }
}

using Weather.Application.DTOs;

namespace Weather.Application.Interfaces;

public interface IWeatherService
{
    Task<WeatherResponse?> GetWeatherAsync(string city, CancellationToken cancellationToken = default);
}

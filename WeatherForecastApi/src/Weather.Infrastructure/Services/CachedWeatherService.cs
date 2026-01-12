using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Weather.Application.DTOs;
using Weather.Application.Interfaces;

namespace Weather.Infrastructure.Services;

public class CachedWeatherService : IWeatherService
{
    private readonly IWeatherService _innerService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CachedWeatherService> _logger;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public CachedWeatherService(
        IWeatherService innerService,
        IMemoryCache cache,
        ILogger<CachedWeatherService> logger)
    {
        _innerService = innerService;
        _cache = cache;
        _logger = logger;
    }

    public async Task<WeatherResponse?> GetWeatherAsync(string city, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(city))
        {
            return null;
        }

        var cacheKey = GenerateCacheKey(city);

        if (_cache.TryGetValue(cacheKey, out WeatherResponse? cachedResponse))
        {
            _logger.LogDebug("Cache hit for city: {City}", city);
            return cachedResponse;
        }

        _logger.LogDebug("Cache miss for city: {City}", city);
        var response = await _innerService.GetWeatherAsync(city, cancellationToken);

        if (response is not null)
        {
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(CacheDuration);

            _cache.Set(cacheKey, response, cacheOptions);
            _logger.LogDebug("Cached weather data for city: {City}", city);
        }

        return response;
    }

    private static string GenerateCacheKey(string city)
    {
        return $"weather:{city.Trim().ToLowerInvariant()}";
    }
}

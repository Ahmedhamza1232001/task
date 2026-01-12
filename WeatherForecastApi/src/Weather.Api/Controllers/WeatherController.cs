using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Weather.Application.DTOs;
using Weather.Application.Interfaces;

namespace Weather.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WeatherController : ControllerBase
{
    private readonly IWeatherService _weatherService;

    public WeatherController(IWeatherService weatherService)
    {
        _weatherService = weatherService;
    }

    /// <summary>
    /// Get current weather for a specified city
    /// </summary>
    /// <param name="city">City name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Weather data for the specified city</returns>
    [HttpGet]
    [ProducesResponseType(typeof(WeatherResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WeatherResponse>> GetWeather(
        [FromQuery] string city,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(city))
        {
            return BadRequest(new { error = "City parameter is required." });
        }

        var weather = await _weatherService.GetWeatherAsync(city, cancellationToken);

        if (weather is null)
        {
            return NotFound(new { error = $"Weather data not found for city: {city}" });
        }

        return Ok(weather);
    }
}

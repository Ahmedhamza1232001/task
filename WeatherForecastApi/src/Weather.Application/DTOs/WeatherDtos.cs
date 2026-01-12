namespace Weather.Application.DTOs;

public record WeatherResponse(
    string City,
    double TemperatureCelsius,
    string Condition,
    DateTime Date
);

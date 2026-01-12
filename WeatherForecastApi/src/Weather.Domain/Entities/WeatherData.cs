namespace Weather.Domain.Entities;

public class WeatherData
{
    public string City { get; set; } = string.Empty;
    public double TemperatureCelsius { get; set; }
    public string Condition { get; set; } = string.Empty;
    public DateTime Date { get; set; }
}

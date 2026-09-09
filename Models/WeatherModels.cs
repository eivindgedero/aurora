using System.Text.Json.Serialization;
namespace aurora.Models;

public record WeatherSettings(
    double Latitude,
    double Longitude
);


public record WeatherForecastResponse
{
    public WeatherProperties Properties { get; init; } = new();
}

public record WeatherProperties
{
    public List<WeatherTimeSeries> Timeseries { get; init; } = [];
}

public record WeatherTimeSeries
{
    public DateTimeOffset Time { get; init; }

    public WeatherData Data { get; init; } = new();
}

public record WeatherData
{
    public WeatherInstant Instant { get; init; } = new();
}

public record WeatherInstant
{
    public WeatherDetails Details { get; init; } = new();
}

public record WeatherDetails
{
    [JsonPropertyName("cloud_area_fraction")]
    public double? CloudAreaFraction { get; init; }

    [JsonPropertyName("fog_area_fraction")]
    public double? FogAreaFraction { get; init; }
}
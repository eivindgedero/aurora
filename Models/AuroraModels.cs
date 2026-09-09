namespace aurora.Models;


public record ForecastAreaSettings(
    int LatitudeMin,
    int LatitudeMax,
    int LongitudeMin,
    int LongitudeMax
);

public record AuroraForecast(
    int Longitude,
    int Latitude,
    int Probability
);

public record NoaaAuroraResponse
{
    public List<int[]> Coordinates { get; init; } = [];
}
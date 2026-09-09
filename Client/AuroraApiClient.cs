using System.Net.Http.Json;
namespace aurora.Client;

using aurora.Models;

public class AuroraApi(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration
)
{
    private readonly ForecastAreaSettings forecastAreaSettings =
        configuration.GetSection("ForecastArea").Get<ForecastAreaSettings>()
        ?? throw new InvalidOperationException(
            "ForecastArea configuration is missing or invalid."
        );

    public async Task<AuroraForecast?> GetBestAuroraLocationAsync(
        CancellationToken stoppingToken
    )
    {
        var client =
            httpClientFactory.CreateClient("AuroraForecast");

        var forecast =
            await client.GetFromJsonAsync<NoaaAuroraResponse>(
                "json/ovation_aurora_latest.json",
                stoppingToken
            );

        if (forecast is null)
        {
            return null;
        }

        return forecast.Coordinates
            .Where(coordinate => coordinate.Length >= 3)
            .Select(coordinate => new AuroraForecast(
                Longitude: coordinate[0],
                Latitude: coordinate[1],
                Probability: coordinate[2]
            ))
            .Where(point =>
                point.Longitude >= forecastAreaSettings.LongitudeMin &&
                point.Longitude <= forecastAreaSettings.LongitudeMax &&
                point.Latitude >= forecastAreaSettings.LatitudeMin &&
                point.Latitude <= forecastAreaSettings.LatitudeMax)
            .MaxBy(point => point.Probability);
    }
}
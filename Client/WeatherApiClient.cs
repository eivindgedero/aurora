using System.Net.Http.Json;
namespace aurora.Client;

using aurora.Models;


public class WeatherApi(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration
)
{
    private readonly WeatherSettings weatherSettings =
        configuration.GetSection("WeatherSettings").Get<WeatherSettings>()
        ?? throw new InvalidOperationException(
            "Weather configuration is missing or invalid."
        );

    public async Task<WeatherTimeSeries> GetCurrentWeatherAsync(
        CancellationToken stoppingToken
    )
    {
        var client =
            httpClientFactory.CreateClient("WeatherForecast");

        var weatherUrl = FormattableString.Invariant(
            $"compact?lat={weatherSettings.Latitude}&lon={weatherSettings.Longitude}"
        );

        var weather =
            await client.GetFromJsonAsync<WeatherForecastResponse>(
                weatherUrl,
                stoppingToken
            ) ?? throw new InvalidOperationException(
                "Weather API returned no data"
            );

        var now = DateTimeOffset.UtcNow;

        return weather.Properties.Timeseries
            .Where(item =>
                item.Data.Instant.Details.CloudAreaFraction is not null)
            .MinBy(item =>
                Math.Abs((item.Time - now).Ticks))
            ?? throw new InvalidOperationException(
                "Weather API returned no valid timeseries data"
            );
    }
}
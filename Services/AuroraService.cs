using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace aurora.Services;

public record AuroraForecast(
    int Longitude,
    int Latitude,
    int Probability
);

public record NoaaAuroraResponse
{
    [JsonPropertyName("coordinates")]
    public List<int[]> Coordinates { get; init; } = [];
}

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

public class AuroraService(
    ILogger<AuroraService> logger,
    IHttpClientFactory httpClientFactory,
    DiscordService discordService
) : BackgroundService
{
    private const int MinimumAuroraProbability = 10;
    private const double MaximumCloudCoverage = 30;

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken
    )
    {
        using var timer = new PeriodicTimer(
            TimeSpan.FromMinutes(30)
        );

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAuroraConditionsAsync(stoppingToken);
            }
            catch (TaskCanceledException)
                when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogWarning(
                    "Request timed out while fetching aurora or weather data"
                );
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Error fetching aurora or weather data"
                );
            }

            try
            {
                if (!await timer.WaitForNextTickAsync(stoppingToken))
                {
                    break;
                }
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        logger.LogInformation("Aurora worker stopped");
    }

    private async Task CheckAuroraConditionsAsync(
        CancellationToken stoppingToken
    )
    {
        var bestAuroraLocation =
            await GetBestAuroraLocationAsync(stoppingToken);

        if (bestAuroraLocation is null)
        {
            logger.LogWarning(
                "No relevant aurora coordinates found"
            );

            return;
        }

        logger.LogInformation(
            "Highest aurora probability: {Probability}% at lat {Latitude}, lon {Longitude}",
            bestAuroraLocation.Probability,
            bestAuroraLocation.Latitude,
            bestAuroraLocation.Longitude
        );

        if (bestAuroraLocation.Probability < MinimumAuroraProbability)
        {
            logger.LogInformation(
                "Aurora probability is too low"
            );

            return;
        }

        var currentWeather = await GetCurrentWeatherAsync(
            bestAuroraLocation,
            stoppingToken
        );

        if (currentWeather is null)
        {
            logger.LogWarning(
                "No relevant weather forecast found"
            );

            return;
        }

        var cloudCoverage =
            currentWeather.Data.Instant.Details.CloudAreaFraction;

        var fogCoverage =
            currentWeather.Data.Instant.Details.FogAreaFraction;

        logger.LogInformation(
            "Weather at {Time}: Clouds {CloudCoverage}%, Fog {fogCoverage}%",
            currentWeather.Time,
            cloudCoverage,
            fogCoverage
        );

        if (cloudCoverage is null)
        {
            logger.LogWarning(
                "Weather forecast did not contain cloud coverage"
            );

            return;
        }

        if (cloudCoverage <= MaximumCloudCoverage)
        {
            logger.LogInformation(
                "Good aurora conditions! Aurora: {AuroraProbability}%, Clouds: {CloudCoverage}%",
                bestAuroraLocation.Probability,
                cloudCoverage
            );
            var mapsUrl =
    $"https://www.google.com/maps/search/?api=1&query={bestAuroraLocation.Latitude},{bestAuroraLocation.Longitude}";

            await discordService.SendNotificationAsync(
                $"Aurora: {bestAuroraLocation.Probability}%, Clouds: {cloudCoverage}%\n" +
                $"[{bestAuroraLocation.Latitude}, {bestAuroraLocation.Longitude}]({mapsUrl})"
            );

            return;
        }

        logger.LogInformation(
            "Aurora activity is high, but cloud coverage is too high: {CloudCoverage}%",
            cloudCoverage
        );
    }

    private async Task<AuroraForecast?> GetBestAuroraLocationAsync(
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
            logger.LogWarning(
                "Aurora API returned no data"
            );

            return null;
        }

        var relevantCoordinates = forecast.Coordinates
            .Where(coordinate => coordinate.Length >= 3)
            .Select(coordinate => new AuroraForecast(
                Longitude: coordinate[0],
                Latitude: coordinate[1],
                Probability: coordinate[2]
            ))
            .Where(point =>
                point.Longitude is >= 5 and <= 7 &&
                point.Latitude is >= 58 and <= 59)
            .ToList();

        return relevantCoordinates
            .MaxBy(point => point.Probability);
    }

    private async Task<WeatherTimeSeries?> GetCurrentWeatherAsync(
        AuroraForecast location,
        CancellationToken stoppingToken
    )
    {
        var client =
            httpClientFactory.CreateClient("WeatherForecast");

        var weatherUrl = FormattableString.Invariant(
            $"compact?lat={location.Latitude}&lon={location.Longitude}"
        );

        var weather =
            await client.GetFromJsonAsync<WeatherForecastResponse>(
                weatherUrl,
                stoppingToken
            );

        if (weather is null)
        {
            logger.LogWarning(
                "Weather API returned no data"
            );

            return null;
        }

        var now = DateTimeOffset.UtcNow;

        return weather.Properties.Timeseries
            .Where(item =>
                item.Data.Instant.Details.CloudAreaFraction is not null)
            .MinBy(item =>
                Math.Abs((item.Time - now).Ticks)
            );
    }
}
namespace aurora.Services;

using aurora.Utilities;
using aurora.Client;

public class AuroraService(
    ILogger<AuroraService> logger,
    AuroraApi auroraApi,
    WeatherApi weatherApi,
    MoonApi moonApi,
    DiscordService discordService
) : BackgroundService
{
    private const int MinimumAuroraProbability = 10;
    private const double MaximumCloudCoverage = 30;

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken
    )
    {
        await discordService.InitializeAsync();

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
                    "Request timed out while fetching aurora, weather or moon data"
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
                    "Error checking aurora conditions"
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
            await auroraApi.GetBestAuroraLocationAsync(
                stoppingToken
            );

        if (bestAuroraLocation is null)
        {
            logger.LogWarning(
                "No relevant aurora coordinates found"
            );
            return;
        }

        if (
            bestAuroraLocation.Probability
            < MinimumAuroraProbability
        )
        {
            logger.LogInformation(
                "Aurora probability is too low"
            );
            return;
        }

        var weather =
            await weatherApi.GetCurrentWeatherAsync(
                stoppingToken
            );

        var moon =
            await moonApi.GetCurrentMoonDataAsync(
                stoppingToken
            );

        var cloudCoverage =
            weather.Data.Instant.Details.CloudAreaFraction;

        var fogCoverage =
            weather.Data.Instant.Details.FogAreaFraction;

        var moonIllumination =
            AuroraCalculations.MoonPhaseToIllumination(
                moon.MoonPhase
            );

        var moonIsUp =
            AuroraCalculations.IsMoonUp(
                DateTimeOffset.UtcNow,
                moon.Moonrise?.Time,
                moon.Moonset?.Time
            );

        var effectiveMoonIllumination =
            moonIsUp ? moonIllumination : 0;

        logger.LogInformation(
            "Weather at {Time}: Clouds {CloudCoverage}%, Fog {FogCoverage}%, Moon illumination {MoonIllumination:F1}%",
            weather.Time,
            cloudCoverage,
            fogCoverage,
            moonIllumination
        );

        if (cloudCoverage is null)
        {
            logger.LogWarning(
                "Weather forecast did not contain cloud coverage"
            );

            return;
        }

        if (cloudCoverage > MaximumCloudCoverage)
        {
            return;
        }

        if (effectiveMoonIllumination > 80)
        {
            return;
        }


        await discordService.SendNotificationAsync(
            $"Aurora: {bestAuroraLocation.Probability}%, " +
            $"Clouds: {cloudCoverage}%, " +
            $"Moon: {effectiveMoonIllumination:F1}%"
        );
    }
}
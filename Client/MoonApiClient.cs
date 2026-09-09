using System.Net.Http.Json;
using System.Text.Json.Serialization;
namespace aurora.Client;

using aurora.Models;


public class MoonApi(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration
)
{
    private readonly WeatherSettings weatherSettings =
        configuration.GetSection("WeatherSettings").Get<WeatherSettings>()
        ?? throw new InvalidOperationException(
            "Weather configuration is missing or invalid."
        );

    public async Task<MoonProperties> GetCurrentMoonDataAsync(
        CancellationToken stoppingToken
    )
    {
        var client =
            httpClientFactory.CreateClient("MoonPhase");

        var moonUrl = FormattableString.Invariant(
            $"moon?lat={weatherSettings.Latitude}&lon={weatherSettings.Longitude}"
        );

        var response =
            await client.GetFromJsonAsync<MoonPhaseResponse>(
                moonUrl,
                stoppingToken
            ) ?? throw new InvalidOperationException(
                "Moon API returned no data"
            );

        return response.Properties;
    }
}
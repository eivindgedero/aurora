using System.Text.Json.Serialization;

namespace aurora.Models;

public record MoonPhaseResponse
{
    public MoonProperties Properties { get; init; } = new();
}

public class MoonProperties
{
    [JsonPropertyName("moonphase")]
    public double MoonPhase { get; set; }

    public MoonHorizonEvent? Moonrise { get; set; }

    public MoonHorizonEvent? Moonset { get; set; }
}

public class MoonHorizonEvent
{
    public DateTimeOffset Time { get; set; }

    public double Azimuth { get; set; }
}
namespace aurora.Utilities;

public static class AuroraCalculations
{
    public static double MoonPhaseToIllumination(
        double moonPhase
    )
    {
        var radians =
            moonPhase * Math.PI / 180.0;

        return (1 - Math.Cos(radians)) / 2 * 100;
    }

    public static bool IsMoonUp(
        DateTimeOffset now,
        DateTimeOffset? moonRise,
        DateTimeOffset? moonSet
    )
    {
        if (moonRise is null || moonSet is null)
        {
            return false;
        }

        if (moonRise < moonSet)
        {
            return now >= moonRise && now < moonSet;
        }

        return now >= moonRise || now < moonSet;
    }
}
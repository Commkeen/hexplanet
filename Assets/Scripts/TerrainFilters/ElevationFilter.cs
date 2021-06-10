using UnityEngine;

public class ElevationFilter
{
    ElevationSettings settings;
    Noise noise = new Noise();

    public ElevationFilter(ElevationSettings settings)
    {
        this.settings = settings;
    }

    public void Evaluate(PolyCell cell)
    {
        if (settings == null) {return;}

        if (settings.seed != noise.Seed)
        {
            noise = new Noise(settings.seed);
        }

        if (settings.minElevation > settings.maxElevation) {settings.maxElevation = settings.minElevation;}

        var noisePosition = cell.center * settings.roughness;
        noisePosition += settings.center;

        float noiseResult = noise.Evaluate(noisePosition)*.5f + .5f;

        if (noiseResult < settings.waterCoverage) {noiseResult = 0;}
        noiseResult = Mathf.InverseLerp(settings.waterCoverage, 1f, noiseResult);

        int result = Mathf.FloorToInt(Mathf.Lerp(settings.minElevation, settings.maxElevation, noiseResult));
        result = Mathf.Clamp(result, settings.minElevation, settings.maxElevation);

        cell.elevation = result;
    }
}
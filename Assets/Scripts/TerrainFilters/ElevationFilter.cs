using UnityEngine;

public class ElevationFilter
{
    ElevationSettings settings;

    public ElevationFilter(ElevationSettings settings)
    {
        this.settings = settings;
    }

    public void Evaluate(PolyCell cell)
    {
        if (settings == null) {return;}

        var noisePosition = new Vector2(cell.center.x, cell.center.y);
        noisePosition += new Vector2(settings.seed, settings.seed);

        float noiseResult = Mathf.PerlinNoise(noisePosition.x, noisePosition.y);

        int result = Mathf.FloorToInt(Mathf.Lerp(settings.minElevation, settings.maxElevation, noiseResult));
        result = Mathf.Clamp(result, settings.minElevation, settings.maxElevation);

        cell.elevation = result;
    }
}
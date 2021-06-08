using UnityEngine;

public class ColorFilter
{
    ColorSettings settings;

    public ColorFilter(ColorSettings settings)
    {
        this.settings = settings;
    }

    public void Evaluate(PolyCell cell)
    {
        if (settings == null) {return;}

        var noisePosition = new Vector2(cell.center.x, cell.center.y);
        noisePosition += new Vector2(settings.noiseOffset, settings.noiseOffset);

        float noiseResult = Mathf.PerlinNoise(noisePosition.x, noisePosition.y);
        //noiseResult *= Mathf.PerlinNoise(noisePosition.x, cell.center.z);
        //noiseResult *= Mathf.PerlinNoise(noisePosition.y, cell.center.z);
        cell.color = settings.colors.Evaluate(noiseResult);
    }
}
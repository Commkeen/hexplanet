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

        if (cell.elevation <= settings.waterHeight)
        {
            cell.color = settings.water;
        }
        else if (cell.elevation <= settings.beachHeight)
        {
            cell.color = settings.beach;
        }
        else if (cell.elevation <= settings.lowlandHeight)
        {
            cell.color = settings.lowland;
        }
        else if (cell.elevation <= settings.highlandHeigh)
        {
            cell.color = settings.highland;
        }
        else
        {
            cell.color = settings.mountain;
        }
    }
}
using UnityEngine;

[System.Serializable]
public struct HexCoordinates
{
    public int X {get; private set;}
    public int Y {get {return -X - Z;}}
    public int Z {get; private set;}

    public HexCoordinates(int x, int z)
    {
        X = x;
        Z = z;
    }

    public static HexCoordinates FromPosition (Vector3 position)
    {
        float x = position.x / (HexMetrics.innerRadius * 2f);
        float y = -x;

        int iX = Mathf.RoundToInt(x);
        int iY = Mathf.RoundToInt(y);
        int iZ = Mathf.RoundToInt(-x - y);

        return new HexCoordinates(iX, iZ);
    }

    public static HexCoordinates FromOffsetCoordinates(int x, int z)
    {
        return new HexCoordinates(x - z/2,z);
    }

    public string ToStringOnSeparateLines()
    {
        return X.ToString() + "\n" + Y.ToString() + "\n" + Z.ToString();
    }
}
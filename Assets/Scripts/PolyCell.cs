using System.Collections.Generic;
using UnityEngine;

public class PolyCell
{
    // Simple grid info
    public int index;
    public int face;
    public List<PolyCell> neighbors;

    // Simple unit sphere geo info
    public Vector3 center;
    public Vector3 normal;
    public List<Vector3> corners;
    
    // Additional generated properties
    public Color color;
    public float elevation;

    // Complex geometry info
    public float heightPerturb;
    

    public PolyCell()
    {

    }

    public int VertexCount()
    {
        return corners.Count + 1;
    }

    public (Vector3, Vector3) GetCornersForSide(int side)
    {
        if (side == corners.Count) {side = 0;}
        if (side == -1) {side = corners.Count-1;}
        Debug.Assert(side >= 0 && side < corners.Count);
        if (side == corners.Count-1)
        {
            return (corners[corners.Count-1], corners[0]);
        }
        return (corners[side], corners[side+1]);
    }

    // Returns the vector from an inner corner to the closest point on the outer side.
    public Vector3 GetBridge(int side, float outerCellSize)
    {
        (Vector3 a, Vector3 b) = GetCornersForSide(side);
        var cornerAFromCenter = a - center;
        var cornerBFromCenter = b - center;
        var midpoint = (cornerAFromCenter + cornerBFromCenter) * 0.5f;
        var midpointScaled = midpoint * outerCellSize;
        return midpointScaled;
    }

    public PolyCell GetNeighborForSegment(int side)
    {
        (Vector3 cornerA, Vector3 cornerB) = GetCornersForSide(side);
        PolyCell result = null;
        foreach (var c in neighbors)
        {
            if (c.corners.Contains(cornerA) && c.corners.Contains(cornerB))
            {
                result = c;
                break;
            }
        }
        Debug.Assert(result != null, "GetNeighborForSegment couldn't find a neighbor!");
        return result;
    }
}
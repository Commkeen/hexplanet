using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

public class PolyCellMeshGenerator
{

    public CellGeometrySettings cellGeometrySettings;
    public float radius = 1f;

    public bool cellIsSelected = false;
    public bool cellIsMouseover = false;
    

    private List<Vector3> _vertices;
    private List<int> _triangles;
    private List<Color> _colors;
    private Dictionary<Vector3, int> _vertexLookup;

    public void SetGeoLists(List<Vector3> vertices, List<int> triangles, List<Color> colors, Dictionary<Vector3, int> vertexLookup)
    {
        _vertices = vertices;
        _triangles = triangles;
        _colors = colors;
        _vertexLookup = vertexLookup;
    }

    public void MeshifyCell(PolyCell cell)
    {
        Profiler.BeginSample("Meshify cell");
        for (int i = 0; i < cell.corners.Count; i++)
        {
            MeshifyCellSegment(cell, i);
        }
        Profiler.EndSample();
    }

    private void MeshifyCellSegment(PolyCell cell, int side)
    {
        var neighbor = cell.GetNeighborForSegment(side);
        var prevNeighbor = cell.GetNeighborForSegment(side-1);
        var nextNeighbor = cell.GetNeighborForSegment(side+1);

        var innerSize = cellGeometrySettings.innerCellSize;

        var color = cell.color;
        if (cellIsSelected) {color = Color.red;}
        else if (cellIsMouseover) {color = Color.yellow;}
        //if (side == 0) {color = Color.Lerp(color, Color.red, 0.4f);}
        //if (side == 1) {color = Color.Lerp(color, Color.red, 0.2f);}

        // The base points on the unit sphere.
        (Vector3 cornerA, Vector3 cornerB) = cell.GetCornersForSide(side);
        var center = cell.center;

        var innerA = Vector3.Lerp(center, cornerA, innerSize);
        var innerB = Vector3.Lerp(center, cornerB, innerSize);

        var innerElevationFactor = 1 + cellGeometrySettings.elevationStep * cell.elevation;

        var centerElevated = center * innerElevationFactor;
        var innerAElevated = innerA * innerElevationFactor;
        var innerBElevated = innerB * innerElevationFactor;

        AddTriangle(centerElevated*radius, innerAElevated*radius, innerBElevated*radius, color);

        // Inner cell is done, now let's draw the outer edges
        var bridge = cell.GetBridge(side, cellGeometrySettings.outerCellSize);
        var bridgeA = innerA + bridge;
        var bridgeB = innerB + bridge;
        
        // Now, the outer quad needs to ramp down to a neighbor...
        var outerElevationFactor = innerElevationFactor;
        if (neighbor.elevation < cell.elevation)
        {
            outerElevationFactor = 1 + cellGeometrySettings.elevationStep * neighbor.elevation;
        }
        var cornerAElevated = cornerA * outerElevationFactor;
        var cornerBElevated = cornerB * outerElevationFactor;
        var bridgeAElevated = bridgeA * outerElevationFactor;
        var bridgeBElevated = bridgeB * outerElevationFactor;
        
        //color = Color.Lerp(color, Color.black, 0.3f);

        if (cell.elevation - neighbor.elevation == 1)
        {
            MeshifyEdgeTerrace(innerAElevated, innerBElevated, cell, bridgeAElevated, bridgeBElevated, neighbor, color);
        }
        else
        {
            AddQuad(innerAElevated*radius, innerBElevated*radius, bridgeAElevated*radius, bridgeBElevated*radius, color);
        }

        MeshifyCorner(cell, neighbor, prevNeighbor, side, cornerA, innerA, bridgeA, true, color);
        MeshifyCorner(cell, neighbor, nextNeighbor, side, cornerB, innerB, bridgeB, false, color);
    }

    private void MeshifyCorner(PolyCell cell, PolyCell fwdNeighbor, PolyCell otherNeighbor, int side,
                               Vector3 outerCorner, Vector3 innerCorner, Vector3 bridgePoint,
                               bool left, Color color)
    {
        var center = cell.center;
        var innerElevationFactor = 1 + cellGeometrySettings.elevationStep * cell.elevation;

        var fwdElevationFactor = innerElevationFactor;
        if (fwdNeighbor.elevation < cell.elevation)
        {
            fwdElevationFactor = 1 + cellGeometrySettings.elevationStep * fwdNeighbor.elevation;
        }

        var otherElevationFactor = innerElevationFactor;
        if (otherNeighbor.elevation < cell.elevation)
        {
            otherElevationFactor = 1 + cellGeometrySettings.elevationStep * otherNeighbor.elevation;
        }

        var innerCornerElevated = innerCorner * innerElevationFactor;
        var outerCornerElevated = outerCorner * fwdElevationFactor;
        var bridgePointElevated = bridgePoint * fwdElevationFactor;

        var fwdIsHigher = cell.elevation < fwdNeighbor.elevation;
        var otherIsHigher = cell.elevation < otherNeighbor.elevation;
        var fwdIsLevel = cell.elevation == fwdNeighbor.elevation;
        var fwdIsTerrace = cell.elevation == fwdNeighbor.elevation + 1;
        var otherIsTerrace = cell.elevation == otherNeighbor.elevation + 1;
        var fwdIsCliff = cell.elevation > fwdNeighbor.elevation + 1;
        var otherIsCliff = cell.elevation > otherNeighbor.elevation + 1;

        var begin = innerCornerElevated;
        var endLeft = left ? outerCornerElevated : bridgePointElevated;
        var endRight = left ? bridgePointElevated : outerCornerElevated;

        // If I'm lower than or equal to both neighbors, I do a normal triangle.
        if (cell.elevation <= otherNeighbor.elevation && cell.elevation <= fwdNeighbor.elevation)
        {
            AddTriangle(begin*radius, endLeft*radius, endRight*radius, color);
            return;
        }

        // Simple slope triangle for cliffs.
        if (fwdIsCliff)
        {
            AddTriangle(begin*radius, endLeft*radius, endRight*radius, color);
            return;
        }

        if (fwdIsLevel && otherIsCliff)
        {
            if (left) {endLeft = outerCorner*otherElevationFactor;}
            if (!left) {endRight = outerCorner*otherElevationFactor;}
            AddTriangle(begin*radius, endLeft*radius, endRight*radius, color);
            return;
        }

        // If forward is a terrace and other isn't a cliff, I always do a simple terrace.
        if (fwdIsTerrace && !otherIsCliff)
        {
            var up = cell.center.normalized;
            MeshifyCornerInternal(begin, cell, endLeft, up, true, endRight, up, true, color);
            return;
        }

        // If forward is higher and other is terrace,
        // we need to triangle fan from terrace other-side to our forward bridge point.
        if (fwdIsHigher && otherIsTerrace)
        {
            var up = cell.center.normalized;
            if (!left) {endRight = outerCorner*otherElevationFactor;}
            if (left) {endLeft = outerCorner*otherElevationFactor;}
            var terraceLeft = left ? otherIsTerrace : fwdIsTerrace;
            var terraceRight = left ? fwdIsTerrace : otherIsTerrace;
            //AddTriangle(begin*radius, endLeft*radius, endRight*radius);
            //AddTriangleColor(color);
            MeshifyCornerInternal(begin, cell, endLeft, up, terraceLeft, endRight, up, terraceRight, color);
            return;
        }

        // we triangle from our inner corner, to fwd's bridge point, to other's outer corner.
        if (fwdIsHigher && otherIsCliff)
        {
            if (left) {endLeft = outerCorner*otherElevationFactor;}
            if (!left) {endRight = outerCorner*otherElevationFactor;}
            AddTriangle(begin*radius, endLeft*radius, endRight*radius, color);
            return;
        }

        // If my side-neighbor is lower than me and my facing neighbor,
        // this is an inner corner and I'll need to begin triangulation from the bottom.
        if (!fwdIsHigher && !fwdIsCliff && otherIsTerrace)
        {
            begin = outerCorner*otherElevationFactor;
            endLeft = left ? bridgePointElevated : innerCornerElevated;
            endRight = left ? innerCornerElevated : bridgePointElevated;
            var leftUp = left ? bridgePointElevated.normalized : cell.center.normalized;
            var rightUp = left ? cell.center.normalized : bridgePointElevated.normalized;
            MeshifyCornerInternal(begin, cell, endLeft, leftUp, true, endRight, rightUp, true, color);
            return;
        }

        // If I'm facing a terrace and my side is a cliff, I have two steps.
        // First I have to do a terrace-fan halfway down my cliff.
        // Second I have to do a sloping quad the rest of the way.
        if (otherIsCliff && fwdIsTerrace)
        {
            // Our outer corner point needs to be halfway up our neighbor sloped corner...
            var outerMidpoint = Vector3.Lerp(innerCornerElevated, outerCorner*otherElevationFactor, 0.5f);
            endLeft = left ? outerMidpoint : bridgePointElevated;
            endRight = left ? bridgePointElevated : outerMidpoint;
            var terraceLeft = left ? otherIsTerrace : fwdIsTerrace;
            var terraceRight = left ? fwdIsTerrace : otherIsTerrace;
            var up = cell.center.normalized;
            MeshifyCornerInternal(begin, cell, endLeft, up, terraceLeft, endRight, up, terraceRight, color);

            AddTriangle(endLeft*radius, outerCorner*otherElevationFactor*radius, endRight*radius, color);
            return;
        }
        AddTriangle(begin*radius, endLeft*radius, endRight*radius, color);
    }

    private void MeshifyEdgeTerrace(Vector3 beginLeft, Vector3 beginRight, PolyCell beginCell,
                                     Vector3 endLeft,   Vector3 endRight,   PolyCell endCell,
                                     Color color)
    {
        var up = beginCell.center.normalized;
        Vector3 v3 = TerraceLerp(beginLeft, endLeft, up, 1);
        Vector3 v4 = TerraceLerp(beginRight, endRight, up, 1);
        
        AddQuad(beginLeft*radius, beginRight*radius, v3*radius, v4*radius, color);

        int terraceSteps = cellGeometrySettings.terracesPerSlope * 2 + 1;
        for (int i = 2; i < terraceSteps; i++)
        {
            var v1 = v3;
            var v2 = v4;
            v3 = TerraceLerp(beginLeft, endLeft, up, i);
            v4 = TerraceLerp(beginRight, endRight, up, i);
            AddQuad(v1*radius, v2*radius, v3*radius, v4*radius, color);
        }

        AddQuad(v3*radius, v4*radius, endLeft*radius, endRight*radius, color);
    }

    private void MeshifyCornerInternal(Vector3 begin, PolyCell beginCell,
                               Vector3 endLeft, Vector3 leftUp, bool terraceLeft,
                               Vector3 endRight, Vector3 rightUp, bool terraceRight,
                               Color color)
    {
        Vector3 v3 = terraceLeft ?  TerraceLerp(begin, endLeft,  leftUp, 1) : endLeft;
        Vector3 v4 = terraceRight ? TerraceLerp(begin, endRight, rightUp, 1) : endRight;

        AddTriangle(begin*radius, v3*radius, v4*radius, color);

        int terraceSteps = cellGeometrySettings.terracesPerSlope * 2 + 1;
        for (int i = 2; i < terraceSteps; i++)
        {
            var v1 = v3;
            var v2 = v4;
            v3 = terraceLeft ? TerraceLerp(begin, endLeft, leftUp, i) : endLeft;
            v4 = terraceRight ? TerraceLerp(begin, endRight, rightUp, i) : endRight;
            if (terraceLeft && terraceRight)
            {
                AddQuad(v1*radius, v2*radius, v3*radius, v4*radius, color);
            }
            else if (terraceLeft)
            {
                AddTriangle(v1*radius, v3*radius, endRight*radius, color);
            }
            else
            {
                AddTriangle(v2*radius, endLeft*radius, v4*radius, color);
            }
        }

        AddQuad(v3*radius, v4*radius, endLeft*radius, endRight*radius, color);
    }

    private void MeshifyCornerTerrace(Vector3 begin, PolyCell beginCell,
                                       Vector3 endLeft, Vector3 endRight, PolyCell endCell,
                                       Color color)
    {
        MeshifyCornerInternal(begin, beginCell, endLeft, beginCell.center.normalized, true,
                                                endRight, beginCell.center.normalized, true, color);
    }

    private void MeshifyInnerCornerTerrace(PolyCell cell, Vector3 begin, Vector3 ownTop, Vector3 sharedTop, bool left, Color color)
    {
        if (left)
        {
            MeshifyCornerInternal(begin, cell, sharedTop, sharedTop.normalized, true, 
                                                ownTop, cell.center.normalized, true, color);
        }
        else
        {
            MeshifyCornerInternal(begin, cell, ownTop, cell.center.normalized, true, 
                                                sharedTop, sharedTop.normalized, true, color);
        }
    }

    private Vector3 TerraceLerp(Vector3 a, Vector3 b, Vector3 up, int step)
    {
        int terraceSteps = cellGeometrySettings.terracesPerSlope * 2 + 1;
        float horizontalTerraceStepSize = 1f/terraceSteps;
        float verticalTerraceStepSize = 1f / (cellGeometrySettings.terracesPerSlope + 1);

        float h = step * horizontalTerraceStepSize;
        float v = ((step + 1) / 2) * verticalTerraceStepSize;

        // These would be the 'y component' of a and b if we were flat
        var aHeight = Vector3.Dot(a,up)*up;
        var bHeight = Vector3.Dot(b,up)*up;

        // And these are the xz component, aka position projected onto the flat surface defined by 'up'
        var flatA = a - aHeight;
        var flatB = b - bHeight;

        // The horizontal component and vertical component of our final offset from flatA
        var hDiff = flatB-flatA;
        var vDiff = bHeight - aHeight;

        var final = flatA + aHeight;
        final += hDiff*h;
        final += vDiff*v;
        return final;
    }

    private void AddTriangle(Vector3 v1, Vector3 v2, Vector3 v3, Color color)
    {
        var i1 = AddVertex(v1, color);
        var i2 = AddVertex(v2, color);
        var i3 = AddVertex(v3, color);
        _triangles.Add(i1);
        _triangles.Add(i2);
        _triangles.Add(i3);
    }

    private void AddTriangleColor(Color color)
    {
        _colors.Add(color);
        _colors.Add(color);
        _colors.Add(color);
    }

    private void AddQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, Color color)
    {
        var i1 = AddVertex(v1, color);
        var i2 = AddVertex(v2, color);
        var i3 = AddVertex(v3, color);
        var i4 = AddVertex(v4, color);
        _triangles.Add(i1);
        _triangles.Add(i3);
        _triangles.Add(i2);
        _triangles.Add(i2);
        _triangles.Add(i3);
        _triangles.Add(i4);
    }

    private void AddQuadColor(Color color)
    {
        _colors.Add(color);
        _colors.Add(color);
        _colors.Add(color);
        _colors.Add(color);
    }

    private int AddVertex(Vector3 vertex, Color color)
    {
        if (_vertexLookup.ContainsKey(vertex))
        {
            return _vertexLookup[vertex];
        }
        var index = _vertices.Count;
        _vertices.Add(vertex);
        _colors.Add(color);
        _vertexLookup[vertex] = index;
        return index;
    }

    
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PolyCellMeshGenerator
{

    public CellGeometrySettings cellGeometrySettings;
    public float radius = 1f;

    public bool cellIsSelected = false;
    public bool cellIsMouseover = false;
    

    private List<Vector3> _vertices = new List<Vector3>();
    private List<int> _triangles = new List<int>();
    private List<Color> _colors = new List<Color>();

    public void SetGeoLists(List<Vector3> vertices, List<int> triangles, List<Color> colors)
    {
        _vertices = vertices;
        _triangles = triangles;
        _colors = colors;
    }

    public void MeshifyCell(PolyCell cell)
    {
        for (int i = 0; i < cell.corners.Count; i++)
        {
            MeshifyCellSegment(cell, i);
        }
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

        AddTriangle(centerElevated*radius, innerAElevated*radius, innerBElevated*radius);
        AddTriangleColor(color);

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
            AddQuad(innerAElevated*radius, innerBElevated*radius, bridgeAElevated*radius, bridgeBElevated*radius);
            AddQuadColor(color);
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

        var fwdIsSlope = cell.elevation == fwdNeighbor.elevation + 1;
        var otherIsSlope = cell.elevation == otherNeighbor.elevation + 1;
        var fwdIsCliff = cell.elevation > fwdNeighbor.elevation + 1;
        var otherIsCliff = cell.elevation > otherNeighbor.elevation + 1;

        // If I'm lower than or equal to both neighbors, I do a normal triangle.
        if (cell.elevation <= otherNeighbor.elevation && cell.elevation <= fwdNeighbor.elevation)
        {
            if (left)
                AddTriangle(innerCornerElevated*radius, outerCornerElevated*radius, bridgePointElevated*radius);
            else
                AddTriangle(innerCornerElevated*radius, bridgePointElevated*radius, outerCornerElevated*radius);
            AddTriangleColor(color);
        }
        // If this side is terraced, I should be terraced as well.
        else if (fwdIsSlope)
        {
            if (left)
                MeshifyCornerTerrace(innerCornerElevated, cell, outerCornerElevated, bridgePointElevated, fwdNeighbor, color);
            else
                MeshifyCornerTerrace(innerCornerElevated, cell, bridgePointElevated, outerCornerElevated, fwdNeighbor, color);
        }
        // If just the adjacent side is terraced, I should terrace, but outerCorner needs to pull from my other neighbor.
        // Additionally, since this corner terrace needs to join with my neighbor's corner terrace,
        // We'll both need to align our 'up' direction for these terraces.
        else if (otherIsSlope && !fwdIsCliff)
        {
            MeshifyInnerCornerTerrace(cell, outerCorner*otherElevationFactor, innerCornerElevated, bridgePointElevated, left, color);
        }
        else
        {
            if (left)
                AddTriangle(innerCornerElevated*radius, outerCornerElevated*radius, bridgePointElevated*radius);
            else
                AddTriangle(innerCornerElevated*radius, bridgePointElevated*radius, outerCornerElevated*radius);
            AddTriangleColor(color);
        }
    }

    private void MeshifyEdgeTerrace(Vector3 beginLeft, Vector3 beginRight, PolyCell beginCell,
                                     Vector3 endLeft,   Vector3 endRight,   PolyCell endCell,
                                     Color color)
    {
        var up = beginCell.center.normalized;
        Vector3 v3 = TerraceLerp(beginLeft, endLeft, up, 1);
        Vector3 v4 = TerraceLerp(beginRight, endRight, up, 1);
        
        AddQuad(beginLeft*radius, beginRight*radius, v3*radius, v4*radius);
        AddQuadColor(color);

        int terraceSteps = cellGeometrySettings.terracesPerSlope * 2 + 1;
        for (int i = 2; i < terraceSteps; i++)
        {
            var v1 = v3;
            var v2 = v4;
            v3 = TerraceLerp(beginLeft, endLeft, up, i);
            v4 = TerraceLerp(beginRight, endRight, up, i);
            AddQuad(v1*radius, v2*radius, v3*radius, v4*radius);
            AddQuadColor(color);
        }

        AddQuad(v3*radius, v4*radius, endLeft*radius, endRight*radius);
        AddQuadColor(color);
    }

    private void MeshifyCornerTerrace(Vector3 begin, PolyCell beginCell,
                                       Vector3 endLeft, Vector3 endRight, PolyCell endCell,
                                       Color color)
    {
        var up = beginCell.center.normalized;
        Vector3 v3 = TerraceLerp(begin, endLeft, up, 1);
        Vector3 v4 = TerraceLerp(begin, endRight, up, 1);
        
        AddTriangle(begin*radius, v3*radius, v4*radius);
        AddTriangleColor(color);

        int terraceSteps = cellGeometrySettings.terracesPerSlope * 2 + 1;
        for (int i = 2; i < terraceSteps; i++)
        {
            var v1 = v3;
            var v2 = v4;
            v3 = TerraceLerp(begin, endLeft, up, i);
            v4 = TerraceLerp(begin, endRight, up, i);
            AddQuad(v1*radius, v2*radius, v3*radius, v4*radius);
            AddQuadColor(color);
        }

        AddQuad(v3*radius, v4*radius, endLeft*radius, endRight*radius);
        AddQuadColor(color);
    }

    private void MeshifyInnerCornerTerrace(PolyCell cell, Vector3 begin, Vector3 ownTop, Vector3 sharedTop, bool left, Color color)
    {
        var endLeft = sharedTop;
        var endRight = ownTop;
        var leftUp = sharedTop.normalized;
        var rightUp = cell.center.normalized;
        if (!left)
        {
            endLeft = ownTop;
            endRight = sharedTop;
            leftUp = cell.center.normalized;
            rightUp = sharedTop.normalized;
        }

        Vector3 v3 = TerraceLerp(begin, endLeft, leftUp, 1);
        Vector3 v4 = TerraceLerp(begin, endRight, rightUp, 1);
        
        AddTriangle(begin*radius, v3*radius, v4*radius);
        AddTriangleColor(color);

        int terraceSteps = cellGeometrySettings.terracesPerSlope * 2 + 1;
        for (int i = 2; i < terraceSteps; i++)
        {
            var v1 = v3;
            var v2 = v4;
            v3 = TerraceLerp(begin, endLeft, leftUp, i);
            v4 = TerraceLerp(begin, endRight, rightUp, i);
            AddQuad(v1*radius, v2*radius, v3*radius, v4*radius);
            AddQuadColor(color);
        }

        AddQuad(v3*radius, v4*radius, endLeft*radius, endRight*radius);
        AddQuadColor(color);
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

    private void AddTriangle(Vector3 v1, Vector3 v2, Vector3 v3)
    {
        int vertexIndex = _vertices.Count;
        _vertices.Add(v1);
        _vertices.Add(v2);
        _vertices.Add(v3);
        _triangles.Add(vertexIndex);
        _triangles.Add(vertexIndex + 1);
        _triangles.Add(vertexIndex + 2);
    }

    private void AddTriangleColor(Color color)
    {
        _colors.Add(color);
        _colors.Add(color);
        _colors.Add(color);
    }

    private void AddQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4)
    {
        int vertexIndex = _vertices.Count;
        _vertices.Add(v1);
        _vertices.Add(v2);
        _vertices.Add(v3);
        _vertices.Add(v4);
        _triangles.Add(vertexIndex);
        _triangles.Add(vertexIndex + 2);
        _triangles.Add(vertexIndex + 1);
        _triangles.Add(vertexIndex + 1);
        _triangles.Add(vertexIndex + 2);
        _triangles.Add(vertexIndex + 3);
    }

    private void AddQuadColor(Color color)
    {
        _colors.Add(color);
        _colors.Add(color);
        _colors.Add(color);
        _colors.Add(color);
    }

    
}

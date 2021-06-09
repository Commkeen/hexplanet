using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

public class HexsphereGenerator
{
    private PolyCell[] _cells;
    public PolyCell[] GetCells() {return _cells;}


    private Vector3[] _points;
    private Vector2Int[] _connections;
    private Vector3Int[] _tris;
    private Dictionary<int,List<int>> _trisByVertex;
    private Dictionary<int,int> _faceByVertex;
    private Dictionary<Vector3Int, Vector3> _centroidLookup;

    public void GenerateHexsphere(int subdivisions, float radius)
    {
        var icosphereGen = new IcosphereGenerator();
        icosphereGen.GenerateIcosphere(subdivisions, radius);

        _points = icosphereGen.GetPoints();
        _connections = icosphereGen.GetConnections();
        _tris = icosphereGen.GetTris();
        _trisByVertex = icosphereGen.GetTrisByVertex();
        _faceByVertex = icosphereGen.GetFacesByVertex();

        _centroidLookup = new Dictionary<Vector3Int, Vector3>();

        GenerateCells();
    }

    // Use _points and _connections to generate a list of PolyCells with position and neighbor info
    private void GenerateCells()
    {
        _cells = new PolyCell[_points.Length];
        for (int i = 0; i < _points.Length; i++)
        {
            var cell = GenerateCell(i);
            _cells[i] = cell;
        }

        for (int i = 0; i < _cells.Length; i++)
        {
            PopulateNeighbors(i);
        }
    }

    // From a vertex in _points, generate a PolyCell whose edgepoints are the centroids of the tris belonging to that vertex.
    private PolyCell GenerateCell(int index)
    {
        var triIndices = _trisByVertex[index];
        var numTris = triIndices.Count;
        Debug.Assert(numTris > 4 && numTris < 7, $"GenerateCell called with {numTris} tris");
        var edgePoints = new List<Vector3>();

        // Get a tri, then the next tri that shares index + 1 vert of previous tri, and so on.
        var lastTri = _tris[triIndices[0]];
        triIndices.RemoveAt(0);
        edgePoints.Add(GetTriangleCentroid(lastTri));
        var connectionPoint = GetNextTriIndex(index, lastTri);
        for (int i = 1; i < numTris; i++)
        {
            bool triFound = false;
            // Find the next tri
            foreach (var nextTriIndex in triIndices)
            {
                var tri = _tris[nextTriIndex];
                if (TriContains(connectionPoint, tri))
                {
                    triFound = true;
                    lastTri = tri;
                    triIndices.Remove(nextTriIndex);
                    edgePoints.Add(GetTriangleCentroid(tri));
                    connectionPoint = GetNextTriIndex(index, tri);
                    break;
                }
            }
            Debug.Assert(triFound);
        }

        float x = 0, y = 0, z = 0;
        foreach (var p in edgePoints)
        {
            x += p.x; y += p.y; z += p.z;
        }
        x /= (float)edgePoints.Count; y /= (float)edgePoints.Count; z /= edgePoints.Count;

        var cell = new PolyCell();
        cell.index = index;
        cell.face = _faceByVertex[index];
        cell.center = new Vector3(x,y,z);
        cell.normal = cell.center.normalized;
        cell.corners = edgePoints;
        var color = new Color((float)index/(float)_points.Length, 0, (float)_points.Length-index/(float)_points.Length, 1);
        cell.color = color;
        return cell;
    }

    private void PopulateNeighbors(int cellIndex)
    {
        var cell = _cells[cellIndex];
        var neighbors = GetNeighbors(cellIndex, _connections);
        var neighborList = new List<PolyCell>();
        foreach (var n in neighbors)
        {
            neighborList.Add(_cells[n]);
        }
        cell.neighbors = neighborList;
        Debug.Assert(neighborList.Count >= 5 && neighborList.Count <= 6, $"{neighborList.Count}");
    }

    private Vector3 GetTriangleCentroid(Vector3Int tri)
    {
        if (_centroidLookup.ContainsKey(tri)) {return _centroidLookup[tri];}
        var result = (_points[tri.x]+_points[tri.y]+_points[tri.z])/3f;
        _centroidLookup.Add(tri, result);
        return result;
    }

    private static int GetNextTriIndex(int index, Vector3Int tri)
    {
        if (tri.x == index) {return tri.z;}
        if (tri.y == index) {return tri.x;}
        if (tri.z == index) {return tri.y;}
        Debug.LogError("Couldn't find index in tri!");
        return 0;
    }

    private static bool TriContains(int index, Vector3Int tri)
    {
        return tri.x == index || tri.y == index || tri.z == index;
    }

    private static int[] GetNeighbors(int index, Vector2Int[] connections)
    {
        var neighbors = new List<int>();
        for (int i = 0; i < connections.Length; i++)
        {
            var edge = connections[i];
            if (edge.x == index) {neighbors.Add(edge.y);}
            if (edge.y == index) {neighbors.Add(edge.x);}
        }
        return neighbors.ToArray();
    }
}

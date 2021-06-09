using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

public class IcosphereGenerator
{
    private List<Vector3> _points;
    private Dictionary<Vector3, int> _pointLookup;
    private List<Vector2Int> _connections;
    private HashSet<Vector2Int> _connectionLookup;
    private List<Vector3Int> _tris;
    private Dictionary<Vector2Int, int[]> _edgeCache;
    private Dictionary<int,List<int>> _trisByVertex;
    private Dictionary<int,int> _faceByVertex;
    private float _radius = 0;

    public Vector3[] GetPoints() {return _points.ToArray();}
    public Vector2Int[] GetConnections() {return _connections.ToArray();}
    public Vector3Int[] GetTris() {return _tris.ToArray();}
    public Dictionary<int,List<int>> GetTrisByVertex() {return _trisByVertex;}
    public Dictionary<int,int> GetFacesByVertex() {return _faceByVertex;}

    public int[] GetTrisFlat()
    {
        var result = new List<int>();
        foreach (var tri in _tris)
        {
            result.Add(tri.x);
            result.Add(tri.y);
            result.Add(tri.z);
        }
        return result.ToArray();
    }

    public void GenerateIcosphere(int subdivisions, float radius)
    {
        Profiler.BeginSample("Generate Icosphere");
        InitBaseGeometry();
        SubdivideGeometry(subdivisions);
        Spherify(radius);
        Profiler.EndSample();
    }

    public void SetRadius(float radius)
    {
        if (_radius == radius) {return;}
        Spherify(radius);
    }

    // Populate the points and connections lists
    private void InitBaseGeometry()
    {
        _points = new List<Vector3>();
        _points.AddRange(Icosahedron.vertices);
        _connections = new List<Vector2Int>();
        _tris = new List<Vector3Int>();
        _tris.AddRange(Icosahedron.triangles);
        _pointLookup = new Dictionary<Vector3, int>();
        for (var i = 0; i < _points.Count; i++)
        {
            _pointLookup.Add(_points[i], i);
        }
        _connectionLookup = new HashSet<Vector2Int>();
        _trisByVertex = new Dictionary<int, List<int>>();
        _faceByVertex = new Dictionary<int, int>();
    }

    private void SubdivideGeometry(int subdivisions)
    {
        //if (subdivisions <= 0) {return;}
        var originalTris = new List<Vector3Int>();
        originalTris.AddRange(_tris);
        _tris = new List<Vector3Int>();
        _edgeCache = new Dictionary<Vector2Int, int[]>();
        for (var i = 0; i < originalTris.Count; i++)
        {
            Profiler.BeginSample("Subdivide Triangle");
            SubdivideTriangle(originalTris[i], subdivisions, i);
            Profiler.EndSample();
        }

        if (subdivisions == 0)
        {
            for (var i = 0; i < _points.Count; i++)
            {
                _faceByVertex[i] = i;
            }
        }

        if (_faceByVertex.Count != _points.Count)
        {
            Debug.LogError($"Face index count {_faceByVertex.Count}, point count {_points.Count}");
        }

    }

    private void Spherify(float radius)
    {
        _radius = radius;
        for (var i = 0; i < _points.Count; i++)
        {
            _points[i] = _points[i].normalized * radius;
        }
    }

    private void SubdivideTriangle(Vector3Int triangle, int subdivisions, int faceIndex)
    {
        //if (subdivisions <= 0) {return;}
        
        var leftEdge  = SubdivideEdge(new Vector2Int(triangle.x, triangle.y), subdivisions, faceIndex);
        var rightEdge = SubdivideEdge(new Vector2Int(triangle.x, triangle.z), subdivisions, faceIndex);
        var topLine = new[]{leftEdge[1], rightEdge[1]}; // These two vertices plus triangle.x form the 'top' triangle in the subdivide.
        AddTriangleConnections(triangle.x, leftEdge[1], rightEdge[1]);
        
        for (var i = 1; i <= subdivisions; i++)
        {
            var bottomLine = SubdivideEdge(new Vector2Int(leftEdge[i+1], rightEdge[i+1]), i, faceIndex);
            var rowLength = topLine.Length; // How many 'up-facing' triangles we have (1 for each topline point)

            for (var k = 0; k < rowLength; k++)
            {
                AddTriangleConnections(topLine[k], bottomLine[k], bottomLine[k+1]); //The 'up-facing' triangle
                if (k < rowLength-1)
                    AddTriangleConnections(topLine[k], bottomLine[k+1], topLine[k+1]); //The 'down-facing' triangle, there's 1 less of these
            }
            topLine = bottomLine;
        }
    }

    private int[] SubdivideEdge(Vector2Int edge, int subdivisions, int faceIndex)
    {
        Profiler.BeginSample("Subdivide Edge");
        if (subdivisions <= 0) {return new[]{edge.x, edge.y};}

        // If we've subdivided this edge before, return the one we already did!
        if (_edgeCache.ContainsKey(edge))
        {
            return _edgeCache[edge];
        }
        var reverseKey = new Vector2Int(edge.y, edge.x);
        if (_edgeCache.ContainsKey(reverseKey))
        {
            var cached = _edgeCache[reverseKey];
            var result = new int[cached.Length];
            for (int i = 0; i < cached.Length; i++)
            {
                result[i] = cached[cached.Length - (i+1)];
            }
            return result;
        }

        var results = new int[subdivisions+2];
        float divisionFactor = 1f / (subdivisions+1); // Each segment is this fraction of the original edge

        var vecA = _points[edge.x];
        var vecB = _points[edge.y];

        results[0] = edge.x;
        for (var i = 1; i <= subdivisions; i++)
        {
            var newVec = Vector3.Lerp(vecA, vecB, divisionFactor*i);
            var newIndex = _points.Count;
            Profiler.BeginSample("Dict lookup");
            if (_pointLookup.ContainsKey(newVec))
            {
                newIndex = _pointLookup[newVec];
            }
            else
            {
                _points.Add(newVec);
                _pointLookup.Add(newVec, newIndex);
            }
            Profiler.EndSample();
            results[i] = newIndex;
        }
        results[results.Length-1] = edge.y;
        _edgeCache.Add(edge, results);

        // Assign these points to the current face
        foreach (var result in results)
        {
            if (!_faceByVertex.ContainsKey(result))
            {
                _faceByVertex[result] = faceIndex;
            }
        }

        Profiler.EndSample();
        return results;
    }

    // Add 3 connections to _connections and to _tris.
    private void AddTriangleConnections(int vecIndexA, int vecIndexB, int vecIndexC)
    {
        Profiler.BeginSample("Add Triangle Connections");
        AddConnection(vecIndexA, vecIndexB);
        AddConnection(vecIndexA, vecIndexC);
        AddConnection(vecIndexB, vecIndexC);
        AddTriangle(vecIndexA, vecIndexB, vecIndexC);
        Profiler.EndSample();
    }

    private void AddTriangle(int vecIndexA, int vecIndexB, int vecIndexC)
    {
        var triIndex = _tris.Count;
        _tris.Add(new Vector3Int(vecIndexA, vecIndexB, vecIndexC));
        AddTriByVertex(vecIndexA, triIndex);
        AddTriByVertex(vecIndexB, triIndex);
        AddTriByVertex(vecIndexC, triIndex);
    }

    private void AddTriByVertex(int vertIndex, int triIndex)
    {
        List<int> triList;
        if (!_trisByVertex.ContainsKey(vertIndex))
        {
            triList = new List<int>();
            _trisByVertex.Add(vertIndex, triList);
        }
        else
        {
            triList = _trisByVertex[vertIndex];
        }
        if (!triList.Contains(triIndex)) {triList.Add(triIndex);}
    }

    // Add to _connections, but check for uniqueness.
    private void AddConnection(int vecIndexA, int vecIndexB)
    {
        Debug.Assert(vecIndexA != vecIndexB, "Invalid AddConnection");
        if (vecIndexA > vecIndexB)
        {
            var temp = vecIndexA;
            vecIndexA = vecIndexB;
            vecIndexB = temp;
        }

        var conn = new Vector2Int(vecIndexA, vecIndexB);
        if (!_connectionLookup.Contains(conn))
        {
            _connections.Add(conn);
            _connectionLookup.Add(conn);
        }
    }
}

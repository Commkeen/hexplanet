using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlanetGrid : MonoBehaviour
{

    public int resolution = 3;

    private PolyCell[] _cells;

    private Vector3[] _points;
    private Vector2Int[] _connections;
    private PlanetMesh _mesh;

    void Awake()
    {
        Init();
    }

    void Start()
    {
        Init();
        GeneratePlanet();
    }

    void OnValidate()
    {
        Init();
        GeneratePlanet();
    }

    void OnDrawGizmosSelected()
    {
        if (_points == null) {return;}
        Gizmos.color = Color.black;
        for (int i = 0; i < _points.Length; i++)
        {
            Gizmos.DrawSphere(_points[i], 0.1f);
        }
    }

    private void Init()
    {
        if (_mesh == null) {_mesh = GetComponentInChildren<PlanetMesh>();}
    }

    // Populate the points and connections lists
    private void InitPlanetGeometry()
    {
        _points = new Vector3[Icosahedron.vertices.Length];
        System.Array.Copy(Icosahedron.vertices, _points, Icosahedron.vertices.Length);
        _connections = new Vector2Int[Icosahedron.edges.Length];
        System.Array.Copy(Icosahedron.edges, _connections, Icosahedron.edges.Length);
    }

    private void GeneratePlanet()
    {
        InitPlanetGeometry();
        GenerateCells();
        //_mesh.GenerateMesh(_cells);
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

    private PolyCell GenerateCell(int index)
    {
        var center = _points[index];
        var cell = new PolyCell();//index, _points[index]);
        var color = new Color(1, (float)index/(float)_points.Length, 0, 1);
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

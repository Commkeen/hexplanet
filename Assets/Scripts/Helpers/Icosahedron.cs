using System.Collections.Generic;
using UnityEngine;

public static class Icosahedron
{
    public const float PHI = 2.61803398875f;
    public const float T = PHI / 2.0f;
    public static Vector3[] vertices = new Vector3[]
    {
        new Vector3(-1f,  T, 0).normalized,
        new Vector3( 1f,  T, 0).normalized,
        new Vector3(-1f, -T, 0).normalized,
        new Vector3( 1f, -T, 0).normalized,

        new Vector3(0, -1f,  T).normalized,
        new Vector3(0,  1f,  T).normalized,
        new Vector3(0, -1f, -T).normalized,
        new Vector3(0,  1f, -T).normalized,

        new Vector3( T, 0, -1f).normalized,
        new Vector3( T, 0,  1f).normalized,
        new Vector3(-T, 0, -1f).normalized,
        new Vector3(-T, 0,  1f).normalized,
    };

    public static Vector2Int[] edges = new Vector2Int[]
    {
        new Vector2Int(0, 1),
        new Vector2Int(0, 5),
        new Vector2Int(0, 7),
        new Vector2Int(0, 10),
        new Vector2Int(0, 11),
        new Vector2Int(1, 5),
        new Vector2Int(1, 7),
        new Vector2Int(1, 8),
        new Vector2Int(1, 9),
        new Vector2Int(2, 3),
        new Vector2Int(2, 4),
        new Vector2Int(2, 6),
        new Vector2Int(2, 10),
        new Vector2Int(2, 11),
        new Vector2Int(3, 4),
        new Vector2Int(3, 6),
        new Vector2Int(3, 8),
        new Vector2Int(3, 9),
        new Vector2Int(4, 5),
        new Vector2Int(4, 9),
        new Vector2Int(4, 11),
        new Vector2Int(5, 9),
        new Vector2Int(5, 11),
        new Vector2Int(6, 7),
        new Vector2Int(6, 8),
        new Vector2Int(6, 10),
        new Vector2Int(7, 8),
        new Vector2Int(7, 10),
        new Vector2Int(8, 9),
        new Vector2Int(10, 11)
    };

    public static Vector3Int[] triangles = new Vector3Int[]
    {
        new Vector3Int(0, 11, 5),
        new Vector3Int(0, 5, 1),
        new Vector3Int(0, 1, 7),
        new Vector3Int(0, 7, 10),
        new Vector3Int(0, 10, 11),
        new Vector3Int(1, 5, 9),
        new Vector3Int(5, 11, 4),
        new Vector3Int(11, 10, 2),
        new Vector3Int(10, 7, 6),
        new Vector3Int(7, 1, 8),
        new Vector3Int(3, 9, 4),
        new Vector3Int(3, 4, 2),
        new Vector3Int(3, 2, 6),
        new Vector3Int(3, 6, 8),
        new Vector3Int(3, 8, 9),
        new Vector3Int(4, 9, 5),
        new Vector3Int(2, 4, 11),
        new Vector3Int(6, 2, 10),
        new Vector3Int(8, 6, 7),
        new Vector3Int(9, 8, 1),
    };

    public static int[] GetNeighbors(int vertex)
    {
        var neighbors = new List<int>();
        for (int i = 0; i < edges.Length; i++)
        {
            var edge = edges[i];
            if (edge.x == vertex) {neighbors.Add(edge.y);}
            if (edge.y == vertex) {neighbors.Add(edge.x);}
        }
        return neighbors.ToArray();
    }
}
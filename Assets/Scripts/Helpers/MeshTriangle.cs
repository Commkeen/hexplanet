using System.Collections.Generic;
using UnityEngine;

public class MeshTriangle
{
    public List<int> vertices;
    public List<MeshTriangle> neighbors;
    public Color color;

    public MeshTriangle(int v1, int v2, int v3)
    {
        vertices = new List<int>() {v1, v2, v3};
        neighbors = new List<MeshTriangle>();
    }

    public bool IsNeighbor(MeshTriangle other)
    {
        int sharedVertices = 0;
        foreach (int vertex in vertices)
        {
            if (other.vertices.Contains(vertex))
            {
                sharedVertices++;
            }
        }
        return sharedVertices > 1;
    }
}
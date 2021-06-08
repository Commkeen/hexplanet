using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class PolyMesh : MonoBehaviour
{
    [Range(0, 30)]
    public int subdivisions = 0;

    [Min(0.5f)]
    public float radius = 1f;
    public Color color = Color.yellow;


    private Dictionary<int, IcosphereGenerator> _generatorCache;
    private Mesh _mesh;
    private MeshCollider _collider;
    private List<Vector3> _vertices;
    private List<int> _triangles;
    private List<Color> _colors;
    private List<Vector3> _normals;
    private List<Vector3> _debugPoints;

    void OnDrawGizmosSelected()
    {
        if (_debugPoints == null) {return;}
        Gizmos.color = Color.blue;
        for (int i = 0; i < _debugPoints.Count; i++)
        {
            Gizmos.DrawSphere(_debugPoints[i], 0.1f);
        }
    }

    public void GenerateMesh()
    {
        Init();
        _mesh.Clear();
        _vertices.Clear();
        _triangles.Clear();
        _colors.Clear();
        _normals.Clear();
        _debugPoints.Clear();

        var generator = GetIcosphere(subdivisions, radius);

        _mesh.vertices = generator.GetPoints();
        _mesh.triangles = generator.GetTrisFlat();

        for (var i = 0; i < _mesh.vertices.Length; i++)
        {
            _colors.Add(color);
            _normals.Add(_mesh.vertices[i].normalized);
        }
        _mesh.colors = _colors.ToArray();
        _mesh.normals = _normals.ToArray();
        
        //_mesh.RecalculateNormals();
        _collider.sharedMesh = _mesh;
    }

    public void ClearIcosphereCache()
    {
        _generatorCache.Clear();
    }

    private void Init()
    {
        _mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = _mesh;
        _collider = GetComponent<MeshCollider>();
        if (_collider == null) {_collider = gameObject.AddComponent<MeshCollider>();}
        _mesh.name = "Poly Mesh";
        _vertices = new List<Vector3>();
        _triangles = new List<int>();
        _colors = new List<Color>();
        _normals = new List<Vector3>();
        _debugPoints = new List<Vector3>();
    }

    private IcosphereGenerator GetIcosphere(int subdivisions, float radius)
    {
        IcosphereGenerator result = null;
        if (_generatorCache == null) {_generatorCache = new Dictionary<int, IcosphereGenerator>();}
        if (_generatorCache.ContainsKey(subdivisions))
        {
            result = _generatorCache[subdivisions];
            result.SetRadius(radius);
            return result;
        }
        result = new IcosphereGenerator();
        result.GenerateIcosphere(subdivisions, radius);
        _generatorCache.Add(subdivisions, result);
        return result;
    }
}

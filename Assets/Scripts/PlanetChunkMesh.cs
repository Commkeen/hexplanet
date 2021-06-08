using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class PlanetChunkMesh : MonoBehaviour
{
    public PlanetMesh planet;

    private PolyCell[] _localCells;

    private Mesh _mesh;
    private MeshCollider _collider;
    private List<Vector3> _vertices;
    private List<int> _triangles;
    private List<Color> _colors;

    public void Init(PlanetMesh planet)
    {
        this.planet = planet;
        if (_mesh == null) {_mesh = new Mesh();}
        GetComponent<MeshFilter>().mesh = _mesh;
        GetComponent<MeshRenderer>().material = planet.cellMaterial;
        _collider = GetComponent<MeshCollider>();
        if (_collider == null) {_collider = gameObject.AddComponent<MeshCollider>();}
        _mesh.name = "Planet Mesh";
        _vertices = new List<Vector3>();
        _triangles = new List<int>();
        _colors = new List<Color>();
    }
    public void InitCells(PolyCell[] cells)
    {
        this._localCells = cells;
    }

    public void RebuildMesh()
    {
        _mesh.Clear();
        _vertices.Clear();
        _triangles.Clear();
        _colors.Clear();

        var colorFilter = new ColorFilter(planet.colorSettings);
        var elevationFilter = new ElevationFilter(planet.elevationSettings);
        foreach (var cell in _localCells)
        {
            elevationFilter.Evaluate(cell);
            colorFilter.Evaluate(cell);
        }
        MeshifyCells(_localCells);

        _mesh.vertices = _vertices.ToArray();
        _mesh.colors = _colors.ToArray();
        _mesh.triangles = _triangles.ToArray();
        _mesh.RecalculateNormals();
    }

    private void MeshifyCells(PolyCell[] cells)
    {
        PolyCellMeshGenerator cellMeshGenerator = new PolyCellMeshGenerator();
        cellMeshGenerator.cellGeometrySettings = planet.cellGeometrySettings;
        cellMeshGenerator.radius = planet.radius;
        cellMeshGenerator.SetGeoLists(_vertices, _triangles, _colors);

        for (int i = 0; i < cells.Length; i++)
        {
            cellMeshGenerator.cellIsMouseover = cells[i].index == planet.mouseOverCell;
            cellMeshGenerator.cellIsSelected = cells[i].index == planet.selectedCell;
            cellMeshGenerator.MeshifyCell(cells[i]);
        }
    }

    public Vector3 GetMousePositionOnMesh()
    {
        if (_collider == null) {_collider = GetComponent<MeshCollider>();}
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, Mathf.Infinity))
        {
            if (hit.collider != _collider) {return Vector3.zero;}
            return hit.point;
        }
        return Vector3.zero;
    }

}

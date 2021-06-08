using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class PlanetMesh : MonoBehaviour
{
    [Range(0, 16)]
    public int subdivisions = 0;

    [Min(0.5f)]
    public float radius = 1f;

    public ColorSettings colorSettings;
    public ElevationSettings elevationSettings;
    public CellGeometrySettings cellGeometrySettings;

    private int mouseOverCell = -1;
    private int selectedCell = -1;

    private PolyCell[] _cells;

    private Dictionary<int, HexsphereGenerator> _generatorCache;

    private Mesh _mesh;
    private MeshCollider _collider;
    private List<Vector3> _vertices;
    private List<int> _triangles;
    private List<Color> _colors;
    private List<Vector3> _debugPoints;

    void OnDrawGizmosSelected()
    {
        if (_debugPoints == null) {return;}
        Gizmos.color = Color.yellow;
        for (int i = 0; i < _debugPoints.Count; i++)
        {
            Gizmos.DrawSphere(_debugPoints[i], 0.1f);
        }
    }

    void Start()
    {
        GenerateMesh();
    }

    void Update()
    {
        mouseOverCell = -1;
        if (Input.GetMouseButtonDown(1))
        {
            selectedCell = -1;
        }
        var mousePos = GetMousePositionOnMesh();
        if (mousePos != Vector3.zero)
        {
            var cell = GetCellAtPosition(mousePos);
            if (cell != null)
            {
                mouseOverCell = cell.index;
                if (Input.GetMouseButtonDown(0))
                {
                    selectedCell = cell.index;
                }
            }
        }

        RebuildMesh();
    }

    private Vector3 GetMousePositionOnMesh()
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

    private PolyCell GetCellAtPosition(Vector3 pos)
    {
        PolyCell cell = null;
        var sqDist = Mathf.Infinity;
        for (int i = 0; i < _cells.Length; i++)
        {
            var c = _cells[i];
            var d = (pos - c.center*radius).sqrMagnitude;
            if (sqDist > d)
            {
                cell = c;
                sqDist = d;
            }
        }
        return cell;
    }

    public void GenerateMesh()
    {
        Init();
        
        var generator = GetHexsphere(subdivisions);
        _cells = generator.GetCells();

        RebuildMesh();
        _collider.sharedMesh = _mesh;
    }

    private void Init()
    {
        if (_mesh == null) {_mesh = new Mesh();}
        GetComponent<MeshFilter>().mesh = _mesh;
        _collider = GetComponent<MeshCollider>();
        if (_collider == null) {_collider = gameObject.AddComponent<MeshCollider>();}
        _mesh.name = "Planet Mesh";
        _vertices = new List<Vector3>();
        _triangles = new List<int>();
        _colors = new List<Color>();
        _debugPoints = new List<Vector3>();
    }

    private void RebuildMesh()
    {
        _mesh.Clear();
        _vertices.Clear();
        _triangles.Clear();
        _colors.Clear();
        _debugPoints.Clear();

        var colorFilter = new ColorFilter(colorSettings);
        var elevationFilter = new ElevationFilter(elevationSettings);
        foreach (var cell in _cells)
        {
            elevationFilter.Evaluate(cell);
            colorFilter.Evaluate(cell);
        }
        MeshifyCells(_cells);

        _mesh.vertices = _vertices.ToArray();
        _mesh.colors = _colors.ToArray();
        _mesh.triangles = _triangles.ToArray();
        _mesh.RecalculateNormals();
    }

    private void MeshifyCells(PolyCell[] cells)
    {
        PolyCellMeshGenerator cellMeshGenerator = new PolyCellMeshGenerator();
        cellMeshGenerator.cellGeometrySettings = cellGeometrySettings;
        cellMeshGenerator.radius = radius;
        cellMeshGenerator.SetGeoLists(_vertices, _triangles, _colors);

        for (int i = 0; i < cells.Length; i++)
        {
            cellMeshGenerator.cellIsMouseover = i == mouseOverCell;
            cellMeshGenerator.cellIsSelected = i == selectedCell;
            cellMeshGenerator.MeshifyCell(cells[i]);
        }
    }

    private HexsphereGenerator GetHexsphere(int subdivisions)
    {
        HexsphereGenerator result = null;
        if (_generatorCache == null) {_generatorCache = new Dictionary<int, HexsphereGenerator>();}
        if (_generatorCache.ContainsKey(subdivisions))
        {
            result = _generatorCache[subdivisions];
            return result;
        }
        result = new HexsphereGenerator();
        result.GenerateHexsphere(subdivisions, 1);
        _generatorCache.Add(subdivisions, result);
        return result;
    }
}

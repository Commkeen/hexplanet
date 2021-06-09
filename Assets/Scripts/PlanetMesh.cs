using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlanetMesh : MonoBehaviour
{
    [Range(0, 16)]
    public int subdivisions = 0;

    [Min(0.5f)]
    public float radius = 1f;

    [Range(20, 100)]
    public int cellsPerChunk = 20;

    public Material cellMaterial;

    public ColorSettings colorSettings;
    public ElevationSettings elevationSettings;
    public CellGeometrySettings cellGeometrySettings;

    public int mouseOverCell = -1;
    public int selectedCell = -1;

    private PolyCell[] _cells;

    public PlanetChunkMesh[] _chunks;

    private Dictionary<int, HexsphereGenerator> _generatorCache;

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
        var mousePos = _chunks[0].GetMousePositionOnMesh();
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
        var generator = GetHexsphere(subdivisions);
        _cells = generator.GetCells();

        InitChunks();

        RebuildMesh();
    }

    private void InitChunks()
    {
        if (_chunks != null)
        {
            foreach (var chunk in _chunks)
            {
                if (chunk?.gameObject == null) {continue;}
                GameObject.DestroyImmediate(chunk.gameObject);
            }
        }

        var numChunks = Icosahedron.triangles.Length; // 1 chunk per original ico face

        _chunks = new PlanetChunkMesh[numChunks];
        for (int i = 0; i < _chunks.Length; i++)
        {
            var chunk = new GameObject("Chunk " + i).AddComponent<PlanetChunkMesh>();
            _chunks[i] = chunk;
            chunk.transform.SetParent(transform, false);
            chunk.Init(this);
        }
    }

    private void RebuildMesh()
    {
        var numChunks = Icosahedron.triangles.Length;
        var cellLists = new List<PolyCell>[numChunks];
        for (int i = 0; i < cellLists.Length; i++)
        {
            cellLists[i] = new List<PolyCell>();
        }

        foreach (var cell in _cells)
        {
            cellLists[cell.face].Add(cell);
        }

        for(int i = 0; i < numChunks; i++)
        {
            var chunk = _chunks[i];
            chunk.InitCells(cellLists[i].ToArray());
            chunk.RebuildMesh();
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

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;

public class PlanetMesh : MonoBehaviour
{
    [Range(0,100f)]
    public float rotateSpeed = 0;
    [Range(0, 30)]
    public int subdivisions = 0;

    private int _currentSubdivs = -1;

    [Min(0.5f)]
    public float radius = 1f;

    [Range(10, 1000)]
    public uint maxRebuildTime = 250;

    public Material cellMaterial;

    public ColorSettings colorSettings;
    public ElevationSettings elevationSettings;
    public CellGeometrySettings cellGeometrySettings;

    public int mouseOverCell = -1;
    public int selectedCell = -1;

    private PolyCell[] _cells;

    public PlanetChunkMesh[] _chunks;

    private IEnumerator _rebuildMeshCoroutine;

    private Dictionary<int, HexsphereGenerator> _generatorCache;

    void Start()
    {
        GenerateMesh();
    }

    void Update()
    {
        var rotEuler = transform.rotation.eulerAngles;
        var rotY = rotEuler.y;
        rotY += rotateSpeed * Time.deltaTime;
        rotEuler = new Vector3(rotEuler.x, rotY, rotEuler.z);
        transform.rotation = Quaternion.Euler(rotEuler);

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

        if (!RebuildDirtyChunks(maxRebuildTime))
        {
            UpdateCellFilters();
        }
    }

    public void OnInspectorChange()
    {
        if (subdivisions != _currentSubdivs)
        {
            GenerateMesh();
            return;
        }

        UpdateCellFilters();
    }

    public void UpdateCellFilters()
    {
        RunCellFilters();
        RebuildMesh();
    }

    public void GenerateMesh()
    {
        _currentSubdivs = subdivisions;
        var generator = GetHexsphere(subdivisions);
        _cells = generator.GetCells();

        InitChunks();
        AssignCellsToChunks();
        RunCellFilters();
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
            chunk.Init(this, i);
        }
    }

    private void RunCellFilters()
    {
        var colorFilter = new ColorFilter(colorSettings);
        var elevationFilter = new ElevationFilter(elevationSettings);
        foreach (var cell in _cells)
        {
            elevationFilter.Evaluate(cell);
            colorFilter.Evaluate(cell);
        }
    }

    private void AssignCellsToChunks()
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
        }
    }

    private void RebuildMesh()
    {
        if (Application.isPlaying)
        {
            DirtyAllChunks();
        }
        else
        {
            RebuildAllChunks();
        }
    }

    private void RebuildAllChunks()
    {
        var numChunks = Icosahedron.triangles.Length;
        for(int i = 0; i < numChunks; i++)
        {
            var chunk = _chunks[i];
            chunk.RebuildMesh();
        }
    }

    private void DirtyAllChunks()
    {
        var numChunks = Icosahedron.triangles.Length;
        for(int i = 0; i < numChunks; i++)
        {
            var chunk = _chunks[i];
            chunk.dirty = true;
        }
    }

    private bool RebuildDirtyChunks(uint maxTime)
    {
        var stopwatch = new Stopwatch();
        var dirtyChunkFound = false;
        var numChunks = Icosahedron.triangles.Length;
        stopwatch.Start();
        for(int i = 0; i < numChunks; i++)
        {
            var chunk = _chunks[i];
            if (chunk.dirty)
            {
                dirtyChunkFound = true;
                chunk.RebuildMesh();
            }
            if (stopwatch.ElapsedMilliseconds > maxTime) {break;}
        }
        stopwatch.Stop();
        return dirtyChunkFound;
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

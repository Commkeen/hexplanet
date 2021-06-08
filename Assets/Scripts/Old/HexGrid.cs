using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HexGrid : MonoBehaviour
{

    public int width = 6;
    public int height = 6;

    public Color defaultColor = Color.white;
    public Color touchedColor = Color.magenta;

    public HexCell cellPrefab;
    public Text cellLabelPrefab;
    
    private HexCell[] _cells;

    private Canvas _gridCanvas;
    private HexMesh _hexMesh;

    void Awake()
    {
        _gridCanvas = GetComponentInChildren<Canvas>();
        _hexMesh = GetComponentInChildren<HexMesh>();

        _cells = new HexCell[height*width];

        for (int z = 0, i = 0; z < height; z++)
        {
            for (int x = 0; x < width; x++)
            {
                CreateCell(x, z, i++);
            }
        }
    }

    void Start()
    {
        _hexMesh.Triangulate(_cells);
    }

    void Update()
    {
        if (Input.GetMouseButton(0))
        {
            HandleInput();
        }
    }

    private void HandleInput()
    {
        var inputRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(inputRay, out hit))
        {
            TouchCell(hit.point);
        }
    }

    private void TouchCell(Vector3 position)
    {
        position = transform.InverseTransformPoint(position);
        HexCoordinates coordinates = HexCoordinates.FromPosition(position);
        Debug.Log("touched at " + position);
    }

    private void CreateCell(int x, int z, int i)
    {
        Vector3 pos;
        pos.x = (x + z * 0.5f - z / 2) * (HexMetrics.innerRadius * 2f);
        pos.y = 0f;
        pos.z = z * (HexMetrics.outerRadius * 1.5f);

        HexCell cell = Instantiate<HexCell>(cellPrefab);
        _cells[i] = cell;
        cell.transform.SetParent(transform, false);
        cell.transform.localPosition = pos;
        cell.coordinates = HexCoordinates.FromOffsetCoordinates(x,z);
        cell.color = defaultColor;

        Text label = Instantiate<Text>(cellLabelPrefab);
        label.rectTransform.SetParent(_gridCanvas.transform, false);
        label.rectTransform.anchoredPosition = new Vector2(pos.x, pos.z);
        label.text = cell.coordinates.ToStringOnSeparateLines();
    }
}

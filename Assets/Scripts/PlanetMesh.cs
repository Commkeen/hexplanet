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

    private void GenerateBaseMesh()
    {
        for (int i = 0; i < Icosahedron.triangles.Length; i++)
        {
            var triangle = Icosahedron.triangles[i];
            var v1 = Icosahedron.vertices[triangle.x];
            var v2 = Icosahedron.vertices[triangle.y];
            var v3 = Icosahedron.vertices[triangle.z];
            AddTriangle(v1, v2, v3);
            AddTriangleColor(Color.green);
        }
    }

    private void MeshifyCells(PolyCell[] cells)
    {
        for (int i = 0; i < cells.Length; i++)
        {
            MeshifyCell(cells[i]);
        }
    }

    private void MeshifyCell(PolyCell cell)
    {
        for (int i = 0; i < cell.corners.Count; i++)
        {
            MeshifyCellSegment(cell, i);
        }
    }

    private void MeshifyCellSegment(PolyCell cell, int side)
    {
        var neighbor = cell.GetNeighborForSegment(side);
        var prevNeighbor = cell.GetNeighborForSegment(side-1);
        var nextNeighbor = cell.GetNeighborForSegment(side+1);

        var innerSize = cellGeometrySettings.innerCellSize;

        var color = cell.color;
        if (selectedCell == cell.index) {color = Color.red;}
        else if (mouseOverCell == cell.index) {color = Color.yellow;}
        //if (side == 0) {color = Color.Lerp(color, Color.red, 0.4f);}
        //if (side == 1) {color = Color.Lerp(color, Color.red, 0.2f);}

        // The base points on the unit sphere.
        (Vector3 cornerA, Vector3 cornerB) = cell.GetCornersForSide(side);
        var center = cell.center;

        var innerA = Vector3.Lerp(center, cornerA, innerSize);
        var innerB = Vector3.Lerp(center, cornerB, innerSize);

        var innerElevationFactor = 1 + cellGeometrySettings.elevationStep * cell.elevation;

        var centerElevated = center * innerElevationFactor;
        var innerAElevated = innerA * innerElevationFactor;
        var innerBElevated = innerB * innerElevationFactor;

        AddTriangle(centerElevated*radius, innerAElevated*radius, innerBElevated*radius);
        AddTriangleColor(color);

        // Inner cell is done, now let's draw the outer edges
        var bridge = cell.GetBridge(side, cellGeometrySettings.outerCellSize);
        var bridgeA = innerA + bridge;
        var bridgeB = innerB + bridge;
        
        // Now, the outer quad needs to ramp down to a neighbor...
        var outerElevationFactor = innerElevationFactor;
        if (neighbor.elevation < cell.elevation)
        {
            outerElevationFactor = 1 + cellGeometrySettings.elevationStep * neighbor.elevation;
        }
        var cornerAElevated = cornerA * outerElevationFactor;
        var cornerBElevated = cornerB * outerElevationFactor;
        var bridgeAElevated = bridgeA * outerElevationFactor;
        var bridgeBElevated = bridgeB * outerElevationFactor;
        
        //color = Color.Lerp(color, Color.black, 0.3f);

        if (cell.elevation - neighbor.elevation == 1)
        {
            MeshifyEdgeTerrace(innerAElevated, innerBElevated, cell, bridgeAElevated, bridgeBElevated, neighbor, color);
        }
        else
        {
            AddQuad(innerAElevated*radius, innerBElevated*radius, bridgeAElevated*radius, bridgeBElevated*radius);
            AddQuadColor(color);
        }

        MeshifyCorner(cell, neighbor, prevNeighbor, side, cornerA, innerA, bridgeA, true, color);
        MeshifyCorner(cell, neighbor, nextNeighbor, side, cornerB, innerB, bridgeB, false, color);
    }

    private void MeshifyCorner(PolyCell cell, PolyCell fwdNeighbor, PolyCell otherNeighbor, int side,
                               Vector3 outerCorner, Vector3 innerCorner, Vector3 bridgePoint,
                               bool left, Color color)
    {
        var center = cell.center;
        var innerElevationFactor = 1 + cellGeometrySettings.elevationStep * cell.elevation;

        var fwdElevationFactor = innerElevationFactor;
        if (fwdNeighbor.elevation < cell.elevation)
        {
            fwdElevationFactor = 1 + cellGeometrySettings.elevationStep * fwdNeighbor.elevation;
        }

        var otherElevationFactor = innerElevationFactor;
        if (otherNeighbor.elevation < cell.elevation)
        {
            otherElevationFactor = 1 + cellGeometrySettings.elevationStep * otherNeighbor.elevation;
        }

        var innerCornerElevated = innerCorner * innerElevationFactor;
        var outerCornerElevated = outerCorner * fwdElevationFactor;
        var bridgePointElevated = bridgePoint * fwdElevationFactor;

        var fwdIsSlope = cell.elevation == fwdNeighbor.elevation + 1;
        var otherIsSlope = cell.elevation == otherNeighbor.elevation + 1;
        var fwdIsCliff = cell.elevation > fwdNeighbor.elevation + 1;
        var otherIsCliff = cell.elevation > otherNeighbor.elevation + 1;

        // If I'm lower than or equal to both neighbors, I do a normal triangle.
        if (cell.elevation <= otherNeighbor.elevation && cell.elevation <= fwdNeighbor.elevation)
        {
            if (left)
                AddTriangle(innerCornerElevated*radius, outerCornerElevated*radius, bridgePointElevated*radius);
            else
                AddTriangle(innerCornerElevated*radius, bridgePointElevated*radius, outerCornerElevated*radius);
            AddTriangleColor(color);
        }
        // If this side is terraced, I should be terraced as well.
        else if (fwdIsSlope)
        {
            if (left)
                MeshifyCornerTerrace(innerCornerElevated, cell, outerCornerElevated, bridgePointElevated, fwdNeighbor, color);
            else
                MeshifyCornerTerrace(innerCornerElevated, cell, bridgePointElevated, outerCornerElevated, fwdNeighbor, color);
        }
        // If just the adjacent side is terraced, I should terrace, but outerCorner needs to pull from my other neighbor.
        // Additionally, since this corner terrace needs to join with my neighbor's corner terrace,
        // We'll both need to align our 'up' direction for these terraces.
        else if (otherIsSlope && !fwdIsCliff)
        {
            MeshifyInnerCornerTerrace(cell, outerCorner*otherElevationFactor, innerCornerElevated, bridgePointElevated, left, color);
        }
        else
        {
            if (left)
                AddTriangle(innerCornerElevated*radius, outerCornerElevated*radius, bridgePointElevated*radius);
            else
                AddTriangle(innerCornerElevated*radius, bridgePointElevated*radius, outerCornerElevated*radius);
            AddTriangleColor(color);
        }
    }

    private void MeshifyEdgeTerrace(Vector3 beginLeft, Vector3 beginRight, PolyCell beginCell,
                                     Vector3 endLeft,   Vector3 endRight,   PolyCell endCell,
                                     Color color)
    {
        var up = beginCell.center.normalized;
        Vector3 v3 = TerraceLerp(beginLeft, endLeft, up, 1);
        Vector3 v4 = TerraceLerp(beginRight, endRight, up, 1);
        
        AddQuad(beginLeft*radius, beginRight*radius, v3*radius, v4*radius);
        AddQuadColor(color);

        int terraceSteps = cellGeometrySettings.terracesPerSlope * 2 + 1;
        for (int i = 2; i < terraceSteps; i++)
        {
            var v1 = v3;
            var v2 = v4;
            v3 = TerraceLerp(beginLeft, endLeft, up, i);
            v4 = TerraceLerp(beginRight, endRight, up, i);
            AddQuad(v1*radius, v2*radius, v3*radius, v4*radius);
            AddQuadColor(color);
        }

        AddQuad(v3*radius, v4*radius, endLeft*radius, endRight*radius);
        AddQuadColor(color);
    }

    private void MeshifyCornerTerrace(Vector3 begin, PolyCell beginCell,
                                       Vector3 endLeft, Vector3 endRight, PolyCell endCell,
                                       Color color)
    {
        var up = beginCell.center.normalized;
        Vector3 v3 = TerraceLerp(begin, endLeft, up, 1);
        Vector3 v4 = TerraceLerp(begin, endRight, up, 1);
        
        AddTriangle(begin*radius, v3*radius, v4*radius);
        AddTriangleColor(color);

        int terraceSteps = cellGeometrySettings.terracesPerSlope * 2 + 1;
        for (int i = 2; i < terraceSteps; i++)
        {
            var v1 = v3;
            var v2 = v4;
            v3 = TerraceLerp(begin, endLeft, up, i);
            v4 = TerraceLerp(begin, endRight, up, i);
            AddQuad(v1*radius, v2*radius, v3*radius, v4*radius);
            AddQuadColor(color);
        }

        AddQuad(v3*radius, v4*radius, endLeft*radius, endRight*radius);
        AddQuadColor(color);
    }

    private void MeshifyInnerCornerTerrace(PolyCell cell, Vector3 begin, Vector3 ownTop, Vector3 sharedTop, bool left, Color color)
    {
        var endLeft = sharedTop;
        var endRight = ownTop;
        var leftUp = sharedTop.normalized;
        var rightUp = cell.center.normalized;
        if (!left)
        {
            endLeft = ownTop;
            endRight = sharedTop;
            leftUp = cell.center.normalized;
            rightUp = sharedTop.normalized;
        }

        Vector3 v3 = TerraceLerp(begin, endLeft, leftUp, 1);
        Vector3 v4 = TerraceLerp(begin, endRight, rightUp, 1);
        
        AddTriangle(begin*radius, v3*radius, v4*radius);
        AddTriangleColor(color);

        int terraceSteps = cellGeometrySettings.terracesPerSlope * 2 + 1;
        for (int i = 2; i < terraceSteps; i++)
        {
            var v1 = v3;
            var v2 = v4;
            v3 = TerraceLerp(begin, endLeft, leftUp, i);
            v4 = TerraceLerp(begin, endRight, rightUp, i);
            AddQuad(v1*radius, v2*radius, v3*radius, v4*radius);
            AddQuadColor(color);
        }

        AddQuad(v3*radius, v4*radius, endLeft*radius, endRight*radius);
        AddQuadColor(color);
    }

    private Vector3 TerraceLerp(Vector3 a, Vector3 b, Vector3 up, int step)
    {
        int terraceSteps = cellGeometrySettings.terracesPerSlope * 2 + 1;
        float horizontalTerraceStepSize = 1f/terraceSteps;
        float verticalTerraceStepSize = 1f / (cellGeometrySettings.terracesPerSlope + 1);

        float h = step * horizontalTerraceStepSize;
        float v = ((step + 1) / 2) * verticalTerraceStepSize;

        // These would be the 'y component' of a and b if we were flat
        var aHeight = Vector3.Dot(a,up)*up;
        var bHeight = Vector3.Dot(b,up)*up;

        // And these are the xz component, aka position projected onto the flat surface defined by 'up'
        var flatA = a - aHeight;
        var flatB = b - bHeight;

        // The horizontal component and vertical component of our final offset from flatA
        var hDiff = flatB-flatA;
        var vDiff = bHeight - aHeight;

        var final = flatA + aHeight;
        final += hDiff*h;
        final += vDiff*v;
        return final;
    }

    /*
    private void MeshifyCells(PolyCell[] cells)
    {
        for (int i = 0; i < cells.Length; i++)
        {
            var cell = cells[i];
            var center = cell.center;
            var neighbors = new Vector3[cell.neighbors.Count];
            for (int k = 0; k < neighbors.Length; k++)
            {
                neighbors[k] = cell.neighbors[k].center;
            }
            var color = cell.color;
            GenerateCell(center, neighbors, color);
        }
    }

    private void GenerateCell(Vector3 center, Vector3[] neighbors, Color color)
    {
        // Step 1: Get edge midpoints
        List<Vector3> edgeMidpoints = new List<Vector3>();
        for (int i = 0; i < neighbors.Length; i++)
        {
            var neighborCenter = neighbors[i];

            var midpoint = GetEdgeMidpointBetweenNeighbors(center, neighborCenter);
            edgeMidpoints.Add(midpoint);
            //_debugPoints.Add(midpoint);
        }

        // Step 2: Sort edge midpoints
        var baseVec = edgeMidpoints[0] - center;
        edgeMidpoints.Sort((a, b) => Vector3.SignedAngle(baseVec, a, center).CompareTo(Vector3.SignedAngle(baseVec, b, center)));
        edgeMidpoints.Add(edgeMidpoints[0]);
        edgeMidpoints.Add(edgeMidpoints[1]);

        // Step 3: For each edge midpoint, draw triangle
        for (int i = 1; i < edgeMidpoints.Count-1; i++)
        {
            var thisMidpoint = edgeMidpoints[i];
            var prevMidpoint = edgeMidpoints[i-1];
            var nextMidpoint = edgeMidpoints[i+1];

            // Get vectors representing the edges for each midpoint
            var thisPointLine = Vector3.Cross(thisMidpoint - center, center);
            var prevPointLine = Vector3.Cross(prevMidpoint - center, center);
            var nextPointLine = Vector3.Cross(nextMidpoint - center, center);

            Vector3 corner1;
            Vector3 corner2;
            ClosestPointsOnTwoLines(out corner1, out var _, prevMidpoint, prevPointLine, thisMidpoint, thisPointLine);
            ClosestPointsOnTwoLines(out corner2, out var _, thisMidpoint, thisPointLine, nextMidpoint, nextPointLine);
            AddTriangle(center, corner1, corner2);
            AddTriangleColor(color);
        }
    }
    */

    private void AddTriangle(Vector3 v1, Vector3 v2, Vector3 v3)
    {
        int vertexIndex = _vertices.Count;
        _vertices.Add(v1);
        _vertices.Add(v2);
        _vertices.Add(v3);
        _triangles.Add(vertexIndex);
        _triangles.Add(vertexIndex + 1);
        _triangles.Add(vertexIndex + 2);
    }

    private void AddTriangleColor(Color color)
    {
        _colors.Add(color);
        _colors.Add(color);
        _colors.Add(color);
    }

    private void AddQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4)
    {
        int vertexIndex = _vertices.Count;
        _vertices.Add(v1);
        _vertices.Add(v2);
        _vertices.Add(v3);
        _vertices.Add(v4);
        _triangles.Add(vertexIndex);
        _triangles.Add(vertexIndex + 2);
        _triangles.Add(vertexIndex + 1);
        _triangles.Add(vertexIndex + 1);
        _triangles.Add(vertexIndex + 2);
        _triangles.Add(vertexIndex + 3);
    }

    private void AddQuadColor(Color color)
    {
        _colors.Add(color);
        _colors.Add(color);
        _colors.Add(color);
        _colors.Add(color);
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

    private static Vector3 GetEdgeMidpointBetweenNeighbors(Vector3 a, Vector3 b)
    {
        Vector3 iPoint;
        Vector3 iVec;
        PlanePlaneIntersection(out iPoint, out iVec, a, a, b, b);
        return ProjectPointOnLine(iPoint, iVec, a);
    }

    private static bool ClosestPointsOnTwoLines(out Vector3 closestPointLine1, out Vector3 closestPointLine2, Vector3 linePoint1, Vector3 lineVec1, Vector3 linePoint2, Vector3 lineVec2){
 
		closestPointLine1 = Vector3.zero;
		closestPointLine2 = Vector3.zero;
 
		float a = Vector3.Dot(lineVec1, lineVec1);
		float b = Vector3.Dot(lineVec1, lineVec2);
		float e = Vector3.Dot(lineVec2, lineVec2);
 
		float d = a*e - b*b;
 
		//lines are not parallel
		if(!Mathf.Approximately(d, 0.0f)){
 
			Vector3 r = linePoint1 - linePoint2;
			float c = Vector3.Dot(lineVec1, r);
			float f = Vector3.Dot(lineVec2, r);
 
			float s = (b*f - c*e) / d;
			float t = (a*f - c*b) / d;
 
			closestPointLine1 = linePoint1 + lineVec1 * s;
			closestPointLine2 = linePoint2 + lineVec2 * t;

			return true;
		}
 
		else{
			return false;
		}
	}

    public static bool PlanePlaneIntersection(out Vector3 linePoint, out Vector3 lineVec, Vector3 plane1Normal, Vector3 plane1Position, Vector3 plane2Normal, Vector3 plane2Position){
 
		linePoint = Vector3.zero;
		lineVec = Vector3.zero;
 
		//We can get the direction of the line of intersection of the two planes by calculating the 
		//cross product of the normals of the two planes. Note that this is just a direction and the line
		//is not fixed in space yet. We need a point for that to go with the line vector.
		lineVec = Vector3.Cross(plane1Normal, plane2Normal);
 
		//Next is to calculate a point on the line to fix it's position in space. This is done by finding a vector from
		//the plane2 location, moving parallel to it's plane, and intersecting plane1. To prevent rounding
		//errors, this vector also has to be perpendicular to lineDirection. To get this vector, calculate
		//the cross product of the normal of plane2 and the lineDirection.		
		Vector3 ldir = Vector3.Cross(plane2Normal, lineVec);		
 
		float denominator = Vector3.Dot(plane1Normal, ldir);
 
		//Prevent divide by zero and rounding errors by requiring about 5 degrees angle between the planes.
		if(Mathf.Abs(denominator) > 0.006f){
 
			Vector3 plane1ToPlane2 = plane1Position - plane2Position;
			float t = Vector3.Dot(plane1Normal, plane1ToPlane2) / denominator;
			linePoint = plane2Position + t * ldir;
 
			return true;
		}
 
		//output not valid
		else{
			return false;
		}
	}

    public static Vector3 ProjectPointOnLine(Vector3 linePoint, Vector3 lineVec, Vector3 point){		
 
		//get vector from point on line to point in space
		Vector3 linePointToPoint = point - linePoint;
 
		float t = Vector3.Dot(linePointToPoint, lineVec);
 
		return linePoint + lineVec * t;
	}
}

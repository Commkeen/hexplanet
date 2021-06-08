using UnityEngine;

[CreateAssetMenu()]
public class CellGeometrySettings : ScriptableObject
{
    [Range(0, 0.07f)]
    public float elevationStep = 0.1f;

    [Range(1, 8)]
    public int terracesPerSlope = 2;

    [Range(0.4f, 0.99f)]
    public float innerCellSize = 0.8f;

    public float outerCellSize {get{return 1f-innerCellSize;}}
}
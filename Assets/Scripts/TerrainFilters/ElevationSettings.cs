using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu()]
public class ElevationSettings : ScriptableObject
{
    public int seed = 0;
    public Vector3 center = Vector3.zero;
    [Min(0)]
    public float roughness = 1;
    [Min(0)]
    public int minElevation = 0;
    [Min(0)]
    public int maxElevation = 8;
    [Range(0f,1f)]
    public float waterCoverage = 1;
}
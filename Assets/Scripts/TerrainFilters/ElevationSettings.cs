using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu()]
public class ElevationSettings : ScriptableObject
{
    public int seed = 0;
    public Vector3 center = Vector3.zero;
    public float roughness = 1;
    public int minElevation = 0;
    public int maxElevation = 8;
}
using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu()]
public class ElevationSettings : ScriptableObject
{
    public float seed = 0;
    public int minElevation = 0;
    public int maxElevation = 5;
}
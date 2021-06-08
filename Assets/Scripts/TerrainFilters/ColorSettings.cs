using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu()]
public class ColorSettings : ScriptableObject
{
    public Gradient colors;
    public float noiseOffset;
}
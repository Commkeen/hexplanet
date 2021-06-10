using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu()]
public class ColorSettings : ScriptableObject
{
    public Color water = Color.blue;
    public int waterHeight = 1;
    public Color beach = Color.yellow;
    public int beachHeight = 2;
    public Color lowland = Color.green;
    public int lowlandHeight = 4;
    public Color highland = Color.green;
    public int highlandHeigh = 6;
    public Color mountain = Color.gray;
}
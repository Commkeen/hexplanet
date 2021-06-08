using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PolyMesh))]
public class PolyMeshEditor : Editor
{
    private PolyMesh _target;

    void OnEnable()
    {
        _target = (PolyMesh)target;
    }

    public override void OnInspectorGUI()
    {
        EditorGUI.BeginChangeCheck();
        base.OnInspectorGUI();
        if (EditorGUI.EndChangeCheck())
        {
            _target.GenerateMesh();
        }

        if (GUILayout.Button("Regenerate Mesh"))
        {
            _target.GenerateMesh();
        }
    }
}
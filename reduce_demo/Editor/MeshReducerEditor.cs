using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MeshReducer))]
public class MeshReducerEditor : Editor {

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        if(GUILayout.Button("Save Mesh Asset"))
        {
            (target as MeshReducer).SaveMeshAsset();
        }
    }
}

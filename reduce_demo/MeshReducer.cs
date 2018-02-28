using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using g3;

[ExecuteInEditMode()]
[RequireComponent(typeof(MeshFilter))]
public class MeshReducer : MonoBehaviour {

    MeshFilter _meshFilter;
    MeshFilter meshFilter
    {
        get {
            if (_meshFilter == null) _meshFilter = GetComponent<MeshFilter>();
            return _meshFilter;
        }
    }

    public int triangleCount = 3000;
    public bool recalculateNormals = true;
    [Range(0, 180)]
    public float normalsAngle = 30;

    int lastTriangleCount;

    public MeshFilter target;

    DMesh3 baseMesh;
    g3UnityUtils.TemporaryMesh temporaryMesh = new g3UnityUtils.TemporaryMesh();

    void ReduceAsync()
    {
        g3UnityUtils.RunMeshFuncAsync(inputTempMesh, temporaryMesh, meshFilter.sharedMesh, Reduce, recalculateNormals, normalsAngle);
        lastTriangleCount = triangleCount;
    }

    DMesh3 Reduce(DMesh3 mesh)
    {
        Reducer r = new Reducer(mesh);
        r.ReduceToTriangleCount(triangleCount);
        return r.Mesh;
    }

    Mesh lastMesh;
    bool lastRecalculateNormals;
    float lastNormalsAngle = -1;

    g3UnityUtils.TemporaryMesh inputTempMesh;

    // Update is called once per frame
    void Update ()
    {
        if (target == null) return;

		if(target.sharedMesh != lastMesh || lastTriangleCount != triangleCount || lastRecalculateNormals != recalculateNormals || lastNormalsAngle != normalsAngle)
        {
            inputTempMesh = g3UnityUtils.UnityMeshToTemporaryMesh(target.sharedMesh);

            lastMesh = target.sharedMesh;
            lastRecalculateNormals = recalculateNormals;
            lastNormalsAngle = normalsAngle;

            if (meshFilter.sharedMesh == null)
                meshFilter.sharedMesh = new Mesh();

            ReduceAsync();
        }
	}

#if UNITY_EDITOR
    [ContextMenu("Save Mesh Asset")]
    public void SaveMeshAsset()
    {
        if (_meshFilter == null || _meshFilter.sharedMesh == null) return;
        SaveMesh(_meshFilter.sharedMesh, "ReducedMesh", true, false);
    }

    public static void SaveMesh(Mesh mesh, string name, bool makeNewInstance, bool optimizeMesh)
    {
        string path = UnityEditor.EditorUtility.SaveFilePanel("Save Separate Mesh Asset", "Assets/", name, "asset");
        if (string.IsNullOrEmpty(path)) return;

        path = UnityEditor.FileUtil.GetProjectRelativePath(path);

        Mesh meshToSave = (makeNewInstance) ? Object.Instantiate(mesh) as Mesh : mesh;

        if (optimizeMesh)
            UnityEditor.MeshUtility.Optimize(meshToSave);

        UnityEditor.AssetDatabase.CreateAsset(meshToSave, path);
        UnityEditor.AssetDatabase.SaveAssets();
    }
#endif
}

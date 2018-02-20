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
    int lastTriangleCount;

    public MeshFilter target;

    DMesh3 baseMesh;
    g3UnityUtils.TemporaryMesh temporaryMesh = new g3UnityUtils.TemporaryMesh();

    void ReduceAsync()
    {
        g3UnityUtils.RunMeshFuncAsync(baseMesh, temporaryMesh, meshFilter.sharedMesh, Reduce);
        lastTriangleCount = triangleCount;
    }

    DMesh3 Reduce(DMesh3 mesh)
    {
        Reducer r = new Reducer(mesh);
        r.ReduceToTriangleCount(triangleCount);
        return r.Mesh;
    }

    Mesh lastMesh;

    // Update is called once per frame
    void Update ()
    {
        if (target == null) return;

		if(target.sharedMesh != lastMesh || lastTriangleCount != triangleCount)
        {
            baseMesh = g3UnityUtils.UnityMeshToDMesh(target.sharedMesh);
            lastMesh = target.sharedMesh;

            if (meshFilter.sharedMesh == null)
                meshFilter.sharedMesh = new Mesh();

            ReduceAsync();
        }
	}
}

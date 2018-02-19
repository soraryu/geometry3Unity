using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using g3;
using System.IO;

public class ReduceDemo : MonoBehaviour
{
    DMesh3 sdfMesh;
    DMesh3 curMesh;

    GameObject meshGO;
    MeshFilter meshFilter;
    g3UnityUtils.TemporaryMesh temporaryMesh = new g3UnityUtils.TemporaryMesh();
    System.Diagnostics.Stopwatch watch;
    string curPath;
    
    int lastTriangleCount;
    float lastEdgeLength;


    DMesh3 bunnyMesh = null;

    // Use this for initialization
    void Start ()
    {
        Loom.Check();

        curPath = Application.dataPath;
        bunnyMesh = g3UnityUtils.UnityMeshToDMesh(mesh);

        lastEdgeLength = edgeLength;
        lastTriangleCount = triangleCount;

        Loom.RunAsync(Task);
    }

    public Mesh mesh;

    void Task()
    {
        watch = new System.Diagnostics.Stopwatch();
        watch.Start();
        
        // LOADING

        // load sample file, convert to unity coordinate system, translate and scale to origin
        if (bunnyMesh == null)
            bunnyMesh = new Sphere3Generator_NormalizedCube().Generate().MakeDMesh();
        MeshTransforms.FlipLeftRightCoordSystems(bunnyMesh);
        MeshTransforms.Translate(bunnyMesh, -bunnyMesh.CachedBounds.Center);
        MeshTransforms.Scale(bunnyMesh, 8.0 / bunnyMesh.CachedBounds.MaxDim);

        // SUBTRACT A SPHERE
        var SphereRadius = 1f;

        // load sample file, convert to unity coordinate system, translate and scale to origin
        Sphere3Generator_NormalizedCube spheregen = new Sphere3Generator_NormalizedCube()
        {
            Radius = SphereRadius
        };
        var sphereMesh = spheregen.Generate().MakeDMesh();
        sphereMesh.ReverseOrientation(true);

        // COMBINE THESE MESHES
        var editor = new MeshEditor(new DMesh3(MeshComponents.All));
        editor.AppendMesh(bunnyMesh);
        editor.AppendMesh(sphereMesh);

        // SDF MERGING
        DMesh3 combinedMesh = editor.Mesh;

        int num_cells = 32;
        double cell_size = combinedMesh.CachedBounds.MaxDim / num_cells;

        MeshSignedDistanceGrid sdf = new MeshSignedDistanceGrid(combinedMesh, cell_size);
        sdf.Compute();

        Log("sdf took " + watch.ElapsedMilliseconds); watch.Reset(); watch.Start();

        DenseGrid3f grid = sdf.Grid;

        var iso = new DenseGridTrilinearImplicit(sdf.Grid, sdf.GridOrigin, sdf.CellSize);
        
        g3.MarchingCubes c = new g3.MarchingCubes();
        c.Implicit = iso;
        c.Bounds = combinedMesh.CachedBounds;
        c.CubeSize = c.Bounds.MaxDim / num_cells;
        c.Bounds.Expand(3 * c.CubeSize);

        c.Generate();

        sdfMesh = new DMesh3(c.Mesh);

        Log("marching cubes took " + watch.ElapsedMilliseconds); watch.Reset(); watch.Start();

        Loom.QueueOnMainThread(() =>
        {
            // load wireframe shader
            Material wireframeShader = g3UnityUtils.SafeLoadMaterial("wireframe_shader/Wireframe");
            
            // create initial mesh
            meshGO = g3UnityUtils.CreateMeshGO("start_mesh", sdfMesh, wireframeShader);
            meshFilter = meshGO.GetComponent<MeshFilter>();

            Log("unity blocking side took " + watch.ElapsedMilliseconds);
        });
    }

    // Update is called once per frame
    void Update () {
        if (triangleCount != lastTriangleCount)
        {
            ReduceAsync();
        }

        if(edgeLength != lastEdgeLength)
        {
            RemeshAsync();
        }
    }

    [ContextMenu("Reduce")]
    void ReduceAsync()
    {
        g3UnityUtils.RunMeshFuncAsync(sdfMesh, temporaryMesh, meshFilter.sharedMesh, Reduce);
        lastTriangleCount = triangleCount;
    }

    [ContextMenu("Remesh")]
    void RemeshAsync()
    {
        g3UnityUtils.RunMeshFuncAsync(sdfMesh, temporaryMesh, meshFilter.sharedMesh, Remesh);
        lastEdgeLength = edgeLength;
    }

    public int triangleCount = 4000;
    public float edgeLength = 0.1f;

    DMesh3 Reduce(DMesh3 mesh)
    {
        Reducer r = new Reducer(mesh);
        r.ReduceToTriangleCount(triangleCount);
        return r.Mesh;
    }

    DMesh3 Remesh(DMesh3 mesh)
    {
        Remesher rem = new Remesher(mesh);
        rem.SetTargetEdgeLength(edgeLength);
        rem.Precompute();

        for (int i = 0; i < 5; i++)
        {
            rem.BasicRemeshPass();
            rem.Mesh.CheckValidity();
        }

        return rem.Mesh;
    }

    public bool logOutput = false;
    void Log(object toLog)
    {
        if(logOutput)
            Debug.Log(toLog);
    }
}

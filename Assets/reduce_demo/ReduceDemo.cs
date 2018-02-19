using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using g3;
using System.IO;

public class ReduceDemo : MonoBehaviour
{
    DMesh3 startMesh;
    DMesh3 curMesh;
    InteractiveReducer reduce;
    Coroutine active_reduce;

    GameObject meshGO;
    MeshFilter meshFilter;
    g3UnityUtils.TemporaryMesh temporaryMesh = new g3UnityUtils.TemporaryMesh();

    string curPath;

	// Use this for initialization
	void Start () {

        Loom.Check();

        curPath = Application.dataPath;
        Loom.RunAsync(Task);
    }

    System.Diagnostics.Stopwatch watch;

    void Task()
    {
        watch = new System.Diagnostics.Stopwatch();
        watch.Start();

        // find path to sample file
        string filePath = Path.Combine(curPath, Path.Combine("..\\sample_files", "bunny_solid.obj"));

        // LOADING

        // load sample file, convert to unity coordinate system, translate and scale to origin
        startMesh = StandardMeshReader.ReadMesh(filePath);
        if (startMesh == null)
            startMesh = new Sphere3Generator_NormalizedCube().Generate().MakeDMesh();
        MeshTransforms.FlipLeftRightCoordSystems(startMesh);
        MeshTransforms.Translate(startMesh, -startMesh.CachedBounds.Center);
        MeshTransforms.Scale(startMesh, 8.0 / startMesh.CachedBounds.MaxDim);


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
        editor.AppendMesh(startMesh);
        editor.AppendMesh(sphereMesh);

        // SDF MERGING
        DMesh3 mesh = editor.Mesh;

        int num_cells = 32;
        double cell_size = mesh.CachedBounds.MaxDim / num_cells;

        MeshSignedDistanceGrid sdf = new MeshSignedDistanceGrid(mesh, cell_size);
        sdf.Compute();

        Debug.Log("sdf took " + watch.ElapsedMilliseconds); watch.Restart();

        DenseGrid3f grid = sdf.Grid;

        var iso = new DenseGridTrilinearImplicit(sdf.Grid, sdf.GridOrigin, sdf.CellSize);
        
        MarchingCubes c = new MarchingCubes();
        c.Implicit = iso;
        c.Bounds = mesh.CachedBounds;
        c.CubeSize = c.Bounds.MaxDim / num_cells;
        c.Bounds.Expand(3 * c.CubeSize);

        c.Generate();
        DMesh3 outputMesh = c.Mesh;
        startMesh = new DMesh3(outputMesh);

        // StandardMeshWriter.WriteMesh("c:\\demo\\output_mesh.obj", c.Mesh, WriteOptions.Defaults);

        Debug.Log("marching cubes took " + watch.ElapsedMilliseconds); watch.Restart();

        // REDUCING

        Reducer r = new Reducer(outputMesh);
        r.ReduceToTriangleCount(1000);
        outputMesh = r.Mesh;

        Debug.Log("reducing took " + watch.ElapsedMilliseconds); watch.Restart();

        Loom.QueueOnMainThread(() =>
        {
            // load wireframe shader
            Material wireframeShader = g3UnityUtils.SafeLoadMaterial("wireframe_shader/Wireframe");
            
            // create initial mesh
            meshGO = g3UnityUtils.CreateMeshGO("start_mesh", outputMesh, wireframeShader);
            meshFilter = meshGO.GetComponent<MeshFilter>();

            Debug.Log("unity blocking side took " + watch.ElapsedMilliseconds);
        });
    }

    int lastTriangleCount;

    // Update is called once per frame
    void Update () {
        if ( Input.GetKeyUp(KeyCode.R) ) {
            if (active_reduce != null)
                StopCoroutine(active_reduce);

            curMesh = new DMesh3(startMesh);
            reduce = new InteractiveReducer(curMesh);
            active_reduce = StartCoroutine(reduce_playback());
        }

        if (triangleCount != lastTriangleCount) { 
            ReduceNow();
            lastTriangleCount = triangleCount;
        }
    }

    public int triangleCount = 4000;
    [ContextMenu("Reduce Now")]
    void ReduceNow()
    {
        Loom.RunAsync(() =>
        {
            watch.Restart();

            var resultMesh = new DMesh3(startMesh);
            Reducer r = new Reducer(resultMesh);
            r.ReduceToTriangleCount(triangleCount);
            resultMesh = r.Mesh;

            //Remesher rem = new Remesher(resultMesh);
            //rem.SetTargetEdgeLength(0.1f);
            //rem.Precompute();

            //for(int i = 0; i < 10; i++) { 
            //    rem.BasicRemeshPass();
            //    rem.Mesh.CheckValidity();
            //}

            Debug.Log("reducing took " + watch.ElapsedMilliseconds); watch.Restart();

            g3UnityUtils.SetTemporaryMesh(temporaryMesh, resultMesh);
            Debug.Log("d3 to temp mesh took " + watch.ElapsedMilliseconds); watch.Restart();
            
            Loom.QueueOnMainThread(() =>
            {
                temporaryMesh.ApplyTo(meshFilter.sharedMesh);
                Debug.Log("unity blocking side took " + watch.ElapsedMilliseconds);
            });
        });
    }

    IEnumerator reduce_playback()
    {
        int iter = 0;
        int N = 100;
        foreach (int i in reduce.ReduceToTriangleCount_Interactive(500) ) {
            if (iter++ % N == 0) {
                g3UnityUtils.SetGOMesh(meshGO, curMesh);
                yield return new WaitForSecondsRealtime(0.001f);
            }
        }
    }

}

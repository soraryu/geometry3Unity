// #define LOGGING

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using g3;

public static class g3UnityUtils
{    
    public static GameObject CreateMeshGO(string name, DMesh3 mesh, Material setMaterial = null, bool bCollider = true)
    {
        var gameObj = new GameObject(name);
        gameObj.AddComponent<MeshFilter>();
        SetGOMesh(gameObj, mesh);
        if (bCollider) {
            gameObj.AddComponent(typeof(MeshCollider));
            gameObj.GetComponent<MeshCollider>().enabled = false;
        }
        if (setMaterial) {
            gameObj.AddComponent<MeshRenderer>().material = setMaterial;
        } else {
            gameObj.AddComponent<MeshRenderer>().material = StandardMaterial(Color.red);
        }
        return gameObj;
    }
    public static GameObject CreateMeshGO(string name, DMesh3 mesh, Colorf color, bool bCollider = true)
    {
        return CreateMeshGO(name, mesh, StandardMaterial(color), bCollider);
    }


    public static void SetGOMesh(GameObject go, DMesh3 mesh)
    {
        DMesh3 useMesh = mesh;
        if ( ! mesh.IsCompact ) {
            useMesh = new DMesh3(mesh, true);
        }


        MeshFilter filter = go.GetComponent<MeshFilter>();
        if (filter == null)
            throw new Exception("g3UnityUtil.SetGOMesh: go " + go.name + " has no MeshFilter");
        Mesh unityMesh = DMeshToUnityMesh(useMesh);
        filter.sharedMesh = unityMesh;
    }

    public static void SetMeshFilterMesh(MeshFilter filter, DMesh3 mesh)
    {
        DMesh3 useMesh = mesh;
        if (!mesh.IsCompact)
        {
            useMesh = new DMesh3(mesh, true);
        }
        
        if (filter == null)
            throw new Exception("g3UnityUtil.SetGOMesh: go " + filter.name + " has no MeshFilter");
        Mesh unityMesh = DMeshToUnityMesh(useMesh, false, filter.sharedMesh);
        filter.sharedMesh = unityMesh;
    }

    public static void SetTemporaryMesh(TemporaryMesh sharedMesh, DMesh3 mesh)
    {
        DMesh3 useMesh = mesh;
        if (!mesh.IsCompact)
        {
            useMesh = new DMesh3(mesh, true);
        }

        if (sharedMesh == null)
            throw new Exception("g3UnityUtil.SetSharedMesh: mesh is null");

        DMeshToTemporaryMesh(useMesh, sharedMesh);
    }

    public class TemporaryMesh
    {
        public List<Vector3> vertices = new List<Vector3>();
        public List<int> triangles = new List<int>();
        public List<Vector3> normals = new List<Vector3>();
        public List<Color> colors = new List<Color>();
        public List<Vector2> uv = new List<Vector2>();
        public int vertexCount
        {
            get { return vertices.Count; }
        }

        public TemporaryMesh()
        {

        }

        public TemporaryMesh(Mesh mesh)
        {
            mesh.GetVertices(this.vertices);
            mesh.GetTriangles(this.triangles, 0);
            mesh.GetNormals(this.normals);
            mesh.GetColors(this.colors);
            mesh.GetUVs(0, this.uv);
        }

        public int subMeshCount
        {
            get { return 1; }
        }
        public List<int> GetTriangles(int submeshIndex)
        {
            return triangles;
        }

        public void ApplyTo(Mesh mesh, bool recalculateNormals = false)
        {
            mesh.Clear();

            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.SetUVs(0, uv);
            if (!recalculateNormals)
                mesh.SetNormals(normals);
            else
                mesh.SetNormals((Vector3[]) null);
            mesh.SetColors(colors);

            if (recalculateNormals)
                mesh.RecalculateNormals();
        }
    }

    /// <summary>
    /// Convert DMesh3 to unity Mesh
    /// </summary>
    public static Mesh DMeshToUnityMesh(DMesh3 m, bool bLimitTo64k = false, Mesh sharedMesh = null)
    {
        if (bLimitTo64k && (m.MaxVertexID > 65535 || m.MaxTriangleID > 65535) ) {
            Debug.Log("g3UnityUtils.DMeshToUnityMesh: attempted to convert DMesh larger than 65535 verts/tris, not supported by Unity!");
            return null;
        }

        Mesh unityMesh = null;
        if (sharedMesh != null)
            unityMesh = sharedMesh;
        else
            unityMesh = new Mesh();
        unityMesh.vertices = dvector_to_vector3(m.VerticesBuffer);
        if (m.HasVertexNormals)
            unityMesh.normals = (m.HasVertexNormals) ? dvector_to_vector3(m.NormalsBuffer) : null;
        if (m.HasVertexColors)
            unityMesh.colors = dvector_to_color(m.ColorsBuffer);
        if (m.HasVertexUVs)
            unityMesh.uv = dvector_to_vector2(m.UVBuffer);
        unityMesh.triangles = dvector_to_int(m.TrianglesBuffer);

        if (m.HasVertexNormals == false)
            unityMesh.RecalculateNormals();

        return unityMesh;
    }

    /// </summary>
    public static void DMeshToTemporaryMesh(DMesh3 m, TemporaryMesh sharedMesh = null)
    {
        var temporaryMesh = sharedMesh;

        dvector_to_vector3(m.VerticesBuffer, ref temporaryMesh.vertices);
        if (m.HasVertexNormals)
            dvector_to_vector3(m.NormalsBuffer, ref temporaryMesh.normals);
        if (m.HasVertexColors)
            dvector_to_color(m.ColorsBuffer, ref temporaryMesh.colors);
        if (m.HasVertexUVs)
             dvector_to_vector2(m.UVBuffer, ref temporaryMesh.uv);
        dvector_to_int(m.TrianglesBuffer, ref temporaryMesh.triangles);
    }

    public static TemporaryMesh UnityMeshToTemporaryMesh(Mesh mesh)
    {
        return new TemporaryMesh(mesh);
    }

    public static DMesh3 TemporaryMeshToDMesh3(TemporaryMesh mesh)
    {
        DMesh3 dmesh;

        var mesh_vertices = mesh.vertices;
        var mesh_vertices_count = mesh_vertices.Count;

        Vector3f[] dmesh_vertices = new Vector3f[mesh_vertices_count];
        for (int i = 0; i < mesh_vertices_count; ++i)
            dmesh_vertices[i] = mesh_vertices[i];

        var mesh_uvs = mesh.uv;
        Vector2f[] dmesh_uvs = null;
        if (mesh_uvs != null)
        {
            dmesh_uvs = new Vector2f[mesh_vertices_count];
            for (int i = 0; i < mesh_vertices_count; ++i)
                dmesh_uvs[i] = mesh_uvs[i];
        }

        var mesh_normals = mesh.normals;
        if (mesh_normals != null)
        {
            Vector3f[] dmesh_normals = new Vector3f[mesh_vertices.Count];
            for (int i = 0; i < mesh.vertexCount; ++i)
                dmesh_normals[i] = mesh_normals[i];

            dmesh = DMesh3Builder.Build(dmesh_vertices, mesh.triangles, dmesh_normals);

        }
        else
        {
            dmesh = DMesh3Builder.Build<Vector3f, int, Vector3f>(dmesh_vertices, mesh.triangles, null, null);
        }

        if (dmesh_uvs != null)
        {
            var dv = new DVector<float>();
            vector2f_to_dvector(dmesh_uvs, ref dv);
            dmesh.UVBuffer = dv;
        }

        return dmesh;
    }

    /// <summary>
    /// Convert unity Mesh to a g3.DMesh3. Ignores UV's.
    /// </summary>
    public static DMesh3 UnityMeshToDMesh(Mesh mesh)
    {
        DMesh3 dmesh;

        var mesh_vertices = mesh.vertices;
        var mesh_vertices_count = mesh_vertices.Length;

        Vector3f[] dmesh_vertices = new Vector3f[mesh_vertices_count];
        for (int i = 0; i < mesh_vertices_count; ++i)
            dmesh_vertices[i] = mesh_vertices[i];

        Vector2[] mesh_uvs = mesh.uv;
        Vector2f[] dmesh_uvs = null;
        if (mesh_uvs != null && mesh_uvs.Length == mesh_vertices_count)
        {
            dmesh_uvs = new Vector2f[mesh_vertices_count];
            for (int i = 0; i < mesh_vertices_count; ++i)
                dmesh_uvs[i] = mesh_uvs[i];
        }

        Vector3[] mesh_normals = mesh.normals;
        if (mesh_normals != null && mesh_normals.Length == mesh_vertices_count) {
            Vector3f[] dmesh_normals = new Vector3f[mesh_vertices_count];
            for (int i = 0; i < mesh_vertices_count; ++i)
                dmesh_normals[i] = mesh_normals[i];

            dmesh = DMesh3Builder.Build(dmesh_vertices, mesh.triangles, dmesh_normals);

        } else {
            dmesh = DMesh3Builder.Build<Vector3f,int,Vector3f>(dmesh_vertices, mesh.triangles, null, null);
        }

        if(dmesh_uvs != null) {
            var dv = new DVector<float>();
            vector2f_to_dvector(dmesh_uvs, ref dv);
            dmesh.UVBuffer = dv;
        }

        return dmesh;
    }



    public static Material StandardMaterial(Colorf color)
    {
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = color;
        return mat;
    }


    public static Material SafeLoadMaterial(string sPath)
    {
        Material mat = null;
        try {
            Material loaded = Resources.Load<Material>(sPath);
            mat = new Material(loaded);
        } catch (Exception e) {
            Debug.Log("g3UnityUtil.SafeLoadMaterial: exception: " + e.Message);
            mat = new Material(Shader.Find("Standard"));
            mat.color = Color.red;
        }
        return mat;
    }

    // per-type conversion functions
    public static Vector3[] dvector_to_vector3(DVector<double> vec)
    {
        int nLen = vec.Length / 3;
        Vector3[] result = new Vector3[nLen];
        for (int i = 0; i < nLen; ++i) {
            result[i].x = (float)vec[3 * i];
            result[i].y = (float)vec[3 * i + 1];
            result[i].z = (float)vec[3 * i + 2];
        }
        return result;
    }
    public static Vector3[] dvector_to_vector3(DVector<float> vec)
    {
        int nLen = vec.Length / 3;
        Vector3[] result = new Vector3[nLen];
        for (int i = 0; i < nLen; ++i) {
            result[i].x = vec[3 * i];
            result[i].y = vec[3 * i + 1];
            result[i].z = vec[3 * i + 2];
        }
        return result;
    }
    public static Vector2[] dvector_to_vector2(DVector<float> vec)
    {
        int nLen = vec.Length / 2;
        Vector2[] result = new Vector2[nLen];
        for (int i = 0; i < nLen; ++i) {
            result[i].x = vec[2 * i];
            result[i].y = vec[2 * i + 1];
        }
        return result;
    }
    public static Color[] dvector_to_color(DVector<float> vec)
    {
        int nLen = vec.Length / 3;
        Color[] result = new Color[nLen];
        for (int i = 0; i < nLen; ++i) {
            result[i].r = vec[3 * i];
            result[i].g = vec[3 * i + 1];
            result[i].b = vec[3 * i + 2];
        }
        return result;
    }
    public static int[] dvector_to_int(DVector<int> vec)
    {
        // todo this could be faster because we can directly copy chunks...
        int nLen = vec.Length;
        int[] result = new int[nLen];
        for (int i = 0; i < nLen; ++i)
            result[i] = vec[i];
        return result;
    }
    
    // per-type conversion functions

    public static void dvector_to_vector3(DVector<double> vec, ref List<Vector3> result)
    {
        int nLen = vec.Length / 3;

        if (result == null || result.Count != nLen) {
            result = new List<Vector3>(new Vector3[nLen]);
        }

        for (int i = 0; i < nLen; ++i)
        {
            result[i] = new Vector3(
                (float)vec[3 * i],
                (float)vec[3 * i + 1],
                (float)vec[3 * i + 2]
                );
        }
    }
    public static void dvector_to_vector3(DVector<float> vec, ref List<Vector3> result)
    {
        int nLen = vec.Length / 3;

        if (result == null || result.Count != nLen) { 
            result = new List<Vector3>(new Vector3[nLen]);
        }

        for (int i = 0; i < nLen; ++i)
        {
            result[i] = new Vector3(
                vec[3 * i],
                vec[3 * i + 1],
                vec[3 * i + 2]
                );
        }
    }
    public static void dvector_to_vector2(DVector<float> vec, ref List<Vector2> result)
    {
        int nLen = vec.Length / 2;

        if (result == null || result.Count != nLen) { 
            result = new List<Vector2>(new Vector2[nLen]);
        }

        for (int i = 0; i < nLen; ++i)
        {
            var r = new Vector2(
                vec[2 * i],
                vec[2 * i + 1]
                );
            result[i] = r;
        }
    }
    public static void vector2f_to_dvector(Vector2f[] vec, ref DVector<float> result)
    {
        int nLen = vec.Length * 2;

        if (result == null || result.Length != nLen)
        {
            result = new DVector<float>(new float[nLen]);
        }

        for (int i = 0; i < nLen / 2; ++i)
        {
            result[2 * i + 0] = vec[i].x;
            result[2 * i + 1] = vec[i].y;
        }
    }
    public static void dvector_to_color(DVector<float> vec, ref List<Color> result)
    {
        int nLen = vec.Length / 3;

        if (result == null || result.Count != nLen) { 
            result = new List<Color>(new Color[nLen]);
        }

        for (int i = 0; i < nLen; ++i)
        {
            result[i] = new Color(
                vec[3 * i], vec[3 * i + 1], vec[3 * i + 2]);
        }
    }
    public static void dvector_to_int(DVector<int> vec, ref List<int> result)
    {
        // todo this could be faster because we can directly copy chunks...
        int nLen = vec.Length;

        if (result == null || result.Count != nLen) { 
            result = new List<int>(new int[nLen]);
        }

        for (int i = 0; i < nLen; ++i)
            result[i] = vec[i];
    }

    public static void RunMeshFuncAsync(DMesh3 baseMesh, g3UnityUtils.TemporaryMesh temporaryMesh, Mesh targetMesh, System.Func<DMesh3, DMesh3> meshFunction, bool recalculateNormals = false, float normalsAngle = 30)
    {
        RunMeshFuncAsync(baseMesh, null, temporaryMesh, targetMesh, meshFunction, recalculateNormals, normalsAngle);
    }

    public static void RunMeshFuncAsync(TemporaryMesh inputTempMesh, g3UnityUtils.TemporaryMesh temporaryMesh, Mesh targetMesh, System.Func<DMesh3, DMesh3> meshFunction, bool recalculateNormals = false, float normalsAngle = 30)
    {
        RunMeshFuncAsync(null, inputTempMesh, temporaryMesh, targetMesh, meshFunction, recalculateNormals, normalsAngle);
    }

    static Dictionary<TemporaryMesh, object> lockObjects = new Dictionary<TemporaryMesh, object>();
    public static void RunMeshFuncAsync(DMesh3 _baseMesh, TemporaryMesh inputTempMesh, g3UnityUtils.TemporaryMesh temporaryMesh, Mesh targetMesh, System.Func<DMesh3, DMesh3> meshFunction, bool recalculateNormals = false, float normalsAngle = 30)
    {
        DMesh3 baseMesh = null;
        if (_baseMesh != null && inputTempMesh == null)
            baseMesh = _baseMesh;

        if (!lockObjects.ContainsKey(temporaryMesh))
            lockObjects.Add(temporaryMesh, new object());

        var locker = lockObjects[temporaryMesh];
        var watch = new System.Diagnostics.Stopwatch();
        
        ThreadUtils.JobScheduler.Instance.AddJob(() =>
        {
            if(_baseMesh == null && inputTempMesh != null)
                baseMesh = g3UnityUtils.TemporaryMeshToDMesh3(inputTempMesh);

            watch.Reset(); watch.Start();
            Log("starting async mesh func");

            var processingMesh = new DMesh3(baseMesh);
            var resultMesh = meshFunction(processingMesh);

            Log("mesh func took " + watch.ElapsedMilliseconds); watch.Reset(); watch.Start();
            lock (locker)
            {
                g3UnityUtils.SetTemporaryMesh(temporaryMesh, resultMesh);
                if (recalculateNormals)
                    temporaryMesh.RecalculateNormals(normalsAngle, true);

                Log("d3 to temp mesh took " + watch.ElapsedMilliseconds); watch.Reset(); watch.Start();
            }
        }, (result) =>
        {
            lock (locker)
            {
                temporaryMesh.ApplyTo(targetMesh, false);
                Log("unity blocking side took " + watch.ElapsedMilliseconds);
            }
        });
    }

    public static void Log(object ob)
    {
#if LOGGING
        Debug.Log(ob);
#endif
    }

    #region Recalculate Normals

    /// <summary>
    ///     Recalculate the normals of a mesh based on an angle threshold. This takes
    ///     into account distinct vertices that have the same position.
    /// </summary>
    /// <param name="mesh"></param>
    /// <param name="angle">
    ///     The smoothing angle. Note that triangles that already share
    ///     the same vertex will be smooth regardless of the angle! (unless you specify "makeVerticesUnique" as well)
    /// </param>
    public static void RecalculateNormals(this g3UnityUtils.TemporaryMesh mesh, float angle, bool makeVerticesUnique = false)
    {
        if (makeVerticesUnique)
        {
            // reconfigure mesh so all triangles have unique vertices
            var tris = mesh.triangles;
            var verts = mesh.vertices;
            var uvs = mesh.uv;

            var c = tris.Count;
            var newVerts = new Vector3[c];
            var newUVs = new Vector2[c];
            var newTris = new int[c];
            for (int i = 0; i < c; i++)
            {
                var vi = tris[i];
                newVerts[i] = verts[vi];
                newUVs[i] = uvs[vi];
                newTris[i] = i;
            }

            mesh.vertices = new List<Vector3>(newVerts);
            mesh.uv = new List<Vector2>(newUVs);
            mesh.triangles = new List<int>(newTris);
        }

        // recalc normals
        var cosineThreshold = Mathf.Cos(angle * Mathf.Deg2Rad);

        var vertices = mesh.vertices;
        var normals = new Vector3[vertices.Count];

        // Holds the normal of each triangle in each sub mesh.
        var triNormals = new Vector3[mesh.subMeshCount][];

        var dictionary = new Dictionary<VertexKey, List<VertexEntry>>(vertices.Count);

        for (var subMeshIndex = 0; subMeshIndex < mesh.subMeshCount; ++subMeshIndex)
        {

            var triangles = mesh.GetTriangles(subMeshIndex);

            triNormals[subMeshIndex] = new Vector3[triangles.Count / 3];

            for (var i = 0; i < triangles.Count; i += 3)
            {
                int i1 = triangles[i];
                int i2 = triangles[i + 1];
                int i3 = triangles[i + 2];

                // Calculate the normal of the triangle
                Vector3 p1 = vertices[i2] - vertices[i1];
                Vector3 p2 = vertices[i3] - vertices[i1];
                Vector3 normal = Vector3.Cross(p1, p2).normalized;
                int triIndex = i / 3;
                triNormals[subMeshIndex][triIndex] = normal;

                List<VertexEntry> entry;
                VertexKey key;

                if (!dictionary.TryGetValue(key = new VertexKey(vertices[i1]), out entry))
                {
                    entry = new List<VertexEntry>(4);
                    dictionary.Add(key, entry);
                }
                entry.Add(new VertexEntry(subMeshIndex, triIndex, i1));

                if (!dictionary.TryGetValue(key = new VertexKey(vertices[i2]), out entry))
                {
                    entry = new List<VertexEntry>();
                    dictionary.Add(key, entry);
                }
                entry.Add(new VertexEntry(subMeshIndex, triIndex, i2));

                if (!dictionary.TryGetValue(key = new VertexKey(vertices[i3]), out entry))
                {
                    entry = new List<VertexEntry>();
                    dictionary.Add(key, entry);
                }
                entry.Add(new VertexEntry(subMeshIndex, triIndex, i3));
            }
        }

        // Each entry in the dictionary represents a unique vertex position.

        foreach (var vertList in dictionary.Values)
        {
            for (var i = 0; i < vertList.Count; ++i)
            {

                var sum = new Vector3();
                var lhsEntry = vertList[i];

                for (var j = 0; j < vertList.Count; ++j)
                {
                    var rhsEntry = vertList[j];

                    if (lhsEntry.VertexIndex == rhsEntry.VertexIndex)
                    {
                        sum += triNormals[rhsEntry.MeshIndex][rhsEntry.TriangleIndex];
                    }
                    else
                    {
                        // The dot product is the cosine of the angle between the two triangles.
                        // A larger cosine means a smaller angle.
                        var dot = Vector3.Dot(
                            triNormals[lhsEntry.MeshIndex][lhsEntry.TriangleIndex],
                            triNormals[rhsEntry.MeshIndex][rhsEntry.TriangleIndex]);
                        if (dot >= cosineThreshold)
                        {
                            sum += triNormals[rhsEntry.MeshIndex][rhsEntry.TriangleIndex];
                        }
                    }
                }

                normals[lhsEntry.VertexIndex] = sum.normalized;
            }
        }

        mesh.normals = new List<Vector3>(normals);
    }

    private struct VertexKey
    {
        private readonly long _x;
        private readonly long _y;
        private readonly long _z;

        // Change this if you require a different precision.
        private const int Tolerance = 100000;

        // Magic FNV values. Do not change these.
        private const long FNV32Init = 0x811c9dc5;
        private const long FNV32Prime = 0x01000193;

        public VertexKey(Vector3 position)
        {
            _x = (long)(Mathf.Round(position.x * Tolerance));
            _y = (long)(Mathf.Round(position.y * Tolerance));
            _z = (long)(Mathf.Round(position.z * Tolerance));
        }

        public override bool Equals(object obj)
        {
            var key = (VertexKey)obj;
            return _x == key._x && _y == key._y && _z == key._z;
        }

        public override int GetHashCode()
        {
            long rv = FNV32Init;
            rv ^= _x;
            rv *= FNV32Prime;
            rv ^= _y;
            rv *= FNV32Prime;
            rv ^= _z;
            rv *= FNV32Prime;

            return rv.GetHashCode();
        }
    }

    private struct VertexEntry
    {
        public int MeshIndex;
        public int TriangleIndex;
        public int VertexIndex;

        public VertexEntry(int meshIndex, int triIndex, int vertIndex)
        {
            MeshIndex = meshIndex;
            TriangleIndex = triIndex;
            VertexIndex = vertIndex;
        }
    }

    #endregion
}

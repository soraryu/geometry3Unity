using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using g3;

public class g3UnityUtils
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
        public List<Vector2> uv1 = new List<Vector2>();

        public void ApplyTo(Mesh mesh, bool recalculateNormals = false)
        {
            mesh.Clear();

            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.SetUVs(0, uv1);
            if (!recalculateNormals)
                mesh.SetNormals(normals);
            else
                mesh.SetNormals(null);
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
        var unityMesh = sharedMesh;

        dvector_to_vector3(m.VerticesBuffer, ref unityMesh.vertices);
        if (m.HasVertexNormals)
            dvector_to_vector3(m.NormalsBuffer, ref unityMesh.normals);
        if (m.HasVertexColors)
            dvector_to_color(m.ColorsBuffer, ref unityMesh.colors);
        if (m.HasVertexUVs)
             dvector_to_vector2(m.UVBuffer, ref unityMesh.uv1);
        dvector_to_int(m.TrianglesBuffer, ref unityMesh.triangles);
    }

    /// <summary>
    /// Convert unity Mesh to a g3.DMesh3. Ignores UV's.
    /// </summary>
    public static DMesh3 UnityMeshToDMesh(Mesh mesh)
    {
        Vector3[] mesh_vertices = mesh.vertices;
        Vector3f[] dmesh_vertices = new Vector3f[mesh_vertices.Length];
        for (int i = 0; i < mesh.vertexCount; ++i)
            dmesh_vertices[i] = mesh_vertices[i];

        Vector3[] mesh_normals = mesh.normals;
        if (mesh_normals != null) {
            Vector3f[] dmesh_normals = new Vector3f[mesh_vertices.Length];
            for (int i = 0; i < mesh.vertexCount; ++i)
                dmesh_normals[i] = mesh_normals[i];

            return DMesh3Builder.Build(dmesh_vertices, mesh.triangles, dmesh_normals);

        } else {
            return DMesh3Builder.Build<Vector3f,int,Vector3f>(dmesh_vertices, mesh.triangles, null, null);
        }
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
}

using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class SubdivideMesh : MonoBehaviour
{
    [Header("Subdivision Settings")]
    public int subdivisions = 5;
    
    [ContextMenu("Subdivide Mesh")]
    public void Subdivide()
    {
        MeshFilter mf = GetComponent<MeshFilter>();
        Mesh mesh = mf.sharedMesh;
        
        for (int i = 0; i < subdivisions; i++)
        {
            mesh = SubdivideMeshOnce(mesh);
        }
        
        mf.mesh = mesh;
        Debug.Log($"Mesh subdivided! Vertices: {mesh.vertexCount}");
    }
    
    Mesh SubdivideMeshOnce(Mesh mesh)
    {
        Vector3[] oldVerts = mesh.vertices;
        int[] oldTris = mesh.triangles;
        Vector2[] oldUVs = mesh.uv;
        
        // Each triangle becomes 4 triangles (12 indices)
        Vector3[] newVerts = new Vector3[oldTris.Length * 4];
        Vector2[] newUVs = new Vector2[oldTris.Length * 4];
        int[] newTris = new int[oldTris.Length * 4];
        
        int vertIndex = 0;
        int triIndex = 0;
        
        for (int i = 0; i < oldTris.Length; i += 3)
        {
            int i0 = oldTris[i];
            int i1 = oldTris[i + 1];
            int i2 = oldTris[i + 2];
            
            Vector3 v0 = oldVerts[i0];
            Vector3 v1 = oldVerts[i1];
            Vector3 v2 = oldVerts[i2];
            
            Vector2 uv0 = i0 < oldUVs.Length ? oldUVs[i0] : Vector2.zero;
            Vector2 uv1 = i1 < oldUVs.Length ? oldUVs[i1] : Vector2.zero;
            Vector2 uv2 = i2 < oldUVs.Length ? oldUVs[i2] : Vector2.zero;
            
            // Calculate midpoints
            Vector3 m01 = (v0 + v1) / 2f;
            Vector3 m12 = (v1 + v2) / 2f;
            Vector3 m20 = (v2 + v0) / 2f;
            
            Vector2 uvm01 = (uv0 + uv1) / 2f;
            Vector2 uvm12 = (uv1 + uv2) / 2f;
            Vector2 uvm20 = (uv2 + uv0) / 2f;
            
            // Triangle 1: v0, m01, m20
            newVerts[vertIndex] = v0;
            newVerts[vertIndex + 1] = m01;
            newVerts[vertIndex + 2] = m20;
            newUVs[vertIndex] = uv0;
            newUVs[vertIndex + 1] = uvm01;
            newUVs[vertIndex + 2] = uvm20;
            newTris[triIndex] = vertIndex;
            newTris[triIndex + 1] = vertIndex + 1;
            newTris[triIndex + 2] = vertIndex + 2;
            vertIndex += 3;
            triIndex += 3;
            
            // Triangle 2: m01, v1, m12
            newVerts[vertIndex] = m01;
            newVerts[vertIndex + 1] = v1;
            newVerts[vertIndex + 2] = m12;
            newUVs[vertIndex] = uvm01;
            newUVs[vertIndex + 1] = uv1;
            newUVs[vertIndex + 2] = uvm12;
            newTris[triIndex] = vertIndex;
            newTris[triIndex + 1] = vertIndex + 1;
            newTris[triIndex + 2] = vertIndex + 2;
            vertIndex += 3;
            triIndex += 3;
            
            // Triangle 3: m20, m12, v2
            newVerts[vertIndex] = m20;
            newVerts[vertIndex + 1] = m12;
            newVerts[vertIndex + 2] = v2;
            newUVs[vertIndex] = uvm20;
            newUVs[vertIndex + 1] = uvm12;
            newUVs[vertIndex + 2] = uv2;
            newTris[triIndex] = vertIndex;
            newTris[triIndex + 1] = vertIndex + 1;
            newTris[triIndex + 2] = vertIndex + 2;
            vertIndex += 3;
            triIndex += 3;
            
            // Triangle 4: m01, m12, m20 (center)
            newVerts[vertIndex] = m01;
            newVerts[vertIndex + 1] = m12;
            newVerts[vertIndex + 2] = m20;
            newUVs[vertIndex] = uvm01;
            newUVs[vertIndex + 1] = uvm12;
            newUVs[vertIndex + 2] = uvm20;
            newTris[triIndex] = vertIndex;
            newTris[triIndex + 1] = vertIndex + 1;
            newTris[triIndex + 2] = vertIndex + 2;
            vertIndex += 3;
            triIndex += 3;
        }
        
        Mesh newMesh = new Mesh();
        newMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32; // Support large meshes
        newMesh.vertices = newVerts;
        newMesh.triangles = newTris;
        newMesh.uv = newUVs;
        newMesh.RecalculateNormals();
        newMesh.RecalculateBounds();
        
        return newMesh;
    }
}
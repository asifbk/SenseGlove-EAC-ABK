using UnityEngine;

public class ExistingModelCarving : MonoBehaviour
{
    private Mesh mesh;
    private Vector3[] originalVertices;
    private Vector3[] modifiedVertices;
    private MeshCollider meshCollider;
    
    [Header("Carving Settings")]
    public float carveRadius = 0.3f;
    public float carveSpeed = 3f;
    public float carveSmoothness = 1.5f;
    public float minVertexHeight = -2f;
    
    [Header("Mesh Subdivision (Recommended)")]
    public bool subdivideOnStart = true;
    public int subdivisionIterations = 2;
    
    void Start()
    {
        InitializeMesh();
    }
    
    void InitializeMesh()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null)
        {
            Debug.LogError("No MeshFilter found on " + gameObject.name);
            return;
        }
        
        // Create a copy of the original mesh
        mesh = Instantiate(meshFilter.sharedMesh);
        mesh.name = "Carvable " + meshFilter.sharedMesh.name;
        meshFilter.mesh = mesh;
        
        // Subdivide for smoother carving
        if (subdivideOnStart)
        {
            Debug.Log($"Subdividing mesh {subdivisionIterations} times...");
            for (int i = 0; i < subdivisionIterations; i++)
            {
                mesh = SubdivideMesh(mesh);
            }
            meshFilter.mesh = mesh;
        }
        
        // Store vertices
        originalVertices = mesh.vertices;
        modifiedVertices = mesh.vertices;
        
        // Setup collider
        meshCollider = GetComponent<MeshCollider>();
        if (meshCollider == null)
            meshCollider = gameObject.AddComponent<MeshCollider>();
        meshCollider.sharedMesh = mesh;
        
        Debug.Log($"✓ Mesh initialized with {modifiedVertices.Length} vertices");
    }
    
    public void CarveAtPosition(Vector3 worldPosition, float drillRadius, float depth)
    {
        if (mesh == null || modifiedVertices == null) return;
        
        Vector3 localPoint = transform.InverseTransformPoint(worldPosition);
        bool meshChanged = false;
        
        for (int i = 0; i < modifiedVertices.Length; i++)
        {
            // Calculate 2D distance (XZ plane)
            float dx = modifiedVertices[i].x - localPoint.x;
            float dz = modifiedVertices[i].z - localPoint.z;
            float distance = Mathf.Sqrt(dx * dx + dz * dz);
            
            if (distance < drillRadius)
            {
                // Smooth falloff
                float influence = 1f - (distance / drillRadius);
                influence = Mathf.Pow(influence, carveSmoothness);
                
                // Target height
                float targetY = localPoint.y - 0.05f;
                
                // Only carve if vertex is above target
                if (modifiedVertices[i].y > targetY && modifiedVertices[i].y > minVertexHeight)
                {
                    float carveAmount = depth * influence * Time.deltaTime;
                    modifiedVertices[i].y = Mathf.Max(
                        modifiedVertices[i].y - carveAmount,
                        Mathf.Max(targetY, minVertexHeight)
                    );
                    meshChanged = true;
                }
            }
        }
        
        if (meshChanged)
        {
            mesh.vertices = modifiedVertices;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            
            // Update collider periodically
            if (Time.frameCount % 10 == 0 && meshCollider != null)
            {
                meshCollider.sharedMesh = null;
                meshCollider.sharedMesh = mesh;
            }
        }
    }
    
    public void ResetMesh()
    {
        if (originalVertices != null && mesh != null)
        {
            modifiedVertices = (Vector3[])originalVertices.Clone();
            mesh.vertices = modifiedVertices;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            
            if (meshCollider != null)
            {
                meshCollider.sharedMesh = null;
                meshCollider.sharedMesh = mesh;
            }
            Debug.Log("✓ Mesh reset!");
        }
    }
    
    Mesh SubdivideMesh(Mesh originalMesh)
    {
        Vector3[] oldVerts = originalMesh.vertices;
        int[] oldTris = originalMesh.triangles;
        Vector2[] oldUVs = originalMesh.uv;
        
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
            
            Vector3 m01 = (v0 + v1) / 2f;
            Vector3 m12 = (v1 + v2) / 2f;
            Vector3 m20 = (v2 + v0) / 2f;
            
            Vector2 uvm01 = (uv0 + uv1) / 2f;
            Vector2 uvm12 = (uv1 + uv2) / 2f;
            Vector2 uvm20 = (uv2 + uv0) / 2f;
            
            // Triangle 1
            newVerts[vertIndex] = v0;
            newVerts[vertIndex + 1] = m01;
            newVerts[vertIndex + 2] = m20;
            newUVs[vertIndex] = uv0;
            newUVs[vertIndex + 1] = uvm01;
            newUVs[vertIndex + 2] = uvm20;
            newTris[triIndex] = vertIndex;
            newTris[triIndex + 1] = vertIndex + 1;
            newTris[triIndex + 2] = vertIndex + 2;
            vertIndex += 3; triIndex += 3;
            
            // Triangle 2
            newVerts[vertIndex] = m01;
            newVerts[vertIndex + 1] = v1;
            newVerts[vertIndex + 2] = m12;
            newUVs[vertIndex] = uvm01;
            newUVs[vertIndex + 1] = uv1;
            newUVs[vertIndex + 2] = uvm12;
            newTris[triIndex] = vertIndex;
            newTris[triIndex + 1] = vertIndex + 1;
            newTris[triIndex + 2] = vertIndex + 2;
            vertIndex += 3; triIndex += 3;
            
            // Triangle 3
            newVerts[vertIndex] = m20;
            newVerts[vertIndex + 1] = m12;
            newVerts[vertIndex + 2] = v2;
            newUVs[vertIndex] = uvm20;
            newUVs[vertIndex + 1] = uvm12;
            newUVs[vertIndex + 2] = uv2;
            newTris[triIndex] = vertIndex;
            newTris[triIndex + 1] = vertIndex + 1;
            newTris[triIndex + 2] = vertIndex + 2;
            vertIndex += 3; triIndex += 3;
            
            // Triangle 4 (center)
            newVerts[vertIndex] = m01;
            newVerts[vertIndex + 1] = m12;
            newVerts[vertIndex + 2] = m20;
            newUVs[vertIndex] = uvm01;
            newUVs[vertIndex + 1] = uvm12;
            newUVs[vertIndex + 2] = uvm20;
            newTris[triIndex] = vertIndex;
            newTris[triIndex + 1] = vertIndex + 1;
            newTris[triIndex + 2] = vertIndex + 2;
            vertIndex += 3; triIndex += 3;
        }
        
        Mesh newMesh = new Mesh();
        newMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        newMesh.vertices = newVerts;
        newMesh.triangles = newTris;
        newMesh.uv = newUVs;
        newMesh.RecalculateNormals();
        newMesh.RecalculateBounds();
        
        return newMesh;
    }
}
using UnityEngine;

public class ExistingModelCarving : MonoBehaviour
{
    private Mesh mesh;
    private Vector3[] originalVertices;
    private Vector3[] modifiedVertices;
    private MeshCollider meshCollider;
    
    [Header("Carving Settings")]
    public float carveRadius = 0.3f;      // kept for compatibility, but now auto-calculated
    public float carveSpeed = 3f;
    public float carveSmoothness = 1.5f;  
    public float minVertexHeight = -2f;
    
    [Header("Circular Drilling Realism")]
    [Tooltip("How much the drill rotation affects carving (0 = uniform, 1 = strong circular pattern)")]
    [Range(0f, 1f)] public float circularCuttingStrength = 0.3f;
    [Tooltip("Adds realistic wood grain tearing randomness")]
    [Range(0f, 0.3f)] public float woodGrainNoise = 0.15f;
    [Tooltip("Sharpness of the drill bit edge (higher = sharper circular holes)")]
    [Range(1f, 3f)] public float edgeSharpness = 2.0f;
    
    [Header("Mesh Subdivision (Recommended)")]
    public bool subdivideOnStart = true;
    public int subdivisionIterations = 2;

    // *** NEW *** — drill bit reference (set by SG_TriggerLogic)
    public Transform drillBit;

    // *** NEW *** — cached drill collider
    private CapsuleCollider drillCollider;
    private Transform lastDrillBit;
    
    private DrillBitCompatibility drillBitCompatibility;

    void Start()
    {
        InitializeMesh();
    }

    // *** NEW *** — this will be called by SG_TriggerLogic
    public void SetDrillBit(Transform bit)
    {
        if (bit != lastDrillBit)
        {
            drillBit = bit;
            drillCollider = drillBit != null ? drillBit.GetComponent<CapsuleCollider>() : null;
            drillBitCompatibility = drillBit != null ? drillBit.GetComponent<DrillBitCompatibility>() : null;
            lastDrillBit = bit;
            
            if (drillBit != null && drillCollider != null)
            {
                float effectiveRadius = drillCollider.radius * Mathf.Max(drillBit.lossyScale.x, drillBit.lossyScale.z);
                Debug.Log($"<color=green>[{gameObject.name}] ✓ Drill bit updated: {drillBit.name}</color>\n" +
                          $"  Collider radius: {drillCollider.radius:F4}m\n" +
                          $"  Scale: X={drillBit.lossyScale.x:F4}, Y={drillBit.lossyScale.y:F4}, Z={drillBit.lossyScale.z:F4}\n" +
                          $"  Effective radius: {effectiveRadius:F6}m ({effectiveRadius * 1000f:F2}mm)\n" +
                          $"  <b>Hole diameter will be: {effectiveRadius * 2000f:F2}mm</b>");
            }
            else if (drillBit != null && drillCollider == null)
            {
                Debug.LogWarning($"[{gameObject.name}] Drill bit {drillBit.name} has NO CapsuleCollider!");
            }
        }
    }
    
    void InitializeMesh()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null)
        {
            Debug.LogError("No MeshFilter found on " + gameObject.name);
            return;
        }
        
        mesh = Instantiate(meshFilter.sharedMesh);
        mesh.name = "Carvable " + meshFilter.sharedMesh.name;
        meshFilter.mesh = mesh;
        
        if (subdivideOnStart)
        {
            Debug.Log($"Subdividing mesh {subdivisionIterations} times...");
            for (int i = 0; i < subdivisionIterations; i++)
            {
                mesh = SubdivideMesh(mesh);
            }
            meshFilter.mesh = mesh;
        }
        
        originalVertices = mesh.vertices;
        modifiedVertices = mesh.vertices;
        
        meshCollider = GetComponent<MeshCollider>();
        if (meshCollider == null)
            meshCollider = gameObject.AddComponent<MeshCollider>();
        meshCollider.sharedMesh = mesh;

        Debug.Log($"✓ Mesh initialized with {modifiedVertices.Length} vertices");
    }
    
    // -------------------------------------------------------
    // *** UPDATED *** CYLINDRICAL DRILLING WITH AUTO DEPTH & RADIUS
    // *** NEW *** CIRCULAR DRILLING PATTERN - follows rotation
    // -------------------------------------------------------
    public void CarveAtPosition(Vector3 worldPosition, float drillRadiusIgnored, float depthIgnored)
    {
        if (mesh == null || modifiedVertices == null) return;

        if (drillBit == null)
        {
            Debug.LogWarning("[ExistingModelCarving] No drill bit reference set. Call SetDrillBit() first.");
            return;
        }

        if (drillCollider == null)
        {
            Debug.LogWarning("[ExistingModelCarving] No drill collider found on drill bit.");
            return;
        }
        
        if (drillBitCompatibility != null && !drillBitCompatibility.IsCompatibleWith(gameObject))
        {
            return;
        }

        Vector3 localPoint = transform.InverseTransformPoint(worldPosition);
        bool meshChanged = false;

        // Calculate actual drill radius from collider
        float drillRadius = drillCollider.radius * Mathf.Max(drillBit.lossyScale.x, drillBit.lossyScale.z);

        // Calculate drilling depth
        float drillTipY_world = drillBit.position.y;
        float surfaceY_world = worldPosition.y;
        float depth = Mathf.Max(0.01f, Mathf.Abs(surfaceY_world - drillTipY_world));

        // *** NEW *** Calculate drill rotation angle for circular carving
        // Use drill's rotation to determine which "side" of the bit is currently cutting
        Vector3 drillForward = drillBit.forward;
        Vector3 drillRight = drillBit.right;
        float rotationAngle = Mathf.Atan2(drillRight.x, drillRight.z) * Mathf.Rad2Deg;

        // *** NEW *** Circular carving pattern parameters
        float innerRadius = drillRadius * 0.85f;    // Full depth circular core
        float middleRadius = drillRadius * 1.0f;    // Cutting edge radius (sharp)
        float outerRadius = drillRadius * 1.3f;     // Smooth transition zone

        // *** NEW *** Add slight randomness for realistic wood grain tearing
        float grainNoise = woodGrainNoise > 0 ? Mathf.PerlinNoise(Time.time * 0.5f, localPoint.x + localPoint.z) * woodGrainNoise : 0f;
        
        for (int i = 0; i < modifiedVertices.Length; i++)
        {
            float dx = modifiedVertices[i].x - localPoint.x;
            float dz = modifiedVertices[i].z - localPoint.z;
            float dist = Mathf.Sqrt(dx * dx + dz * dz);

            // *** NEW *** Calculate angle to vertex for circular pattern
            float vertexAngle = Mathf.Atan2(dz, dx) * Mathf.Rad2Deg;
            float angleDiff = Mathf.DeltaAngle(vertexAngle, rotationAngle);
            
            // *** NEW *** Circular cutting pattern - stronger carving aligned with drill edges
            // Real drill bits have flutes (cutting edges) that carve more aggressively
            float angularFactor = 1.0f + Mathf.Abs(Mathf.Cos(angleDiff * Mathf.Deg2Rad)) * circularCuttingStrength;

            if (dist < innerRadius)
            {
                // CORE: Full depth circular hole
                float targetY = localPoint.y - depth;

                if (modifiedVertices[i].y > minVertexHeight)
                {
                    modifiedVertices[i].y = Mathf.Lerp(
                        modifiedVertices[i].y,
                        targetY,
                        carveSpeed * Time.deltaTime * angularFactor
                    );
                }

                meshChanged = true;
            }
            else if (dist < middleRadius)
            {
                // CUTTING EDGE: Sharp circular transition (drill bit edge)
                float edgeBlend = (dist - innerRadius) / (middleRadius - innerRadius);
                
                // *** NEW *** Sharp edge for realistic circular cutting
                float sharpness = 1.0f - Mathf.Pow(edgeBlend, edgeSharpness); // Configurable sharpness
                float targetY = localPoint.y - depth * sharpness;

                if (modifiedVertices[i].y > minVertexHeight)
                {
                    modifiedVertices[i].y = Mathf.Lerp(
                        modifiedVertices[i].y,
                        targetY,
                        carveSpeed * Time.deltaTime * angularFactor * (1.0f + grainNoise)
                    );
                }

                meshChanged = true;
            }
            else if (dist < outerRadius)
            {
                // OUTER TRANSITION: Smooth blending zone
                float t = (dist - middleRadius) / (outerRadius - middleRadius);
                t = t * t; // Quadratic easing for smooth transition
                
                float smoothTargetY = Mathf.Lerp(localPoint.y - depth * 0.3f, modifiedVertices[i].y, t);

                modifiedVertices[i].y = Mathf.Lerp(
                    modifiedVertices[i].y,
                    smoothTargetY,
                    carveSpeed * Time.deltaTime * 0.5f // Slower carving at outer edge
                );

                meshChanged = true;
            }
        }
        
        if (meshChanged)
        {
            mesh.vertices = modifiedVertices;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            
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

    // -------------------------------------------------------
    // Subdivision (UNCHANGED)
    // -------------------------------------------------------
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
            
            Vector3 m01 = (v0 + v1) * 0.5f;
            Vector3 m12 = (v1 + v2) * 0.5f;
            Vector3 m20 = (v2 + v0) * 0.5f;
            
            Vector2 uvm01 = (uv0 + uv1) * 0.5f;
            Vector2 uvm12 = (uv1 + uv2) * 0.5f;
            Vector2 uvm20 = (uv2 + uv0) * 0.5f;
            
            // TRIANGLE 1
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
            
            // TRIANGLE 2
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
            
            // TRIANGLE 3
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
            
            // TRIANGLE 4 (center)
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

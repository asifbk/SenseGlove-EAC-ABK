using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles real-time mesh deformation and engraving when drill bit touches the table.
/// Updates mesh collider dynamically for accurate collision with deformed mesh.
/// </summary>
public class DrillEngravingSystem : MonoBehaviour
{
    [Header("Engraving Settings")]
    [Tooltip("Radius of the engraving crater in world units.")]
    public float engravingRadius = 0.5f;

    [Tooltip("How deep the engraving goes per second when drilling.")]
    public float engravingDepthPerSecond = 0.5f;

    [Tooltip("The drill bit transform (child object that touches the table).")]
    public Transform drillBit;

    [Tooltip("The SG_TriggerDrillLogic script to check if drill is spinning.")]
    public SG_TriggerDrillLogic drillLogic;

    [Tooltip("The Table object that will be engraved (the mesh to deform).")]
    public GameObject tableObject;

    [Header("Performance")]
    [Tooltip("Update mesh collider every N frames (higher = better performance, less accurate).")]
    public int colliderUpdateFrequency = 5;

    // ------------------- INTERNAL STATE -------------------

    private Mesh deformingMesh;
    private Mesh originalMesh;
    private Vector3[] meshVertices;
    private Vector3[] originalVertices;
    private MeshCollider meshCollider;
    private int frameCounter = 0;
    private bool isTouchingTable = false;

    void Start()
    {
        Debug.Log("DrillEngravingSystem Start() called!");

        // Get the mesh collider from the table
        meshCollider = tableObject.GetComponent<MeshCollider>();
        if (meshCollider == null)
        {
            Debug.LogError("DrillEngravingSystem: Table must have a MeshCollider!");
            return;
        }

        // Get the original mesh from the collider
        originalMesh = meshCollider.sharedMesh;
        if (originalMesh == null)
        {
            Debug.LogError("DrillEngravingSystem: MeshCollider has no mesh assigned!");
            return;
        }

        // Create a copy of the mesh
        deformingMesh = Instantiate(originalMesh);
        deformingMesh.name = "Deforming Mesh";

        // Assign the deforming mesh to the mesh filter
        MeshFilter meshFilter = tableObject.GetComponent<MeshFilter>();
        if (meshFilter != null)
        {
            meshFilter.mesh = deformingMesh;
        }

        // Assign the deforming mesh to the mesh collider
        meshCollider.sharedMesh = null; // Clear first
        meshCollider.sharedMesh = deformingMesh;

        // Store vertex data
        meshVertices = deformingMesh.vertices;
        originalVertices = new Vector3[meshVertices.Length];
        System.Array.Copy(meshVertices, originalVertices, meshVertices.Length);

        Debug.Log($"Mesh initialized with {meshVertices.Length} vertices");
        
        // Debug: print first few vertex positions
        for (int i = 0; i < Mathf.Min(5, meshVertices.Length); i++)
        {
            Debug.Log($"Vertex {i}: {meshVertices[i]}");
        }

        if (drillBit == null)
            Debug.LogError("DrillEngravingSystem: Drill Bit transform not assigned!");

        if (drillLogic == null)
            Debug.LogError("DrillEngravingSystem: SG_TriggerDrillLogic not assigned!");
    }

    void Update()
    {
        // Only engrave if drill bit is touching and drill is spinning
        if (!isTouchingTable || drillLogic == null || drillLogic.TriggerPressure <= 0.05f)
            return;

        Debug.Log($"Engraving! Trigger Pressure: {drillLogic.TriggerPressure}");

        // Engrave the mesh
        EngraveMesh();

        // Update mesh collider periodically
        frameCounter++;
        if (frameCounter >= colliderUpdateFrequency)
        {
            UpdateMeshCollider();
            frameCounter = 0;
        }
    }

    void EngraveMesh()
    {
        if (deformingMesh == null || drillBit == null || tableObject == null)
            return;

        Vector3 drillBitPos = drillBit.position;
        float depthThisFrame = engravingDepthPerSecond * Time.deltaTime;

        // Convert world position to local position relative to the table
        Vector3 localDrillPos = tableObject.transform.InverseTransformPoint(drillBitPos);

        int verticesAffected = 0;

        // Loop through all vertices and deform those within the engraving radius
        for (int i = 0; i < meshVertices.Length; i++)
        {
            Vector3 vertexPos = meshVertices[i];
            float distanceToDrill = Vector3.Distance(vertexPos, localDrillPos);

            // Only affect vertices within the engraving radius
            if (distanceToDrill < engravingRadius)
            {
                verticesAffected++;

                // Calculate falloff (smoother crater edges)
                float falloff = 1f - (distanceToDrill / engravingRadius);
                falloff = falloff * falloff; // Quadratic falloff for smoother results

                // Move vertex downward (along local Y axis, adjust if needed)
                meshVertices[i].y -= depthThisFrame * falloff;
            }
        }

        if (verticesAffected > 0)
        {
            Debug.Log($"Affected {verticesAffected} vertices! Drill local pos: {localDrillPos}");
        }

        // Apply the deformed vertices back to the mesh
        deformingMesh.vertices = meshVertices;
        deformingMesh.RecalculateNormals();
        deformingMesh.RecalculateBounds();
    }

    void UpdateMeshCollider()
    {
        if (meshCollider == null)
            return;

        // Disable and re-enable to force update
        meshCollider.convex = false;
        meshCollider.sharedMesh = null;
        meshCollider.sharedMesh = deformingMesh;
    }

    void OnTriggerEnter(Collider collision)
    {
        if (collision.gameObject.CompareTag("Wood"))
        {
            isTouchingTable = true;
            Debug.Log("Drill bit started touching table - engraving started!");
        }
    }

    void OnTriggerStay(Collider collision)
    {
        if (collision.gameObject.CompareTag("Wood"))
        {
            isTouchingTable = true;
        }
    }

    void OnTriggerExit(Collider collision)
    {
        if (collision.gameObject.CompareTag("Wood"))
        {
            isTouchingTable = false;
            Debug.Log("Drill bit left table - engraving stopped!");
        }
    }

    // Optional: Reset the table mesh to original state
    public void ResetMesh()
    {
        if (meshVertices == null || originalVertices == null)
            return;

        System.Array.Copy(originalVertices, meshVertices, meshVertices.Length);
        deformingMesh.vertices = meshVertices;
        deformingMesh.RecalculateNormals();
        deformingMesh.RecalculateBounds();
        UpdateMeshCollider();

        Debug.Log("Table mesh reset to original state!");
    }
}
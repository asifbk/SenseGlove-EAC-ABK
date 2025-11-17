using System.Collections.Generic;
using UnityEngine;

public class DrillHoleCreator : MonoBehaviour
{
    [Header("Hole Prefab Settings")]
    public GameObject holeDecalPrefab;

    [Header("Surface Settings")]
    public string drillBitTag = "DrillBit";
    
    [Header("Drillable Surfaces")]
    [Tooltip("Add surfaces/objects that can be drilled here")]
    public List<GameObject> drillableSurfaces = new List<GameObject>();
    
    [Tooltip("Alternatively, add tags for drillable surfaces")]
    public List<string> drillableTags = new List<string>();

    [Header("Spawn Settings")]
    [Range(0.0001f, 0.01f)] public float surfaceOffset = 0.0005f;
    [Range(0.05f, 2.0f)] public float spawnCooldown = 0.1f;

    [Header("Particle & Sound (Optional)")]
    public ParticleSystem drillParticles;

    [Header("Debug Options")]
    public bool debugLogs = true;

    private float lastSpawnTime = -1f;

    private void Start()
    {
        Debug.Log("[DrillHoleCreator] Script initialized on: " + gameObject.name);
        Debug.Log("[DrillHoleCreator] Hole Prefab assigned: " + (holeDecalPrefab != null));
        Debug.Log("[DrillHoleCreator] Drillable Surfaces count: " + drillableSurfaces.Count);
        Debug.Log("[DrillHoleCreator] Drillable Tags count: " + drillableTags.Count);
        
        // Check Rigidbody
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            Debug.Log("[DrillHoleCreator] Rigidbody found: Body Type = " + rb.constraints);
            Debug.Log("[DrillHoleCreator] Is Kinematic = " + rb.isKinematic);
        }
        else
        {
            Debug.LogWarning("[DrillHoleCreator] WARNING: No Rigidbody found on this object!");
        }
        
        // Check Collider
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Debug.Log("[DrillHoleCreator] Collider found: " + col.GetType().Name);
            Debug.Log("[DrillHoleCreator] Is Trigger = " + col.isTrigger);
        }
        else
        {
            Debug.LogWarning("[DrillHoleCreator] WARNING: No Collider found on this object!");
        }
    }

    private void Update()
    {
        // Alternative detection using raycasts (in case collision detection fails)
        DetectDrillingWithRaycast();
    }

    /// <summary>
    /// Checks if a GameObject is in the drillable surfaces list or has a drillable tag
    /// </summary>
    private bool IsDrillableSurface(GameObject surface)
    {
        // Check if surface is in the drillable surfaces list
        if (drillableSurfaces.Contains(surface))
            return true;

        // Check if surface has a drillable tag
        foreach (string tag in drillableTags)
        {
            if (surface.CompareTag(tag))
                return true;
        }

        return false;
    }

    private void OnCollisionStay(Collision collision)
    {
        Debug.Log($"[DrillHoleCreator] OnCollisionStay called with: {collision.gameObject.name}");

        // Prevent spam - check cooldown
        float timeSinceLastSpawn = Time.time - lastSpawnTime;
        if (timeSinceLastSpawn < spawnCooldown)
        {
            Debug.Log($"[DrillHoleCreator] Cooldown active. Wait: {spawnCooldown - timeSinceLastSpawn:F2}s");
            return;
        }

        // Check if the colliding surface is drillable
        if (!IsDrillableSurface(collision.gameObject))
        {
            Debug.Log($"[DrillHoleCreator] {collision.gameObject.name} is NOT drillable");
            return;
        }

        Debug.Log($"[DrillHoleCreator] {collision.gameObject.name} IS drillable!");

        // Check if holeDecalPrefab is assigned
        if (holeDecalPrefab == null)
        {
            Debug.LogError("[DrillHoleCreator] ERROR: holeDecalPrefab is NULL!");
            return;
        }

        Debug.Log($"[DrillHoleCreator] Contact points count: {collision.contactCount}");

        foreach (ContactPoint contact in collision.contacts)
        {
            Debug.Log($"[DrillHoleCreator] Contact - ThisCollider: {contact.thisCollider.gameObject.name} (tag: {contact.thisCollider.tag}), OtherCollider: {contact.otherCollider.gameObject.name} (tag: {contact.otherCollider.tag})");

            // Check BOTH colliders
            if (contact.thisCollider.CompareTag(drillBitTag) || contact.otherCollider.CompareTag(drillBitTag))
            {
                Debug.Log($"[DrillHoleCreator] ✅ Drill bit found! Creating hole...");

                // Get the surface that was hit (the one that's NOT the drill bit)
                GameObject hitSurface = contact.otherCollider.gameObject.CompareTag(drillBitTag) ? 
                    contact.thisCollider.gameObject : contact.otherCollider.gameObject;

                // Position the hole slightly above the surface
                Vector3 hitPoint = contact.point + contact.normal * surfaceOffset;
                
                // Use fixed rotation (90, 0, 0) for top surface drilling
                Quaternion rotation = Quaternion.Euler(90f, 0f, 0f);

                Debug.Log($"[DrillHoleCreator] Hit point: {hitPoint}, Normal: {contact.normal}, Surface: {hitSurface.name}, Rotation: (90, 0, 0)");

                // Parent container for cleanliness
                GameObject parent = GameObject.Find("DrilledHoles");
                if (parent == null)
                {
                    parent = new GameObject("DrilledHoles");
                    Debug.Log("[DrillHoleCreator] Created 'DrilledHoles' parent");
                }

                // Spawn decal
                GameObject hole = Instantiate(holeDecalPrefab, hitPoint, rotation, parent.transform);
                hole.name = "Hole_" + System.DateTime.Now.Ticks;

                // Optional dust
                if (drillParticles != null && !drillParticles.isPlaying)
                    drillParticles.Play();

                Debug.Log($"[DrillHoleCreator] 🕳️ Hole created at {hitPoint} on {hitSurface.name}");

                lastSpawnTime = Time.time;
                break;
            }
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (drillParticles != null && drillParticles.isPlaying)
            drillParticles.Stop();
    }

    /// <summary>
    /// Alternative detection method using raycasts (for when OnCollisionStay doesn't work)
    /// </summary>
    private void DetectDrillingWithRaycast()
    {
        Collider drillCollider = GetComponent<Collider>();
        if (drillCollider == null) return;

        // Cast a ray forward from the drill bit
        Vector3 rayStart = transform.position;
        Vector3 rayDirection = transform.forward;
        float rayDistance = 0.5f;

        if (Physics.Raycast(rayStart, rayDirection, out RaycastHit hit, rayDistance))
        {
            // Check if hit surface is drillable
            if (IsDrillableSurface(hit.collider.gameObject))
            {
                // Prevent spam
                if (Time.time - lastSpawnTime < spawnCooldown) return;

                Debug.Log($"[DrillHoleCreator] 🎯 Raycast hit drillable surface: {hit.collider.gameObject.name}");

                // Check if holeDecalPrefab is assigned
                if (holeDecalPrefab == null)
                {
                    Debug.LogError("[DrillHoleCreator] ERROR: holeDecalPrefab is NULL!");
                    return;
                }

                Vector3 hitPoint = hit.point + hit.normal * surfaceOffset;
                // Use fixed rotation (90, 0, 0) for top surface drilling
                Quaternion rotation = Quaternion.Euler(90f, 0f, 0f);

                // Parent container for cleanliness
                GameObject parent = GameObject.Find("DrilledHoles");
                if (parent == null)
                {
                    parent = new GameObject("DrilledHoles");
                }

                // Spawn decal
                GameObject hole = Instantiate(holeDecalPrefab, hitPoint, rotation, parent.transform);
                hole.name = "Hole_" + System.DateTime.Now.Ticks;

                // Optional dust
                if (drillParticles != null && !drillParticles.isPlaying)
                    drillParticles.Play();

                Debug.Log($"[DrillHoleCreator] 🕳️ Hole created via raycast at {hitPoint}");

                lastSpawnTime = Time.time;
            }
        }
    }
}

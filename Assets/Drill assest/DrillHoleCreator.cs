using UnityEngine;

public class DrillHoleCreator : MonoBehaviour
{
    [Header("Hole Prefab Settings")]
    [Tooltip("Assign the DrillHoleDecal prefab from your Project window.")]
    public GameObject holeDecalPrefab;

    [Header("Surface Settings")]
    [Tooltip("Tag of the object that can be drilled.")]
    public string woodTag = "Wood";
    [Tooltip("Tag applied to the drill bit collider.")]
    public string drillBitTag = "DrillBit";

    [Header("Spawn Settings")]
    [Tooltip("Small offset from the surface to avoid z-fighting.")]
    [Range(0.0001f, 0.01f)] public float surfaceOffset = 0.0005f;
    [Tooltip("Minimum time between hole spawns (in seconds).")]
    public float spawnCooldown = 0.08f;

    [Header("Particle & Sound")]
    [Tooltip("Optional: Assign a ParticleSystem to emit dust while drilling.")]
    public ParticleSystem drillParticles;
    [Tooltip("Optional: Assign a looping AudioSource for drill sound.")]
    public AudioSource drillAudio;

    [Header("Debug Options")]
    public bool debugLogs = true;

    private float lastSpawnTime = -1f;
    private bool isDrilling = false;

    private void Start()
    {
        if (holeDecalPrefab == null)
            Debug.LogError("[DrillHoleCreator] ❌ Hole decal prefab not assigned in Inspector!");

        if (debugLogs)
            Debug.Log("[DrillHoleCreator] ✅ Initialized and waiting for drill contact...");
    }

    private void OnCollisionStay(Collision collision)
    {
        // Cooldown check
        if (Time.time - lastSpawnTime < spawnCooldown) return;

        // Ensure surface is drillable
        if (!collision.gameObject.CompareTag(woodTag)) return;

        // Detect if the drill bit (not handle or body) is touching
        bool bitTouched = false;
        foreach (ContactPoint contact in collision.contacts)
        {
            if (contact.thisCollider.CompareTag(drillBitTag))
            {
                bitTouched = true;
                SpawnHole(contact);
                break;
            }
        }

        if (bitTouched)
        {
            lastSpawnTime = Time.time;
            SetDrillingState(true);
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag(woodTag))
            SetDrillingState(false);
    }

    private void SpawnHole(ContactPoint contact)
    {
        Vector3 hitPoint = contact.point + contact.normal * surfaceOffset;
        Quaternion rotation = Quaternion.LookRotation(contact.normal);

        // Create or reuse parent container
        GameObject parentObj = GameObject.Find("DrilledHoles");
        if (parentObj == null)
            parentObj = new GameObject("DrilledHoles");

        GameObject hole = Instantiate(holeDecalPrefab, hitPoint, rotation, parentObj.transform);
        hole.transform.Rotate(contact.normal, Random.Range(0f, 360f), Space.World);

        if (debugLogs)
            Debug.Log($"[DrillHoleCreator] 🕳️ Hole spawned on {contact.otherCollider.name} at {hitPoint}");
    }

    private void SetDrillingState(bool active)
    {
        if (active == isDrilling) return;
        isDrilling = active;

        if (drillParticles != null)
        {
            if (active && !drillParticles.isPlaying)
                drillParticles.Play();
            else if (!active && drillParticles.isPlaying)
                drillParticles.Stop();
        }

        if (drillAudio != null)
        {
            if (active && !drillAudio.isPlaying)
                drillAudio.Play();
            else if (!active && drillAudio.isPlaying)
                drillAudio.Stop();
        }
    }
}

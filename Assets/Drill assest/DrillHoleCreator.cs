using UnityEngine;

public class DrillHoleCreator : MonoBehaviour
{
    [Header("Hole Prefab Settings")]
    public GameObject holeDecalPrefab;

    [Header("Surface Settings")]
    public string woodTag = "Wood";
    public string drillBitTag = "DrillBit";

    [Header("Spawn Settings")]
    [Range(0.0001f, 0.01f)] public float surfaceOffset = 0.0005f;
    public float spawnCooldown = 0.08f;

    [Header("Particle & Sound (Optional)")]
    public ParticleSystem drillParticles;

    [Header("Debug Options")]
    public bool debugLogs = true;

    private float lastSpawnTime = -1f;

    private void OnCollisionStay(Collision collision)
    {
        // Prevent spam
        if (Time.time - lastSpawnTime < spawnCooldown) return;

        if (!collision.gameObject.CompareTag(woodTag)) return;

        foreach (ContactPoint contact in collision.contacts)
        {
            if (contact.thisCollider.CompareTag(drillBitTag))
            {
                Vector3 hitPoint = contact.point + contact.normal * surfaceOffset;
                Quaternion rotation = Quaternion.LookRotation(contact.normal);

                // Parent container for cleanliness
                GameObject parent = GameObject.Find("DrilledHoles");
                if (parent == null) parent = new GameObject("DrilledHoles");

                // Spawn decal
                GameObject hole = Instantiate(holeDecalPrefab, hitPoint, rotation, parent.transform);
                hole.transform.Rotate(Vector3.up, Random.Range(0f, 360f), Space.World);

                // Optional dust
                if (drillParticles != null && !drillParticles.isPlaying)
                    drillParticles.Play();

                if (debugLogs)
                    Debug.Log($"[DrillHoleCreator] 🕳️ Hole created at {hitPoint} on {collision.gameObject.name}");

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
}

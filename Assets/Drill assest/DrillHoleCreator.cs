using UnityEngine;

public class DrillHoleCreator : MonoBehaviour
{
    [Header("Assign the prefab from Project (blue cube)")]
    public GameObject holeDecalPrefab;

    [Header("The tag on drillable surfaces")]
    public string woodTag = "Wood";

    [Header("Lift decal off surface to avoid z-fighting")]
    [Range(0f, 0.01f)] public float surfaceOffset = 0.0005f;

    [Header("Debug logging")]
    public bool debugLogs = true;

    void Start()
    {
        if (holeDecalPrefab == null)
            Debug.LogError("[DrillHoleCreator] HoleDecalPrefab is not assigned.");
    }

    void OnCollisionEnter(Collision collision)
    {
        TrySpawnHole(collision);
    }

    // Optional: allow multiple holes while pressing/spinning (rate-limited)
    float lastSpawnTime = -1f;
    public float spawnCooldown = 0.08f; // seconds between holes
    void OnCollisionStay(Collision collision)
    {
        if (Time.time - lastSpawnTime >= spawnCooldown)
        {
            if (TrySpawnHole(collision)) lastSpawnTime = Time.time;
        }
    }

    bool TrySpawnHole(Collision collision)
    {
        // Find a parent tagged Wood if the immediate collider isn't
        Transform t = collision.collider.transform;
        Transform tagged = null;
        while (t != null)
        {
            if (t.CompareTag(woodTag)) { tagged = t; break; }
            t = t.parent;
        }
        if (tagged == null)
        {
            if (debugLogs) Debug.LogWarning(
                $"[DrillHoleCreator] Surface tag didn’t match. Expected '{woodTag}' on {collision.collider.name}");
            return false;
        }

        ContactPoint cp = collision.contacts[0];
        Vector3 pos = cp.point + cp.normal * surfaceOffset;

        // Our prefab’s Z+ should face the normal → LookRotation(normal)
        Quaternion rot = Quaternion.LookRotation(cp.normal);

        var hole = Object.Instantiate(holeDecalPrefab, pos, rot);
        hole.transform.SetParent(tagged, true);
        hole.transform.Rotate(cp.normal, Random.Range(0f, 360f), Space.World); // small random twist

        if (debugLogs) Debug.Log($"[DrillHoleCreator] Spawned hole on {tagged.name} at {pos}");
        return true;
    }
}

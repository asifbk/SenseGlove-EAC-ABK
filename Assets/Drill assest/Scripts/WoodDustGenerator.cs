using UnityEngine;

public class WoodDustGenerator : MonoBehaviour
{
    [Header("Dust Settings")]
    public int particleCount = 12;        // how many dust pieces per burst
    public float particleSize = 0.025f;   // size of each dust quad
    public float forceForward = 2.0f;     // force of dust ejection
    public float forceRandom = 0.5f;      // random spray factor
    public float lifeTime = 1.0f;         // dust disappears after this time

    public Transform drillTip;            // assign from SG_TriggerLogic in Start()

    /// <summary>
    /// Call this whenever drilling happens.
    /// Creates physical wood chips using no particle system.
    /// </summary>
    public void SpawnDust()
    {
        if (drillTip == null) return;

        for (int i = 0; i < particleCount; i++)
        {
            // create quad
            GameObject dust = GameObject.CreatePrimitive(PrimitiveType.Quad);
            dust.transform.position = drillTip.position;
            dust.transform.localRotation = Random.rotation;
            dust.transform.localScale = Vector3.one * particleSize;

            // remove collider (we don't need it)
            Destroy(dust.GetComponent<Collider>());

            // add rigidbody for physics
            Rigidbody rb = dust.AddComponent<Rigidbody>();
            rb.mass = 0.005f;
            rb.useGravity = true;

            // forward force + random spray
            Vector3 sprayDir = drillTip.forward * forceForward +
                               Random.insideUnitSphere * forceRandom;

            rb.AddForce(sprayDir, ForceMode.Impulse);

            // auto destroy
            Destroy(dust, lifeTime);
        }
    }
}

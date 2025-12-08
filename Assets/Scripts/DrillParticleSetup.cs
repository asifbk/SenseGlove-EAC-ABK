using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class DrillParticleSetup : MonoBehaviour
{
    [Header("Setup Options")]
    public bool createSmokeParticles = true;
    public bool createBurningSmellParticles = true;
    public Transform drillBitTip;

    [Header("Click the button below to create particles")]
    public bool setupButtonPlaceholder;

    [ContextMenu("Setup Particle Systems")]
    public void SetupParticleSystems()
    {
        if (drillBitTip == null)
        {
            Debug.LogError("Drill Bit Tip is not assigned!");
            return;
        }

        if (createSmokeParticles)
        {
            CreateSmokeParticleSystem();
        }

        if (createBurningSmellParticles)
        {
            CreateBurningSmellParticleSystem();
        }

        Debug.Log("Particle systems created successfully!");
    }

    void CreateSmokeParticleSystem()
    {
        GameObject smokeObj = new GameObject("SmokeParticles");
        smokeObj.transform.SetParent(drillBitTip);
        smokeObj.transform.localPosition = Vector3.zero;
        smokeObj.transform.localRotation = Quaternion.identity;

        ParticleSystem ps = smokeObj.AddComponent<ParticleSystem>();
        
        var main = ps.main;
        main.duration = 5.0f;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 3.0f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 0.8f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.15f);
        main.startColor = new Color(0.4f, 0.4f, 0.4f, 0.8f);
        main.gravityModifier = -0.2f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.playOnAwake = false;

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 20f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 15f;
        shape.radius = 0.02f;
        shape.rotation = new Vector3(-90, 0, 0);

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { 
                new GradientColorKey(new Color(0.5f, 0.5f, 0.5f), 0.0f),
                new GradientColorKey(new Color(0.3f, 0.3f, 0.3f), 1.0f)
            },
            new GradientAlphaKey[] { 
                new GradientAlphaKey(0.8f, 0.0f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        colorOverLifetime.color = gradient;

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0.0f, 0.3f);
        curve.AddKey(0.5f, 1.0f);
        curve.AddKey(1.0f, 1.5f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);

        var renderer = smokeObj.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = CreateDefaultParticleMaterial();

        Debug.Log("Smoke particles created at: " + smokeObj.transform.position);
    }

    void CreateBurningSmellParticleSystem()
    {
        GameObject burnObj = new GameObject("BurningSmellParticles");
        burnObj.transform.SetParent(drillBitTip);
        burnObj.transform.localPosition = Vector3.zero;
        burnObj.transform.localRotation = Quaternion.identity;

        ParticleSystem ps = burnObj.AddComponent<ParticleSystem>();
        
        var main = ps.main;
        main.duration = 5.0f;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(2.0f, 4.0f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.08f);
        main.startColor = new Color(0.8f, 0.6f, 0.3f, 0.6f);
        main.gravityModifier = -0.1f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.playOnAwake = false;

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 10f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.03f;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { 
                new GradientColorKey(new Color(0.9f, 0.7f, 0.4f), 0.0f),
                new GradientColorKey(new Color(0.6f, 0.5f, 0.3f), 0.5f),
                new GradientColorKey(new Color(0.4f, 0.4f, 0.4f), 1.0f)
            },
            new GradientAlphaKey[] { 
                new GradientAlphaKey(0.6f, 0.0f),
                new GradientAlphaKey(0.3f, 0.5f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        colorOverLifetime.color = gradient;

        var velocityOverLifetime = ps.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.space = ParticleSystemSimulationSpace.World;
        velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(-0.1f, 0.1f);
        velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(0.2f, 0.4f);
        velocityOverLifetime.z = new ParticleSystem.MinMaxCurve(-0.1f, 0.1f);

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0.0f, 0.5f);
        curve.AddKey(0.3f, 1.0f);
        curve.AddKey(1.0f, 0.8f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.3f;
        noise.frequency = 1.0f;
        noise.scrollSpeed = 0.5f;

        var renderer = burnObj.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = CreateDefaultParticleMaterial();

        Debug.Log("Burning smell particles created at: " + burnObj.transform.position);
    }

    Material CreateDefaultParticleMaterial()
    {
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        mat.SetFloat("_Surface", 1);
        mat.SetFloat("_Blend", 0);
        mat.SetFloat("_AlphaClip", 0);
        mat.SetFloat("_SrcBlend", 5);
        mat.SetFloat("_DstBlend", 10);
        mat.SetFloat("_ZWrite", 0);
        mat.renderQueue = 3000;
        mat.color = Color.white;
        return mat;
    }
}

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class VRMaterialEnhancer : MonoBehaviour
{
    [Header("Hand Material Settings")]
    [Tooltip("Apply realistic skin material to hand models")]
    public bool enhanceHandMaterials = true
;
    public Color skinColor = new Color(0.95f, 0.82f, 0.74f, 1f);
    [Range(0f, 1f)] public float skinSmoothness = 0.4f;
    [Range(0f, 1f)] public float skinNormalStrength = 0.3f;
    public bool enableSubsurfaceScattering = true;

    [Header("Wood Material Settings")]
    public bool enhanceWoodMaterials = true;
    public Color woodTint = new Color(0.8f, 0.6f, 0.4f, 1f);
    [Range(0f, 1f)] public float woodSmoothness = 0.2f;
    [Range(0f, 2f)] public float woodNormalStrength = 1.0f;
    [Range(0f, 1f)] public float woodAOStrength = 0.8f;

    [Header("Brass Material Settings")]
    public bool enhanceBrassMaterials = true;
    public Color brassColor = new Color(0.85f, 0.65f, 0.13f, 1f);
    [Range(0f, 1f)] public float brassMetallic = 0.9f;
    [Range(0f, 1f)] public float brassSmoothness = 0.6f;
    [Range(0f, 2f)] public float brassNormalStrength = 1.2f;

    [Header("Cast Iron/Metal Settings")]
    public bool enhanceMetalMaterials = true;
    public Color metalColor = new Color(0.3f, 0.3f, 0.35f, 1f);
    [Range(0f, 1f)] public float metalMetallic = 0.95f;
    [Range(0f, 1f)] public float metalSmoothness = 0.5f;
    [Range(0f, 2f)] public float metalNormalStrength = 1.5f;

    [Header("References")]
    public SkinnedMeshRenderer[] handRenderers;
    public Renderer[] woodRenderers;
    public Renderer[] brassRenderers;
    public Renderer[] metalRenderers;

    [Header("Texture Paths")]
    public string brassFolderPath = "Assets/Drill assest/drill_texture";
    public string woodFolderPath = "Assets/Drill assest/Materials/Wood";

    void Start()
    {
        if (Application.isPlaying)
        {
            ApplyEnhancements();
        }
    }

    public void ApplyEnhancements()
    {
        if (enhanceHandMaterials)
            EnhanceHandMaterials();
        
        if (enhanceWoodMaterials)
            EnhanceWoodMaterials();
        
        if (enhanceBrassMaterials)
            EnhanceBrassMaterials();
        
        if (enhanceMetalMaterials)
            EnhanceMetalMaterials();

        Debug.Log("<color=lime>[VRMaterialEnhancer] ✓ All materials enhanced for realistic VR!</color>");
    }

    void EnhanceHandMaterials()
    {
        if (handRenderers == null || handRenderers.Length == 0)
        {
            Debug.LogWarning("[VRMaterialEnhancer] No hand renderers assigned. Searching scene...");
            handRenderers = FindObjectsByType<SkinnedMeshRenderer>(FindObjectsSortMode.None);
        }

        foreach (var renderer in handRenderers)
        {
            if (renderer == null) continue;

            Material[] materials = renderer.materials;
            bool modified = false;

            for (int i = 0; i < materials.Length; i++)
            {
                Material mat = materials[i];
                if (mat == null) continue;

                if (mat.name.ToLower().Contains("hand") || 
                    mat.name.ToLower().Contains("skin") ||
                    mat.name.ToLower().Contains("hologram"))
                {
                    SetupRealisticSkin(mat);
                    modified = true;
                    Debug.Log($"<color=cyan>[Hand] Enhanced: {mat.name} on {renderer.gameObject.name}</color>");
                }
            }

            if (modified)
            {
                renderer.materials = materials;
            }
        }
    }

    void EnhanceWoodMaterials()
    {
        if (woodRenderers == null || woodRenderers.Length == 0)
        {
            Debug.LogWarning("[VRMaterialEnhancer] No wood renderers assigned. Searching scene...");
            GameObject[] woodObjects = GameObject.FindGameObjectsWithTag("Wood");
            woodRenderers = new Renderer[woodObjects.Length];
            for (int i = 0; i < woodObjects.Length; i++)
            {
                woodRenderers[i] = woodObjects[i].GetComponent<Renderer>();
            }
        }

        foreach (var renderer in woodRenderers)
        {
            if (renderer == null) continue;

            Material[] materials = renderer.materials;
            bool modified = false;

            for (int i = 0; i < materials.Length; i++)
            {
                Material mat = materials[i];
                if (mat == null) continue;

                SetupRealisticWood(mat);
                modified = true;
                Debug.Log($"<color=orange>[Wood] Enhanced: {mat.name} on {renderer.gameObject.name}</color>");
            }

            if (modified)
            {
                renderer.materials = materials;
            }
        }
    }

    void EnhanceBrassMaterials()
    {
        if (brassRenderers == null || brassRenderers.Length == 0)
        {
            Debug.LogWarning("[VRMaterialEnhancer] No brass renderers assigned.");
            return;
        }

        foreach (var renderer in brassRenderers)
        {
            if (renderer == null) continue;

            Material[] materials = renderer.materials;
            bool modified = false;

            for (int i = 0; i < materials.Length; i++)
            {
                Material mat = materials[i];
                if (mat == null) continue;

                SetupRealisticBrass(mat);
                modified = true;
                Debug.Log($"<color=yellow>[Brass] Enhanced: {mat.name} on {renderer.gameObject.name}</color>");
            }

            if (modified)
            {
                renderer.materials = materials;
            }
        }
    }

    void EnhanceMetalMaterials()
    {
        if (metalRenderers == null || metalRenderers.Length == 0)
        {
            Debug.LogWarning("[VRMaterialEnhancer] No metal renderers assigned.");
            return;
        }

        foreach (var renderer in metalRenderers)
        {
            if (renderer == null) continue;

            Material[] materials = renderer.materials;
            bool modified = false;

            for (int i = 0; i < materials.Length; i++)
            {
                Material mat = materials[i];
                if (mat == null) continue;

                SetupRealisticMetal(mat);
                modified = true;
                Debug.Log($"<color=gray>[Metal] Enhanced: {mat.name} on {renderer.gameObject.name}</color>");
            }

            if (modified)
            {
                renderer.materials = materials;
            }
        }
    }

    void SetupRealisticSkin(Material mat)
    {
        mat.SetColor("_BaseColor", skinColor);
        mat.SetColor("_Color", skinColor);
        
        mat.SetFloat("_Metallic", 0f);
        mat.SetFloat("_Smoothness", skinSmoothness);
        
        if (mat.HasProperty("_BumpScale"))
            mat.SetFloat("_BumpScale", skinNormalStrength);
        
        if (mat.HasProperty("_SpecularHighlights"))
            mat.SetFloat("_SpecularHighlights", 0.5f);
        
        if (mat.HasProperty("_EnvironmentReflections"))
            mat.SetFloat("_EnvironmentReflections", 0.3f);
        
        if (mat.HasProperty("_ReceiveShadows"))
            mat.SetFloat("_ReceiveShadows", 1f);

        mat.EnableKeyword("_NORMALMAP");
        mat.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
    }

    void SetupRealisticWood(Material mat)
    {
        mat.shader = Shader.Find("Universal Render Pipeline/Lit");
        
        mat.SetColor("_BaseColor", woodTint);
        mat.SetColor("_Color", woodTint);
        
        mat.SetFloat("_Metallic", 0f);
        mat.SetFloat("_Smoothness", woodSmoothness);
        
        if (mat.HasProperty("_BumpScale"))
            mat.SetFloat("_BumpScale", woodNormalStrength);
        
        if (mat.HasProperty("_OcclusionStrength"))
            mat.SetFloat("_OcclusionStrength", woodAOStrength);

#if UNITY_EDITOR
        Texture2D normalMap = AssetDatabase.LoadAssetAtPath<Texture2D>($"{woodFolderPath}/Wood02_Normal.png");
        Texture2D aoMap = AssetDatabase.LoadAssetAtPath<Texture2D>($"{woodFolderPath}/Wood02_Ambient_Occlusion.png");
        Texture2D albedoMap = AssetDatabase.LoadAssetAtPath<Texture2D>($"{woodFolderPath}/Wood02_Base_Color.png");
        Texture2D roughnessMap = AssetDatabase.LoadAssetAtPath<Texture2D>($"{woodFolderPath}/Wood02_Roughness.png");

        if (albedoMap != null)
        {
            mat.SetTexture("_BaseMap", albedoMap);
            mat.SetTexture("_MainTex", albedoMap);
        }

        if (normalMap != null)
        {
            mat.SetTexture("_BumpMap", normalMap);
            mat.EnableKeyword("_NORMALMAP");
        }

        if (aoMap != null)
        {
            mat.SetTexture("_OcclusionMap", aoMap);
        }

        if (roughnessMap != null)
        {
            mat.SetTexture("_MetallicGlossMap", roughnessMap);
        }
#endif

        mat.EnableKeyword("_NORMALMAP");
    }

    void SetupRealisticBrass(Material mat)
    {
        mat.shader = Shader.Find("Universal Render Pipeline/Lit");
        
        mat.SetColor("_BaseColor", brassColor);
        mat.SetColor("_Color", brassColor);
        
        mat.SetFloat("_Metallic", brassMetallic);
        mat.SetFloat("_Smoothness", brassSmoothness);
        
        if (mat.HasProperty("_BumpScale"))
            mat.SetFloat("_BumpScale", brassNormalStrength);
        
        if (mat.HasProperty("_SpecularHighlights"))
            mat.SetFloat("_SpecularHighlights", 1f);
        
        if (mat.HasProperty("_EnvironmentReflections"))
            mat.SetFloat("_EnvironmentReflections", 1f);

#if UNITY_EDITOR
        Texture2D normalMap = AssetDatabase.LoadAssetAtPath<Texture2D>($"{brassFolderPath}/dull-brass_normal-ogl.png");
        Texture2D albedoMap = AssetDatabase.LoadAssetAtPath<Texture2D>($"{brassFolderPath}/dull-brass_albedo.png");
        Texture2D aoMap = AssetDatabase.LoadAssetAtPath<Texture2D>($"{brassFolderPath}/dull-brass_ao.png");

        if (albedoMap != null)
        {
            mat.SetTexture("_BaseMap", albedoMap);
            mat.SetTexture("_MainTex", albedoMap);
        }

        if (normalMap != null)
        {
            mat.SetTexture("_BumpMap", normalMap);
            mat.EnableKeyword("_NORMALMAP");
        }

        if (aoMap != null)
        {
            mat.SetTexture("_OcclusionMap", aoMap);
        }
#endif

        mat.EnableKeyword("_NORMALMAP");
        mat.EnableKeyword("_METALLICSPECGLOSSMAP");
    }

    void SetupRealisticMetal(Material mat)
    {
        mat.shader = Shader.Find("Universal Render Pipeline/Lit");
        
        mat.SetColor("_BaseColor", metalColor);
        mat.SetColor("_Color", metalColor);
        
        mat.SetFloat("_Metallic", metalMetallic);
        mat.SetFloat("_Smoothness", metalSmoothness);
        
        if (mat.HasProperty("_BumpScale"))
            mat.SetFloat("_BumpScale", metalNormalStrength);
        
        if (mat.HasProperty("_SpecularHighlights"))
            mat.SetFloat("_SpecularHighlights", 1f);
        
        if (mat.HasProperty("_EnvironmentReflections"))
            mat.SetFloat("_EnvironmentReflections", 1f);

        mat.EnableKeyword("_NORMALMAP");
        mat.EnableKeyword("_METALLICSPECGLOSSMAP");
    }
}

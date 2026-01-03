using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(VRMaterialEnhancer))]
public class VRMaterialEnhancerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        VRMaterialEnhancer enhancer = (VRMaterialEnhancer)target;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("VR Material Enhancement System", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("This tool enhances materials for realistic VR appearance using PBR (Physically Based Rendering) workflows.", MessageType.Info);
        EditorGUILayout.Space(10);

        DrawDefaultInspector();

        EditorGUILayout.Space(10);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        if (GUILayout.Button("🖐️ Auto-Find Hand Renderers", GUILayout.Height(30)))
        {
            FindHandRenderers(enhancer);
        }

        if (GUILayout.Button("🌲 Auto-Find Wood Objects (Tag: 'Wood')", GUILayout.Height(30)))
        {
            FindWoodRenderers(enhancer);
        }

        EditorGUILayout.Space(5);

        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("✨ APPLY ALL ENHANCEMENTS ✨", GUILayout.Height(40)))
        {
            ApplyEnhancements(enhancer);
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space(5);

        if (GUILayout.Button("🔄 Reset to Default Settings", GUILayout.Height(25)))
        {
            ResetToDefaults(enhancer);
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(10);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Material Stats", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Hand Renderers: {(enhancer.handRenderers != null ? enhancer.handRenderers.Length : 0)}");
        EditorGUILayout.LabelField($"Wood Renderers: {(enhancer.woodRenderers != null ? enhancer.woodRenderers.Length : 0)}");
        EditorGUILayout.LabelField($"Brass Renderers: {(enhancer.brassRenderers != null ? enhancer.brassRenderers.Length : 0)}");
        EditorGUILayout.LabelField($"Metal Renderers: {(enhancer.metalRenderers != null ? enhancer.metalRenderers.Length : 0)}");
        EditorGUILayout.EndVertical();
    }

    void FindHandRenderers(VRMaterialEnhancer enhancer)
    {
        SkinnedMeshRenderer[] allRenderers = FindObjectsByType<SkinnedMeshRenderer>(FindObjectsSortMode.None);
        System.Collections.Generic.List<SkinnedMeshRenderer> handRenderers = new System.Collections.Generic.List<SkinnedMeshRenderer>();

        foreach (var renderer in allRenderers)
        {
            if (renderer.gameObject.name.ToLower().Contains("hand") ||
                renderer.transform.parent != null && renderer.transform.parent.name.ToLower().Contains("hand"))
            {
                handRenderers.Add(renderer);
            }
        }

        enhancer.handRenderers = handRenderers.ToArray();
        EditorUtility.SetDirty(enhancer);
        
        Debug.Log($"<color=cyan>[VRMaterialEnhancer] Found {handRenderers.Count} hand renderers!</color>");
    }

    void FindWoodRenderers(VRMaterialEnhancer enhancer)
    {
        GameObject[] woodObjects = GameObject.FindGameObjectsWithTag("Wood");
        System.Collections.Generic.List<Renderer> woodRenderers = new System.Collections.Generic.List<Renderer>();

        foreach (var obj in woodObjects)
        {
            Renderer renderer = obj.GetComponent<Renderer>();
            if (renderer != null)
            {
                woodRenderers.Add(renderer);
            }
        }

        enhancer.woodRenderers = woodRenderers.ToArray();
        EditorUtility.SetDirty(enhancer);
        
        Debug.Log($"<color=orange>[VRMaterialEnhancer] Found {woodRenderers.Count} wood renderers!</color>");
    }

    void ApplyEnhancements(VRMaterialEnhancer enhancer)
    {
        Undo.RecordObject(enhancer, "Apply Material Enhancements");
        
        enhancer.ApplyEnhancements();
        
        EditorUtility.SetDirty(enhancer);
        
        EditorUtility.DisplayDialog(
            "Material Enhancement Complete",
            "All materials have been enhanced for realistic VR!\n\n" +
            "Check the Console for detailed logs.",
            "OK"
        );
    }

    void ResetToDefaults(VRMaterialEnhancer enhancer)
    {
        if (EditorUtility.DisplayDialog(
            "Reset to Defaults",
            "Reset all settings to default values?",
            "Yes",
            "Cancel"))
        {
            Undo.RecordObject(enhancer, "Reset Material Settings");
            
            enhancer.skinColor = new Color(0.95f, 0.82f, 0.74f, 1f);
            enhancer.skinSmoothness = 0.4f;
            enhancer.skinNormalStrength = 0.3f;
            
            enhancer.woodTint = new Color(0.8f, 0.6f, 0.4f, 1f);
            enhancer.woodSmoothness = 0.2f;
            enhancer.woodNormalStrength = 1.0f;
            enhancer.woodAOStrength = 0.8f;
            
            enhancer.brassColor = new Color(0.85f, 0.65f, 0.13f, 1f);
            enhancer.brassMetallic = 0.9f;
            enhancer.brassSmoothness = 0.6f;
            enhancer.brassNormalStrength = 1.2f;
            
            enhancer.metalColor = new Color(0.3f, 0.3f, 0.35f, 1f);
            enhancer.metalMetallic = 0.95f;
            enhancer.metalSmoothness = 0.5f;
            enhancer.metalNormalStrength = 1.5f;
            
            EditorUtility.SetDirty(enhancer);
            
            Debug.Log("<color=green>[VRMaterialEnhancer] Reset to default settings!</color>");
        }
    }
}

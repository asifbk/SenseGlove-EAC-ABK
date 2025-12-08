using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(DrillHeatSystem))]
public class DrillHeatSystemEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        DrillHeatSystem heatSystem = (DrillHeatSystem)target;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Runtime Info", EditorStyles.boldLabel);

        if (Application.isPlaying)
        {
            float heatPercent = heatSystem.GetHeatPercentage();
            
            EditorGUI.ProgressBar(
                EditorGUILayout.GetControlRect(false, 25), 
                heatPercent, 
                $"Heat: {heatPercent * 100:F1}%"
            );

            EditorGUILayout.Space(5);

            GUI.color = heatSystem.IsBurning() ? Color.yellow : Color.gray;
            EditorGUILayout.LabelField($"Burning Smell: {(heatSystem.IsBurning() ? "ACTIVE" : "Inactive")}");
            
            GUI.color = heatSystem.IsOverheating() ? Color.red : Color.gray;
            EditorGUILayout.LabelField($"Overheating Smoke: {(heatSystem.IsOverheating() ? "ACTIVE" : "Inactive")}");
            
            GUI.color = Color.white;

            EditorGUILayout.Space(10);

            if (GUILayout.Button("Force Cool Down"))
            {
                SerializedProperty currentHeat = serializedObject.FindProperty("currentHeat");
                currentHeat.floatValue = 0f;
                serializedObject.ApplyModifiedProperties();
            }

            if (GUILayout.Button("Force Overheat"))
            {
                SerializedProperty currentHeat = serializedObject.FindProperty("currentHeat");
                currentHeat.floatValue = 100f;
                serializedObject.ApplyModifiedProperties();
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Enter Play Mode to see heat visualization", MessageType.Info);
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Quick Setup", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Auto-Find Drill Bit"))
        {
            SG_TriggerLogic triggerLogic = heatSystem.GetComponent<SG_TriggerLogic>();
            if (triggerLogic != null)
            {
                SerializedProperty drillBitTip = serializedObject.FindProperty("drillBitTip");
                
                Transform foundTip = FindDrillBitInChildren(heatSystem.transform);
                if (foundTip != null)
                {
                    drillBitTip.objectReferenceValue = foundTip;
                    serializedObject.ApplyModifiedProperties();
                    Debug.Log($"Found drill bit: {foundTip.name}");
                }
                else
                {
                    Debug.LogWarning("Could not find drill bit automatically!");
                }
            }
        }

        if (GUILayout.Button("Auto-Find Particle Systems"))
        {
            SerializedProperty smokeParticles = serializedObject.FindProperty("smokeParticles");
            SerializedProperty burningParticles = serializedObject.FindProperty("burningSmellParticles");

            Transform drillBit = heatSystem.drillBitTip;
            if (drillBit != null)
            {
                ParticleSystem[] particles = drillBit.GetComponentsInChildren<ParticleSystem>();
                foreach (ParticleSystem ps in particles)
                {
                    if (ps.name.ToLower().Contains("smoke"))
                    {
                        smokeParticles.objectReferenceValue = ps;
                        Debug.Log($"Found smoke particles: {ps.name}");
                    }
                    else if (ps.name.ToLower().Contains("burn") || ps.name.ToLower().Contains("smell"))
                    {
                        burningParticles.objectReferenceValue = ps;
                        Debug.Log($"Found burning particles: {ps.name}");
                    }
                }
                serializedObject.ApplyModifiedProperties();
            }
            else
            {
                Debug.LogWarning("Assign Drill Bit Tip first!");
            }
        }

        EditorGUILayout.Space(5);
        EditorGUILayout.HelpBox(
            "Use DrillParticleSetup component to automatically create particle systems!", 
            MessageType.Info
        );
    }

    Transform FindDrillBitInChildren(Transform parent)
    {
        foreach (Transform child in parent.GetComponentsInChildren<Transform>())
        {
            string name = child.name.ToLower();
            if (name.Contains("drillbit") || name.Contains("drill bit"))
            {
                return child;
            }
        }
        return null;
    }
}

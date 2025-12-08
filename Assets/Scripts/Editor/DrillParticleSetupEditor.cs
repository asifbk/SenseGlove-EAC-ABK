using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(DrillParticleSetup))]
public class DrillParticleSetupEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        DrillParticleSetup setup = (DrillParticleSetup)target;

        EditorGUILayout.Space(15);
        
        GUI.backgroundColor = Color.green;
        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = 14;
        buttonStyle.fontStyle = FontStyle.Bold;
        buttonStyle.fixedHeight = 40;

        if (GUILayout.Button("SETUP PARTICLE SYSTEMS", buttonStyle))
        {
            if (setup.drillBitTip == null)
            {
                EditorUtility.DisplayDialog(
                    "Missing Reference", 
                    "Please assign the Drill Bit Tip field first!\n\nDrag the 'Drillbit' GameObject from your Hierarchy to the 'Drill Bit Tip' field above.", 
                    "OK"
                );
            }
            else
            {
                setup.SetupParticleSystems();
                EditorUtility.DisplayDialog(
                    "Success!", 
                    "Particle systems created successfully!\n\nYou can now:\n1. Add DrillHeatSystem component\n2. Remove this DrillParticleSetup component", 
                    "Great!"
                );
            }
        }
        
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space(10);
        EditorGUILayout.HelpBox(
            "INSTRUCTIONS:\n" +
            "1. Assign 'Drill Bit Tip' field above (drag Drillbit GameObject here)\n" +
            "2. Click the green 'SETUP PARTICLE SYSTEMS' button\n" +
            "3. Add DrillHeatSystem component to DummyDrill-Rigged\n" +
            "4. Remove this DrillParticleSetup component", 
            MessageType.Info
        );
    }
}

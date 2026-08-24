using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GravityManager))]
public sealed class GravityManagerEditor : Editor
{
    private SerializedProperty initialZoneProperty;
    private SerializedProperty manualTestZoneProperty;

    private void OnEnable()
    {
        initialZoneProperty = serializedObject.FindProperty("initialZone");
        manualTestZoneProperty = serializedObject.FindProperty("manualTestZone");
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GravityManager manager = (GravityManager)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Play Mode Zone Test", EditorStyles.boldLabel);

        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.ObjectField(
                "Current Zone",
                manager.CurrentZone,
                typeof(GravityZone),
                true);
        }

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Zone test buttons are available in Play Mode.", MessageType.Info);
        }

        DrawZoneButton(manager, initialZoneProperty, "Apply Initial Zone");
        DrawZoneButton(manager, manualTestZoneProperty, "Apply Manual Test Zone");
    }

    private static void DrawZoneButton(
        GravityManager manager,
        SerializedProperty zoneProperty,
        string fallbackLabel)
    {
        GravityZone zone = zoneProperty.objectReferenceValue as GravityZone;
        string label = zone != null ? $"Apply {zone.name}" : fallbackLabel;

        using (new EditorGUI.DisabledScope(!Application.isPlaying || zone == null))
        {
            if (GUILayout.Button(label, GUILayout.Height(28f)))
            {
                manager.ActivateZone(zone);
            }
        }
    }
}

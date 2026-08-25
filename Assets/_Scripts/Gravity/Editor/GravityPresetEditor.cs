using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GravityPreset))]
public sealed class GravityPresetEditor : Editor
{
    private SerializedProperty modeProperty;
    private SerializedProperty directionProperty;
    private SerializedProperty strengthProperty;
    private SerializedProperty periodicDirectionsProperty;
    private SerializedProperty changeIntervalProperty;
    private SerializedProperty warningDurationProperty;

    private void OnEnable()
    {
        modeProperty = serializedObject.FindProperty("mode");
        directionProperty = serializedObject.FindProperty("direction");
        strengthProperty = serializedObject.FindProperty("strength");
        periodicDirectionsProperty = serializedObject.FindProperty("periodicDirections");
        changeIntervalProperty = serializedObject.FindProperty("changeInterval");
        warningDurationProperty = serializedObject.FindProperty("warningDuration");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(modeProperty);
        GravityPresetMode mode = (GravityPresetMode)modeProperty.enumValueIndex;

        switch (mode)
        {
            case GravityPresetMode.Fixed:
                DrawFixedFields();
                break;
            case GravityPresetMode.Periodic:
                DrawPeriodicFields();
                break;
            case GravityPresetMode.ZeroGravity:
                EditorGUILayout.HelpBox(
                    "Zero Gravity keeps the current direction and Presentation Up, and applies Strength 0.",
                    MessageType.Info);
                break;
        }

        serializedObject.ApplyModifiedProperties();

        GravityPreset preset = (GravityPreset)target;
        if (!preset.TryValidate(out string error))
        {
            EditorGUILayout.HelpBox(error, MessageType.Error);
        }
    }

    private void DrawFixedFields()
    {
        EditorGUILayout.PropertyField(directionProperty);
        EditorGUILayout.PropertyField(strengthProperty);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("World Axis Presets", EditorStyles.boldLabel);

        DrawPresetRow("World +X", Vector3.right, "World -X", Vector3.left);
        DrawPresetRow("World +Y", Vector3.up, "World -Y", Vector3.down);
        DrawPresetRow("World +Z", Vector3.forward, "World -Z", Vector3.back);
    }

    private void DrawPeriodicFields()
    {
        EditorGUILayout.PropertyField(strengthProperty);
        EditorGUILayout.PropertyField(periodicDirectionsProperty, true);
        EditorGUILayout.PropertyField(changeIntervalProperty);
        EditorGUILayout.PropertyField(warningDurationProperty);
    }

    private void DrawPresetRow(
        string leftLabel,
        Vector3 leftDirection,
        string rightLabel,
        Vector3 rightDirection)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button(leftLabel))
            {
                directionProperty.vector3Value = leftDirection;
            }

            if (GUILayout.Button(rightLabel))
            {
                directionProperty.vector3Value = rightDirection;
            }
        }
    }
}

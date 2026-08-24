using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GravityPreset))]
public sealed class GravityPresetEditor : Editor
{
    private SerializedProperty directionProperty;
    private SerializedProperty strengthProperty;

    private void OnEnable()
    {
        directionProperty = serializedObject.FindProperty("direction");
        strengthProperty = serializedObject.FindProperty("strength");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(directionProperty);
        EditorGUILayout.PropertyField(strengthProperty);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("World Axis Presets", EditorStyles.boldLabel);

        DrawPresetRow("World +X", Vector3.right, "World -X", Vector3.left);
        DrawPresetRow("World +Y", Vector3.up, "World -Y", Vector3.down);
        DrawPresetRow("World +Z", Vector3.forward, "World -Z", Vector3.back);

        DrawValidationMessages();
        serializedObject.ApplyModifiedProperties();
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

    private void DrawValidationMessages()
    {
        Vector3 direction = directionProperty.vector3Value;
        if (!IsFinite(direction) || direction.sqrMagnitude < Mathf.Epsilon)
        {
            EditorGUILayout.HelpBox(
                "Direction must be a finite, non-zero vector.",
                MessageType.Error);
        }

        float strength = strengthProperty.floatValue;
        if (float.IsNaN(strength) || float.IsInfinity(strength) || strength < 0f)
        {
            EditorGUILayout.HelpBox(
                "Strength must be a finite value greater than or equal to zero.",
                MessageType.Error);
        }
    }

    private static bool IsFinite(Vector3 value)
    {
        return !float.IsNaN(value.x)
            && !float.IsInfinity(value.x)
            && !float.IsNaN(value.y)
            && !float.IsInfinity(value.y)
            && !float.IsNaN(value.z)
            && !float.IsInfinity(value.z);
    }
}

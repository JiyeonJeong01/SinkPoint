using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GravityManager))]
public sealed class GravityManagerEditor : Editor
{
    private readonly GravityPresetSceneSelector presetSelector = new GravityPresetSceneSelector();

    private void OnEnable()
    {
        GravityManager manager = (GravityManager)target;
        presetSelector.Refresh(manager, manager.CurrentPreset != null ? manager.CurrentPreset : manager.InitialPreset);
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GravityManager manager = (GravityManager)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Gravity Preset Select", EditorStyles.boldLabel);

        presetSelector.Refresh(manager, manager.CurrentPreset != null ? manager.CurrentPreset : manager.InitialPreset);
        presetSelector.DrawPopup(manager);

        GravityPreset selectedPreset = presetSelector.SelectedPreset;
        using (new EditorGUI.DisabledScope(!Application.isPlaying || selectedPreset == null))
        {
            if (GUILayout.Button("Apply Selected Preset", GUILayout.Height(28f)))
            {
                manager.ApplyPreset(selectedPreset);
            }
        }

        using (new EditorGUI.DisabledScope(!Application.isPlaying || manager.InitialPreset == null))
        {
            if (GUILayout.Button("Restore Initial Preset", GUILayout.Height(28f)))
            {
                manager.RestoreInitialPreset();
            }
        }

        using (new EditorGUI.DisabledScope(!Application.isPlaying || manager.CurrentPreset == null))
        {
            if (GUILayout.Button("Restart Current Preset", GUILayout.Height(28f)))
            {
                manager.RestoreCurrentPresetImmediately();
            }
        }

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Preset test buttons are available in Play Mode.", MessageType.Info);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Play Mode Preset Info", EditorStyles.boldLabel);

        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.ObjectField(
                "Current Preset",
                manager.CurrentPreset,
                typeof(GravityPreset),
                true);

            EditorGUILayout.EnumPopup(
                "Current Mode",
                manager.CurrentPreset != null
                    ? manager.CurrentPreset.Mode
                    : GravityPresetMode.Fixed);

            EditorGUILayout.Vector3Field("Direction", manager.Direction);
            EditorGUILayout.FloatField("Strength", manager.Strength);
            EditorGUILayout.Toggle("Is Transitioning", manager.IsTransitioning);
            EditorGUILayout.ObjectField(
                "Target Preset",
                manager.TargetPreset,
                typeof(GravityPreset),
                true);
            EditorGUILayout.Slider("Progress", manager.TransitionProgress, 0f, 1f);
            EditorGUILayout.Vector3Field("Presentation Up", manager.PresentationUp);
            EditorGUILayout.Toggle("Periodic Running", manager.IsPeriodicRunning);
            EditorGUILayout.Toggle("Warning Active", manager.IsWarningActive);
            EditorGUILayout.Vector3Field("Next Direction", manager.NextPeriodicDirection);
            EditorGUILayout.FloatField(
                "Seconds Until Change",
                manager.SecondsUntilNextGravityChange);
        }

        if (Application.isPlaying)
        {
            Repaint();
        }
    }

}

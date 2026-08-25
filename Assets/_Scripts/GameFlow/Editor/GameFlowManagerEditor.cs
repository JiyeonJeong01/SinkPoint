using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GameFlowManager))]
public sealed class GameFlowManagerEditor : OdinEditor
{
    private readonly GravityPresetSceneSelector presetSelector = new GravityPresetSceneSelector();

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        GameFlowManager manager = (GameFlowManager)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Gravity Preset Select", EditorStyles.boldLabel);

        GravityPreset fallback = manager.CurrentGravityPreset != null
            ? manager.CurrentGravityPreset
            : manager.GravityManager != null
                ? manager.GravityManager.InitialPreset
                : null;
        presetSelector.Refresh(manager, fallback);
        presetSelector.DrawPopup(manager);

        GravityPreset selectedPreset = presetSelector.SelectedPreset;
        using (new EditorGUI.DisabledScope(
                   !Application.isPlaying
                   || manager.GravityManager == null
                   || selectedPreset == null))
        {
            if (GUILayout.Button("Apply Selected Preset", GUILayout.Height(28f)))
            {
                manager.DebugApplyGravityPreset(selectedPreset);
            }
        }

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Preset test buttons are available in Play Mode.", MessageType.Info);
        }
        else if (manager.GravityManager == null)
        {
            EditorGUILayout.HelpBox("GravityManager is not assigned.", MessageType.Warning);
        }

        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.ObjectField(
                "Current Preset",
                manager.CurrentGravityPreset,
                typeof(GravityPreset),
                true);
        }

        if (Application.isPlaying)
        {
            Repaint();
        }
    }
}

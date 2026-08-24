using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GravityManager))]
public sealed class GravityManagerEditor : Editor
{
    private const string SessionKeyPrefix = "SinkPoint.GravityManagerEditor.SelectedPreset.";

    private readonly List<GravityPreset> scenePresets = new List<GravityPreset>();
    private string[] presetLabels = Array.Empty<string>();
    private int selectedPresetIndex;

    private void OnEnable()
    {
        RefreshScenePresets();
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GravityManager manager = (GravityManager)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Gravity Preset Select", EditorStyles.boldLabel);

        RefreshScenePresets();
        DrawPresetSelector(manager);

        GravityPreset selectedPreset = GetSelectedPreset();
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

    private void DrawPresetSelector(GravityManager manager)
    {
        if (scenePresets.Count == 0)
        {
            EditorGUILayout.HelpBox("No GravityPreset exists in the manager's scene.", MessageType.Warning);
            return;
        }

        int nextIndex = EditorGUILayout.Popup("Selected Preset", selectedPresetIndex, presetLabels);
        if (nextIndex == selectedPresetIndex)
        {
            return;
        }

        selectedPresetIndex = nextIndex;
        SaveSelection(manager, GetSelectedPreset());
    }

    private void RefreshScenePresets()
    {
        GravityManager manager = target as GravityManager;
        if (manager == null || manager.gameObject == null)
        {
            return;
        }

        GravityPreset previousSelection = GetSelectedPreset();
        scenePresets.Clear();
        scenePresets.AddRange(
            Resources.FindObjectsOfTypeAll<GravityPreset>()
                .Where(preset => preset != null
                    && !EditorUtility.IsPersistent(preset)
                    && preset.gameObject.scene == manager.gameObject.scene)
                .OrderBy(preset => GetHierarchyPath(preset.transform), StringComparer.Ordinal));

        presetLabels = scenePresets
            .Select(preset => GetHierarchyPath(preset.transform))
            .ToArray();

        GravityPreset savedSelection = LoadSelection(manager);
        GravityPreset desiredSelection = previousSelection != null
            ? previousSelection
            : savedSelection != null
                ? savedSelection
                : manager.CurrentPreset != null
                    ? manager.CurrentPreset
                    : manager.InitialPreset;

        selectedPresetIndex = Mathf.Max(0, scenePresets.IndexOf(desiredSelection));
    }

    private GravityPreset GetSelectedPreset()
    {
        return selectedPresetIndex >= 0 && selectedPresetIndex < scenePresets.Count
            ? scenePresets[selectedPresetIndex]
            : null;
    }

    private static string GetHierarchyPath(Transform transform)
    {
        string path = transform.name;
        Transform current = transform.parent;
        while (current != null)
        {
            path = $"{current.name}/{path}";
            current = current.parent;
        }

        return path;
    }

    private static void SaveSelection(GravityManager manager, GravityPreset preset)
    {
        string value = preset == null
            ? string.Empty
            : GlobalObjectId.GetGlobalObjectIdSlow(preset).ToString();
        SessionState.SetString(GetSessionKey(manager), value);
    }

    private static GravityPreset LoadSelection(GravityManager manager)
    {
        string value = SessionState.GetString(GetSessionKey(manager), string.Empty);
        if (string.IsNullOrEmpty(value) || !GlobalObjectId.TryParse(value, out GlobalObjectId globalId))
        {
            return null;
        }

        return GlobalObjectId.GlobalObjectIdentifierToObjectSlow(globalId) as GravityPreset;
    }

    private static string GetSessionKey(GravityManager manager)
    {
        GlobalObjectId globalId = GlobalObjectId.GetGlobalObjectIdSlow(manager);
        return SessionKeyPrefix + globalId;
    }
}

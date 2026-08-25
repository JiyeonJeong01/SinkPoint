using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public sealed class GravityPresetSceneSelector
{
    private const string SessionKeyPrefix = "SinkPoint.GravityPresetSceneSelector.Selected.";

    private readonly List<GravityPreset> scenePresets = new List<GravityPreset>();
    private string[] presetLabels = Array.Empty<string>();
    private int selectedPresetIndex;

    public GravityPreset SelectedPreset => selectedPresetIndex >= 0
        && selectedPresetIndex < scenePresets.Count
            ? scenePresets[selectedPresetIndex]
            : null;

    public void Refresh(Component owner, GravityPreset fallback)
    {
        if (owner == null || owner.gameObject == null)
        {
            return;
        }

        GravityPreset previousSelection = SelectedPreset;
        scenePresets.Clear();
        scenePresets.AddRange(
            Resources.FindObjectsOfTypeAll<GravityPreset>()
                .Where(preset => preset != null
                    && !EditorUtility.IsPersistent(preset)
                    && preset.gameObject.scene == owner.gameObject.scene)
                .OrderBy(preset => GetHierarchyPath(preset.transform), StringComparer.Ordinal));

        presetLabels = scenePresets
            .Select(preset => GetHierarchyPath(preset.transform))
            .ToArray();

        GravityPreset savedSelection = LoadSelection(owner);
        GravityPreset desiredSelection = previousSelection != null
            ? previousSelection
            : savedSelection != null
                ? savedSelection
                : fallback;

        selectedPresetIndex = Mathf.Max(0, scenePresets.IndexOf(desiredSelection));
    }

    public void DrawPopup(Component owner)
    {
        if (scenePresets.Count == 0)
        {
            EditorGUILayout.HelpBox("No GravityPreset exists in this scene.", MessageType.Warning);
            return;
        }

        int nextIndex = EditorGUILayout.Popup("Selected Preset", selectedPresetIndex, presetLabels);
        if (nextIndex == selectedPresetIndex)
        {
            return;
        }

        selectedPresetIndex = nextIndex;
        SaveSelection(owner, SelectedPreset);
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

    private static void SaveSelection(Component owner, GravityPreset preset)
    {
        string value = preset == null
            ? string.Empty
            : GlobalObjectId.GetGlobalObjectIdSlow(preset).ToString();
        SessionState.SetString(GetSessionKey(owner), value);
    }

    private static GravityPreset LoadSelection(Component owner)
    {
        string value = SessionState.GetString(GetSessionKey(owner), string.Empty);
        if (string.IsNullOrEmpty(value) || !GlobalObjectId.TryParse(value, out GlobalObjectId globalId))
        {
            return null;
        }

        return GlobalObjectId.GlobalObjectIdentifierToObjectSlow(globalId) as GravityPreset;
    }

    private static string GetSessionKey(Component owner)
    {
        GlobalObjectId globalId = GlobalObjectId.GetGlobalObjectIdSlow(owner);
        return SessionKeyPrefix + globalId;
    }
}

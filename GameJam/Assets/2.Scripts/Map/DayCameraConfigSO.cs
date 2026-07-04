using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Camera setup that starts applying on a given day (days count from 1).
/// </summary>
[System.Serializable]
public class DayCameraSettings
{
    [Min(1)] public int day = 1;

    [Tooltip("Camera keeps the map character centered.")]
    public bool followCharacter = true;

    [Tooltip("Player can pan by dragging. Off = locked camera, only script-driven panning (tutorial focus etc).")]
    public bool dragEnabled = false;

    [Tooltip("Orthographic size the camera is locked to on this day.")]
    public float cameraSize = 10f;

    [Tooltip("Name of a CameraBoundsBox object in the scene; the view is clamped inside its BoxCollider2D. Empty = only the map sprite bounds apply. Use the Create/Sync button below to generate the boxes.")]
    public string boundingBoxName;
}

/// <summary>
/// Sparse per-day camera configs for the overview map. A day without an explicit
/// entry falls back to the closest earlier day: entries for 1, 2 and 6 mean days
/// 3–5 reuse the day-2 entry. Bounding boxes live in the scene as BoxCollider2D
/// objects (created via the inspector button) and are resolved by name at runtime.
/// </summary>
[CreateAssetMenu(menuName = "Configs/Day Camera Config")]
public class DayCameraConfigSO : ScriptableObject
{
    [SerializeField] private List<DayCameraSettings> configs = new List<DayCameraSettings>();

    public IReadOnlyList<DayCameraSettings> Configs => configs;

    /// <summary>Entry with the highest day that is ≤ the requested day; null when the list is empty.</summary>
    public DayCameraSettings GetConfigForDay(int day)
    {
        DayCameraSettings best = null;
        DayCameraSettings earliest = null;

        foreach (var c in configs)
        {
            if (c == null) continue;
            if (earliest == null || c.day < earliest.day) earliest = c;
            if (c.day <= day && (best == null || c.day > best.day)) best = c;
        }

        if (best != null) return best;

        if (earliest != null)
        {
            Debug.LogWarning($"[DayCameraConfigSO] No entry at or before day {day} — using the day {earliest.day} entry.");
            return earliest;
        }

        Debug.LogWarning("[DayCameraConfigSO] Config list is empty.");
        return null;
    }
}

using UnityEngine;

[System.Serializable]
public class DayCameraConfig
{
    public int dayNumber = 1;

    [Header("Camera")]
    public float cameraSize = 5f;

    [Header("Follow")]
    public bool followCharacter = true;

    [Header("Drag")]
    public bool dragEnabled = false;
    public bool allowZoom = false;
    [Tooltip("Absolute world-space bounds. (0,0) = no limit.")]
    public Vector2 dragBoundsMin = Vector2.zero;
    public Vector2 dragBoundsMax = Vector2.zero;
}

[CreateAssetMenu(menuName = "Configs/Map Day Camera Config")]
public class MapDayCameraConfigSO : ScriptableObject
{
    [SerializeField] private DayCameraConfig[] configs;

    public DayCameraConfig GetConfigForDay(int day)
    {
        if (configs == null || configs.Length == 0)
        {
            Debug.LogWarning("[MapDayCameraConfigSO] No configs defined — returning default.");
            return new DayCameraConfig();
        }

        foreach (var c in configs)
            if (c != null && c.dayNumber == day)
                return c;

        Debug.Log($"[MapDayCameraConfigSO] No config found for day {day} — falling back to first entry.");
        return configs[0];
    }
}

using UnityEngine;

/// <summary>
/// Global switch that freezes map interaction (node clicks, camera pan/zoom) while a
/// scripted moment plays — e.g. a construction node's build animation.
/// </summary>
public static class MapInputLock
{
    public static bool IsLocked { get; private set; }

    public static void Lock() => IsLocked = true;
    public static void Unlock() => IsLocked = false;

    // Statics survive play sessions when Enter Play Mode's domain reload is off.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetForNewSession() => IsLocked = false;
}

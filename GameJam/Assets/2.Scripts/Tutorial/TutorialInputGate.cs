using System;
using UnityEngine;

/// <summary>
/// Small global gate used while the Day 1 tutorial is guiding a single target.
/// It does not use MapInputLock because the highlighted map node still needs to
/// receive clicks.
/// </summary>
public static class TutorialInputGate
{
    public const string JournalButtonTargetId = "JournalButton";
    public const string NextDayButtonTargetId = "NextDayButton";

    public static bool IsActive { get; private set; }
    public static string AllowedTargetId { get; private set; }

    public static event Action OnStateChanged;

    public static void Begin(string allowedTargetId = null)
    {
        IsActive = true;
        AllowedTargetId = allowedTargetId;
        OnStateChanged?.Invoke();
    }

    public static void SetAllowedTarget(string targetId)
    {
        if (!IsActive)
        {
            Begin(targetId);
            return;
        }

        AllowedTargetId = targetId;
        OnStateChanged?.Invoke();
    }

    public static void BlockAll()
    {
        SetAllowedTarget(null);
    }

    public static void Clear()
    {
        IsActive = false;
        AllowedTargetId = null;
        OnStateChanged?.Invoke();
    }

    public static bool Allows(string targetId)
    {
        if (!IsActive) return true;
        return !string.IsNullOrEmpty(targetId) && targetId == AllowedTargetId;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetForNewSession()
    {
        IsActive = false;
        AllowedTargetId = null;
        OnStateChanged = null;
    }
}

using UnityEngine;

/// <summary>
/// Dev cheats for skipping ahead. Kept as its own component so it can be disabled or
/// removed for release without touching anything else.
/// Right Arrow — instantly fulfill the current day's requirements, making the
/// "Advance to Next Day" button appear (does not advance the day itself).
/// </summary>
public class MapDebugCheats : MonoBehaviour
{
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            var dm = DayManager.Instance;
            if (dm == null) return;

            Debug.Log($"[Cheat] Fulfilling Day {dm.CurrentDay} requirements.");
            dm.CheatCompleteCurrentObjective();
        }
    }
}

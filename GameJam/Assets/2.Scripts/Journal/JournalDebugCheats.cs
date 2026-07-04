using UnityEngine;

/// <summary>
/// Dev cheats for exercising the Journal without quest/dialogue systems in place.
/// Own component (like MapDebugCheats) so it can be removed for release untouched.
/// J — unlock the next locked journal entry (in day order).
/// K — discover the next hidden clue of an unlocked entry.
/// </summary>
public class JournalDebugCheats : MonoBehaviour
{
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.J))
        {
            var id = JournalManager.Instance?.CheatUnlockNextEntry();
            Debug.Log(id != null
                ? $"[Cheat] Unlocked journal entry '{id}'."
                : "[Cheat] No locked journal entries left.");
        }

        if (Input.GetKeyDown(KeyCode.K))
        {
            var id = JournalManager.Instance?.CheatDiscoverNextClue();
            Debug.Log(id != null
                ? $"[Cheat] Discovered clue '{id}'."
                : "[Cheat] No hidden clues left on unlocked entries.");
        }
    }
}

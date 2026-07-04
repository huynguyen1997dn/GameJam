using System.Collections;
using UnityEngine;

/// <summary>
/// Ensures the Day 1 tutorial exists in scenes that instantiate the map managers
/// outside the edited Managers prefab.
/// </summary>
public class Day1TutorialBootstrap : MonoBehaviour
{
    private static Day1TutorialBootstrap _instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (_instance != null) return;

        var bootstrapObject = new GameObject("Day1TutorialBootstrap");
        DontDestroyOnLoad(bootstrapObject);
        _instance = bootstrapObject.AddComponent<Day1TutorialBootstrap>();
    }

    private IEnumerator Start()
    {
        while (true)
        {
            TryCreateTutorialController();
            yield return null;
        }
    }

    private void TryCreateTutorialController()
    {
        if (FindObjectOfType<Day1TutorialController>() != null) return;
        if (FindObjectOfType<DayManager>() == null || FindObjectOfType<GameStateManager>() == null) return;

        var controllerObject = new GameObject("Day1TutorialController");
        controllerObject.AddComponent<Day1TutorialController>();
    }
}

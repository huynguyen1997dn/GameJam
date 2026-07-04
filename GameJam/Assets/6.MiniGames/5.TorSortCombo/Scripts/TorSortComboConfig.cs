using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/TorSortCombo Config", fileName = "TorSortComboConfig")]
public class TorSortComboConfig : ScriptableObject
{
    [TitleGroup("Phases")]
    [Tooltip("TorPainting manager prefab, played as phase 1")]
    public GameObject torPaintingPrefab;
    [Tooltip("SortHome manager prefab, played as phase 2")]
    public GameObject sortHomePrefab;
    [Tooltip("Delay in seconds between finishing phase 1 and starting phase 2")]
    public float phaseTransitionDelay = 1f;
}

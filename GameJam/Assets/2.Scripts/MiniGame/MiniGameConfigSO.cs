using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "MiniGameConfig", menuName = "Configs/Mini Game Config")]
public class MiniGameConfigSO : ScriptableObject
{
    [System.Serializable]
    public class MiniGameEntry
    {
        public MiniGameType type;
        public GameObject prefab;
        public string displayName;
        public Sprite icon;
    }

    [SerializeField] private List<MiniGameEntry> _entries = new();
    public IReadOnlyList<MiniGameEntry> Entries => _entries;
}

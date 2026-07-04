using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

public class NPCManager : Singleton<NPCManager>
{
    [SerializeField] private List<NPCSpawnEntry> spawnEntries;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private float spawnRadius = 3f;

    private readonly List<NPCController> _allNPCs = new();
    [OdinSerialize]
    [ShowInInspector]
    [DictionaryDrawerSettings(
        KeyLabel = "NPC Type",
        ValueLabel = "NPC Controllers"
    )]
    private Dictionary<NPCType, List<NPCController>> _npcsByType = new();

    public IReadOnlyList<NPCController> AllNPCs => _allNPCs;

    protected override void Awake()
    {
        base.Awake();
        foreach (NPCType type in System.Enum.GetValues(typeof(NPCType)))
            _npcsByType[type] = new List<NPCController>();
    }

    private void Start()
    {
        SpawnAll();
    }

    private void SpawnAll()
    {
        if (spawnPoint == null)
        {
            Debug.LogError("[NPCManager] spawnPoint is not assigned.");
            return;
        }

        foreach (var entry in spawnEntries)
        {
            if (entry.npcPrefab == null) continue;

            for (int i = 0; i < entry.count; i++)
            {
                Vector3 randomOffset = Random.insideUnitSphere * spawnRadius;
                randomOffset.y = 0;
                Vector3 position = spawnPoint.position + randomOffset;

                GameObject go = Instantiate(entry.npcPrefab, position, Quaternion.identity, transform);
                go.name = $"{entry.npcType}_{i + 1}";

                NPCController npc = go.GetComponent<NPCController>();
                if (npc == null)
                {
                    Debug.LogWarning($"[NPCManager] Prefab {entry.npcPrefab.name} missing NPCController component.");
                    continue;
                }

                _allNPCs.Add(npc);
                _npcsByType[entry.npcType].Add(npc);
                
            }
        }

        // All NPCs start wandering by default
        foreach (var npc in _allNPCs)
            npc.StartWandering();
    }

    public List<NPCController> GetNPCsOfType(NPCType type)
    {
        return _npcsByType.TryGetValue(type, out var list) ? list : new List<NPCController>();
    }

    public void Unlock(NPCController npc, string skinName)
    {
        npc.Unlock(skinName);
    }

    public void UnlockAllOfType(NPCType type, string skinName)
    {
        foreach (var npc in GetNPCsOfType(type))
            npc.Unlock(skinName);
    }

    public void UnlockAll(string skinName)
    {
        foreach (var npc in _allNPCs)
            npc.Unlock(skinName);
    }
}

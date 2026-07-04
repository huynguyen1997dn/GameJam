using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum NPCActionType
{
    Idle,
    Gather,
    Sit,
}

[System.Serializable]
public class NPCActionEntry
{
    public NPCType npcType;
    public int npcCount = 1;
    public string targetNodeId;
    public Vector3 offsetFromNode;
    public NPCActionType actionType;
    public float duration = -1f;
}

[System.Serializable]
public class DayBehaviorConfig
{
    public int day;
    public List<NPCActionEntry> actions;
}

public class NPCBehaviorManager : Singleton<NPCBehaviorManager>
{
    [SerializeField] private List<DayBehaviorConfig> dayBehaviors;

    private Dictionary<string, Vector3> _nodePositions;
    private readonly Dictionary<NPCController, Coroutine> _activeBehaviors = new();
    private bool _hasInitialized;
    private bool _namesAssigned;
    [Header("NPC Behaviors")]
    [SerializeField] private GameObject _goBrigrd;


    protected override void Awake()
    {
        base.Awake();
        CacheNodePositions();
    }

    private void OnEnable()
    {
        var dm = DayManager.Instance;
        if (dm != null)
            dm.OnDayChanged += HandleDayChanged;
    }

    private IEnumerator Start()
    {
        yield return null; // wait one frame for NPCManager.Start() to spawn NPCs

        if (!_hasInitialized)
        {
            _hasInitialized = true;
            var dm = DayManager.Instance;
            if (dm != null)
                ApplyBehaviorForDay(dm.CurrentDay);
        }
    }

    private void OnDisable()
    {
        var dm = DayManager.Instance;
        if (dm != null)
            dm.OnDayChanged -= HandleDayChanged;
    }

    private void CacheNodePositions()
    {
        _nodePositions = new Dictionary<string, Vector3>();
        MapNode[] nodes = FindObjectsOfType<MapNode>();
        foreach (var node in nodes)
        {
            if (string.IsNullOrEmpty(node.nodeId)) continue;
            Vector3 pos = node.destinationWaypoint != null
                ? node.destinationWaypoint.transform.position
                : node.transform.position;
            _nodePositions[node.nodeId] = pos;
        }
    }

    private void HandleDayChanged(int newDay)
    {
        Debug.LogError("HandleDayChanged" + newDay);

        StopAllBehaviors();
        ApplyBehaviorForDay(newDay);
    }

    private void ApplyBehaviorForDay(int day)
    {
        ApplyNPCNames(day);

        _goBrigrd.SetActive(day<2);

        var config = dayBehaviors.FirstOrDefault(b => b.day == day);
        if (config == null)
        {
            // No specific config for this day — let NPCs wander freely
            foreach (var npc in NPCManager.Instance.AllNPCs)
                npc.StartWandering();
            return;
        }


        List<NPCController> assigned = new List<NPCController>();

        foreach (var action in config.actions)
        {
            List<NPCController> pool = NPCManager.Instance.GetNPCsOfType(action.npcType)
                .Where(n => !assigned.Contains(n))
                .ToList();


            int count = Mathf.Min(action.npcCount, pool.Count);
            for (int i = 0; i < count; i++)
            {
                NPCController npc = pool[i];
                assigned.Add(npc);
                ExecuteAction(npc, action);
            }
        }

        // Unassigned NPCs continue wandering
        foreach (var npc in NPCManager.Instance.AllNPCs)
        {
            if (!assigned.Contains(npc))
                npc.StartWandering();
        }
    }

    private void ApplyNPCNames(int day)
    {
        var allNpcs = NPCManager.Instance.AllNPCs;

        if (day <= 2)
        {
            foreach (var npc in allNpcs)
            {
                npc.SetSpineColor(Color.gray);
                npc.SetNameText("???");
            }
            return;
        }

        if (!_namesAssigned)
        {
            var namePool = new List<string>(NPCController.NPC_NAMES);
            for (int i = 0; i < namePool.Count; i++)
            {
                int swap = UnityEngine.Random.Range(i, namePool.Count);
                (namePool[i], namePool[swap]) = (namePool[swap], namePool[i]);
            }

            int count = Mathf.Min(allNpcs.Count, namePool.Count);
            for (int i = 0; i < count; i++)
                allNpcs[i].SetNameText(namePool[i]);

            _namesAssigned = true;
        }
        else
        {
            foreach (var npc in allNpcs)
            {
                if (!string.IsNullOrEmpty(npc.AssignedName))
                    npc.SetNameText(npc.AssignedName);
            }
        }

        Color lysaColor = day > 4 ? Color.white : Color.gray;
        foreach (var npc in allNpcs)
        {

            if (npc.AssignedName == "lysa")
                npc.SetSpineColor(lysaColor);
        }
    }

    private void ExecuteAction(NPCController npc, NPCActionEntry action)
    {
        Vector3 targetPos = npc.transform.position;
        if (!string.IsNullOrEmpty(action.targetNodeId) && _nodePositions.TryGetValue(action.targetNodeId, out var nodePos))
            targetPos = nodePos + action.offsetFromNode;

        float stayDuration = action.duration;
        NPCController captured = npc;

        npc.MoveTo(targetPos, () =>
        {
            if (stayDuration > 0)
            {
                var routine = StartCoroutine(WaitThenWander(captured, stayDuration));
                _activeBehaviors[captured] = routine;
            }
        });
    }

    private IEnumerator WaitThenWander(NPCController npc, float duration)
    {
        yield return new WaitForSeconds(duration);
        npc.StartWandering();
        _activeBehaviors.Remove(npc);
    }

    public void StopAllBehaviors()
    {
        foreach (var kvp in _activeBehaviors)
        {
            if (kvp.Value != null)
                StopCoroutine(kvp.Value);
        }
        _activeBehaviors.Clear();

        foreach (var npc in NPCManager.Instance.AllNPCs)
            npc.StopAllMovement();
    }
}

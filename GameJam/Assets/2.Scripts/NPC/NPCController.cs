using System;
using System.Collections;
using Spine.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.AI;

public class NPCController : MonoBehaviour
{
    [SerializeField] private NPCType npcType;
    [SerializeField] private string npcId;
    [SerializeField] private SkeletonAnimation _skeletonAnimation;
    [SerializeField] private TextMeshPro _nameText;

    [SerializeField] private NavMeshAgent _agent;
    private bool _isUnlocked;
    private Coroutine _moveRoutine;
    private Coroutine _wanderRoutine;
    private string _assignedName;

    public static readonly string[] NPC_NAMES = { "aila", "borin", "cora", "edda", "finn", "milo", "nora" };
    public static readonly string NPC_LITTLE_GIRL = "lysa";
    public static readonly string NPC_OLD_MAIN = "borin";

    public NPCType NPCType => npcType;
    public string NPCId => npcId;
    public bool IsUnlocked => _isUnlocked;
    public NavMeshAgent Agent => _agent;
    public string AssignedName => _assignedName;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        if (_skeletonAnimation == null)
            _skeletonAnimation = GetComponentInChildren<SkeletonAnimation>();
        if (_nameText == null)
            _nameText = GetComponentInChildren<TextMeshPro>();
    }

    private void Start()
    {
        if (_agent != null)
            _agent.updateRotation = false;
    }

    public void Unlock(string skinName)
    {
        if (_isUnlocked) return;
        _isUnlocked = true;
    }

    public void SetNameText(string name)
    {
        _assignedName = name;
        if (_nameText != null)
            _nameText.text = name;
    }

    public void SetNameColor(Color color)
    {
        if (_nameText != null)
            _nameText.color = color;
    }

    public void SetSpineColor(Color color)
    {
        if (_skeletonAnimation != null && _skeletonAnimation.Skeleton != null)
            _skeletonAnimation.Skeleton.SetColor(color);
    }

    public void MoveTo(Vector3 position, Action onArrive = null)
    {
        StopAllRoutines();
        _moveRoutine = StartCoroutine(MoveToRoutine(position, onArrive));
    }

    private IEnumerator MoveToRoutine(Vector3 position, Action onArrive)
    {
        if (_agent == null || !_agent.isOnNavMesh)
        {
            onArrive?.Invoke();
            yield break;
        }

        _agent.SetDestination(position);

        float timeout = 10f;
        float elapsed = 0f;
        while (_agent.pathPending || _agent.remainingDistance > _agent.stoppingDistance + 0.1f)
        {
            elapsed += Time.deltaTime;
            if (elapsed > timeout) break;
            yield return null;
        }

        onArrive?.Invoke();
    }

    public void StartWandering(float radius = 5f, float minWait = 2f, float maxWait = 5f)
    {
        StopAllRoutines();
        _wanderRoutine = StartCoroutine(WanderRoutine(radius, minWait, maxWait));
    }

    public void StopAllMovement()
    {
        StopAllRoutines();
        if (_agent != null && _agent.isOnNavMesh)
            _agent.ResetPath();
    }

    private void StopAllRoutines()
    {
        if (_moveRoutine != null) { StopCoroutine(_moveRoutine); _moveRoutine = null; }
        if (_wanderRoutine != null) { StopCoroutine(_wanderRoutine); _wanderRoutine = null; }
    }

    private IEnumerator WanderRoutine(float radius, float minWait, float maxWait)
    {
        while (true)
        {
            if (_agent != null && _agent.isOnNavMesh)
            {
                Vector3 randomDir = UnityEngine.Random.insideUnitSphere * radius;
                randomDir.y = 0;
                Vector3 targetPos = transform.position + randomDir;

                if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, radius, NavMesh.AllAreas))
                    _agent.SetDestination(hit.position);
            }

            yield return new WaitForSeconds(UnityEngine.Random.Range(minWait, maxWait));
        }
    }

    private void OnDrawGizmosSelected()
    {
        // if (skeletonAnimation != null && skeletonAnimation.Skeleton != null)
        //     Gizmos.DrawIcon(transform.position + Vector3.up * 1.5f, skeletonAnimation.Skeleton.Skin?.Name ?? "gray", true);
    }
}

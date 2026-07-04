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
    [SerializeField] private SkeletonAnimation _skeletonFront;
    [SerializeField] private SkeletonAnimation _skeletonBack;
    [SerializeField] private TextMeshPro _nameText;

    [SerializeField] private NavMeshAgent _agent;
    private bool _isUnlocked;
    private Coroutine _moveRoutine;
    private Coroutine _wanderRoutine;
    private string _assignedName;

    private SkeletonAnimation _currentSkeleton;
    private string _currentAnim;
    private float _lastDirZ;

    public static readonly string[] NPC_NAMES = { "aila", "cora", "edda", "finn", "milo", "nora" };
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
        var skeletons = GetComponentsInChildren<SkeletonAnimation>();
        if (_skeletonFront == null && skeletons.Length > 0)
            _skeletonFront = skeletons[0];
        if (_skeletonBack == null && skeletons.Length > 1)
            _skeletonBack = skeletons[1];
        if (_nameText == null)
            _nameText = GetComponentInChildren<TextMeshPro>();
    }

    private void Start()
    {
        if (_agent != null)
            _agent.updateRotation = false;
        PlayAnimation(_skeletonFront ?? _skeletonBack, "1.Idle");
    }

    private void Update()
    {
        if (_agent == null || !_agent.isOnNavMesh) return;

        bool moving = _agent.velocity.sqrMagnitude > 0.01f;

        if (moving)
        {
            Vector3 vel = _agent.velocity;
            bool useBack = vel.z > 0.01f || vel.x < -0.01f;
            SkeletonAnimation target = useBack ? _skeletonBack : _skeletonFront;
            if (target != null)
                PlayAnimation(target, "3.Run");
        }
        else
        {
            PlayAnimation(_skeletonFront ?? _skeletonBack, "1.Idle");
        }
    }

    private void PlayAnimation(SkeletonAnimation skeleton, string anim)
    {
        if (skeleton == null) return;
        if (skeleton == _currentSkeleton && _currentAnim == anim) return;
        if (skeleton.AnimationState == null) return;

        if (_skeletonFront != null) _skeletonFront.gameObject.SetActive(skeleton == _skeletonFront);
        if (_skeletonBack != null) _skeletonBack.gameObject.SetActive(skeleton == _skeletonBack);

        skeleton.AnimationState.SetAnimation(0, anim, true);
        _currentSkeleton = skeleton;
        _currentAnim = anim;
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
        if (_skeletonFront != null && _skeletonFront.Skeleton != null)
            _skeletonFront.Skeleton.SetColor(color);
        if (_skeletonBack != null && _skeletonBack.Skeleton != null)
            _skeletonBack.Skeleton.SetColor(color);
    }

    public void TeleportTo(Vector3 position, Action onArrive = null)
    {
        StopAllRoutines();
        if (_agent != null && _agent.isOnNavMesh)
        {
            _agent.Warp(position);
            _agent.ResetPath();
        }
        else
        {
            transform.position = position;
        }
        onArrive?.Invoke();
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

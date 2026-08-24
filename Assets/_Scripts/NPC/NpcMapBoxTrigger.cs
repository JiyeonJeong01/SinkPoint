using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider), typeof(Rigidbody))]
public sealed class NpcMapBoxTrigger : MonoBehaviour
{
    [SerializeField] private NpcInteraction owner;
    [SerializeField] private LayerMask detectionMask = ~0;
    [SerializeField] private bool showDebugLog;

    private readonly Collider[] overlapResults = new Collider[16];
    private BoxCollider boxCollider;

    private void Reset()
    {
        ConfigureTriggerPhysics();
    }

    private void Awake()
    {
        ConfigureTriggerPhysics();
        owner ??= GetComponentInParent<NpcInteraction>();
    }

    private void FixedUpdate()
    {
        PollOverlaps();
    }

    private void ConfigureTriggerPhysics()
    {
        boxCollider = GetComponent<BoxCollider>();
        boxCollider.isTrigger = true;
        boxCollider.size = Vector3.one;

        // NPC 쪽에서 Trigger 이벤트가 확실히 발생하도록 트리거 자식에 kinematic Rigidbody를 둡니다.
        Rigidbody body = GetComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (owner != null)
        {
            LogTrigger("Enter", other);
            owner.HandlePlayerEntered(other);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (owner != null)
        {
            owner.HandlePlayerEntered(other);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (owner != null)
        {
            LogTrigger("Exit", other);
            owner.HandlePlayerExited(other);
        }
    }

    private void PollOverlaps()
    {
        if (owner == null || boxCollider == null)
        {
            return;
        }

        Vector3 worldCenter = transform.TransformPoint(boxCollider.center);
        Vector3 halfExtents = Vector3.Scale(boxCollider.size, transform.lossyScale) * 0.5f;
        int hitCount = Physics.OverlapBoxNonAlloc(
            worldCenter,
            halfExtents,
            overlapResults,
            transform.rotation,
            detectionMask,
            QueryTriggerInteraction.Collide);

        bool foundPlayer = false;
        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = overlapResults[i];
            if (hit == null || hit == boxCollider || hit.transform.IsChildOf(transform))
            {
                continue;
            }

            if (owner.HandlePlayerDetected(hit))
            {
                foundPlayer = true;
            }
        }

        if (!foundPlayer)
        {
            owner.HandlePlayerDetectionLost();
        }
    }

    private void LogTrigger(string phase, Collider other)
    {
        if (showDebugLog)
        {
            Debug.Log($"[{nameof(NpcMapBoxTrigger)}] {phase}: {other.name}", this);
        }
    }

#if UNITY_EDITOR
    public void EditorConfigure(NpcInteraction interaction)
    {
        owner = interaction;
    }
#endif
}

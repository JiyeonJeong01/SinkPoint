using UnityEngine;

[DisallowMultipleComponent]
public sealed class GravitySourceOrbTrigger : MonoBehaviour
{
    [SerializeField] private GravitySourceEndingController endingController;
    [SerializeField] private Collider triggerCollider;

    private void Awake()
    {
        ResolveReferences();
    }

    private void Reset()
    {
        ResolveReferences();
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        TryTriggerEnding(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryTriggerEnding(other);
    }

    private void TryTriggerEnding(Collider other)
    {
        PlayerInput playerInput = other != null ? other.GetComponentInParent<PlayerInput>() : null;
        if (playerInput == null)
        {
            return;
        }

        ResolveReferences();
        endingController?.HandleOrbTriggered(playerInput);
    }

    public void SetTriggerEnabled(bool enabled)
    {
        ResolveReferences();
        if (triggerCollider != null)
        {
            triggerCollider.enabled = enabled;
        }
    }

    private void ResolveReferences()
    {
        endingController ??= GetComponentInParent<GravitySourceEndingController>();
        endingController ??= FindFirstObjectByType<GravitySourceEndingController>();
        triggerCollider ??= GetComponent<Collider>();
    }

#if UNITY_EDITOR
    public void EditorConfigure(GravitySourceEndingController controller, Collider collider)
    {
        endingController = controller;
        triggerCollider = collider;
    }
#endif
}

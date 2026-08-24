using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
public sealed class NpcMapBoxTrigger : MonoBehaviour
{
    [SerializeField] private NpcInteraction owner;

    private void Reset()
    {
        BoxCollider boxCollider = GetComponent<BoxCollider>();
        boxCollider.isTrigger = true;
        boxCollider.size = Vector3.one;
    }

    private void Awake()
    {
        owner ??= GetComponentInParent<NpcInteraction>();
    }

    private void OnTriggerEnter(Collider other)
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
            owner.HandlePlayerExited(other);
        }
    }

#if UNITY_EDITOR
    public void EditorConfigure(NpcInteraction interaction)
    {
        owner = interaction;
    }
#endif
}

using UnityEngine;

[DisallowMultipleComponent]
public sealed class GravityManager : MonoBehaviour
{
    [SerializeField] private GravityState gravityState;
    [SerializeField] private GravityZone initialZone;

    public GravityZone CurrentZone { get; private set; }

    private void Awake()
    {
        gravityState ??= GetComponent<GravityState>();

        if (gravityState == null)
        {
            Debug.LogError($"{nameof(GravityManager)} on '{name}' requires a {nameof(GravityState)} reference.", this);
            enabled = false;
            return;
        }

        if (initialZone != null)
        {
            ActivateZone(initialZone);
        }
    }

    public bool ActivateZone(GravityZone zone)
    {
        if (zone == null)
        {
            Debug.LogError("[GravityManager] Cannot activate a null GravityZone.", this);
            return false;
        }

        if (gravityState == null || !gravityState.SetGravity(zone.Direction, zone.Strength))
        {
            Debug.LogError($"[GravityManager] Failed to activate GravityZone '{zone.name}'.", zone);
            return false;
        }

        CurrentZone = zone;
        return true;
    }
}

using UnityEngine;

[DisallowMultipleComponent]
public sealed class GravityManager : MonoBehaviour
{
    [Header("Gravity")]
    [SerializeField] private GravityState gravityState;
    [SerializeField] private GravityZone initialZone;

    [Header("Manual Test")]
    [Tooltip("Play Mode 중 GravityManager Inspector의 테스트 버튼으로 적용할 Zone입니다.")]
    [SerializeField] private GravityZone manualTestZone;

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

using UnityEngine;

[DisallowMultipleComponent]
public sealed class GravityDebugController : MonoBehaviour
{
    private enum ControlMode
    {
        FollowGameFlow,
        Manual
    }

    [SerializeField] private GravityManager gravityManager;
    [SerializeField] private ControlMode controlMode = ControlMode.FollowGameFlow;
    [SerializeField] private GravityZone selectedZone;

    [ContextMenu("Apply Selected Zone")]
    private void ApplySelectedZone()
    {
        if (controlMode != ControlMode.Manual)
        {
            Debug.LogWarning("[GravityDebugController] Switch Control Mode to Manual before applying a zone.", this);
            return;
        }

        if (gravityManager == null)
        {
            Debug.LogError("[GravityDebugController] GravityManager is not assigned.", this);
            return;
        }

        gravityManager.ActivateZone(selectedZone);
    }
}

using UnityEngine;

/// <summary>
/// Normal Zone 지네처럼 바닥 한 면에서만 플레이어를 추적하는 이동 컴포넌트입니다.
/// waypoint 없이 현재 중력 기준 바닥 위에서 로컬 추적만 수행하는 첫 전투 MVP용입니다.
/// </summary>
public sealed class CentipedeFloorMover : MonsterNavTargetMover
{
    protected override void ResolveSceneReferences()
    {
        base.ResolveSceneReferences();

        Transform centipedeNavTarget = transform.Find("Nav Target");
        if (centipedeNavTarget != null)
        {
            navTarget = centipedeNavTarget;
        }
    }

    private void FixedUpdate()
    {
        if (stateMachine == null || stateMachine.State == MonsterState.Dead)
        {
            return;
        }

        Transform target = stateMachine.Target;
        if (target == null || stateMachine.State == MonsterState.Attack)
        {
            return;
        }

        Vector3 floorNormal = gravityState != null ? -gravityState.Direction : Vector3.up;
        MoveNavTargetPositionOnly(target.position, floorNormal, Time.fixedDeltaTime);
    }

    /// <summary>
    /// 지네 prefab의 Nav Target은 기존 Follow/Worm 체인이 따라가는 선행 목표점입니다.
    /// Spider처럼 root 회전까지 제어하면 다리 기준축이 흔들릴 수 있으므로 위치만 이동합니다.
    /// </summary>
    private void MoveNavTargetPositionOnly(Vector3 destination, Vector3 floorNormal, float deltaTime)
    {
        if (navTarget == null)
        {
            return;
        }

        Vector3 normal = floorNormal.sqrMagnitude < Mathf.Epsilon
            ? Vector3.up
            : floorNormal.normalized;
        Vector3 toDestination = destination - navTarget.position;
        Vector3 moveDirection = Vector3.ProjectOnPlane(toDestination, normal);

        if (moveDirection.magnitude <= stoppingDistance)
        {
            return;
        }

        navTarget.position += moveDirection.normalized * moveSpeed * deltaTime;
    }
}

using UnityEngine;

/// <summary>
/// Normal Zone 지네처럼 바닥 한 면에서만 플레이어를 추적하는 이동 컴포넌트입니다.
/// waypoint 없이 현재 중력 기준 바닥 위에서 로컬 추적만 수행하는 첫 전투 MVP용입니다.
/// </summary>
public sealed class CentipedeFloorMover : MonsterNavTargetMover
{
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
        MoveNavTargetToward(target.position, floorNormal, Time.fixedDeltaTime);
    }
}

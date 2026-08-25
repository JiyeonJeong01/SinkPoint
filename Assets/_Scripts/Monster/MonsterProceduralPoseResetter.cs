using DistantLands;
using UnityEngine;

/// <summary>
/// NavTarget을 따라 몸과 다리가 절차적으로 움직이는 몬스터의 리스폰 포즈를 정리합니다.
/// Kaiju 에셋의 Leg는 런타임에 IK target을 부모에서 분리하므로, 루트만 순간이동하면 발 target이 예전 월드 위치에 남을 수 있습니다.
/// </summary>
public static class MonsterProceduralPoseResetter
{
    private const float kFallbackRayDistance = 100f;

    /// <summary>
    /// 리스폰 직후 Leg/LegIK target을 현재 root.up 기준 지면에 다시 붙이고, Follow 몸체를 NavTarget에 맞춰 정렬합니다.
    /// </summary>
    public static void ResetProceduralPose(Transform monsterRoot)
    {
        if (monsterRoot == null)
        {
            return;
        }

        SnapFollowBodies(monsterRoot);
        ResetLegTargets(monsterRoot);
    }

    private static void SnapFollowBodies(Transform monsterRoot)
    {
        Follow[] follows = monsterRoot.GetComponentsInChildren<Follow>(true);
        for (int i = 0; i < follows.Length; i++)
        {
            Follow follow = follows[i];
            if (follow == null || follow.target == null)
            {
                continue;
            }

            follow.transform.position = follow.target.position;
            if (follow.matchTargetRotation)
            {
                follow.transform.rotation = follow.target.rotation;
                continue;
            }

            if (!follow.alignUpToTarget)
            {
                continue;
            }

            Vector3 up = follow.target.up.sqrMagnitude > Mathf.Epsilon
                ? follow.target.up.normalized
                : Vector3.up;
            Vector3 forward = Vector3.ProjectOnPlane(follow.transform.forward, up);
            if (forward.sqrMagnitude < 0.0001f)
            {
                forward = Vector3.ProjectOnPlane(follow.target.forward, up);
            }

            if (forward.sqrMagnitude > 0.0001f)
            {
                follow.transform.rotation = Quaternion.LookRotation(forward.normalized, up);
            }
        }
    }

    private static void ResetLegTargets(Transform monsterRoot)
    {
        Leg[] legs = monsterRoot.GetComponentsInChildren<Leg>(true);
        for (int i = 0; i < legs.Length; i++)
        {
            ResetLegTarget(legs[i]);
        }
    }

    private static void ResetLegTarget(Leg leg)
    {
        if (leg == null)
        {
            return;
        }

        if (leg.IKSolver == null)
        {
            leg.IKSolver = leg.GetComponent<LegIK>();
        }

        if (leg.IKSolver == null || leg.IKSolver.target == null || leg.nextFootTarget == null)
        {
            return;
        }

        Transform root = leg.root != null ? leg.root : leg.transform.root;
        Vector3 up = root != null && root.up.sqrMagnitude > Mathf.Epsilon
            ? root.up.normalized
            : Vector3.up;
        float scale = GetReferenceScale(root != null ? root : leg.transform);
        Vector3 origin = leg.nextFootTarget.position;
        Vector3 targetPosition = origin;
        Quaternion targetRotation = leg.IKSolver.target.rotation;

        if (TryFindFootPoint(leg, origin, -up, scale, out RaycastHit hit))
        {
            targetPosition = hit.point + up * leg.groundOffset * scale;
            targetRotation = BuildFootRotation(hit.point - targetPosition, up, targetRotation);
        }

        leg.IKSolver.target.SetPositionAndRotation(targetPosition, targetRotation);

        if (leg.currentTarget != null)
        {
            leg.currentTarget.SetPositionAndRotation(targetPosition, targetRotation);
        }

        leg.grounded = true;
    }

    private static bool TryFindFootPoint(Leg leg, Vector3 origin, Vector3 direction, float scale, out RaycastHit hit)
    {
        float rayDistance = leg.maxRayDistance > 0f
            ? leg.maxRayDistance * scale
            : kFallbackRayDistance;
        int mask = leg.layerMask.value != 0 ? leg.layerMask.value : Physics.DefaultRaycastLayers;

        return Physics.Raycast(
            origin,
            direction,
            out hit,
            rayDistance,
            mask,
            QueryTriggerInteraction.Ignore);
    }

    private static Quaternion BuildFootRotation(Vector3 lookDirection, Vector3 up, Quaternion fallback)
    {
        if (lookDirection.sqrMagnitude < 0.0001f)
        {
            return fallback;
        }

        Vector3 normalizedLook = lookDirection.normalized;
        if (Mathf.Abs(Vector3.Dot(normalizedLook, up.normalized)) > 0.999f)
        {
            return fallback;
        }

        return Quaternion.LookRotation(normalizedLook, up);
    }

    private static float GetReferenceScale(Transform reference)
    {
        Vector3 scale = reference != null ? reference.lossyScale : Vector3.one;
        float largestAxis = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
        return Mathf.Max(largestAxis, 0.0001f);
    }
}

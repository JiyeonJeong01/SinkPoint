using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class GravityState : MonoBehaviour
{
    [Tooltip("현재 중력 방향입니다. 런타임 전환은 GravityManager.ActivateZone을 사용합니다.")]
    [SerializeField] private Vector3 gravityDirection = Vector3.down;

    [Tooltip("현재 중력 세기입니다. 런타임 전환은 GravityManager.ActivateZone을 사용합니다.")]
    [SerializeField, Min(0f)] private float gravityStrength = 9.81f;

    public Vector3 Direction => gravityDirection;
    public float Strength => gravityStrength;
    public Vector3 Gravity => gravityDirection * gravityStrength;

    public event Action Changed;

    public bool SetGravity(Vector3 direction, float strength)
    {
        if (!IsFinite(direction) || direction.sqrMagnitude < Mathf.Epsilon)
        {
            Debug.LogError("[GravityState] Gravity direction must be a finite, non-zero vector.", this);
            return false;
        }

        if (float.IsNaN(strength) || float.IsInfinity(strength) || strength < 0f)
        {
            Debug.LogError("[GravityState] Gravity strength must be a finite value greater than or equal to zero.", this);
            return false;
        }

        Vector3 normalizedDirection = direction.normalized;
        if (gravityDirection == normalizedDirection && Mathf.Approximately(gravityStrength, strength))
        {
            return true;
        }

        gravityDirection = normalizedDirection;
        gravityStrength = strength;
        Changed?.Invoke();
        return true;
    }

    private void OnValidate()
    {
        if (gravityDirection.sqrMagnitude < Mathf.Epsilon)
        {
            gravityDirection = Vector3.down;
        }
        else
        {
            gravityDirection.Normalize();
        }

        gravityStrength = Mathf.Max(0f, gravityStrength);
    }

    private static bool IsFinite(Vector3 value)
    {
        return !float.IsNaN(value.x)
            && !float.IsInfinity(value.x)
            && !float.IsNaN(value.y)
            && !float.IsInfinity(value.y)
            && !float.IsNaN(value.z)
            && !float.IsInfinity(value.z);
    }
}

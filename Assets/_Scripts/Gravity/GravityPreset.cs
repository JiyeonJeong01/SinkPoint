using UnityEngine;

public enum GravityPresetMode
{
    Fixed,
    Periodic,
    ZeroGravity
}

[DisallowMultipleComponent]
public sealed class GravityPreset : MonoBehaviour
{
    [SerializeField] private GravityPresetMode mode = GravityPresetMode.Fixed;
    [SerializeField] private Vector3 direction = Vector3.down;
    [SerializeField] private float strength = 9.81f;

    [Header("Periodic")]
    [SerializeField] private Vector3[] periodicDirections = System.Array.Empty<Vector3>();
    [SerializeField, Min(0f)] private float changeInterval = 4f;
    [SerializeField, Min(0f)] private float warningDuration = 1f;

    public GravityPresetMode Mode => mode;
    public Vector3 Direction => direction;
    public float Strength => mode == GravityPresetMode.ZeroGravity ? 0f : strength;
    public int PeriodicDirectionCount => periodicDirections?.Length ?? 0;
    public float ChangeInterval => changeInterval;
    public float WarningDuration => warningDuration;

    public Vector3 GetPeriodicDirection(int index)
    {
        return periodicDirections[index];
    }

    public bool TryValidate(out string error)
    {
        if (mode == GravityPresetMode.Fixed && !IsValidDirection(direction))
        {
            error = "Fixed direction must be a finite, non-zero vector.";
            return false;
        }

        if (mode != GravityPresetMode.ZeroGravity
            && (float.IsNaN(strength) || float.IsInfinity(strength) || strength < 0f))
        {
            error = "Strength must be a finite value greater than or equal to zero.";
            return false;
        }

        if (mode == GravityPresetMode.Periodic)
        {
            if (periodicDirections == null || periodicDirections.Length == 0)
            {
                error = "Periodic presets require at least one direction.";
                return false;
            }

            for (int i = 0; i < periodicDirections.Length; i++)
            {
                if (!IsValidDirection(periodicDirections[i]))
                {
                    error = $"Periodic direction at index {i} must be a finite, non-zero vector.";
                    return false;
                }
            }

            if (float.IsNaN(changeInterval)
                || float.IsInfinity(changeInterval)
                || changeInterval <= 0f)
            {
                error = "Change interval must be a finite value greater than zero.";
                return false;
            }

            if (float.IsNaN(warningDuration)
                || float.IsInfinity(warningDuration)
                || warningDuration < 0f
                || warningDuration > changeInterval)
            {
                error = "Warning duration must be between zero and the change interval.";
                return false;
            }
        }

        error = string.Empty;
        return true;
    }

    private void OnValidate()
    {
        if (IsValidDirection(direction))
        {
            direction.Normalize();
        }

        if (periodicDirections == null)
        {
            return;
        }

        for (int i = 0; i < periodicDirections.Length; i++)
        {
            if (IsValidDirection(periodicDirections[i]))
            {
                periodicDirections[i].Normalize();
            }
        }
    }

    private static bool IsValidDirection(Vector3 value)
    {
        return IsFinite(value) && value.sqrMagnitude >= Mathf.Epsilon;
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

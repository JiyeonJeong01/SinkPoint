using UnityEngine;

[DisallowMultipleComponent]
public sealed class GravityState : MonoBehaviour
{
    [SerializeField] private Vector3 gravityDirection = Vector3.down;
    [SerializeField, Min(0f)] private float gravityStrength = 9.81f;

    public Vector3 Direction => gravityDirection;
    public float Strength => gravityStrength;
    public Vector3 Gravity => gravityDirection * gravityStrength;

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
}

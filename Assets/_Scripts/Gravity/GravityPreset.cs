using UnityEngine;

[DisallowMultipleComponent]
public sealed class GravityPreset : MonoBehaviour
{
    [SerializeField] private Vector3 direction = Vector3.down;
    [SerializeField] private float strength = 9.81f;

    public Vector3 Direction => direction;
    public float Strength => strength;

    private void OnValidate()
    {
        if (direction.sqrMagnitude >= Mathf.Epsilon)
        {
            direction.Normalize();
        }
    }
}

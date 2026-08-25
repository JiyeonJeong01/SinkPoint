using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public sealed class GravityBody : MonoBehaviour
{
    [SerializeField] private GravityState gravityState;

    private Rigidbody body;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        body.useGravity = false;
    }

    private void OnEnable()
    {
        if (gravityState == null)
        {
            Debug.LogError($"{nameof(GravityBody)} on '{name}' requires a {nameof(GravityState)} reference.", this);
            enabled = false;
            return;
        }

        gravityState.Changed += OnGravityChanged;
    }

    private void OnDisable()
    {
        if (gravityState != null)
        {
            gravityState.Changed -= OnGravityChanged;
        }
    }

    private void FixedUpdate()
    {
        body.AddForce(gravityState.Gravity, ForceMode.Acceleration);
    }

    private void OnGravityChanged()
    {
        body.WakeUp();
    }
}

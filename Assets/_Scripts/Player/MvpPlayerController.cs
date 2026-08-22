using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider), typeof(MvpPlayerInput))]
public sealed class MvpPlayerController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MvpPlayerInput input;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private MvpGravityState gravityState;

    [Header("Movement")]
    [SerializeField, Min(0f)] private float moveSpeed = 3f;
    [SerializeField, Min(0f)] private float rotationSpeed = 720f;

    [Header("Grounding")]
    [SerializeField, Range(0f, 89f)] private float maxGroundAngle = 50f;
    [SerializeField, Min(0f)] private float groundProbeDistance = 0.15f;

    private readonly RaycastHit[] groundHits = new RaycastHit[8];

    private Rigidbody body;
    private CapsuleCollider capsule;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();
        input ??= GetComponent<MvpPlayerInput>();
        body.useGravity = false;
    }

    private void Start()
    {
        if (input != null && cameraTransform != null && gravityState != null)
        {
            return;
        }

        Debug.LogError(
            $"{nameof(MvpPlayerController)} on '{name}' requires Input, Camera Transform, and Gravity State references.",
            this);
        enabled = false;
    }

    private void FixedUpdate()
    {
        Vector3 gravityDirection = gravityState.Direction;
        Vector3 up = -gravityDirection;
        bool isGrounded = TryGetGroundNormal(gravityDirection, up, out Vector3 groundNormal);

        Vector3 cameraForward = Vector3.ProjectOnPlane(cameraTransform.forward, up).normalized;
        if (cameraForward.sqrMagnitude < Mathf.Epsilon)
        {
            cameraForward = Vector3.ProjectOnPlane(transform.forward, up).normalized;
        }

        Vector3 cameraRight = Vector3.Cross(up, cameraForward).normalized;
        Vector2 moveInput = Vector2.ClampMagnitude(input.Move, 1f);
        Vector3 moveDirection = cameraForward * moveInput.y + cameraRight * moveInput.x;

        if (isGrounded && moveDirection.sqrMagnitude > Mathf.Epsilon)
        {
            moveDirection = Vector3.ProjectOnPlane(moveDirection, groundNormal).normalized;
        }

        Vector3 gravityVelocity = Vector3.Project(body.linearVelocity, gravityDirection);
        if (isGrounded && Vector3.Dot(gravityVelocity, gravityDirection) > 0f)
        {
            gravityVelocity = Vector3.zero;
        }

        body.linearVelocity = moveDirection * moveSpeed + gravityVelocity;

        Vector3 appliedGravity = isGrounded
            ? -groundNormal * gravityState.Strength
            : gravityState.Gravity;
        body.AddForce(appliedGravity, ForceMode.Acceleration);

        Quaternion targetRotation = Quaternion.LookRotation(cameraForward, up);
        Quaternion nextRotation = Quaternion.RotateTowards(
            body.rotation,
            targetRotation,
            rotationSpeed * Time.fixedDeltaTime);
        body.MoveRotation(nextRotation);
    }

    private bool TryGetGroundNormal(
        Vector3 gravityDirection,
        Vector3 up,
        out Vector3 groundNormal)
    {
        Vector3 scale = transform.lossyScale;
        float radiusScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
        float radius = capsule.radius * radiusScale * 0.9f;
        float halfHeight = Mathf.Max(capsule.height * Mathf.Abs(scale.y) * 0.5f, radius);
        float castDistance = halfHeight - radius + groundProbeDistance;
        Vector3 origin = transform.TransformPoint(capsule.center);

        int hitCount = Physics.SphereCastNonAlloc(
            origin,
            radius,
            gravityDirection,
            groundHits,
            castDistance,
            Physics.AllLayers,
            QueryTriggerInteraction.Ignore);

        float nearestDistance = float.PositiveInfinity;
        groundNormal = up;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = groundHits[i];
            if (hit.collider == capsule || hit.distance >= nearestDistance)
            {
                continue;
            }

            float groundAngle = Vector3.Angle(hit.normal, up);
            if (groundAngle > maxGroundAngle)
            {
                continue;
            }

            nearestDistance = hit.distance;
            groundNormal = hit.normal;
        }

        return nearestDistance < float.PositiveInfinity;
    }
}

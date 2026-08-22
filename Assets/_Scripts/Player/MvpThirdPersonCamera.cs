using UnityEngine;

[DisallowMultipleComponent]
public sealed class MvpThirdPersonCamera : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MvpPlayerInput input;
    [SerializeField] private Transform target;
    [SerializeField] private Transform cameraPivot;

    [Header("Look")]
    [SerializeField, Min(0f)] private float mouseSensitivity = 2f;
    [SerializeField] private float minPitch = -40f;
    [SerializeField] private float maxPitch = 70f;

    private float yaw;
    private float pitch;

    private void Awake()
    {
        cameraPivot ??= transform.Find("CameraPivot");
        yaw = transform.eulerAngles.y;
        pitch = cameraPivot != null ? NormalizeAngle(cameraPivot.localEulerAngles.x) : 0f;
    }

    private void Start()
    {
        if (input != null && target != null && cameraPivot != null)
        {
            return;
        }

        Debug.LogError(
            $"{nameof(MvpThirdPersonCamera)} on '{name}' requires Input, Target, and Camera Pivot references.",
            this);
        enabled = false;
    }

    private void OnEnable()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnDisable()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void LateUpdate()
    {
        yaw += input.Look.x * mouseSensitivity;
        pitch = Mathf.Clamp(pitch - input.Look.y * mouseSensitivity, minPitch, maxPitch);

        transform.SetPositionAndRotation(
            target.position,
            Quaternion.AngleAxis(yaw, Vector3.up));
        cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private static float NormalizeAngle(float angle)
    {
        return angle > 180f ? angle - 360f : angle;
    }
}

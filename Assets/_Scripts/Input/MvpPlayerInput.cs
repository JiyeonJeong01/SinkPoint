using UnityEngine;

/// <summary>
/// MVP용 입력 입구입니다.
/// 다른 플레이어 스크립트는 Input.GetKey/GetAxis를 직접 호출하지 말고 이 클래스의 값을 읽습니다.
/// </summary>
public class MvpPlayerInput : MonoBehaviour
{
    [Header("Input Locks")]
    [Tooltip("WASD, Jump, Shift처럼 플레이어 몸을 움직이는 입력을 허용합니다.")]
    [SerializeField] private bool allowMovement = true;

    [Tooltip("마우스 시야 회전 입력을 허용합니다.")]
    [SerializeField] private bool allowLook = true;

    [Tooltip("좌클릭 발사와 R 재장전을 허용합니다.")]
    [SerializeField] private bool allowCombat = true;

    [Tooltip("우클릭 그래플링 훅 발사를 허용합니다.")]
    [SerializeField] private bool allowGrapple = true;

    [Tooltip("I 상호작용 입력을 허용합니다. 대화 중에도 보통 이 값은 켜둡니다.")]
    [SerializeField] private bool allowInteract = true;

    private bool jumpPressedPending;
    private double jumpPressedAtRealtime;

    public bool AllowMovement
    {
        get => allowMovement;
        set
        {
            allowMovement = value;
            if (!allowMovement)
            {
                ClearMovementInput();
            }
        }
    }

    public bool AllowLook
    {
        get => allowLook;
        set => allowLook = value;
    }

    public bool AllowCombat
    {
        get => allowCombat;
        set => allowCombat = value;
    }

    public bool AllowGrapple
    {
        get => allowGrapple;
        set => allowGrapple = value;
    }

    public bool AllowInteract
    {
        get => allowInteract;
        set => allowInteract = value;
    }

    public Vector2 Move { get; private set; }
    public Vector2 Look { get; private set; }
    public float CameraZoomDelta { get; private set; }
    public bool SprintOrCrouchHeld { get; private set; }
    public bool ReloadPressed { get; private set; }
    public bool InteractPressed { get; private set; }
    public bool FirePressed { get; private set; }
    public bool FireHeld { get; private set; }
    public bool GrapplePressed { get; private set; }

    private void Update()
    {
        ReadMovementInput();
        ReadLookInput();
        ReadActionInput();
    }

    private void OnDisable()
    {
        ClearMovementInput();
    }

    internal bool TryConsumeJumpPressed(out double pressedAtRealtime)
    {
        if (!jumpPressedPending)
        {
            pressedAtRealtime = 0d;
            return false;
        }

        pressedAtRealtime = jumpPressedAtRealtime;
        jumpPressedPending = false;
        return true;
    }

    public void SetGameplayInput()
    {
        SetInputAllowed(
            movement: true,
            look: true,
            combat: true,
            grapple: true,
            interact: true
        );
    }

    public void SetDialogueInput(bool keepLookEnabled = false)
    {
        // 대화 중에는 이동/전투/그래플을 막고, I 입력만 남겨서 다음 대사 넘김에 쓸 수 있게 둡니다.
        SetInputAllowed(
            movement: false,
            look: keepLookEnabled,
            combat: false,
            grapple: false,
            interact: true
        );
    }

    public void SetCutsceneInput()
    {
        // 컷신이나 엔딩처럼 플레이어 조작을 모두 막아야 할 때 사용합니다.
        SetInputAllowed(
            movement: false,
            look: false,
            combat: false,
            grapple: false,
            interact: false
        );
    }

    public void SetInputAllowed(
        bool movement,
        bool look,
        bool combat,
        bool grapple,
        bool interact)
    {
        AllowMovement = movement;
        allowLook = look;
        allowCombat = combat;
        allowGrapple = grapple;
        allowInteract = interact;
    }

    private void ReadMovementInput()
    {
        if (!allowMovement)
        {
            ClearMovementInput();
            return;
        }

        Move = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical")
        );

        // 렌더 프레임에서 감지한 눌림을 다음 물리 프레임이 소비할 때까지 보존합니다.
        if (Input.GetKeyDown(KeyCode.Space))
        {
            jumpPressedPending = true;
            jumpPressedAtRealtime = Time.realtimeSinceStartupAsDouble;
        }

        // Shift 자체는 입력만 전달합니다. 대쉬/웅크리기 해석은 PlayerController가 상태에 따라 결정합니다.
        SprintOrCrouchHeld = Input.GetKey(KeyCode.LeftShift);
    }

    private void ClearMovementInput()
    {
        Move = Vector2.zero;
        SprintOrCrouchHeld = false;
        jumpPressedPending = false;
        jumpPressedAtRealtime = 0d;
    }

    private void ReadLookInput()
    {
        if (!allowLook)
        {
            Look = Vector2.zero;
            CameraZoomDelta = 0f;
            return;
        }

        Look = new Vector2(
            Input.GetAxis("Mouse X"),
            Input.GetAxis("Mouse Y")
        );
        CameraZoomDelta = Input.mouseScrollDelta.y;
    }

    private void ReadActionInput()
    {
        if (allowCombat)
        {
            // 발사/재장전은 한 번 눌렀는지와 누르고 있는지를 구분해두면 단발/연사 둘 다 빠르게 붙일 수 있습니다.
            FirePressed = Input.GetMouseButtonDown(0);
            FireHeld = Input.GetMouseButton(0);
            ReloadPressed = Input.GetKeyDown(KeyCode.R);
        }
        else
        {
            FirePressed = false;
            FireHeld = false;
            ReloadPressed = false;
        }

        GrapplePressed = allowGrapple && Input.GetMouseButtonDown(1);

        // 상호작용은 대화 중에도 허용될 수 있으므로 전투/이동 차단과 별도로 관리합니다.
        InteractPressed = allowInteract && Input.GetKeyDown(KeyCode.I);
    }
}

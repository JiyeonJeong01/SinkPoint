using UnityEngine;

internal enum PlayerMotionStateId
{
    Uninitialized,
    Grounded,
    Airborne,
    ZeroGravity
}

internal readonly struct PlayerFixedContext
{
    internal readonly Vector3 GravityDirection;
    internal readonly Vector3 Up;
    internal readonly Vector3 GroundNormal;
    internal readonly Vector3 MoveDirection;
    internal readonly bool HasGravity;
    internal readonly bool IsGrounded;
    internal readonly bool JumpRequested;
    internal readonly float VerticalSpeed;

    internal PlayerFixedContext(
        Vector3 gravityDirection,
        Vector3 up,
        Vector3 groundNormal,
        Vector3 moveDirection,
        bool hasGravity,
        bool isGrounded,
        bool jumpRequested,
        float verticalSpeed)
    {
        GravityDirection = gravityDirection;
        Up = up;
        GroundNormal = groundNormal;
        MoveDirection = moveDirection;
        HasGravity = hasGravity;
        IsGrounded = isGrounded;
        JumpRequested = jumpRequested;
        VerticalSpeed = verticalSpeed;
    }
}

internal interface IPlayerMotionState
{
    PlayerMotionStateId Id { get; }
    void Enter(PlayerController owner);
    void FixedTick(PlayerController owner, PlayerFixedContext context);
    void Exit(PlayerController owner);
}

internal sealed class PlayerMotionStateMachine
{
    private readonly IPlayerMotionState groundedState = new GroundedMotionState();
    private readonly IPlayerMotionState airborneState = new AirborneMotionState();
    private readonly IPlayerMotionState zeroGravityState = new ZeroGravityMotionState();

    private IPlayerMotionState currentState;

    internal PlayerMotionStateId CurrentId => currentState?.Id ?? PlayerMotionStateId.Uninitialized;

    internal bool FixedTick(PlayerController owner, PlayerFixedContext context)
    {
        bool isLeavingGround = CurrentId == PlayerMotionStateId.Airborne && context.VerticalSpeed > 0f;
        bool isEffectivelyGrounded = context.IsGrounded && !isLeavingGround;
        bool shouldJump = context.HasGravity && isEffectivelyGrounded && context.JumpRequested;
        IPlayerMotionState nextState = SelectState(context, shouldJump, isEffectivelyGrounded);
        ChangeState(owner, nextState);

        if (shouldJump)
        {
            owner.ApplyJump(context);
            return true;
        }

        currentState.FixedTick(owner, context);
        return false;
    }

    private IPlayerMotionState SelectState(
        PlayerFixedContext context,
        bool shouldJump,
        bool isEffectivelyGrounded)
    {
        if (!context.HasGravity)
        {
            return zeroGravityState;
        }

        if (shouldJump)
        {
            return airborneState;
        }

        return isEffectivelyGrounded ? groundedState : airborneState;
    }

    private void ChangeState(PlayerController owner, IPlayerMotionState nextState)
    {
        if (ReferenceEquals(currentState, nextState))
        {
            return;
        }

        currentState?.Exit(owner);
        currentState = nextState;
        currentState.Enter(owner);
    }
}

internal sealed class GroundedMotionState : IPlayerMotionState
{
    public PlayerMotionStateId Id => PlayerMotionStateId.Grounded;

    public void Enter(PlayerController owner)
    {
    }

    public void FixedTick(PlayerController owner, PlayerFixedContext context)
    {
        owner.ApplyGroundedMotion(context);
    }

    public void Exit(PlayerController owner)
    {
    }
}

internal sealed class AirborneMotionState : IPlayerMotionState
{
    public PlayerMotionStateId Id => PlayerMotionStateId.Airborne;

    public void Enter(PlayerController owner)
    {
    }

    public void FixedTick(PlayerController owner, PlayerFixedContext context)
    {
        owner.ApplyAirborneMotion(context);
    }

    public void Exit(PlayerController owner)
    {
    }
}

internal sealed class ZeroGravityMotionState : IPlayerMotionState
{
    public PlayerMotionStateId Id => PlayerMotionStateId.ZeroGravity;

    public void Enter(PlayerController owner)
    {
        owner.EnterZeroGravity();
    }

    public void FixedTick(PlayerController owner, PlayerFixedContext context)
    {
    }

    public void Exit(PlayerController owner)
    {
    }
}

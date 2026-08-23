using UnityEngine;

internal enum MvpPlayerMotionStateId
{
    Uninitialized,
    Grounded,
    Airborne,
    ZeroGravity
}

internal readonly struct MvpPlayerFixedContext
{
    internal readonly Vector3 GravityDirection;
    internal readonly Vector3 Up;
    internal readonly Vector3 GroundNormal;
    internal readonly Vector3 MoveDirection;
    internal readonly bool HasGravity;
    internal readonly bool IsGrounded;
    internal readonly bool JumpRequested;
    internal readonly float VerticalSpeed;

    internal MvpPlayerFixedContext(
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

internal interface IMvpPlayerState
{
    MvpPlayerMotionStateId Id { get; }
    void Enter(MvpPlayerController owner);
    void FixedTick(MvpPlayerController owner, MvpPlayerFixedContext context);
    void Exit(MvpPlayerController owner);
}

internal sealed class MvpPlayerStateMachine
{
    private readonly IMvpPlayerState groundedState = new MvpGroundedState();
    private readonly IMvpPlayerState airborneState = new MvpAirborneState();
    private readonly IMvpPlayerState zeroGravityState = new MvpZeroGravityState();

    private IMvpPlayerState currentState;

    internal MvpPlayerMotionStateId CurrentId => currentState?.Id ?? MvpPlayerMotionStateId.Uninitialized;

    internal bool FixedTick(MvpPlayerController owner, MvpPlayerFixedContext context)
    {
        bool isLeavingGround = CurrentId == MvpPlayerMotionStateId.Airborne && context.VerticalSpeed > 0f;
        bool isEffectivelyGrounded = context.IsGrounded && !isLeavingGround;
        bool shouldJump = context.HasGravity && isEffectivelyGrounded && context.JumpRequested;
        IMvpPlayerState nextState = SelectState(context, shouldJump, isEffectivelyGrounded);
        ChangeState(owner, nextState);

        if (shouldJump)
        {
            owner.ApplyJump(context);
            return true;
        }

        currentState.FixedTick(owner, context);
        return false;
    }

    private IMvpPlayerState SelectState(
        MvpPlayerFixedContext context,
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

    private void ChangeState(MvpPlayerController owner, IMvpPlayerState nextState)
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

internal sealed class MvpGroundedState : IMvpPlayerState
{
    public MvpPlayerMotionStateId Id => MvpPlayerMotionStateId.Grounded;

    public void Enter(MvpPlayerController owner)
    {
    }

    public void FixedTick(MvpPlayerController owner, MvpPlayerFixedContext context)
    {
        owner.ApplyGroundedMotion(context);
    }

    public void Exit(MvpPlayerController owner)
    {
    }
}

internal sealed class MvpAirborneState : IMvpPlayerState
{
    public MvpPlayerMotionStateId Id => MvpPlayerMotionStateId.Airborne;

    public void Enter(MvpPlayerController owner)
    {
    }

    public void FixedTick(MvpPlayerController owner, MvpPlayerFixedContext context)
    {
        owner.ApplyAirborneMotion(context);
    }

    public void Exit(MvpPlayerController owner)
    {
    }
}

internal sealed class MvpZeroGravityState : IMvpPlayerState
{
    public MvpPlayerMotionStateId Id => MvpPlayerMotionStateId.ZeroGravity;

    public void Enter(MvpPlayerController owner)
    {
    }

    public void FixedTick(MvpPlayerController owner, MvpPlayerFixedContext context)
    {
    }

    public void Exit(MvpPlayerController owner)
    {
    }
}

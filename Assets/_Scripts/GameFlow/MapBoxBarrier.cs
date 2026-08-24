using System.Collections;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// Zone 경계를 막는 검은색 바리게이트입니다.
/// GameFlowManager는 Open/Close만 요청하고, 실제 이동 애니메이션과 중복 실행 방어는 이 클래스가 담당합니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class MapBoxBarrier : MonoBehaviour
{
    private enum BarrierState
    {
        Open,
        Opening,
        Closed,
        Closing
    }

    [Header("Movement")]
    [SerializeField, Tooltip("실제로 움직일 바리게이트 본체입니다. 비워두면 이 오브젝트가 움직입니다.")]
    private Transform movingBody;
    [SerializeField, Tooltip("열린 위치입니다. 플레이어가 통과 가능한 위치에 둡니다.")]
    private Transform startPosition;
    [SerializeField, Tooltip("닫힌 위치입니다. 플레이어의 진행 경로를 막는 위치에 둡니다.")]
    private Transform targetPosition;
    [SerializeField, Min(0.01f), Tooltip("Open/Close 이동 애니메이션 시간입니다.")]
    private float moveDuration = 0.6f;
    [SerializeField, Tooltip("바리게이트 이동에 사용할 이징입니다.")]
    private Ease moveEase = Ease.InOutCubic;

    [Header("Initial State")]
    [SerializeField, Tooltip("켜면 씬 시작 시 닫힌 위치에서 시작합니다. 꺼두면 열린 위치에서 시작합니다.")]
    private bool startClosed = true;

    [Header("Debug Readout")]
    [SerializeField, Tooltip("현재 바리게이트 상태입니다. 런타임 확인용입니다.")]
    private BarrierState state = BarrierState.Closed;

    private Tween moveTween;
    private Transform Body => movingBody != null ? movingBody : transform;

    public bool IsOpen => state == BarrierState.Open;
    public bool IsClosed => state == BarrierState.Closed;
    public bool IsMoving => state == BarrierState.Opening || state == BarrierState.Closing;

    private void Awake()
    {
        ResolveSceneReferences();
        SetImmediate(startClosed);
    }

    private void Reset()
    {
        ResolveSceneReferences();
    }

    private void OnDestroy()
    {
        KillMoveTween();
    }

    /// <summary>
    /// 바리게이트를 닫고, 닫힘 애니메이션이 끝날 때까지 기다립니다.
    /// 이미 닫혀 있거나 닫는 중이면 중복 실행하지 않습니다.
    /// </summary>
    public IEnumerator CloseRoutine()
    {
        if (state == BarrierState.Closed || state == BarrierState.Closing)
        {
            yield break;
        }

        Tween tween = MoveTo(targetPosition, BarrierState.Closing, BarrierState.Closed);
        if (tween != null)
        {
            yield return tween.WaitForCompletion();
        }
    }

    /// <summary>
    /// 바리게이트를 열고, 열림 애니메이션이 끝날 때까지 기다립니다.
    /// 이미 열려 있거나 여는 중이면 중복 실행하지 않습니다.
    /// </summary>
    public IEnumerator OpenRoutine()
    {
        if (state == BarrierState.Open || state == BarrierState.Opening)
        {
            yield break;
        }

        Tween tween = MoveTo(startPosition, BarrierState.Opening, BarrierState.Open);
        if (tween != null)
        {
            yield return tween.WaitForCompletion();
        }
    }

    public void Close()
    {
        StartCoroutine(CloseRoutine());
    }

    public void Open()
    {
        StartCoroutine(OpenRoutine());
    }

    public void SetImmediate(bool closed)
    {
        KillMoveTween();

        Transform destination = closed ? targetPosition : startPosition;
        if (destination == null)
        {
            Debug.LogWarning($"[{nameof(MapBoxBarrier)}] {name} requires Start Position and Target Position.", this);
            return;
        }

        Body.position = destination.position;
        Body.rotation = destination.rotation;
        state = closed ? BarrierState.Closed : BarrierState.Open;
    }

    private Tween MoveTo(Transform destination, BarrierState movingState, BarrierState completedState)
    {
        if (destination == null)
        {
            Debug.LogWarning($"[{nameof(MapBoxBarrier)}] {name} cannot move because a position reference is missing.", this);
            return null;
        }

        KillMoveTween();
        state = movingState;

        moveTween = Body.DOMove(destination.position, moveDuration)
            .SetEase(moveEase)
            .OnComplete(() =>
            {
                Body.rotation = destination.rotation;
                state = completedState;
                moveTween = null;
            });

        return moveTween;
    }

    private void ResolveSceneReferences()
    {
        movingBody ??= transform;
    }

    private void KillMoveTween()
    {
        if (moveTween == null)
        {
            return;
        }

        moveTween.Kill();
        moveTween = null;
    }

    private void OnValidate()
    {
        moveDuration = Mathf.Max(0.01f, moveDuration);
    }
}

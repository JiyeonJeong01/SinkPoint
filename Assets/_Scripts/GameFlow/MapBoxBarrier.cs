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

    [Header("Audio")]
    [SerializeField, Tooltip("바리게이트가 열리기 시작할 때 한 번 재생할 사운드입니다.")]
    private AudioClip openSound;
    [SerializeField, Tooltip("비워두면 같은 오브젝트의 AudioSource를 자동으로 사용합니다.")]
    private AudioSource audioSource;
    [SerializeField, Range(0f, 1f)]
    private float openSoundVolume = 0.8f;

    [Header("Guide Light")]
    [SerializeField, Tooltip("바리게이트가 열렸을 때 점광원이 회전할 중심입니다. 비워두면 바리게이트 위치에 자동 생성합니다.")]
    private Transform guideLightCenter;
    [SerializeField, Tooltip("바리게이트가 열렸을 때 켜질 점광원입니다. 비워두면 자동 생성합니다.")]
    private Light guidePointLight;
    [SerializeField, Min(0f), Tooltip("중심에서 점광원이 도는 반지름입니다.")]
    private float guideLightOrbitRadius = 1.8f;
    [SerializeField, Tooltip("점광원의 중심 기준 높이입니다.")]
    private float guideLightHeight = 1.2f;
    [SerializeField, Tooltip("점광원이 초당 회전하는 각도입니다.")]
    private float guideLightOrbitSpeed = 110f;
    [SerializeField, Tooltip("점광원 색입니다.")]
    private Color guideLightColor = new Color(0.55f, 0.85f, 1f, 1f);
    [SerializeField, Min(0f), Tooltip("점광원 밝기입니다.")]
    private float guideLightIntensity = 4.5f;
    [SerializeField, Min(0f), Tooltip("점광원 범위입니다.")]
    private float guideLightRange = 8f;

    [Header("Debug Readout")]
    [SerializeField, Tooltip("현재 바리게이트 상태입니다. 런타임 확인용입니다.")]
    private BarrierState state = BarrierState.Closed;

    private Tween moveTween;
    private Transform guideLightTransform;
    private float guideLightAngle;
    private bool guideLightActive;
    private Transform Body => movingBody != null ? movingBody : transform;

    public bool IsOpen => state == BarrierState.Open;
    public bool IsClosed => state == BarrierState.Closed;
    public bool IsMoving => state == BarrierState.Opening || state == BarrierState.Closing;

    private void Awake()
    {
        ResolveSceneReferences();
        ResolveGuideLightReferences();
        SetImmediate(startClosed);
    }

    private void Reset()
    {
        ResolveSceneReferences();
        ResolveGuideLightReferences();
    }

    private void OnDestroy()
    {
        KillMoveTween();
    }

    private void Update()
    {
        if (!guideLightActive || guidePointLight == null || guideLightCenter == null)
        {
            return;
        }

        guideLightAngle += guideLightOrbitSpeed * Time.deltaTime;
        ApplyGuideLightPosition();
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
        SetGuideLightActive(!closed);
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
        if (movingState == BarrierState.Opening)
        {
            PlayOpenSound();
            SetGuideLightActive(true);
        }

        moveTween = Body.DOMove(destination.position, moveDuration)
            .SetEase(moveEase)
            .OnComplete(() =>
            {
                Body.rotation = destination.rotation;
                state = completedState;
                moveTween = null;
                SetGuideLightActive(completedState == BarrierState.Open);
            });

        return moveTween;
    }

    private void ResolveSceneReferences()
    {
        movingBody ??= transform;
        audioSource ??= GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;
    }

    private void ResolveGuideLightReferences()
    {
        if (guideLightCenter == null)
        {
            GameObject centerObject = new GameObject("GuideLight_OrbitCenter");
            centerObject.transform.SetParent(transform, false);
            centerObject.transform.localPosition = Vector3.zero;
            guideLightCenter = centerObject.transform;
        }

        if (guidePointLight == null)
        {
            GameObject lightObject = new GameObject("Guide Point Light");
            lightObject.transform.SetParent(guideLightCenter, false);
            guidePointLight = lightObject.AddComponent<Light>();
        }

        guideLightTransform = guidePointLight.transform;
        guidePointLight.type = LightType.Point;
        guidePointLight.color = guideLightColor;
        guidePointLight.intensity = guideLightIntensity;
        guidePointLight.range = guideLightRange;
        guidePointLight.shadows = LightShadows.None;
        SetGuideLightActive(false);
    }

    private void SetGuideLightActive(bool active)
    {
        guideLightActive = active;
        if (guidePointLight == null)
        {
            return;
        }

        guidePointLight.enabled = active;
        if (active)
        {
            ApplyGuideLightPosition();
        }
    }

    private void ApplyGuideLightPosition()
    {
        if (guideLightTransform == null || guideLightCenter == null)
        {
            return;
        }

        float radians = guideLightAngle * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(
            Mathf.Cos(radians) * guideLightOrbitRadius,
            guideLightHeight,
            Mathf.Sin(radians) * guideLightOrbitRadius);
        guideLightTransform.position = guideLightCenter.position + offset;
    }

    private void PlayOpenSound()
    {
        if (openSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(openSound, openSoundVolume);
        }
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
        guideLightOrbitRadius = Mathf.Max(0f, guideLightOrbitRadius);
        guideLightIntensity = Mathf.Max(0f, guideLightIntensity);
        guideLightRange = Mathf.Max(0f, guideLightRange);
#if UNITY_EDITOR
        if (openSound == null)
        {
            openSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(
                "Assets/Audios/Environment/SpookyDoor/SpookyDoor_1.wav");
        }
#endif
    }
}

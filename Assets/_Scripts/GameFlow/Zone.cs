using UnityEngine;

public enum ZoneId
{
    Zone01_Entry,
    Zone02_Normal,
    Zone03_GravityShift,
    Zone04_Inversion,
    Zone05_ZeroGravitySource
}

/// <summary>
/// 씬의 Zone 루트가 자기 정체성을 직접 가지게 하는 표식 컴포넌트입니다.
/// GameFlowManager와 Trigger는 숫자 인덱스 대신 ZoneId로 Zone을 찾습니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class Zone : MonoBehaviour
{
    [SerializeField, Tooltip("이 오브젝트가 대표하는 진행 Zone입니다.")]
    private ZoneId zoneId;

    public ZoneId Id => zoneId;
}

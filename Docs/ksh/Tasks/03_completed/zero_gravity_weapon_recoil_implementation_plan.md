# 무중력 무기 발사 반작용 구현 실행 계획

문서 작성일: 2026-08-25
현재 상태: 완료 — 코드·자동 컴파일·사용자 Play Mode 검증 완료

계획 프로필: `standard`

기준 문서:

- [SinkPoint MVP 게임 기획서](../../../GameDesign_MVP.md)
- [플레이어·중력 파트 마스터 계획](../../Player_Gravity_Master_Plan.md)
- [Zone 기반 중력 시스템 구현 완료 계획](../03_completed/gravity_zone_system_implementation_plan.md)

## 1. 목표

그래플링 훅이 없는 현재 무중력 구간에서도 무기 발사를 이용해 제한적으로 이동·조향·제동할 수 있게 한다.

실제 탄환이 발사될 때마다 발사 방향 반대로 질량과 무관한 속도 변화를 추가하되, 플레이어 전체 이동 속도에 상한을 적용해 연사로 무한 가속되지 않게 한다. 이 기능은 그래플 구현 후에도 무중력 보조 이동 수단으로 유지한다.

## 2. 범위

- 실제 발사마다 무중력 반작용 요청
- `PlayerController`의 무중력 상태·전환 상태 판정
- Rigidbody `VelocityChange` 기반 반작용
- 전체 이동 속도 상한과 상한 초과 상태의 제동 허용
- Inspector 설정과 런타임 관찰값
- 컴파일·Play Mode 회귀 검증

## 3. 하지 않을 것

- 그래플링 훅 또는 로프 물리 구현
- Zone·Trigger별 `GravityPreset` 연결
- 일반 중력 상태의 무기 반동이나 넉백
- 카메라 흔들림, 총기 시각 반동, 애니메이션, VFX와 사운드
- 명중·피해·탄약·연사 간격 변경
- 무중력 공중 제동이나 전역 Drag 추가
- `Original_GamePlayScene`, Collider, Packages, ProjectSettings 또는 Build Settings 변경

## 4. 현재 구현과 전제

- `PlayerCombatController.FireShot()`은 카메라 중심 조준점과 총구를 이용해 최종 `shotDirection`을 계산하고, 명중 여부와 무관하게 실제 발사 횟수를 확정한다.
- 현재 연사 간격은 `0.15초`다.
- `PlayerController`는 플레이어 Rigidbody와 `PlayerMotionStateMachine`의 현재 상태를 소유한다.
- `ZeroGravityMotionState`는 진입 시 선속도·각속도를 한 번 초기화하고 이후 물리 프레임에서 속도를 덮어쓰지 않는다.
- `PlayerController`는 중력 전환 중 여부를 이미 추적한다.
- `GravityManager`와 무중력 Preset의 기존 public 계약은 변경하지 않는다.

## 5. 책임 경계와 데이터 흐름

```text
PlayerCombatController.FireShot()
  └─ 실제 shotDirection 확정
      └─ PlayerController.TryApplyZeroGravityRecoil(-shotDirection)
          ├─ ZeroGravity 상태와 중력 전환 여부 검사
          ├─ 전체 속도 상한을 고려한 적용 속도 계산
          └─ Rigidbody.AddForce(delta, ForceMode.VelocityChange)
```

### 5.1 `PlayerCombatController`

- 실제 발사 시점과 발사 방향만 소유한다.
- `shotCount`가 증가하는 실제 발사마다 명중 여부와 관계없이 반작용을 한 번 요청한다.
- 몬스터 피해와 피격 Rigidbody 밀기 결과는 반작용 발동 여부에 영향을 주지 않는다.
- 같은 GameObject의 `PlayerController`를 `Awake()`에서 캐시한다. `[RequireComponent(typeof(PlayerController))]`로 필수 관계를 명시하고 별도 씬 참조는 추가하지 않는다.
- 반작용 크기, 무중력 판정과 속도 제한 로직은 소유하지 않는다.

### 5.2 `PlayerController`

다음 공개 진입점을 추가한다.

```csharp
public bool TryApplyZeroGravityRecoil(Vector3 recoilDirection)
```

- 적용했으면 `true`, 상태나 입력이 유효하지 않아 적용하지 않았으면 `false`를 반환한다.
- 활성화 설정이 꺼졌거나, 현재 상태가 `ZeroGravity`가 아니거나, 중력 전환 중이면 적용하지 않는다.
- NaN·Infinity·영벡터 방향과 0 이하 반작용 크기 또는 속도 상한은 적용하지 않는다.
- 호출자가 전달한 방향을 정규화하며, `PlayerCombatController`는 `-shotDirection`을 전달한다.
- Rigidbody와 설정값, 현재 전체 속도, 상한 계산을 단독 소유한다.

## 6. 반작용과 속도 상한 계약

초기 Inspector 값은 다음으로 둔다.

- `enableZeroGravityRecoil = true`
- `zeroGravityRecoilVelocityChange = 0.3f`
- `maxZeroGravityRecoilSpeed = 3.0f`

적용 계산은 다음 순서를 사용한다.

1. `currentVelocity = body.linearVelocity`
2. `requestedDelta = recoilDirection.normalized * zeroGravityRecoilVelocityChange`
3. `candidateVelocity = currentVelocity + requestedDelta`
4. 현재 속력이 상한 이하라면 `candidateVelocity`를 최대 속력까지 `ClampMagnitude`한다.
5. 현재 속력이 이미 상한을 초과했다면 후보 속력이 현재 속력보다 작거나 같은 경우에만 허용한다. 현재 속력을 더 키우는 요청은 적용하지 않는다.
6. `appliedDelta = resolvedVelocity - currentVelocity`를 구하고, 유효한 변화가 있을 때만 `body.AddForce(appliedDelta, ForceMode.VelocityChange)`를 호출한다.

이 규칙으로 다음 동작을 보장한다.

- 상한 이하에서는 연사로 가속할 수 있지만 `3.0`을 넘지 않는다.
- 상한에서 다른 방향을 쏘면 속력 상한 안에서 방향을 바꿀 수 있다.
- 외부 힘 때문에 이미 상한을 넘은 속도를 반작용 시스템이 강제로 `3.0`으로 잘라내지 않는다.
- 상한 초과 상태에서도 현재 속력을 낮추는 반대 방향 발사는 허용한다.
- 반작용이 현재 속력을 더 키우는 경우에는 아무 힘도 추가하지 않는다.

## 7. Inspector와 런타임 관찰값

`PlayerController`의 기본 Inspector에 직렬화 필드로 다음 항목을 구분해 표시한다.

- 설정: 활성화 여부, 발사당 속도 변화, 최대 전체 속도
- 런타임: 마지막 요청 적용 여부, 현재 전체 속도, 속도 상한 도달 여부

런타임 값은 Play Mode에서 발사 시 갱신하고, 무중력 이탈이나 중력 전환으로 요청이 거부된 경우에도 마지막 요청 결과가 드러나게 한다. 관찰값은 게임 로직의 정본으로 사용하지 않는다.

## 8. 예상 변경 파일

- `Assets/_Scripts/Player/PlayerCombatController.cs`
- `Assets/_Scripts/Player/PlayerController.cs`

Player Prefab에서 두 컴포넌트가 같은 GameObject에 있음을 확인했으므로 씬, Prefab과 `GravityPreset` 데이터는 변경하지 않는다.

## 9. 실행 순서

1. `[PlayerController 반작용 API와 설정값 구현]` → verify: `[일반 중력·전환 중·잘못된 방향 요청이 Rigidbody를 바꾸지 않음]`
2. `[전체 속도 상한과 상한 초과 제동 규칙 구현]` → verify: `[상한 이하 가속, 상한 제한, 초과 상태 가속 차단과 감속 허용]`
3. `[FireShot의 실제 발사마다 -shotDirection 요청 연결]` → verify: `[명중·빗나감 모두 발사당 정확히 1회 요청]`
4. `[Inspector 설정·런타임 관찰값 추가]` → verify: `[Play Mode에서 적용 여부·현재 속도·상한 상태 확인 가능]`
5. `[컴파일과 좁은 자동 검증]` → verify: `[런타임·Editor 어셈블리 오류 0건, 기존 사격·중력 동작 회귀 없음]`
6. `[사용자 Play Mode 체감 검증]` → verify: `[반작용 크기와 최대 속도가 이동·조향·제동 수단으로 적절함]`

## 10. Play Mode 테스트

### 정상 경로

- Normal·Shift·Periodic 같은 중력 세기 `0`이 아닌 상태에서 발사해도 플레이어 속도가 변하지 않는다.
- Zero Gravity 진입 직후 빗나가는 단발을 쏘면 발사 방향 반대로 속도가 생긴다.
- 물체나 몬스터를 맞혀도 플레이어 반작용은 동일하게 한 번 적용된다.
- `0.1초` 간격으로 연사해도 전체 속력이 `3.0`을 넘지 않는다.
- 정지 상태에서 반대 방향으로 나눠 발사하면 이동 방향을 바꿀 수 있다.
- 이동 방향으로 발사해 반작용이 현재 진행 반대가 되면 감속할 수 있다.

### 경계·실패 경로

- 속력 상한에서 추가 가속 방향의 발사는 속력을 늘리지 않는다.
- 외부 설정으로 상한을 넘긴 상태에서는 가속 요청을 막지만 감속 요청은 허용한다.
- 중력 전환 도중 발사해도 반작용이 적용되지 않으며 전환 종료 후 정상적으로 다시 적용된다.
- Zero Gravity를 벗어나면 이후 발사 반작용이 즉시 중단된다.
- 리스폰으로 속도와 현재 Preset을 복구한 뒤에도 무중력 여부에 맞게 동작한다.
- 빠른 연사, 상태 전환과 리스폰 반복 후 NaN 속도, 상태 고착과 Console 오류가 없다.

### 회귀 확인

- 기존 카메라 조준 Ray, 총구 Ray, 명중 피해와 피격 Rigidbody 밀기가 변하지 않는다.
- Zero Gravity 진입 1회 속도 초기화와 `GravityBody` 관성 계약이 유지된다.
- Periodic 실행·취소, Player·Camera 중력 전환과 리스폰 복구가 기존처럼 동작한다.

## 11. 완료 기준

- 실제 발사마다 명중 여부와 관계없이 발사 방향 반대로 반작용이 적용된다.
- 반작용은 무중력 상태에서만, 중력 전환 중이 아닐 때만 동작한다.
- 전체 속력 상한을 지키면서 조향과 제동이 가능하다.
- 초기값 `0.3 / 3.0`을 Inspector에서 조정하고 런타임 결과를 관찰할 수 있다.
- 런타임·Editor 어셈블리 컴파일 오류와 신규 Console 오류가 없다.
- 기존 사격·중력·리스폰 동작에 회귀가 없다.
- 사용자가 Play Mode에서 조작감과 동작을 확인하기 전에는 이 계획을 완료 처리하지 않는다.

## 12. 문서 상태 관리

- 구현을 시작하면 이 문서를 `Docs/ksh/Tasks/02_in-progress`로 이동한다.
- 구현·자동 검증 결과를 각 단계 아래에 기록하되 사용자 Play Mode 확인과 구분한다.
- 사용자 확인까지 완료되면 `Docs/ksh/Codex_Usage_Records.md`에 하나의 완료 작업 단위로 기록하고 `Docs/ksh/Tasks/03_completed`로 이동한다.

## 13. 구현·검증 기록

2026-08-25 코드 구현과 자동 검증:

1. `PlayerController.TryApplyZeroGravityRecoil(Vector3)`와 Inspector 설정값을 구현했다. 비활성화, 비무중력 상태, 중력 전환 중, 유효하지 않은 방향·설정값은 `false`로 거부한다.
2. 현재 전체 속력이 상한 이하일 때 후보 속도를 `ClampMagnitude`하고, 이미 상한을 넘은 경우 현재 속력을 늘리지 않는 요청만 허용하도록 구현했다.
3. `PlayerCombatController.FireShot()`에서 실제 `shotCount` 증가 직후 `-shotDirection` 반작용을 한 번 요청하도록 연결했다. 명중·피해·피격 Rigidbody 밀기 분기와 독립적이다.
4. 마지막 요청 적용 여부, 현재 전체 속력과 상한 도달 여부를 `PlayerController` Inspector 런타임 값으로 추가했다.
5. `dotnet build Assembly-CSharp.csproj --no-restore`: 오류 0건. 기존 외부 에셋·타 파트 경고 28건이며 이번 변경의 신규 경고는 없다.
6. `dotnet build Assembly-CSharp-Editor.csproj --no-restore`: 오류 0건, 경고 0건.
7. 최신 `Editor.log` 범위에서 `error CS`, 컴파일 실패와 예외 문자열이 없음을 확인했다.
8. 사용자가 Play Mode 테스트 성공을 확인했다. 무중력 상태가 아닐 때 반작용이 발동하는 버그는 테스트 범위에서 발견되지 않았다.

확인된 기존 값:

- `PlayerCombatController` 코드 초기값은 `fireInterval = 0.1f`지만 Player Prefab 직렬화 값은 `0.15`다. 이번 작업은 연사 간격을 변경하지 않았다.

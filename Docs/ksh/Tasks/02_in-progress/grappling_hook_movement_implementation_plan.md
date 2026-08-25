# 그래플링 훅 이동 시스템 구현 실행 계획

문서 작성일: 2026-08-25

현재 상태: 구현 진행 중 — 입력·그래플 상태·Rigidbody 당김·Prefab/Scene 연결 구현 착수

계획 프로필: `deep`

기준 문서:

- [SinkPoint MVP 게임 기획서](../../../GameDesign_MVP.md)
- [플레이어·중력 파트 마스터 계획](../../Player_Gravity_Master_Plan.md)
- [무중력 무기 발사 반작용 구현 실행 계획](../03_completed/zero_gravity_weapon_recoil_implementation_plan.md)
- [TPS 이동형 Tracer Bolt 빠른 검증 계획](../03_completed/tps_moving_tracer_bolt_test_plan.md)

## 1. 목표

우클릭을 누르면 카메라 중심으로 정적 지형을 조준하고, 총구에서 선 형태의 훅이 실제 발사 속도를 가지고 목표점까지 날아간 뒤 플레이어를 끌어당기는 MVP 그래플링 훅을 구현한다.

그래플은 Zone 05 무중력 구간의 주 이동 수단이면서, 일반·방향성 중력 상태에서도 중력을 거슬러 위쪽 지형으로 올라갈 수 있는 보조 이동 수단으로 동작한다. 실제 훅 모델, 로프·진자 시뮬레이션과 스윙은 만들지 않고, 현재 Rigidbody 플레이어 이동과 충돌을 보존하는 속도 제어형 당김까지만 구현한다.

핵심 사용자 동작은 다음과 같다.

```text
우클릭 누름
  → 카메라 중심 조준과 총구 기준 최종 표면 확정
  → LineRenderer 훅이 저장한 목표점까지 속도 기반 이동
  → 도착 후 Player Rigidbody 당김 시작
  → 우클릭 해제 / 목표 도착 / 시간 초과 / 상태 취소 시 종료
```

그래플을 누르고 있는 동안에는 그래플이 좌클릭 사격보다 우선한다. 새 총알은 발사하지 않으며, 그래플이 끝난 뒤에는 좌클릭을 새로 눌러야 다시 사격한다.

## 2. 확정한 설계 결정

- 입력은 우클릭 홀드 방식이다.
- `우클릭 Down`이 한 번의 발사를 시작하고, `우클릭 Held`가 `Launching`과 `Pulling`을 유지하며, `우클릭 Up`이 즉시 취소한다.
- 쿨다운은 두지 않는다. 해제 후 다시 누르면 항상 새 발사를 시작하며 한 번에 그래플 하나만 존재한다.
- 목표는 발사 순간 Raycast로 먼저 확정하지만, 당김은 선 끝이 저장된 목표점에 도착한 뒤 시작한다.
- 훅의 비행 시간은 시각 전용이 아니라 실제 당김 시작 지연으로 사용한다.
- 그래플은 일반·방향성·주기형·무중력 상태에서 사용할 수 있지만 중력 표현 전환 중에는 시작하거나 유지하지 않는다.
- 실제 로프·스프링·진자 상태를 만들지 않고 목표 방향 속도 모터를 사용한다.
- `GrapplingHook`은 입력, 조준, 상태, 목표와 LineRenderer를 소유한다.
- `PlayerController`는 Rigidbody, 당김 속도, 도착 판정과 종료 시 속도 정리를 소유한다.
- `PlayerMotionStateMachine`에는 `Grappling` 상태를 추가하지 않는다. 그래플은 기존 `Grounded / Airborne / ZeroGravity` 위에 적용되는 이동 오버레이다.
- 그래플 중에는 새 좌클릭 사격만 막는다. 수동·자동 재장전, 카메라 Look과 기존 이동 입력은 별도 규칙을 추가하지 않고 유지한다.
- 좌·우클릭이 같은 프레임에 들어오면 그래플이 우선한다.
- 그래플 중 좌클릭은 그래플을 취소하지 않는다.
- 기존 사격 Tracer와 그래플 선은 동시에 같은 `LineRenderer`를 공유하지 않는다.

## 3. 범위

- `PlayerInput`의 우클릭 Held 상태 제공
- 정적 지형을 찾는 카메라 중심·총구 기준 2단계 Raycast
- `Idle / Launching / Pulling` 그래플 상태
- 발사 속도를 가진 LineRenderer 훅 이동과 연결선 유지
- 일반·방향성 중력에서 중력을 이길 수 있는 Rigidbody 당김
- 무중력에서 관성을 이용한 이동·방향 전환·제동 보조
- 최대 당김 속도, 도착 안전 거리와 시간 초과
- Trigger, 플레이어 자신, 몬스터와 동적 Rigidbody를 유효 앵커에서 제외
- 입력 해제, 입력 잠금, 중력 전환, 사망·리스폰과 비활성화 시 상태 정리
- 그래플 우선 좌클릭 사격 차단과 새 클릭 요구
- Player Prefab의 별도 그래플 LineRenderer와 참조 구성
- `GamePlayScene_Player`의 카메라 참조 연결
- Inspector 설정·런타임 관찰값
- 컴파일, Prefab·Scene 직렬화, 자동 Play Mode 계측과 사용자 Play Mode 검증
- 확정된 일반 중력 그래플 범위를 마스터 계획에 반영

## 4. 하지 않을 것

- 실제 훅·총 모델, 손·무기 교체와 그래플 전용 애니메이션
- 실제 투사체 GameObject, Rigidbody 훅과 충돌 콜백
- `SpringJoint`, `ConfigurableJoint`, 로프 분절, 줄 감기와 진자·스윙 물리
- 곡선 로프, 장애물 모서리 감기와 연결 중 경로 재탐색
- 움직이는 Rigidbody, 몬스터, 이동 발판을 따라가는 동적 앵커
- 그래플로 몬스터나 물체를 플레이어 쪽으로 당기는 기능
- 그래플 중 사격 허용, 반작용 상쇄·보정과 공중 전투 완성도
- 조준 가능 표면 하이라이트, 크로스헤어 색 변화와 신규 HUD
- 그래플 전용 VFX, SFX, 카메라 흔들림, FOV 연출과 후처리
- 쿨다운, 횟수 제한, 자원·스태미나와 업그레이드 시스템
- 범용 능력·액션 조정자 또는 공통 투사체 프레임워크
- 팀장 소유 `Assets/_Scenes/Original_GamePlayScene.unity` 수정
- 기존 지형·Trigger Collider 위치, 회전, 크기와 재질 변경
- `Packages`, `ProjectSettings`, Build Settings와 active build target 변경
- 별도 승인 없는 WebGL 빌드

## 5. 현재 상태와 근거

### 5.1 입력

- `Assets/_Scripts/Input/PlayerInput.cs`가 모든 플레이어 입력의 단일 입구다.
- 현재 `AllowGrapple`과 우클릭 Down 기반 `GrapplePressed`가 이미 있다.
- 대화 입력은 그래플을 차단하고, 컷신 입력은 모든 입력을 차단한다.
- 홀드 유지와 해제를 판단할 `GrappleHeld`는 아직 없다.

### 5.2 조준과 사격

- `ThirdPersonCameraController.CreateCenterRay()`가 실제 Gameplay Camera의 화면 중앙 Ray를 제공한다.
- `PlayerCombatController`는 카메라 중심 aim point를 구한 뒤 총구에서 다시 검사해 가까운 장애물을 우선하는 2단계 조준을 사용한다.
- 사격은 `LateUpdate()`에서 `FirePressed`로 연사를 시작하고 `FireHeld`로 유지한다.
- `StopFiring()`은 연사 상태와 다음 발사 시각을 초기화한다. 따라서 그래플 중 한 번 호출하면, 좌클릭을 계속 누르고 있어도 그래플 종료 뒤 `FirePressed`가 새로 들어오기 전에는 연사가 재개되지 않는다.
- 기존 이동형 Shot Tracer는 `Muzzle`의 단일 `LineRenderer`와 `M_ShotTracer` 머티리얼을 사용한다.

### 5.3 플레이어 물리와 중력

- `PlayerController`가 플레이어 Rigidbody, 이동 속도, 사용자 정의 중력, Ground Probe, 중력 전환 위치 잠금과 무중력 반작용을 소유한다.
- Rigidbody 변경은 `FixedUpdate()` 경로에서 처리한다.
- `Grounded`와 `Airborne` 이동은 매 물리 프레임 속도를 다시 구성하므로, 외부 컴포넌트가 별도로 당김 힘만 추가하면 다음 프레임에 목표 방향 속도가 지워질 수 있다.
- `ZeroGravityMotionState`는 일반 이동 속도를 덮어쓰지 않아 현재 관성과 무기 반작용을 유지한다.
- 중력 전환 중 `PlayerController`는 위치와 속도를 잠그므로 그래플과 동시에 유지할 수 없다.
- Player Prefab은 보간과 연속 충돌 검사를 사용하는 비키네마틱 Rigidbody와 CapsuleCollider를 이미 가진다.

### 5.4 사망과 리스폰

- `PlayerHealth.Died`가 사망을 알리고 `GameFlowManager`가 즉시 `RespawnController`를 호출한다.
- 리스폰은 Rigidbody 선속도·각속도를 비우고 Player 위치·회전을 옮기지만 그래플 상태는 알지 못한다.
- 따라서 `GrapplingHook`이 `Died`에 구독해 리스폰 위치 이동 전에 선과 당김 요청을 정리한다.

### 5.5 기존 XR 그래플 코드

- `Assets/XRI Starter Kit/Assets/WIP/GrapplingGun.cs`는 XR 입력, 훅 Prefab, Coroutine과 `transform.position` 이동을 전제로 한다.
- 현재 TPS 입력·Rigidbody 소유권과 맞지 않으므로 복사하거나 런타임 의존하지 않는다.

## 6. 필요한 가정과 문서 정합성

- 그래플의 주 목적은 Zone 05 무중력 구간 통과지만, 사용자가 일반 중력에서도 위로 올라가는 용도를 명시적으로 승인했다.
- 기존 마스터 계획의 “무중력 그래플” 표현보다 사용 범위가 넓다. 구현과 함께 기술 방향·단계 3 설명을 “일반·방향성 중력 보조 이동 및 무중력 핵심 이동”으로 좁게 갱신한다.
- 게임 기획서의 무중력 그래플 최소 요구사항은 그대로 충족하므로 `Docs/GameDesign_MVP.md`의 범위는 변경하지 않는다.
- 그래플 가능한 표면은 현재 Layer를 재사용한다. 새 Tag·Layer와 `ProjectSettings/TagManager.asset` 변경은 필요하지 않다.
- 정적 지형은 연결 중 이동하지 않는다. 목표점과 표면 노멀은 발사 시 월드 좌표로 저장한다.
- 그래플 중 기존 WASD 입력은 유지하되 새 공중 조향 규칙을 만들지 않는다. PlayerController의 기존 접선 이동과 그래플 목표 방향 속도를 합성한다.
- 플레이어가 목표 표면까지 정확히 접촉할 필요는 없다. Capsule 중심과 표면 사이에 안전 거리를 남기고 도착 처리한다.
- 초기 수치는 첫 Play Mode를 위한 출발점이며 최종 체감은 사용자가 Inspector에서 조정한 뒤 확정한다.

## 7. 책임 경계와 데이터 흐름

```text
PlayerInput.Update()
  ├─ GrapplePressed: 우클릭 Down
  └─ GrappleHeld: 우클릭 유지

GrapplingHook.LateUpdate()
  ├─ 입력·상태 취소 조건 검사
  ├─ Camera Center Ray로 aim point 계산
  ├─ Muzzle Ray로 실제 첫 표면과 앵커 유효성 확정
  ├─ LineRenderer 끝점을 발사 속도로 이동
  └─ 도착 시 PlayerController.TryBeginGrapplePull(anchor, normal)

PlayerController.FixedUpdate()
  ├─ 기존 Grounded / Airborne / ZeroGravity 선택 유지
  ├─ 기존 이동·중력 계산
  ├─ 활성 그래플의 목표 방향 속도 오버레이 적용
  ├─ 최대 당김 속도와 안전 거리 판정
  └─ 도착 시 표면 안쪽 속도 제거 후 당김 종료

PlayerCombatController.LateUpdate()
  ├─ 기존 재장전 처리 유지
  ├─ GrappleHeld 또는 GrapplingHook.IsBusy이면 StopFiring()
  └─ 그래플이 아닐 때만 기존 FirePressed / FireHeld 처리
```

### 7.1 `PlayerInput`

다음 읽기 전용 값을 추가한다.

```csharp
public bool GrappleHeld { get; private set; }
```

- `GrapplePressed = allowGrapple && Input.GetMouseButtonDown(1)`은 유지한다.
- `GrappleHeld = allowGrapple && Input.GetMouseButton(1)`을 같은 입력 읽기 단계에서 갱신한다.
- `allowGrapple == false`이면 두 값 모두 `false`가 된다.
- 다른 플레이어 스크립트에서 `Input.GetMouseButton*`을 직접 호출하지 않는다.
- 별도 `GrappleReleased`는 추가하지 않고 `IsBusy && !GrappleHeld`로 해제를 판단한다.

### 7.2 `GrapplingHook`

새 `Assets/_Scripts/Player/GrapplingHook.cs`를 추가한다.

- `[DefaultExecutionOrder(110)]`으로 Player의 시각 회전과 기존 사격 갱신 뒤에 LineRenderer 시작점을 최종 총구 위치에 맞춘다.
- `[RequireComponent]`로 같은 GameObject의 `PlayerInput`, `PlayerController`, `PlayerHealth` 관계를 명시한다.

필수 참조:

- 같은 GameObject의 `PlayerInput`
- 같은 GameObject의 `PlayerController`
- 같은 GameObject의 `PlayerHealth`
- `ThirdPersonCameraController`
- 현재 Player 모델의 `Muzzle` 또는 `MuzzleVfxAnchor`
- 그래플 전용 `LineRenderer`

소유 상태:

```csharp
private enum GrappleState
{
    Idle,
    Launching,
    Pulling,
}
```

외부 읽기 계약:

```csharp
internal bool IsBusy { get; }
```

- `Idle`이 아니면 `true`다.
- `PlayerCombatController`는 물리나 목표 데이터에 접근하지 않고 이 값과 `PlayerInput.GrappleHeld`만 사용한다.
- `OnDisable`, 사망과 모든 취소 경로는 LineRenderer를 숨기고 `PlayerController.CancelGrapplePull()`을 호출한 뒤 `Idle`로 돌아간다.
- `OnEnable / OnDisable`에서 `PlayerHealth.Died` 구독 수명을 대칭으로 관리한다.
- 카메라·총구·LineRenderer 같은 필수 참조가 없으면 명확한 Error를 남기고 컴포넌트를 비활성화한다.

### 7.3 `PlayerController`

그래플 물리의 내부 진입점과 상태를 추가한다.

```csharp
internal bool TryBeginGrapplePull(Vector3 anchorPoint, Vector3 surfaceNormal)
internal void CancelGrapplePull()
internal bool IsGrapplePullActive { get; }
```

- `TryBeginGrapplePull()`은 유효한 좌표·노멀, 비전환 상태와 활성 Rigidbody를 검사한다.
- 앵커, 표면 노멀과 현재 당김 속도를 `PlayerController`가 저장한다. Pulling 경과 시간과 시간 초과는 `GrapplingHook`이 소유한다.
- `CancelGrapplePull()`은 그래플 내부 상태만 초기화하며 임의로 Player 전체 속도를 0으로 만들지 않는다.
- 중력 전환 시작 시 기존 위치 잠금 전에 그래플 당김을 취소한다.
- `GrapplingHook`은 Rigidbody와 `linearVelocity`에 직접 접근하지 않는다.

### 7.4 `PlayerCombatController`

- 같은 GameObject의 `GrapplingHook`을 캐시한다.
- 기존 재장전 진행·시작 판정은 유지한다.
- 새 총알 발사 판단 전에 `input.GrappleHeld || grapplingHook.IsBusy`를 검사한다.
- 조건이 참이면 `StopFiring()` 후 반환한다.
- 우클릭과 좌클릭이 같은 렌더 프레임에 시작돼도 `GrappleHeld`가 참이므로 그래플이 우선한다.
- 그래플 전에 이미 발사된 한 발의 피해, Muzzle Flash와 이동형 Tracer 수명은 취소하지 않는다.
- 그래플은 현재 재장전을 취소하지 않고, 재장전도 그래플을 취소하지 않는다.
- 그래플 종료 후 좌클릭이 계속 Held여도 `FirePressed`가 새로 발생하지 않으므로 연사를 자동 재개하지 않는다.

## 8. 조준과 앵커 판정 계약

발사 순간 다음 두 검사를 순서대로 수행한다.

```text
1. Camera Center Ray
   → 사용자가 바라보는 aim point 또는 최대 사거리 종점

2. Muzzle → aim point Ray
   → 총구에서 실제로 처음 만나는 Collider와 최종 endpoint
```

- 두 검사 모두 재사용 `RaycastHit[16]` 버퍼, `Physics.DefaultRaycastLayers`와 `Physics.RaycastNonAlloc`을 사용한다.
- `QueryTriggerInteraction.Ignore`로 Trigger를 제외한다.
- 결과 순서는 보장되지 않으므로 플레이어 자식 Collider를 제외한 최단 hit를 직접 고른다.
- 최종 첫 hit가 플레이어 자신이면 제외하고 다음 물리 표면을 찾는다.
- 최종 첫 blocker가 유효하지 않은 표면이면 그 뒤 지형을 관통해 앵커로 선택하지 않는다.
- 유효 앵커는 `grappleSurfaceMask`에 포함되고 `attachedRigidbody == null`인 정적 지형이다. Rigidbody가 붙은 kinematic 이동 발판도 이번 MVP에서는 제외한다.
- Monster, `MonsterAttack`, 동적 `GravityBody`와 일반 동적 Rigidbody에는 연결하지 않는다.
- 유효하지 않은 Collider를 맞히거나 아무것도 맞히지 못하면 당김을 시작하지 않는다.
- 실패 발사도 endpoint까지 선이 날아간 뒤 사라져 입력이 들어왔다는 피드백을 제공한다.
- 유효 hit에서는 `point`, `normal`, `collider`를 저장한다. 연결 전에 Collider가 사라지거나 비활성화되면 취소한다.
- 연결된 지형은 정적이라는 가정 아래 Pulling 중 매 프레임 재 Raycast하거나 목표점을 갱신하지 않는다.

## 9. 그래플 상태 계약

### 9.1 `Idle`

- LineRenderer와 PlayerController 당김 상태가 비활성이다.
- `GrapplePressed`가 들어오고 입력·생명·중력 전환 상태가 유효하면 한 번 발사한다.
- 우클릭을 계속 누른 상태에서 실패가 끝나도 자동 재발사하지 않는다. 반드시 해제 후 다시 눌러야 한다.

### 9.2 `Launching`

- 발사 순간의 월드 `launchOrigin`, 최종 `endpoint`, 거리, 방향과 유효 앵커 여부를 저장한다.
- 진행률은 `travelled = hookLaunchSpeed * elapsed`를 전체 거리에 대해 정규화해 계산한다.
- LineRenderer 시작점은 매 `LateUpdate()` 현재 총구를 따라간다.
- LineRenderer 끝점은 고정된 `launchOrigin`에서 저장한 `endpoint`까지 이동한다.
- 플레이어가 발사 중 움직여도 훅 머리의 월드 이동 경로는 발사 당시 경로를 유지하고, 선 시작점만 현재 총구를 따라간다.
- endpoint 도착 시 유효 앵커면 `TryBeginGrapplePull()`을 호출하고 성공하면 `Pulling`으로 전환한다.
- 실패 endpoint이거나 PlayerController가 요청을 거부하면 선을 숨기고 `Idle`로 돌아간다.
- 발사 중 우클릭 해제, 입력 차단, 사망, 중력 전환과 비활성화는 즉시 취소한다.

### 9.3 `Pulling`

- LineRenderer는 현재 총구와 저장한 anchor point를 연결한다.
- `PlayerController.IsGrapplePullActive`가 참인 동안 유지한다.
- 우클릭 해제, 입력 차단, 사망, 중력 전환, target Collider 무효와 최대 당김 시간 초과 시 취소한다.
- PlayerController가 도착 판정으로 당김을 종료하면 LineRenderer를 숨기고 `Idle`로 돌아간다.
- 연결 중 새 우클릭 Down은 발생할 수 없으므로 기존 연결을 재지정하지 않는다. 해제 후 새 Down이 새 발사를 시작한다.

## 10. 당김 물리 계약

초기 Inspector 값은 다음으로 시작한다.

`GrapplingHook`:

- `maxGrappleRange = 30f`
- `hookLaunchSpeed = 40f`
- `maxPullDuration = 3f`
- `grappleSurfaceMask = Default | WalkableSurface | Obstacle`
- `grappleLineColor = cyan 계열`

`PlayerController`:

- `grapplePullAcceleration = 30f`
- `maxGrapplePullSpeed = 12f`
- `grappleStopDistance = 1.1f`

적용 원칙:

1. 실제 물리 목표는 `arrivalPoint = anchorPoint + surfaceNormal * grappleStopDistance`다.
2. 당김 방향은 Player Rigidbody 중심에서 `arrivalPoint`까지 계산한다.
3. 그래플은 별도 누적 당김 속도를 0에서 최대 속도까지 `grapplePullAcceleration * fixedDeltaTime`만큼 증가시킨다.
4. 기존 이동·중력 계산 뒤 목표 방향 속도 성분을 그래플 당김 속도로 합성한다.
5. 목표 방향에 수직인 기존 속도 성분은 보존해 WASD 접선 이동, 무중력 관성과 외부 힘을 모두 제거하지 않는다.
6. 일반·방향성 중력에서 목표 방향 성분이 다음 물리 프레임에 지워지지 않도록 당김 속도는 PlayerController가 별도 상태로 유지한다.
7. 최대 속도는 그래플이 만든 목표 방향 성분에 적용하며 외부 힘으로 생긴 모든 속도를 전역 Clamp하지 않는다.
8. Player 중심이 `arrivalPoint`에 도달하거나 이번 물리 Step에서 지나칠 거리면 도착 처리한다.
9. 도착 시 anchor 표면 안쪽으로 향하는 속도 성분만 제거하고 접선 속도와 표면 바깥쪽 속도는 보존한다.
10. 수동 취소와 시간 초과에서는 현재 속도를 강제로 지우지 않고 그래플 가속만 중단한다.

이 규칙으로 일반 중력에서는 `30m/s²` 출발 가속이 `9.81m/s²` 중력을 이겨 위쪽 앵커로 이동할 수 있고, 무중력에서는 같은 설정이 빠른 이동·방향 전환 수단이 된다. 수치가 너무 강하거나 충돌이 불안정하면 구조를 바꾸기 전에 가속도, 최대 속도와 정지 거리를 Inspector에서 순서대로 조정한다.

## 11. 사격 배타성과 입력 우선순위

| 현재 입력·상태 | 결과 |
|---|---|
| 좌클릭만 Down/Held | 기존 사격·연사 |
| 우클릭 Down/Held | 그래플 발사·유지 |
| 같은 프레임 좌·우클릭 Down | 그래플 우선, 새 총알 없음 |
| 사격 연사 중 우클릭 Down | 이미 확정된 마지막 한 발은 유지, 이후 연사 중단 |
| 그래플 중 좌클릭 Down/Held | 사격 무시, 그래플 유지 |
| 그래플 해제 후 좌클릭 계속 Held | 자동 사격 없음 |
| 그래플 해제 후 새 좌클릭 Down | 사격 재개 |
| 그래플 중 R | 기존 재장전 규칙 유지 |

- `PlayerInput.AllowCombat`을 그래플이 임의로 끄거나 복구하지 않는다.
- 대화·컷신이 소유한 입력 잠금을 그래플 종료가 풀지 않는다.
- 사격 배타성은 `PlayerCombatController` 내부 발사 조건으로만 적용한다.
- 그래플 중 사격 허용은 초기 MVP 검증 이후 별도 결정으로 남긴다. 허용할 경우 무중력 반작용과 당김의 힘 합성 정책을 함께 설계해야 한다.

## 12. LineRenderer와 Prefab 구성

Player Prefab에 기존 Shot Tracer와 분리된 `GrappleRope` 자식을 추가한다.

- `LineRenderer.useWorldSpace = true`
- `positionCount = 2`
- 기본 비활성
- 그림자·조명 데이터 생성 비활성
- 기존 `Assets/_Custom/Material/M_ShotTracer.mat` 재사용
- Shot Tracer보다 구분되는 청록색 start/end color
- 초기 폭 `0.025 ~ 0.03m` 후보
- 발사와 당김 모두 같은 그래플 LineRenderer 하나를 재사용

`GrapplingHook` 컴포넌트는 Player 루트에 추가하고 다음을 연결한다.

- `input`, `playerController`, `playerHealth`: 같은 GameObject
- `muzzle`: 현재 `MuzzleVfxAnchor` 우선, 없으면 `Muzzle`
- `grappleLine`: 새 `GrappleRope` LineRenderer
- `aimCamera`: `GamePlayScene_Player`의 `ThirdPersonCameraController`

Prefab의 `aimCamera`는 Scene 외부 참조이므로 비워둘 수 있다. `GamePlayScene_Player`의 Player Prefab instance override에서 명시적으로 연결하고, 단독 Prefab 테스트를 위해 비어 있을 때 `ThirdPersonCameraController` 하나를 찾는 기존 스타일의 fallback을 허용한다. 연결된 Inspector 참조를 fallback이 덮어쓰지 않는다.

## 13. Inspector 런타임 관찰값

`GrapplingHook`에 다음 값을 Play Mode에서 확인 가능하게 둔다.

- 현재 `GrappleState`
- 마지막 발사의 유효 앵커 여부
- 현재 또는 마지막 target Collider
- 저장한 anchor point와 surface normal
- 발사 전체 거리와 Launch 진행률
- Pulling 경과 시간
- 종료 사유: Release, Miss, Arrived, Timeout, InputBlocked, GravityTransition, Died, TargetInvalid, Disabled

`PlayerController`에 다음 값을 추가한다.

- 그래플 당김 활성 여부
- 현재 그래플 당김 속도
- 현재 arrival point까지 거리
- 마지막 도착 판정 여부

런타임 관찰값은 Inspector 표시와 검증용이며 게임 로직의 정본으로 다시 읽지 않는다.

## 14. 예상 변경 파일

필수:

- `Assets/_Scripts/Input/PlayerInput.cs`
- `Assets/_Scripts/Player/GrapplingHook.cs`
- `Assets/_Scripts/Player/GrapplingHook.cs.meta`
- `Assets/_Scripts/Player/PlayerController.cs`
- `Assets/_Scripts/Player/PlayerCombatController.cs`
- `Assets/_Custom/Prefabs/Player/Player.prefab`
- `Assets/_Scenes/GamePlayScene_Player.unity`
- `Docs/ksh/Player_Gravity_Master_Plan.md`
- 이 실행 계획서

필요한 경우에만:

- `Assets/_Scripts/Player/PlayerAnimationController.cs`: 기존 `IsFiring` 회귀가 재현될 때만 읽기 경로를 조정하며 새 Grappling Animator 파라미터는 추가하지 않는다.
- 별도 그래플 머티리얼은 기존 `M_ShotTracer`가 LineRenderer 색을 반영하지 못할 때만 검토한다.

수정 금지:

- `Assets/_Scenes/Original_GamePlayScene.unity`
- 기존 지형·Trigger Collider와 Transform
- XR Starter Kit 원본 그래플·훅·VFX 자산
- `Packages/`
- `ProjectSettings/`
- Build Settings와 active build target

## 15. 실행 순서

1. `[입력 홀드 계약 추가]` → verify: `[PlayerInput만 우클릭을 읽고 GrapplePressed/Held가 허용·차단 상태에 맞게 갱신됨]`
2. `[GrapplingHook 상태와 취소 수명 구현]` → verify: `[Idle/Launching/Pulling 전이와 Release·InputBlocked·Died·Disabled 정리가 한 경로로 수렴함]`
3. `[카메라→총구 2단계 앵커 판정 구현]` → verify: `[플레이어·Trigger·몬스터·동적 Rigidbody를 앵커로 사용하지 않고 가까운 실제 blocker를 관통하지 않음]`
4. `[속도 기반 훅 선 발사 구현]` → verify: `[거리/속도에 맞춰 선 끝이 이동하고 유효 hit 도착 전에는 Player가 당겨지지 않음]`
5. `[PlayerController 그래플 물리 오버레이 구현]` → verify: `[일반 중력에서 상승, 무중력에서 이동, 최대 속도·안전 거리·접선 속도 보존이 동작함]`
6. `[도착·시간 초과·중력 전환 종료 계약 구현]` → verify: `[표면 밀어붙임, 상태 고착, 전환 잠금 충돌과 잔류 LineRenderer 없음]`
7. `[그래플 우선 사격 차단 구현]` → verify: `[동시 입력·연사 중 전환·Held 잔류에서 새 총알이 없고 새 좌클릭 후 사격이 복구됨]`
8. `[Player Prefab과 Player Scene 참조 구성]` → verify: `[별도 GrappleRope, 카메라·총구·Player 참조, Missing Script/Reference 없음]`
9. `[마스터 계획 정합성 갱신]` → verify: `[무중력 핵심 이동과 일반·방향성 중력 보조 이동 범위가 이번 계약과 일치함]`
10. `[컴파일·정적·자동 Play Mode 검증]` → verify: `[런타임·Editor 오류 0건, 상태·속도·사격 배타성 계측 통과, 보호 영역 diff 없음]`
11. `[사용자 실제 Play Mode 체감 검증]` → verify: `[일반 중력 상승과 Zone 05 무중력 연속 이동이 편리하고 예측 가능함]`

## 16. Play Mode 검증

### 16.1 발사와 시각 피드백

- 정면의 정적 벽을 향해 우클릭하면 선 끝이 즉시 순간이동하지 않고 저장된 목표점까지 이동한다.
- 가까운 벽과 최대 사거리 부근 벽의 도착 시간이 거리와 `hookLaunchSpeed`에 비례한다.
- 훅 머리가 도착하기 전에는 Player 속도에 그래플 당김이 추가되지 않는다.
- 발사 중 Player와 총구를 움직여도 훅 머리는 발사 당시 월드 경로를 유지하고 선 시작점만 현재 총구를 따라간다.
- 허공, Monster, Trigger와 동적 `GravityBody`를 향한 발사는 선 피드백 뒤 당김 없이 종료된다.
- 총구 앞 가까운 장애물이 카메라 aim point보다 먼저 있으면 가까운 장애물을 최종 blocker로 사용한다.

### 16.2 일반·방향성 중력

- Normal 중력에서 위쪽 벽이나 천장에 연결하면 낙하 중력보다 강하게 목표 방향으로 이동한다.
- 수평·측면 앵커에서도 기존 중력은 유지되고 그래플 방향 이동이 발생한다.
- 방향성 중력에서 현재 world Up이 달라도 그래플은 월드 Y가 아니라 anchor 방향을 사용한다.
- 당김 중 WASD를 입력하면 그래플 축에 수직인 기존 이동 성분이 완전히 사라지지 않는다.
- 목표 표면 약 `1.1m` 앞에서 종료하고 Capsule이 표면을 계속 밀거나 진동하지 않는다.
- 도착 후 표면 안쪽 속도만 제거되고 접선 방향 속도는 보존된다.

### 16.3 무중력

- 정지 상태에서 그래플 연결 후 목표 방향으로 가속하고 최대 당김 속도를 넘지 않는다.
- 이동 중 다른 방향으로 새로 연결하면 기존 관성을 전역 0으로 만들지 않고 새 목표 방향으로 전환할 수 있다.
- 우클릭을 중간에 놓으면 즉시 당김이 멈추고 당시 관성이 유지된다.
- 여러 벽을 순서대로 반복 연결해 Zone 05에서 다음 진행 지점까지 이동할 수 있다.
- 그래플 종료 뒤 무기 발사 반작용이 기존 `0.5 / 3.0` Prefab 설정과 속도 제한 규칙으로 다시 동작한다.

### 16.4 사격 배타성

- 좌클릭 연사 중 우클릭을 누르면 이미 확정된 마지막 한 발 뒤 추가 탄약 감소·피해·Muzzle Flash가 없다.
- 좌·우클릭을 같은 프레임에 누르면 그래플만 시작하고 탄약이 줄지 않는다.
- 그래플 중 좌클릭을 눌러도 그래플이 취소되지 않고 총알이 발사되지 않는다.
- 그래플을 해제한 뒤 좌클릭을 계속 누르고 있어도 연사가 자동 재개되지 않는다.
- 좌클릭을 놓았다 다시 누르면 기존 연사 간격, 피해, Tracer와 Muzzle Flash로 정상 복구된다.
- 그래플 중 R 재장전과 이미 진행 중인 재장전은 기존 규칙으로 완료된다.

### 16.5 취소와 실패 경로

- `Launching`과 `Pulling` 각각에서 우클릭을 놓으면 한 프레임 안에 선과 당김이 정리된다.
- `maxPullDuration`이 지나면 현재 속도를 0으로 만들지 않고 종료한다.
- 대화·컷신 입력으로 `AllowGrapple`이 꺼지면 즉시 정리되고 입력 복구 뒤 새 Down으로 다시 사용할 수 있다.
- 중력 전환이 시작되면 그래플이 먼저 취소되고 Player 위치 잠금·Camera Roll·전환 완료가 기존처럼 동작한다.
- 사망·리스폰 때 이전 anchor 선이 남지 않고 리스폰 위치로 당기는 힘이 이어지지 않는다.
- 대상 Collider가 연결 전 비활성화되거나 삭제되면 NullReferenceException 없이 종료한다.
- 컴포넌트 비활성화·Scene 종료 뒤 LineRenderer와 PlayerController 당김 상태가 남지 않는다.

### 16.6 회귀 확인

- 일반 이동, Sprint, Crouch, Jump, Ground Snap과 MeshCollider 접지가 기존처럼 동작한다.
- Shot Tracer와 GrappleRope가 서로의 위치·색·활성 상태를 덮어쓰지 않는다.
- 카메라 중심 사격 Ray, 총구 장애물 우선, 탄약·재장전·피해·물리 밀기가 변하지 않는다.
- 무중력 사격 반작용과 전체 속도 상한이 그래플 비활성 상태에서 유지된다.
- Periodic 방향 전환, Zero Gravity 진입, 사망·리스폰과 `PresentationUp` 계약에 회귀가 없다.
- `Original_GamePlayScene`, Collider, Packages, ProjectSettings와 Build Settings에 diff가 없다.

## 17. 정적·자동 검증 기준

- `GrapplingHook.cs`와 `.meta` GUID 존재 및 참조 일치
- Player Prefab에 `GrapplingHook` 하나와 그래플 전용 LineRenderer 하나만 존재
- Shot Tracer와 GrappleRope가 서로 다른 component fileID를 사용
- GrappleRope가 world space, 2 points, 기본 비활성, 예상 머티리얼·폭·색을 사용
- Player Scene의 `aimCamera` override가 현재 `ThirdPersonCameraController`를 참조
- Grapple용 Raycast 버퍼가 프레임마다 새 배열을 생성하지 않음
- 모든 종료 경로가 LineRenderer, GrapplingHook 상태와 PlayerController 당김 상태를 함께 정리
- 그래플이 `Input.GetMouseButton*`을 직접 호출하지 않음
- 새 `GrapplingHook`이 Player Rigidbody 속도를 직접 변경하지 않으며, 기존 `RespawnController` 초기화 경로 외 그래플 당김은 `PlayerController`만 적용
- 마스터 계획의 그래플 범위가 이번 사용자 결정과 일치
- Missing Script, missing serialized reference와 YAML fileID 중복 없음
- `git diff --check`
- `Assembly-CSharp.csproj`와 `Assembly-CSharp-Editor.csproj` 오류 0건
- 기존 외부 에셋·타 파트 경고와 이번 변경 신규 경고를 구분
- Console을 비운 새 Play Mode에서 신규 Error와 `NullReferenceException` 0건

## 18. 완료 기준

- 우클릭 홀드로 선 형태의 훅이 거리·속도에 맞게 목표점까지 날아간 뒤에만 당김을 시작한다.
- 유효한 정적 지형만 앵커로 사용하며 실패 발사는 안전하게 끝난다.
- 일반·방향성 중력에서 위쪽 목표로 이동할 수 있고, 무중력에서 반복 그래플로 구간을 통과할 수 있다.
- PlayerController가 Rigidbody 당김을 단독 소유하고 기존 이동 상태와 중력 전환을 깨뜨리지 않는다.
- 도착 안전 거리, 최대 당김 속도와 시간 초과로 표면 관통·지속 밀기·무한 가속을 막는다.
- 그래플 중 새 좌클릭 사격이 발생하지 않고, 그래플 종료 뒤 새 좌클릭으로 정상 복구된다.
- 재장전, 기존 사격 Tracer, 무중력 반작용, 중력 전환과 리스폰에 회귀가 없다.
- Inspector에서 상태, 목표, 진행률, 당김 속도와 종료 사유를 관찰할 수 있다.
- 런타임·Editor 어셈블리 컴파일 오류와 신규 Console Error가 없다.
- 사용자가 실제 Play Mode에서 일반 중력 상승과 Zone 05 무중력 반복 이동의 조작감을 확인하기 전에는 완료 처리하지 않는다.

## 19. 문서와 작업 상태 관리

- 사용자 실행 승인 전에는 이 문서를 `Docs/ksh/Tasks/01_planned`에 둔다.
- 구현을 시작하면 이 문서를 `Docs/ksh/Tasks/02_in-progress`로 이동한다.
- 각 단계의 코드·정적·자동 Play Mode 결과와 사용자 Play Mode 결과를 구분해 기록한다.
- 컴파일과 자동 계측만으로 실제 마우스 홀드 조작감, 상승 가능 여부와 무중력 구간 통과를 완료로 간주하지 않는다.
- 사용자 Play Mode 확인까지 완료되면 `Docs/ksh/Codex_Usage_Records.md`에 하나의 의미 있는 완료 작업 단위로 기록하고 문서를 `Docs/ksh/Tasks/03_completed`로 이동한다.
- 계획서 작성만 수행한 현재 단계에서는 Usage Record를 추가하지 않는다.
- WebGL 검증은 별도 명시적 승인과 재임포트·설정 변경 가능성 안내 후에만 수행한다.

# TPS 플레이어 상태 FSM·애니메이션 컨트롤 구조 실행 계획

문서 작성일: 2026-08-23
현재 상태: 완료
계획 프로필: `deep`

## 목표

현재 `MvpPlayerController.FixedUpdate`에 한 덩어리로 들어 있는 이동·지면 판정·중력 적용을, 물리 규칙이 실제로 달라지는 플레이어 상태 단위로 분리한다.

일반·측면·역중력은 서로 다른 플레이어 FSM으로 복제하지 않고 하나의 방향 독립적인 이동 FSM이 `MvpGravityState.Direction`과 `Strength`를 입력으로 받게 한다. 초기 구현 상태는 `Grounded`, `Airborne`, `ZeroGravity`로 제한하고, 그래플·순간 대쉬·웅크리기·전투는 실제 동작을 구현하는 변경에서 같은 계약 위에 확장한다.

게임플레이 FSM과 Animator 상태 머신을 분리한다. 게임플레이 FSM은 Rigidbody 이동 규칙의 정본이고, Animator는 그 결과를 읽어 현재 `TS-Armies_Recon_B` 모델의 인플레이스 Infantry 애니메이션으로 표현한다. 애니메이션이나 Animation Event가 이동·중력·상태 전환을 결정하지 않는다.

## 범위

- 현재 플레이어·중력·입력·VisualRoot·모델 Animator 구조의 기준선 기록
- 게임플레이 상태 계약 `IMvpPlayerState`와 이를 구현하는 일반 C# State 클래스 구성
- State 인스턴스를 한 번 생성하고 전환을 관리하는 `MvpPlayerStateMachine` 구성
- `MvpPlayerController`가 Rigidbody·Collider·센서 계산과 물리 적용을 계속 단독 소유하도록 책임 분리
- 일반·측면·역중력에 공통으로 사용하는 `Grounded`, `Airborne` 상태 구현
- 중력 세기 `0`을 별도 물리 규칙으로 처리하는 `ZeroGravity` 상태 구현
- 현재 입력에 이미 존재하는 Space 점프를 Grounded→Airborne 전환과 연결
- Animator 파라미터만 갱신하는 `MvpPlayerAnimationController` 추가
- 현재 Infantry 인플레이스 클립을 사용하는 기본 이동·점프·무중력 Animator Controller 구성
- Player Prefab의 nested 모델 Animator에 Controller를 override로 연결하고 Apply Root Motion 비활성화
- Unity 재컴파일, Prefab·scene override, Play Mode 물리·상태·애니메이션 검증
- 후속 Grappling·Dash·Crouch·Upper Body Combat가 따라야 할 확장 계약 문서화

## 하지 않을 것

- 일반·측면·역중력마다 별도의 Player State 클래스 또는 상태 머신 복제
- 중력 방향이나 세기의 정본을 `MvpPlayerController` 또는 State 클래스에 복사 저장
- `MvpGravityState`의 런타임 변경 API, GravityManager 또는 GravityEventTrigger 연결 구현
- 그래플 Raycast·목표 저장·LineRenderer·끌어당김 구현 또는 빈 `GrapplingState` 선행 추가
- 순간 대쉬, 달리기·대쉬 수치, 웅크리기 Collider 변경 또는 빈 `DashState` 선행 추가
- 사격·재장전 게임 로직, 탄환 판정, 무기 상태 또는 호출되지 않는 전투 애니메이션 API 추가
- Upper Body Avatar Mask, Aim Blend Tree, Animation Rigging, 손 IK 또는 총구 보정 추가
- Root Motion 클립을 이용한 Rigidbody 이동
- Toon Soldiers 원본 Prefab·FBX·애니메이션 import 설정 수정
- `Assets/_Scenes/Original_GamePlayScene.unity` 또는 팀장 Collider 수정
- 현재 작업 트리의 카메라·문서·ProjectSettings 관련 기존 변경 정리 또는 되돌리기
- 범용 능력 시스템, ScriptableObject 상태 정의 또는 여러 캐릭터를 위한 프레임워크 구축

## 현재 상태와 확인된 사실

- `MvpPlayerController`는 Player 루트의 Rigidbody·CapsuleCollider·`MvpPlayerInput`을 요구한다.
- 현재 `FixedUpdate`가 카메라 기준 이동 방향, 중력 방향 기준 지면 판정, 선형 속도, 중력 힘과 Rigidbody up축 정렬을 모두 처리한다.
- 지면 여부는 현재 중력 방향 SphereCast와 중력 반대 방향 `up`을 기준으로 판단하므로 방향 독립 FSM으로 옮길 수 있다.
- 지상과 공중 모두 평면 이동 속도를 `moveSpeed`로 즉시 설정하는 현재 조작 특성을 1차 상태 분리에서 보존한다.
- `MvpGravityState`는 정규화된 `Direction`, 0 이상 `Strength`, 두 값을 곱한 `Gravity`를 제공하지만 런타임 변경 메서드나 이벤트는 아직 없다.
- `MvpPlayerInput`에는 `JumpPressed`, `SprintOrCrouchHeld`, Fire·Reload·Grapple 입력이 있지만 현재 PlayerController가 소비하는 값은 `Move`뿐이다.
- `MvpPlayer` 물리 루트와 `TS-Armies_Recon_B` 사이에는 identity `VisualRoot`가 있고, VisualRoot는 카메라의 중력 평면상 정면과 중력 up을 따른다.
- nested 모델 Animator의 Avatar는 유효한 Humanoid이고 Controller는 비어 있으며 Apply Root Motion은 켜져 있다.
- 현재 모델에는 M4 계열 소총, ACOG와 소음기가 활성화되어 있어 Infantry 애니메이션 세트를 기준으로 한다.
- Infantry 세트에는 인플레이스 클립 69개와 별도 root_motion 클립 17개가 있다.
- Unity Preview Scene 샘플링에서 Infantry Idle·전후좌우 이동·Sprint·점프 3단계·Roll·Crouch·Shoot·Reload·Aim Up/Down이 현재 모델 Avatar에 오류 없이 적용됐다.
- Animation Rigging 패키지는 현재 설치되어 있지 않다.
- Rigidbody 제약 값 `80`은 X/Z 회전 고정이므로 측면·역중력의 물리 정렬 완성은 별도 방향성 중력 변경에서 재검토해야 한다. 이번 계획은 제약을 바꾸거나 방향성 중력 완료를 주장하지 않는다.

## 문서 정합성 차이와 이번 계획의 기준

- 현재 `Player_Gravity_Master_Plan.md`는 Left Shift를 달리기 또는 순간 대쉬, Left Ctrl을 웅크리기로 분리한다.
- 현재 프로젝트 작업 지침과 실제 `MvpPlayerInput`은 Shift 눌림 상태 하나를 `SprintOrCrouchHeld`로 전달하고, 해석을 PlayerController가 담당하도록 한다.
- 이번 상태·애니메이션 기반 작업은 입력 public 계약을 변경하지 않고 현재 `SprintOrCrouchHeld`를 보존한다.
- Dash·Sprint·Crouch의 실제 구현 단계에 들어가기 전에는 두 문서 중 어느 입력 계약을 정본으로 삼을지 사용자 확인이 필요하다.
- 해당 결정 전에는 Ctrl 입력, DashState, Crouch stance와 관련 Animator 파라미터를 추측으로 추가하지 않는다.
- 이번 계획서는 마스터 플랜의 범위나 핵심 기술 방향을 바꾸지 않으며 `Player_Gravity_Master_Plan.md`를 수정하지 않는다.

## 핵심 설계 결정

### 1. 중력은 환경 데이터, 플레이어 State는 물리 동작

```text
GravityEventTrigger / 후속 GravityManager
                 │
                 ▼
MvpGravityState
├─ Direction
├─ Strength
└─ Gravity
                 │ 현재 값만 제공
                 ▼
MvpPlayerController
├─ 입력·카메라 기준 계산
├─ 지면 SphereCast
├─ Rigidbody / CapsuleCollider 소유
├─ MvpPlayerStateMachine 소유
└─ State가 요청한 물리 동작 적용
                 │ 상태·속도 결과 제공
                 ▼
MvpPlayerAnimationController
└─ Animator 파라미터 갱신
                 │
                 ▼
TS-Armies_Recon_B Animator
```

- 일반·측면·역중력의 차이는 `Direction` 값뿐이므로 같은 `Grounded`·`Airborne` 구현을 사용한다.
- `ZeroGravity`는 중력·지면·이동 규칙이 달라지므로 별도 Player State로 둔다.
- 후속 `Grappling`과 순간 `Dash`는 목표·지속시간·종료 조건과 독립 물리 규칙이 생기는 시점에 State로 추가한다.
- 단순 Sprint는 속도 modifier, Crouch는 Grounded 내부 stance로 우선 검토하며 독립 클래스로 자동 승격하지 않는다.

### 2. 인터페이스는 계약이고 실제 State는 클래스

`IMvpPlayerState`는 모든 State가 제공해야 하는 최소 계약을 정의한다. 인터페이스 자체가 상태 동작을 보유하거나 인스턴스가 되는 것은 아니며, 각 상태는 이 인터페이스를 구현하는 `sealed class`다.

```csharp
internal interface IMvpPlayerState
{
    MvpPlayerMotionStateId Id { get; }
    void Enter(MvpPlayerController owner);
    void FixedTick(MvpPlayerController owner, MvpPlayerFixedContext context);
    void Exit(MvpPlayerController owner);
}
```

초기 구현 클래스:

```text
MvpGroundedState    : IMvpPlayerState
MvpAirborneState    : IMvpPlayerState
MvpZeroGravityState : IMvpPlayerState
```

- 세 State와 인터페이스, enum, 상태 머신은 `MvpPlayerStateMachine.cs` 한 파일에 둔다.
- State는 `MonoBehaviour`가 아니며 GameObject에 각각 컴포넌트로 붙이지 않는다.
- State 인스턴스는 `Awake`에서 한 번 만들고 전환할 때 재사용한다.
- State 전환이나 `FixedTick`에서 매 프레임 새 State, 배열, delegate 또는 컬렉션을 만들지 않는다.
- 후속 상태는 실제 기능 구현과 검증 조건이 생길 때 같은 파일에 추가한다.

### 3. Rigidbody 쓰기는 MvpPlayerController만 소유

- State 클래스는 `GetComponent`, Physics Query 또는 입력 API를 직접 호출하지 않는다.
- Controller가 한 번 계산한 `MvpPlayerFixedContext`를 현재 State에 전달한다.
- Context에는 현재 물리 프레임의 중력 방향, up, 지면 여부·법선, 카메라 기준 이동 방향과 입력 edge만 담는다.
- Rigidbody와 Collider는 Context에 넣지 않는다.
- State는 Controller의 제한된 `internal` 물리 메서드를 호출하고 Controller가 실제 `linearVelocity`, `AddForce`, `MoveRotation`을 적용한다.
- State별 메서드가 같은 프레임에 중복 호출되지 않도록 상태 머신이 현재 State 하나만 실행한다.

잠정 Context 데이터:

```csharp
internal readonly struct MvpPlayerFixedContext
{
    internal readonly Vector3 GravityDirection;
    internal readonly Vector3 Up;
    internal readonly Vector3 GroundNormal;
    internal readonly Vector3 MoveDirection;
    internal readonly bool HasGravity;
    internal readonly bool IsGrounded;
    internal readonly bool JumpPressed;
}
```

- 구체 생성자와 필드 노출 범위는 같은 Assembly 안의 실제 호출부에 필요한 최소 범위로 제한한다.
- `HasGravity`는 `MvpGravityState.Strength`가 명시적으로 0인지 판단하며, Slow Gravity를 ZeroGravity로 오인하는 임의의 큰 임계값을 두지 않는다.
- 중력 방향은 Strength가 0이어도 마지막 유효 방향을 유지하여 VisualRoot up과 중력 복구 기준이 갑자기 사라지지 않게 한다.

### 4. 전환 판단과 물리 적용 순서

각 `FixedUpdate`는 아래 순서로 한 번 실행한다.

```text
1. MvpGravityState 현재 값 읽기
2. 새 중력 방향으로 Ground Probe
3. 카메라 기준 MoveDirection 계산
4. MvpPlayerFixedContext 생성
5. 현재 State의 전환 조건 평가
6. 필요 시 Exit → Current 교체 → Enter
7. 최종 Current State의 물리 동작을 같은 FixedUpdate에서 한 번 적용
8. Rigidbody up을 중력 up으로 정렬
9. Animator가 읽을 상태·속도 결과 갱신
```

- 전환된 새 State의 물리 규칙이 다음 프레임까지 지연되지 않도록 같은 FixedUpdate에서 실행한다.
- 한 프레임의 연쇄 전환은 `Grounded/Airborne → ZeroGravity`처럼 필요한 경우에만 허용하고 최대 횟수를 제한해 무한 전환을 막는다.
- 상태 전환 직후 이전 State의 이동 또는 중력이 같은 프레임에 적용되지 않아야 한다.
- 중력 방향 변경은 별도 `SideGravityState` 전환이 아니라 새 방향으로 재계산된 Ground Probe 결과에 따라 Grounded와 Airborne 사이를 바꾼다.

## 초기 게임플레이 상태와 전환표

| 현재 상태 | 조건 | 다음 상태 | 같은 프레임의 핵심 처리 |
| --- | --- | --- | --- |
| 초기화 | `HasGravity == false` | `ZeroGravity` | 중력·평면 이동 덮어쓰기 없음 |
| 초기화 | 중력 있음 + 지면 있음 | `Grounded` | 지면 이동과 접지력 적용 |
| 초기화 | 중력 있음 + 지면 없음 | `Airborne` | 공중 이동과 중력 적용 |
| Grounded | `HasGravity == false` | `ZeroGravity` | 이전 지상 속도 덮어쓰기·접지력 중단 |
| Grounded | `JumpPressed` | `Airborne` | 현재 up 방향 점프 속도 적용 |
| Grounded | `IsGrounded == false` | `Airborne` | 기존 중력 방향 속도 보존 후 중력 적용 |
| Airborne | `HasGravity == false` | `ZeroGravity` | 현재 Rigidbody 속도 보존, 중력 중단 |
| Airborne | `IsGrounded == true` | `Grounded` | 중력 방향 낙하 속도 제거 후 지면 이동 |
| ZeroGravity | `HasGravity == true` | `Grounded` 또는 `Airborne` | 새 방향 Ground Probe 결과로 즉시 결정 |

전환 우선순위:

1. 중력 유무 변화
2. Grounded에서 점프 입력
3. 새 중력 방향 기준 지면 접촉 변화
4. 현재 상태 유지

`JumpPressed`는 Grounded에서만 소비한다. Airborne과 ZeroGravity에서는 같은 입력을 무시하고 입력 버퍼·코요테 타임·다단 점프는 이번 범위에서 추가하지 않는다.

## 상태별 물리 계약

### Grounded

- 현재 Ground Probe의 `groundNormal` 평면에 카메라 기준 MoveDirection을 투영한다.
- 현재 코드와 같은 `moveSpeed`로 평면 선형 속도를 설정한다.
- 중력 방향 성분이 지면 쪽 낙하 속도라면 제거한다.
- `-groundNormal * gravityStrength`를 접지력으로 적용한다.
- Space 입력 시 평면 속도는 보존하고 `up * jumpSpeed` 방향 속도를 설정한 뒤 Airborne으로 전환한다.
- 최초 점프 속도는 Inspector 직렬화 후보값으로 시작하고, 실제 장애물 높이와 체공 시간 기준으로 Play Mode에서 조정한다.

### Airborne

- 현재 동작 보존을 위해 1차 구현에서는 Grounded와 같은 카메라 기준 평면 이동 속도를 허용한다.
- 경사 법선 투영은 사용하지 않는다.
- 중력 방향 속도를 보존하고 `gravityState.Gravity`를 가속도로 적용한다.
- 공중 제어율, 낙하 최대 속도와 점프 높이 곡선은 이번 상태 분리와 섞지 않는다.
- 새 중력 방향의 유효 지면을 감지하면 Grounded로 전환한다.

### ZeroGravity

- `AddForce`로 중력을 적용하지 않는다.
- WASD로 기존 속도를 `moveSpeed`에 강제 덮어쓰지 않는다.
- 진입 순간의 Rigidbody 선형 속도를 보존한다.
- 임의 drag, 자동 정지, 자유 비행 추진을 추가하지 않는다.
- Grappling이 구현되기 전 ZeroGravity 검증은 속도 보존과 중력 미적용까지로 제한한다.
- 후속 Grappling 종료 후 중력이 계속 0이면 이 상태로 돌아오도록 확장한다.

### 공통 중력축 정렬

- Rigidbody up축 정렬은 State 클래스가 아니라 Controller의 공통 후처리로 유지한다.
- Strength 0에서도 방향 벡터가 유효하면 VisualRoot의 표현 up은 유지한다.
- ZeroGravity에서 물리 루트 회전을 계속 중력 up에 맞출지는 최초 Play Mode 체감으로 확인하되, 이번 1차 기본값은 현재 정렬 경로를 유지한다.
- 방향성 중력에서 X/Z 회전 제약이 실제 정렬을 막는 문제는 별도 중력 변경의 성공 조건으로 다룬다.

## 게임플레이 FSM과 Animator FSM의 분리

```text
게임플레이 상태                Animator 표현
Grounded + 속도 0         →   Combat Idle
Grounded + 이동           →   2D 방향 이동 Blend Tree
Grounded → Airborne 점프  →   Jump Start
Airborne                  →   Jump Air Loop
Airborne → Grounded       →   Jump Land 후 Locomotion
ZeroGravity               →   Jump Air Loop 재사용
```

- Animator는 State 전환을 요청하거나 Rigidbody를 움직이지 않는다.
- Animation Event는 상태 변경·점프 힘·발사 판정에 사용하지 않는다.
- Animator의 Exit Time은 시각 전환에만 사용하며, 게임 상태 복귀를 지연시키지 않는다.
- Grounded인데 Land 클립이 재생 중이어도 Controller는 즉시 지상 이동을 처리할 수 있다.
- ZeroGravity와 Airborne이 같은 Jump Air 클립을 공유해도 게임플레이 상태는 구분된 상태로 유지한다.

## Animator Controller 구성

### 자산 위치

```text
Assets/_Custom/Animations/Player/
└─ MvpPlayer.controller
```

- 원본 Toon Soldiers 폴더에는 새 Controller나 Mask를 만들지 않는다.
- `MvpPlayer.prefab`의 nested `TS-Armies_Recon_B` Animator에 Controller를 instance override로 연결한다.
- 같은 override에서 Apply Root Motion을 `false`로 명시한다.
- 원본 모델 Prefab의 Controller·Apply Root Motion 값은 수정하지 않는다.

### Base Layer

```text
Entry
  └─ Locomotion
       ├─ Idle 중심
       └─ 전/후/좌/우 방향 이동

Locomotion ── Jump 시작 ──▶ JumpStart ──▶ JumpAir
     ▲                                  │
     └──────────── JumpLand ◀───────────┘

Any relevant state ── ZeroGravity ──▶ JumpAir 재사용
```

초기 클립 후보:

| 표현 | Infantry 클립 |
| --- | --- |
| Idle | `infantry_combat_idle` |
| 전진 | `infantry_combat_run` |
| 후진 | `infantry_combat_run_back` |
| 좌 이동 | `infantry_combat_run_left` |
| 우 이동 | `infantry_combat_run_right` |
| 점프 시작 | `infantry_jump_1_start` |
| 공중·무중력 | `infantry_jump_2_air` |
| 착지 | `infantry_jump_3_land` |

- 초기 이동은 현재 `moveSpeed = 3`과 맞춰 Combat Run 방향 세트를 사용한다.
- Play Mode에서 발 미끄러짐이 크면 Animator 재생 속도를 제한적으로 조정하고, Walk/Run 이중 Blend Tree는 실제 속도 단계가 생길 때 추가한다.
- Sprint·Roll·Crouch·Shoot·Reload·Aim 클립은 존재하지만 해당 게임 로직 단계 전에는 Controller에 연결하지 않는다.

### 초기 Animator 파라미터

| 이름 | 타입 | 공급자 | 의미 |
| --- | --- | --- | --- |
| `MoveX` | Float | AnimationController | VisualRoot 로컬 기준 좌우 속도 정규화 |
| `MoveY` | Float | AnimationController | VisualRoot 로컬 기준 전후 속도 정규화 |
| `MoveSpeed` | Float | AnimationController | 중력 평면상 실제 속도 정규화 |
| `IsGrounded` | Bool | Gameplay FSM 결과 | 현재 게임 상태가 Grounded인지 여부 |
| `IsZeroGravity` | Bool | Gameplay FSM 결과 | 현재 게임 상태가 ZeroGravity인지 여부 |
| `VerticalSpeed` | Float | AnimationController | 현재 up 기준 Rigidbody 속도 성분 |

- 파라미터 문자열은 `Animator.StringToHash`로 한 번 계산해 사용한다.
- MoveX·MoveY·MoveSpeed에는 `Animator.SetFloat` damping을 사용해 입력 노이즈만 완화한다.
- Locomotion에서 `IsGrounded == false`이고 `VerticalSpeed`가 양수면 JumpStart, 양수가 아니면 JumpAir로 전환한다.
- JumpStart는 짧은 시작 클립 후 JumpAir로 이어지고, JumpAir는 `IsGrounded == true`일 때 JumpLand로 전환한다.
- JumpLand는 시각적 Exit Time 후 Locomotion으로 복귀하지만 실제 지상 이동은 착지한 물리 프레임부터 즉시 허용한다.
- 원시 `JumpPressed`나 Trigger를 Animator에 전달하지 않아 게임 FSM이 AnimationController를 직접 호출하지 않게 한다.
- Animator 상태 이름을 코드에서 반복 CrossFade하는 방식보다 파라미터 기반 전이를 기본으로 한다.

## MvpPlayerAnimationController 책임

- Player 루트에 컴포넌트로 추가하고 `MvpPlayerController`, Player Rigidbody, VisualRoot, nested 모델 Animator를 직렬화 참조로 받는다.
- `Update`에서 가장 최근 물리 상태와 Rigidbody 실제 속도를 읽어 Animator 파라미터를 갱신한다.
- 입력 API를 직접 읽지 않는다.
- Rigidbody 속도나 Transform을 변경하지 않는다.
- `GetComponent`를 매 프레임 호출하지 않고 `Awake`에서 참조를 캐시한다.
- VisualRoot의 로컬 전후좌우를 기준으로 현재 선형 속도를 분해하여 MoveX·MoveY를 계산한다.
- 중력 방향 성분을 제외한 평면 속도로 MoveSpeed를 계산한다.
- `IsGrounded`, `IsZeroGravity`, `VerticalSpeed`의 연속 상태값만으로 점프·낙하·착지 표현을 선택하며 Controller가 AnimationController를 직접 호출하지 않는다.
- 사격·재장전·Aim은 전투 상태의 정본이 생길 때 별도 Upper Body Layer와 함께 추가하고 현재 컴포넌트가 원시 Fire 입력을 선행 소비하지 않는다.

## 파일별 변경 계획

| 파일 | 변경 방향 |
| --- | --- |
| `Assets/_Scripts/Player/MvpPlayerController.cs` | 상태 머신 소유, FixedContext 생성, 상태별 물리 helper, 점프 수치와 읽기 전용 상태 결과 추가 |
| `Assets/_Scripts/Player/MvpPlayerStateMachine.cs` | interface, enum, state machine, Grounded·Airborne·ZeroGravity 클래스 추가 |
| `Assets/_Scripts/Player/MvpPlayerAnimationController.cs` | Rigidbody·상태 결과를 Animator 파라미터로 변환 |
| `Assets/_Scripts/Gravity/MvpGravityState.cs` | 중력 0 여부를 의미하는 최소 read-only 계약이 실제 중복을 줄일 때만 추가 |
| `Assets/_Custom/Animations/Player/MvpPlayer.controller` | Base locomotion·jump·zero-gravity 표현 그래프 |
| `Assets/_Custom/Prefabs/Player/MvpPlayer.prefab` | AnimationController 컴포넌트와 nested Animator Controller·Root Motion override 연결 |

- `MvpPlayerInput.cs`는 이번 변경에서 public 입력 계약이나 현재 사용자 카메라 변경을 수정하지 않는다.
- `MvpThirdPersonCamera.cs`, CameraRig Prefab, Original 씬과 ProjectSettings는 범위 밖이다.
- `GamePlayScene_Player`는 Prefab 참조만으로 동작하면 수정하지 않는다. scene override 추가가 반드시 필요할 때만 영향과 이유를 먼저 확인한다.

## 후속 확장 계약

### Grappling

- 실제 Raycast·목표점·당김·종료 조건을 구현하는 변경에서 `MvpGrapplingState`를 추가한다.
- ZeroGravity에서 유효 적중 시 Grappling, 도착·취소·시간 초과 시 ZeroGravity로 돌아간다.
- Grappling 중 중력이 복구되면 새 중력 방향 기준 Grounded 또는 Airborne으로 종료한다.
- 그래플 힘은 GrapplingState가 Controller의 전용 helper를 통해 적용하며 AnimationController는 Jump Air를 우선 재사용한다.

### Dash·Sprint·Crouch

- 지속 속도 증가라면 Grounded 내부 speed modifier로 처리한다.
- 지속시간·방향 고정·재사용 대기시간·취소 규칙이 있는 순간 대쉬로 확정될 때만 `MvpDashState`를 추가한다.
- Crouch는 Grounded 내부 stance로 시작하고 Capsule 높이·복귀 공간 검사·좁은 통로 규칙이 커질 때만 클래스를 분리한다.
- 입력 계약이 확정되기 전 Animator에 Sprint/Crouch 파라미터와 전이를 만들지 않는다.

### Combat·Aim

- 사격·재장전 정본을 소유하는 전투 컴포넌트가 생기면 Upper Body Avatar Mask와 Action Layer를 추가한다.
- `infantry_combat_shoot`, `infantry_combat_reload`, Aim Up/Down을 우선 사용한다.
- 이동 FSM과 전투 Action 상태를 조합한 `GroundedShooting`, `AirborneReloading` 같은 State 클래스를 만들지 않는다.
- 실제 탄환·그래플 판정은 카메라 중심 방향을 사용하고 Player 루트·VisualRoot·총구 forward를 정본으로 바꾸지 않는다.

## 실패 케이스와 대응

| 실패 케이스 | 계획된 대응 |
| --- | --- |
| 중력 방향 변경 직후 기존 바닥을 계속 Grounded로 판단 | 매 FixedUpdate 새 gravityDirection으로 Ground Probe를 먼저 재계산 |
| State 전환 프레임에 이전 중력이 한 번 더 적용됨 | 전환 후 최종 State의 물리 동작만 한 번 실행 |
| Strength 0인데 이동 코드가 속도를 `moveSpeed`로 덮어씀 | ZeroGravity가 중력·평면 속도 덮어쓰기 모두 중단 |
| Grounded/Airborne 경계에서 Animator가 빠르게 깜박임 | 실제 probe 문제를 먼저 확인하고 Animator transition duration으로 물리 상태 오류를 숨기지 않음 |
| 점프 시작 클립이 낙하나 상태 진동에서 반복됨 | Trigger 대신 IsGrounded·VerticalSpeed 조건을 사용하고 물리 Ground Probe를 먼저 안정화 |
| Animator가 Rigidbody를 이동시킴 | nested Animator Apply Root Motion false와 인플레이스 클립 확인 |
| VisualRoot 회전과 이동 방향 애니메이션이 한 프레임 어긋남 | 실제 속도를 VisualRoot 로컬로 변환하고 Update/LateUpdate 순서를 Play Mode에서 확인 |
| 방향성 중력에서 Collider가 회전하지 않음 | Rigidbody X/Z 제약 문제로 분리하고 이번 상태 계획 성공으로 과장하지 않음 |
| 그래플·전투용 빈 State/API가 남음 | 실제 기능 변경 전에는 클래스와 public API를 만들지 않음 |
| 사용자 카메라 변경과 Prefab 변경이 섞임 | 시작·종료 Git diff에서 기존 변경과 이번 대상 파일을 분리 |

## 초기 튜닝 후보

| 항목 | 초기 방향 | 확정 방법 |
| --- | --- | --- |
| 점프 속도 | Inspector 직렬화 값으로 시작 | 실제 BoxCollider 장애물과 체공 시간 Play Mode 확인 |
| 지상·공중 이동 속도 | 기존 `moveSpeed = 3` 보존 | 상태 분리 전후 위치 변화 비교 |
| ZeroGravity 중력 | Strength 정확히 `0` | AddForce 미호출과 속도 보존 확인 |
| 이동 Blend Tree | Combat Run 2D 방향 세트 | 전후좌우 입력·발 미끄러짐 확인 |
| Animator float damping | 짧은 시각 보간 | 입력 중단 반응성과 떨림을 함께 확인 |

구체 점프 속도와 Animator damping 수치는 실행 전 문서에서 임의로 고정하지 않고 Play Mode 기준선으로 확정한다. 수치 튜닝이 상태 책임이나 public 계약을 바꾸지는 않아야 한다.

## 실행 계획

1. `[Git·Unity·플레이어 기준선 기록]` → verify: `[기존 미커밋 파일 분리, 대상 코드·Prefab hash 기록, Unity ready·Console 기준선, 일반 중력 WASD·정지·낙하 동작 기록]`
2. `[상태 계약과 상태 머신 골격 구현]` → verify: `[IMvpPlayerState·StateMachine·enum·FixedContext 컴파일, State 인스턴스 1회 생성, per-tick 할당 없음]`
3. `[기존 FixedUpdate를 Grounded·Airborne 물리 helper로 분리]` → verify: `[일반 중력의 이동 속도·카메라 기준 방향·지면 유지·낙하가 변경 전과 일치]`
4. `[점프와 Grounded↔Airborne 전환 연결]` → verify: `[Grounded에서 Space 1회만 승인, up 방향 점프, 공중 재점프 없음, 새 중력 방향 지면에 착지]`
5. `[ZeroGravity 상태와 중력 유무 전환 연결]` → verify: `[Strength 0에서 중력·WASD 속도 덮어쓰기 중단, 현재 속도 보존, 중력 복구 시 Grounded/Airborne 즉시 선택]`
6. `[공통 Rigidbody up 정렬과 VisualRoot 경계 보존]` → verify: `[상태 분리 후 기존 일반 중력 회전·카메라 방향 시각 회전 유지, 방향성 중력 제약 한계 별도 기록]`
7. `[MvpPlayerAnimationController 추가]` → verify: `[입력·Transform·Rigidbody 쓰기 없음, 실제 속도·게임 상태 기반 파라미터, hash 캐시와 프레임당 GC Alloc 0 B]`
8. `[MvpPlayer Animator Controller와 Base Layer 구성]` → verify: `[Idle·전후좌우 이동·JumpStart/Air/Land·ZeroGravity Air 전환, 모든 motion이 Infantry 인플레이스 클립]`
9. `[Player Prefab 연결과 Root Motion 비활성화]` → verify: `[nested Prefab Connected, 원본 Toon Soldiers diff 없음, Controller override 유효, Apply Root Motion false, 모델 위치·회전·스케일 불변]`
10. `[Unity 재컴파일·저장·재로드 검증]` → verify: `[컴파일 오류·Missing Script·Missing Reference 0건, Prefab·scene reference 유지, 기존 카메라 변경 보존]`
11. `[Play Mode 물리 상태 전환 검증]` → verify: `[Grounded↔Airborne, 점프·착지, Strength 0 진입·복구, 중력 방향 변경 시 새 probe 기준 전환과 신규 Console 오류 없음]`
12. `[Play Mode 애니메이션 검증]` → verify: `[Idle·전후좌우·대각선·점프·착지·무중력 표현, Root Motion 위치 이탈 없음, 큰 발 미끄러짐·팝핑·모델 관통 기록]`
13. `[성능·상태 안정성 검증]` → verify: `[안정 상태 반복 GC Alloc 0 B, State 인스턴스 재생성 없음, Grounded/Airborne 프레임 진동과 JumpStart 반복 없음]`
14. `[최종 diff·범위·문서 검증]` → verify: `[변경 대상이 Player 상태·애니메이션 파일과 Prefab에 한정, Input·Camera·Original 씬·원본 애셋·Package·ProjectSettings 의도하지 않은 diff 없음]`
15. `[결과 기록과 계획 상태 갱신]` → verify: `[실행·검증 결과와 사람이 확정한 점프·애니메이션 수치를 Codex 활용 기록에 한 항목으로 남기고 완료 시 계획서를 03_completed로 이동]`

## 검증 기준

- 일반 중력의 기존 WASD 이동 방향, 속도, 경사 투영, 지면 유지와 중력 적용이 상태 분리 전과 동일하다.
- State 클래스는 `MonoBehaviour`가 아니고 `IMvpPlayerState` 구현 인스턴스를 재사용한다.
- Rigidbody·CapsuleCollider·Physics Query와 실제 속도·힘·회전 쓰기는 `MvpPlayerController` 한 곳이 소유한다.
- 일반·측면·역중력은 같은 Grounded·Airborne 클래스가 gravityDirection/up 값만 바꿔 사용한다.
- Strength 0은 ZeroGravity로 전환되어 중력과 지상 이동 속도 덮어쓰기를 중단한다.
- 중력 복구 시 새 중력 방향 Ground Probe 결과에 따라 같은 FixedUpdate에서 Grounded 또는 Airborne을 선택한다.
- Space 점프는 Grounded에서 한 번만 실행되고 현재 중력의 반대 방향으로 작동한다.
- Animator는 게임 상태를 제어하지 않고 Controller가 제공한 상태·실제 속도만 표현한다.
- Player가 이동하지 않을 때 Combat Idle, 이동할 때 방향 Blend Tree, 점프할 때 Start→Air→Land가 재생된다.
- ZeroGravity는 Jump Air Loop를 재사용하되 게임 상태는 Airborne과 구분된다.
- Apply Root Motion은 Player Prefab instance에서 비활성화되고 Rigidbody 위치가 애니메이션에 의해 바뀌지 않는다.
- 모델 nested Prefab 연결, 로컬 위치 `(0, -0.235, 0)`, identity 회전과 스케일 `0.3`이 유지된다.
- Toon Soldiers 원본 Prefab·FBX·meta·animation import 설정에는 diff가 없다.
- VisualRoot와 카메라의 기존 현재 프레임 시각 회전 계약을 보존한다.
- 안정 상태의 FixedUpdate·Update에서 반복 managed allocation이 없다.
- 컴파일 오류, Missing Script, Missing Reference와 신규 Console 오류가 없다.
- 방향성 중력의 Rigidbody X/Z 회전 제약과 실제 Grappling·Dash·Crouch·Combat은 이번 완료 결과로 주장하지 않는다.

## 중단 및 복구 조건

- 상태 분리 후 일반 중력의 현재 이동·지면 유지가 달라지면 Animator 작업으로 넘어가지 않고 Grounded·Airborne helper의 동작 보존부터 복구한다.
- State 전환 프레임에 두 State가 동시에 `linearVelocity` 또는 `AddForce`를 적용하면 상태 머신 실행 순서를 수정하고 임시 flag로 숨기지 않는다.
- ZeroGravity 판단이 Slow Gravity까지 잘못 흡수하면 큰 임계값 fallback을 추가하지 않고 중력 상태 계약을 다시 분리한다.
- nested Toon Soldiers Prefab이 Unpacked되거나 원본 Animator 설정에 diff가 생기면 저장하지 않고 Player Prefab override만 다시 구성한다.
- Apply Root Motion 비활성 상태에서도 모델이 Player 루트에서 이동하면 사용하는 클립, Animator hierarchy와 Transform curve를 먼저 조사한다.
- Animator transition duration으로 실제 Grounded probe 진동을 숨기지 않는다. 물리 상태가 흔들리면 probe·Collider·중력 방향을 먼저 진단한다.
- 입력 계약 차이가 Dash·Crouch 구현을 막으면 현재 상태·기본 애니메이션 완료와 분리하고 사용자 결정 없이 Master Plan이나 Input public API를 변경하지 않는다.
- 방향성 중력에서 X/Z 회전 제약 때문에 물리 루트가 정렬되지 않으면 이번 상태·Animator 변경과 섞어 임의 해제하지 않고 별도 방향성 중력 작업으로 보고한다.
- 새 public API, Package 설치, Original 씬 수정 또는 전투 시스템 변경이 필요해지면 현재 범위를 넘으므로 진행 전 영향과 대안을 사용자에게 알린다.

## 완료 후 기록

- 계획만 작성한 현재 단계에서는 `Docs/ksh/Codex_Usage_Records.md`에 별도 항목을 추가하지 않는다.
- 구현과 검증까지 완료되면 상태 책임, 인터페이스와 구현 클래스 분리, 방향 독립 중력 처리, Root Motion 정책과 Animator 데이터 흐름을 한 완료 항목으로 기록한다.
- 사람이 직접 결정한 점프 속도, 이동 애니메이션 세트, Animator damping과 ZeroGravity 회전 체감을 구분해 기록한다.
- 실행 중 팀 합의나 핵심 방향이 바뀌면 사용자 확인 후 `Player_Gravity_Master_Plan.md`를 갱신한다.
- 완료 시 이 계획서의 상태를 완료로 바꾸고 `Docs/ksh/Tasks/03_completed`로 이동한다.

# Zone 기반 중력 시스템 구현 실행 계획

문서 작성일: 2026-08-24  
현재 상태: Phase 2.5 구현·사용자 Play Mode 검증 완료 · Phase 3 물리 Preset 책임 명칭 정리·컴파일 검증 완료, 사용자 Play Mode 검증 대기

계획 프로필: `deep`

기준 문서:

- [SinkPoint MVP 게임 기획서](../../../GameDesign_MVP.md)
- [플레이어·중력 파트 마스터 계획](../../Player_Gravity_Master_Plan.md)

## 1. 목표

`GamePlayScene_Player`의 순차형 Zone 진행에 맞춰 일반 중력, 한 번의 방향 전환, 주기적 방향 전환과 무중력을 단계적으로 구현한다.

중력 상태의 소유권, Zone 활성화, 플레이어·카메라·동적 Rigidbody의 반응을 분리하고 각 Phase가 독립적으로 Play Mode 검증을 통과한 뒤 다음 Phase로 진행한다.

최종적으로 다음 흐름을 안정적으로 재현하는 것이 이번 계획의 목표다.

```text
Normal Gravity
  → Gravity Shift: 한 번의 90도 방향 전환
  → Reverse Gravity: 예고 후 주기적 방향 전환
  → Zero Gravity: 중력 제거와 그래플 구현을 위한 상태 인계
```

이번 계획은 중력 시스템과 무중력 상태 기반까지 다룬다. 그래플 이동 자체는 중력 시스템 검증이 끝난 뒤 별도 실행 계획에서 구현한다.

## 2. 범위

- 실행 중 중력 방향과 세기를 안전하게 변경하는 공통 상태
- 현재 활성 Zone을 기준으로 중력 동작을 시작·교체·종료하는 조정자
- 기존 `GravityEventTrigger`와 실제 중력 변경의 연결
- 한 번 적용하고 유지하는 고정 방향 중력
- 방향 목록과 시간을 사용하는 주기적 중력
- 세기 `0`인 무중력 진입과 일반 중력 복귀
- 방향성 중력에 맞춘 플레이어 Rigidbody, Ground Probe와 카메라 기준축
- 명시적으로 선택한 동적 Rigidbody에만 사용자 정의 중력을 적용하는 `GravityBody`
- Zone 수와 무관하게 실제 `GravityPreset`을 선택해 운영 경로로 실행하는 Play Mode 테스트 UI
- 환경의 실제 중력 변경과 플레이어·카메라의 짧은 회전 전환을 분리한 `PresentationUp` 계약
- 중력 변경 시 이전 주기 동작 취소, Rigidbody 깨우기와 리스폰 상태 복구
- Zone별 Play Mode 검증과 전체 구간 회귀 검증

## 3. 하지 않을 것

- 팀장 소유 `Assets/_Scenes/Original_GamePlayScene.unity` 수정
- 지형을 구성하는 바위·벽·바닥·기둥에 Rigidbody 또는 사용자 정의 중력 적용
- 씬의 모든 Rigidbody를 검색해서 자동 등록하는 전역 처리
- 살아 있는 모든 몬스터를 Rigidbody 물리 오브젝트로 전환
- 거미의 표면 부착, 지렁이 잠복과 공중몹 비행 규칙 재작성
- 실제 로프, 스윙, 줄 감기와 그래플 이동 구현
- Phase 2.5의 기능적 Roll 전환을 넘어서는 완성형 중력 전환 VFX·SFX·카메라 연출
- 여러 Zone이 동시에 서로 다른 중력을 유지하는 로컬 중력장
- ScriptableObject 기반 범용 중력 프레임워크
- 여러 씬과 게임 모드를 포괄하는 범용 서비스 로케이터 또는 싱글턴 재설계
- 아이템, 인벤토리, 무기, 전투와 적 AI 확장
- 사용자 승인 없는 실제 WebGL 빌드, Build Settings 또는 활성 빌드 타깃 변경
- 현재 사용자의 미커밋 `Docs/GameDesign_MVP.md` 변경을 덮어쓰기·정리·되돌리기

## 4. 현재 상태와 근거

### 4.1 중력 상태

- `Assets/_Scripts/Gravity/GravityState.cs`는 정규화된 `Direction`, 0 이상 `Strength`, 두 값을 곱한 `Gravity`, 런타임 `SetGravity()`와 `Changed` 이벤트를 제공한다.
- `GamePlayScene_Player/GravitySystem`에는 아래 방향 `(0, -1, 0)`, 세기 `9.81`의 `GravityState`가 하나 배치돼 있다.

### 4.2 Zone과 게임 진행

- `GravityEventTrigger`는 플레이어가 처음 닿는 `OnTriggerEnter`에서 `Triggered` 이벤트를 한 번 발생시킨다.
- `GameFlowManager`는 이 이벤트를 받아 `GravityManager.ApplyPreset()`을 호출한 뒤 `GameFlowState`를 갱신한다.
- 현재 이벤트 타입은 `ShiftGravity`, `Inversion`, `FastDown`, `Slow`, `ZeroGravity`다.
- 씬에는 `Zone_02_Normal`, `Zone_03_GravityShift`, `Zone_04_Inversion`, `Zone_05_ZeroGravity`와 다섯 종류의 중력 이벤트 트리거가 이미 존재한다.

### 4.3 플레이어와 카메라

- `PlayerController`는 Unity 기본 중력을 끄고 `GravityState.Gravity`를 `ForceMode.Acceleration`으로 적용한다.
- 이동, 점프와 Ground Probe는 현재 중력 방향과 반대 방향을 Up으로 사용한다.
- 플레이어 정렬은 `Rigidbody.MoveRotation`으로 구현돼 있지만 Player Prefab의 Rigidbody는 현재 X/Z 회전이 고정돼 있다.
- 이 제약은 90도 방향 전환을 막을 수 있으므로 Phase 1에서 실제 동작을 확인하고, 방향성 정렬을 방해하면 X/Z 회전 고정을 제거한다.
- `ThirdPersonCameraController`는 현재 yaw 축으로 `Vector3.up`을 사용한다. 방향성 중력에서 카메라가 월드 Up에 남지 않도록 Phase 1에서 현재 중력 Up 기준으로 바꿔야 한다.
- `PlayerMotionStateMachine`은 `Strength == 0`에서 `ZeroGravity`를 선택하지만 `ZeroGravityMotionState.FixedTick()`은 현재 비어 있다.

### 4.4 몬스터와 지형

- 현재 지네·거미·지렁이 이동 스크립트는 Rigidbody 힘으로 이동하지 않는다.
- 지네와 지렁이는 `GravityState.Direction`을 바닥 기준 계산에 사용하고, 거미는 waypoint의 표면 normal을 사용한다.
- 현재 확인한 지네·거미·공중몹 Prefab에는 Rigidbody가 없다.
- 지형 바위와 BoxCollider는 정적 충돌체로 유지한다.
- 중력 연출은 별도 박스·돌·금속 잔해 또는 죽은 몬스터 대체물에만 `GravityBody`를 붙여 만든다.

### 4.5 검증 기반

- 현재 프로젝트에는 전용 Gameplay/EditMode 테스트 코드와 테스트 asmdef가 없다.
- 각 Phase는 Unity 스크립트 재컴파일, Console 오류 검사와 `GamePlayScene_Player` Play Mode 수동 검증을 기본으로 한다.
- 새로운 테스트 인프라를 만들기보다 각 Phase의 좁은 테스트 배치와 재현 절차를 문서화한다.

## 5. 필요한 가정

- 스테이지는 Entry에서 Source로 순서대로 진행하며 플레이어가 활성화한 Zone 하나가 현재 게임플레이 중력을 결정한다.
- `GravityPreset`은 공간 Zone이 아니라 방향과 세기를 담은 물리 설정이다. 환경 Trigger가 선택한 Preset은 다음 Preset 적용 전까지 유지된다.
- MVP에서는 이전 구역으로의 역행과 Zone 건너뛰기를 지원하지 않는다. 문과 진행 차단은 GameFlow·구역 연출이 담당하며 중력 시스템은 통과 순서 인덱스나 진입 면을 별도로 검증하지 않는다.
- 이전 Trigger를 반대 방향으로 다시 통과하거나 Trigger에서 Exit해도 이전 중력을 복원하지 않는다. 체크포인트 리스폰만 저장된 Zone을 명시적으로 복구한다.
- 이전 Zone과 다음 Zone이 서로 다른 중력을 동시에 유지할 필요는 없다.
- 문이 닫히는 것만으로 이전 구역의 렌더링·물리 처리가 자동으로 중단된다고 가정하지 않는다. 이전 Zone의 전투나 물리 오브젝트가 새 중력에 반응하면 안 되거나 비용을 줄여야 하는 경우 GameFlow가 해당 Zone 루트나 동적 오브젝트를 비활성화하거나 진행 경계로 격리한다.
- 첫 구현은 `GamePlayScene_Player`의 기존 Zone Collider와 BoxCollider 지형을 사용한다.
- Phase 3 방향은 Normal `-Y`, Shift `+X`, Inversion `-X`로 확정한다. Phase 4의 변경 간격과 예고 시간만 Scene View의 실제 Zone 길이와 Play Mode 체감을 확인한 뒤 Inspector 값으로 확정한다.
- `FastDown`과 `Slow`는 이벤트 타입을 제거하지는 않되 이번 MVP의 중력 Zone으로 구성하지 않는다.
- 플레이어와 카메라의 방향성 중력 대응은 중력 시스템의 일부다. 둘 중 하나라도 월드 Up에 남으면 Phase 1을 완료하지 않는다.
- Phase 2.5의 실제 게임 진행 전환은 환경의 중력을 먼저 확정하고, 플레이어만 월드 위치와 속도를 고정한 채 새 Up으로 회전한 뒤 해제한다.
- 카메라는 현재 시선 전방축을 중심으로 화면 기준 반시계 방향으로 Roll하며, 플레이어에게는 지형이 시계 방향으로 회전하는 느낌을 준다.
- 전환 중에는 전체 시간이나 `GravityBody`를 멈추지 않는다. 동적 환경 물체는 전환 시작 즉시 새 중력에 반응한다.
- 각 Phase 구현은 사용자 승인 후 시작하며, 이전 Phase의 완료 기준을 통과하기 전에는 다음 Phase 코드를 추가하지 않는다.

## 6. 책임 경계와 데이터 흐름

```text
GravityEventTrigger
  └─ 플레이어 첫 접촉과 one-shot 판정만 소유
        ↓
GameFlowManager
  ├─ GameFlowState 갱신
  └─ 해당 GravityPreset을 GravityManager에 적용 요청
        ↓
GravityManager
  ├─ 이전 주기 동작 중단
  ├─ 현재 활성 Zone 소유
  ├─ 고정·주기·무중력 동작 실행
  ├─ GravityState 물리값 변경
  └─ 전환 상태·PresentationUp·진행률 소유
        ├──────────────────────────────┐
        ↓                              ↓
GravityState
  ├─ 현재 Direction·Strength·Gravity 제공
  └─ 값 변경 이벤트 발생
        ├─ GravityBody: 선택된 동적 Rigidbody에 즉시 힘 적용
        └─ 몬스터 이동: 필요한 경우 바닥 방향만 참고
                                       PresentationUp
                                         ├─ PlayerController: 위치 고정·몸 회전·완료 후 새 중력 적용
                                         └─ ThirdPersonCameraController: 반시계 Roll·완료 후 Orbit 기준 재구성
```

### 6.1 `GravityState`

`GravityState`는 현재 값의 단일 정본이다.

- `SetGravity(Vector3 direction, float strength)` 형태의 런타임 변경 진입점을 제공한다.
- 0 벡터 방향은 거부하거나 안전한 기본 방향으로 정규화하되 조용한 fallback으로 잘못된 Zone 설정을 숨기지 않는다.
- 세기는 0 이상으로 제한한다.
- 실제 값이 달라졌을 때만 변경 이벤트를 한 번 발생시킨다.
- 구독자는 이벤트 인자에 복제된 상태를 보관하기보다 이벤트 후 `GravityState`의 현재 값을 읽는다.
- 타이머, Zone 판정, Rigidbody 목록과 게임 진행 상태는 소유하지 않는다.

### 6.2 `GravityManager`

`GravityManager`는 실행 순서와 활성 중력 동작을 소유한다.

- 씬의 단일 `GravityState`를 직렬화 참조로 받는다.
- 현재 적용된 `GravityPreset`을 기억한다.
- 새 Zone 활성화 전에 이전 Zone의 주기 동작과 예고를 반드시 취소한다.
- 고정형 Zone은 값을 한 번 적용하고 유지한다.
- 주기형 Zone은 직렬화된 방향 목록을 지정 간격으로 순환한다.
- 무중력 Zone은 이전 반복을 중단하고 세기를 정확히 `0`으로 설정한다.
- 전환 시작·중간 재요청·완료·취소와 `PresentationUp`의 단일 진행률을 소유한다.
- 실제 중력은 `GravityState`에 한 번 적용하고, 플레이어와 카메라가 별도 Tween으로 서로 다른 시간에 도착하지 않게 한다.
- 리스폰 요청에는 체크포인트 Zone의 초기 상태를 즉시 재적용할 수 있어야 한다.
- 물리 힘을 직접 적용하거나 플레이어·몬스터를 검색하지 않는다.

### 6.3 `GravityPreset`

각 `GravityPreset`은 기획 구역 의미와 무관하게 물리 방향·세기 설정만 소유한다.

- 실제 중력 설정 오브젝트는 `GravitySystem` 아래에서 관리하고, 환경의 `GravityEventTrigger`가 필요한 설정을 직렬화 참조한다.
- 초기 구현은 Inspector 직렬화 데이터로 구성하고 ScriptableObject는 만들지 않는다.
- 필요한 설정은 동작 종류, 방향 또는 방향 목록, 세기, 변경 간격과 예고 시간이다.
- 고정형 중력의 정본은 현재 `Vector3 direction`이며 Phase 3에서는 월드 축으로 해석한다. 방향 enum과 벡터를 동시에 직렬화해 두 번째 정본을 만들지 않는다.
- Inspector 작성 편의를 위해 `World +X`, `World -X`, `World +Y`, `World -Y`, `World +Z`, `World -Z` 프리셋을 제공할 수 있지만, 프리셋은 기존 벡터 값을 설정할 뿐 런타임 방향 계약을 제한하지 않는다.
- 프리셋 이름은 관점에 따라 달라지는 Left·Right·Forward 대신 월드 축과 부호를 명시한다.
- 디버그 적용도 선택한 실제 `GravityPreset`을 `GravityManager.ApplyPreset()`에 전달한다. 방향 전용 Apply API나 디버그 enum으로 `GravityState`를 직접 덮어쓰지 않는다.
- Player 판정과 첫 Enter 이후의 one-shot 여부는 기존 `GravityEventTrigger`가 계속 소유한다.

### 6.4 `GravityBody`

`GravityBody`는 사용자 정의 중력을 실제로 받아야 하는 동적 Rigidbody에만 붙인다.

- 같은 오브젝트의 Rigidbody를 요구한다.
- Unity 기본 `useGravity`를 끈다.
- `FixedUpdate`에서 현재 `GravityState.Gravity`를 `ForceMode.Acceleration`으로 적용한다.
- 중력 변경 시 잠든 Rigidbody를 깨워 새 방향에 반응시킨다.
- 플레이어에는 붙이지 않는다. `PlayerController`와 힘이 중복되기 때문이다.
- 지형, 고정 바위, 벽, 바닥, 기둥과 몬스터 이동 Prefab에는 붙이지 않는다.

### 6.5 `GameFlowManager`

- 기존 트리거 구독과 `GameFlowState` 갱신 책임을 유지한다.
- 트리거가 가리키는 `GravityPreset`을 `GravityManager`에 전달하는 조정만 추가한다.
- 구체적인 방향 벡터, 세기, 타이머와 Coroutine을 소유하지 않는다.
- 사망·리스폰 시 현재 진행 상태에 대응하는 Zone 복구를 요청한다.

## 7. 오브젝트별 중력 반응 계약

| 대상 | 방향 참고 | 실제 중력 힘 | 적용 방식 |
| --- | --- | --- | --- |
| 플레이어 | O | O | `PlayerController`; 전환 중 위치·속도 고정 후 완료 시 새 중력 적용 |
| 카메라 | O | X | `PresentationUp`을 따라 반시계 Roll 후 새 Up으로 yaw·pitch 기준 재계산 |
| 박스·작은 돌·금속 잔해 | O | O | 명시적 `GravityBody`; 전환 시작 즉시 새 중력 적용 |
| 정적 바위·벽·바닥·기둥 | X | X | Rigidbody·GravityBody 없음 |
| 지네 | O | X | 현재 중력 기준 바닥 이동 |
| 거미 | 선택적 | X | waypoint 표면 normal과 부착 이동 유지 |
| 땅 지렁이 | O | X | 현재 중력 기준 출현 면 계산 |
| 공중몹 | X | X | 자체 비행 유지, MVP 제외 가능 |
| 투사체 | X | X | MVP 직선 공격 규칙 유지 |
| 죽은 몬스터 대체물 | O | O | 필요할 때만 별도 물리 오브젝트 사용 |

살아 있는 몬스터의 실제 낙하는 이번 계획에서 구현하지 않는다. 향후 필요하면 `MonsterState.Falling`과 별도 물리 전환 계약을 새로운 계획으로 다룬다.

## 8. Phase 진행 규칙

- 각 Phase는 코드 작성 전에 대상 파일과 현재 Git diff를 다시 확인한다.
- 변경은 `GamePlayScene_Player`, 플레이어·중력 코드와 전용 Prefab에 한정한다.
- Unity 재컴파일 오류가 있으면 Play Mode로 넘어가지 않는다.
- 자동으로 확인할 수 있는 참조·컴포넌트·Console 상태를 먼저 확인하고, 실제 조작과 체감은 사용자가 Play Mode에서 확인한다.
- Phase 완료 기준을 모두 만족하지 못하면 다음 Phase를 시작하지 않는다.
- Phase 2.5와 Zero Gravity에서 명시적으로 합의한 1회 전환 처리를 제외하고, 실패를 추가 보간, 임의 fallback, Trigger 중복 차단 또는 매 프레임 속도 덮어쓰기로 숨기지 않는다.
- Phase 중 새로운 public API, 씬 소유권 변경, 몬스터 구조 변경 또는 ProjectSettings 수정이 필요해지면 작업을 멈추고 영향과 대안을 먼저 공유한다.

## 9. Phase 1 — 고정 90도 방향 전환 세로 조각

### 목표

기본 아래 중력에서 한 방향의 90도 중력으로 한 번 전환하고, 플레이어와 카메라가 새 바닥 기준으로 계속 조작되는 최소 완성 단위를 만든다.

### 범위

- `GravityState` 런타임 변경 API와 변경 이벤트
- 고정형 한 종류만 처리하는 최소 `GravityManager`
- 한 개 `GravityPreset`의 방향·세기 설정
- `GravityEventTrigger` → `GameFlowManager` → `GravityManager` 연결
- 실제 Zone 이동 없이 선택한 `GravityPreset`을 즉시 적용하는 Phase 1 전용 수동 테스트 하네스
- Player Rigidbody 회전 제약 검토와 방향성 정렬
- 카메라의 월드 Up 의존 제거
- `Zone_03_GravityShift`의 한 번 전환

### 구현 방향

- 첫 방향은 실제 Zone 벽 Collider를 새 바닥으로 사용할 수 있는 90도 축으로 정한다.
- Player는 현재 `GravityState`를 계속 참조하며 별도 중력 상태 복사본을 만들지 않는다.
- Rigidbody의 X/Z 회전 고정이 `MoveRotation`을 막으면 해당 제약을 제거하고, 회전은 `PlayerController.AlignWithGravity()`가 계속 소유한다.
- 카메라는 `Vector3.up` 대신 현재 중력 Up을 사용한다.
- 중력 Up이 바뀌는 프레임에는 기존 카메라 전방을 새 Up 평면에 투영해 가능한 한 시선 방향을 보존한다.
- 투영 결과가 거의 0이면 플레이어 또는 카메라의 안정적인 보조 전방을 사용한다.
- Phase 1에서는 전환 연출을 추가하지 않고 기존 정렬 속도로 기능을 먼저 검증한다.
- 방향 전환 시 플레이어 선속도는 우선 보존한다. 통제 불가능한 발사나 관통이 재현될 때만 이전·새 중력축 속도 정책을 별도 근거로 조정한다.
- 수동 테스트는 `GameFlowState`나 `GravityEventType`을 복제하지 않고 `GravityManager`의 Preset 참조를 사용한다.
- `GravityManager`의 Custom Inspector에서 실제 `GravityPreset`을 선택하며 운영 경로와 동일한 `ApplyPreset(GravityPreset)` 진입점을 호출한다.
- 두 버튼은 Play Mode에서만 활성화되고 Zone 참조가 비어 있으면 해당 버튼을 비활성화한다.
- 디버그 경로는 `GravityState` 직렬화 필드나 `GameFlowManager.CurrentState`를 직접 덮어쓰지 않는다. 그래야 이전 Zone 취소, 활성 Zone 교체와 중력 변경 이벤트까지 같이 검증된다.
- Phase 1에서는 Normal을 `initialPreset`, 90도 방향 전환을 테스트 Preset으로 구성한다.
- 별도 DebugController와 제어 모드를 두지 않으며 Inspector 버튼은 사용자가 명시적으로 누를 때만 동작한다.

### 예상 변경 파일

- `Assets/_Scripts/Gravity/GravityState.cs`
- `Assets/_Scripts/Gravity/GravityManager.cs` 신규
- `Assets/_Scripts/Gravity/GravityPreset.cs` 신규
- `Assets/_Scripts/GameFlow/GameFlowManager.cs`
- 필요할 경우 `Assets/_Scripts/GameFlow/Triggers/GravityEventTrigger.cs`
- `Assets/_Scripts/Player/PlayerController.cs`
- `Assets/_Scripts/Player/ThirdPersonCameraController.cs`
- `Assets/_Custom/Prefabs/Player/Player.prefab`
- `Assets/_Scenes/GamePlayScene_Player.unity`

### 실행 순서

1. `[Git·씬·Console 기준선과 사용자 미커밋 변경 기록]` → verify: `[GameDesign과 Master Plan 변경 보존, Original·ProjectSettings 의도하지 않은 diff 없음]`
2. `[GravityState 런타임 변경 계약 구현]` → verify: `[정규화, 세기 0 이상, 동일 값 중복 이벤트 방지, 컴파일 오류 0건]`
3. `[최소 GravityManager와 고정형 GravityPreset 구현]` → verify: `[새 Preset 적용 1회당 GravityState가 정확히 한 번 변경]`
4. `[GravityManager Inspector 테스트 진입점 구현]` → verify: `[모든 테스트가 동일한 ApplyPreset 경로만 호출]`
5. `[GravityEventTrigger 첫 Enter와 GameFlowManager 연결]` → verify: `[첫 접촉에 Flow 상태와 중력 상태가 함께 한 번 변경]`
6. `[Player Rigidbody 제약과 정렬 경로 수정]` → verify: `[90도 회전 가능, 물리 루트와 VisualRoot가 서로 싸우지 않음]`
7. `[카메라를 현재 중력 Up 기준으로 수정]` → verify: `[새 벽 착지 후 좌우 yaw·상하 pitch·카메라 충돌이 새 기준으로 동작]`
8. `[Zone_03_GravityShift에 첫 고정 방향 설정]` → verify: `[Inspector 참조 누락과 잘못된 0 방향 없음]`
9. `[수동 전환과 트리거 전환 비교]` → verify: `[같은 Zone을 선택했을 때 GravityState·Player·Camera 결과가 동일]`
10. `[Play Mode 세로 조각 테스트]` → verify: `[트리거 통과→낙하→정렬→착지→이동·점프까지 한 흐름으로 성공]`
11. `[종료 후 diff·씬 저장 상태 확인]` → verify: `[Original, 지형 Collider, Package, ProjectSettings 변경 없음]`

### Play Mode 테스트

- Normal 구간에서 기존 이동·점프·달리기·웅크리기·사격이 회귀하지 않는다.
- 플레이어를 이동하지 않고 수동으로 Normal과 Shift를 반복 선택할 수 있다.
- 수동 적용과 실제 Trigger 적용이 같은 `GravityManager.ApplyPreset()` 경로를 통해 같은 중력·플레이어·카메라 결과를 낸다.
- Inspector 버튼을 누르지 않으면 수동 테스트가 후속 트리거 상태를 덮어쓰지 않는다.
- Shift Trigger에 처음 닿는 순간 중력이 한 번만 90도 변경된다.
- 플레이어가 새 중력 방향으로 떨어지고 몸의 Up이 새 바닥 normal과 맞는다.
- 새 바닥에서 WASD, 점프, 달리기와 웅크리기가 가능하다.
- 카메라가 월드 Up에 남거나 갑자기 뒤집히지 않는다.
- 플레이어가 이동 중 트리거를 통과해도 비정상적인 고속 발사·회전·Collider 관통이 없다.
- 동일 one-shot 트리거 재접촉으로 중력이 중복 변경되지 않는다.
- 신규 Console 오류, Missing Reference와 NaN 회전이 없다.

### 완료 기준

- 사용자 조작으로 `Normal → 90도 Shift → 새 바닥 이동·점프`를 반복 재현할 수 있다.
- 수동 Zone 선택으로 Normal·90도 Shift 반응을 동선과 독립적으로 반복 검증할 수 있고, 트리거 전환과 결과가 일치한다.
- 플레이어와 카메라 모두 현재 중력 Up을 사용한다.
- 기존 기본 플레이와 전투 입력이 유지된다.
- Phase 1 외의 주기 중력, 동적 잔해와 무중력 코드는 아직 추가하지 않는다.

### 구현·검증 결과 (2026-08-24)

- `GravityState.SetGravity()`와 `Changed`, 고정형 `GravityPreset`, `GravityManager`를 구현했다.
- `GravityEventTrigger.Preset` → `GameFlowManager` → `GravityManager.ApplyPreset()` 경로를 연결했다.
- `GravitySystem` 아래 Normal `(0, -1, 0)`과 Shift `(+1, 0, 0)` Zone을 구성하고 Shift Trigger, Camera, Flow 참조를 저장했다.
- Player Rigidbody의 X/Z 회전 고정을 제거하고 Camera yaw·pitch 기준을 현재 중력 Up으로 변경했다.
- Unity 스크립트 재컴파일은 `failed=false`, 컴파일 오류 0건으로 완료했다.
- Play Mode에서 Normal 초기값과 임시 Shift 초기 Zone 활성화를 각각 확인했다. Shift에서 `GravityState.Direction == (+1, 0, 0)`이 적용되고 Player와 Camera Rig가 같은 축으로 이동했으며 신규 게임플레이 오류는 없었다. 테스트 후 초기 Zone은 Normal로 복구했다.
- 반대편 통과 판정을 제거하고 Player가 Trigger에 처음 닿는 순간 한 번 실행하도록 단순화했다.
- 별도 `GravityDebugController`를 제거하고 `GravityManager`의 Play Mode 전용 Inspector 버튼에서 Normal ↔ Shift를 반복 적용하도록 검증 진입점을 모았다.
- 사용자 Play Mode 테스트에서 Trigger 첫 접촉 중력 전환, Player와 두 테스트 Cube의 새 벽 착지, 착지 후 Player 이동을 확인했다.
- 남은 사용자 검증은 새 Inspector 버튼의 Normal ↔ Shift 반복, 사격 피격 이동과 Runtime Shot Debug 값 확인이다.

### 중단 조건

- Rigidbody 제약을 제거한 뒤 충돌 토크로 플레이어가 계속 쓰러지면 임시 Freeze를 되돌려 숨기지 않고 회전 소유권을 다시 설계한다.
- 카메라 기준축 변경이 조준 Ray와 VisualRoot 방향을 어긋나게 하면 중력 기능 완료로 보지 않는다.
- 새 바닥 Collider가 실제 진행에 사용할 수 없으면 지형을 임의 수정하지 않고 팀장 Collider 인계 문제로 보고한다.

## 10. Phase 2 — 선택적 동적 Rigidbody 중력 반응

### 목표

플레이어와 별도로, 명시적으로 선택한 박스·작은 돌·금속 잔해가 같은 중력 상태를 받아 새 방향으로 떨어지게 한다.

### 범위

- `GravityBody` 구현
- 단순 Cube와 연출용 잔해의 opt-in 구성
- 중력 변경 시 잠든 Rigidbody 깨우기
- 질량과 무관한 동일 가속도 확인
- 정적 지형의 중력 비참여 확인
- 테스트 Cube의 통로 이동과 사격 피격 이동

### 구현 방향

- `GravityBody`가 있는 동적 Rigidbody만 사용자 정의 중력을 받는다.
- `useGravity = false`를 강제하고 현재 `GravityState.Gravity`를 `ForceMode.Acceleration`으로 적용한다.
- `GravityState` 변경 이벤트에서 Rigidbody를 `WakeUp()`한다.
- Phase 2에서는 중력 반응 배율, 항력 프리셋과 오브젝트별 예외를 추가하지 않는다.
- 지형용 환경 Prefab 원본을 수정하지 않는다. 테스트 Cube 또는 별도 연출용 Prefab을 사용한다.
- Scene 전용 Cube 두 개는 `0.4` 크기로 줄여 Shift Trigger 직전의 Normal 중력 바닥에 배치한다.
- 사격 Ray가 비키네마틱 `GravityBody`를 맞힌 경우에만 Rigidbody를 깨우고 `1.5`의 `ForceMode.VelocityChange`를 무게중심에 적용한다.
- 사격으로 인한 회전보다 통로 운반에 필요한 병진 이동을 우선하며, 마지막 Collider·Rigidbody·힘 적용 여부를 Inspector에서 확인한다.

### 예상 변경 파일

- `Assets/_Scripts/Gravity/GravityBody.cs` 신규
- `Assets/_Scripts/Gravity/Editor/GravityManagerEditor.cs` 신규
- `Assets/_Scripts/Player/PlayerCombatController.cs`
- `Assets/_Custom/Prefabs/Player/Player.prefab`
- `Assets/_Scenes/GamePlayScene_Player.unity`

### 실행 순서

1. `[GravityBody 최소 계약 구현]` → verify: `[Rigidbody 필수, useGravity false, FixedUpdate AddForce, 이벤트 구독 해제 대칭]`
2. `[질량이 다른 테스트 오브젝트 2개 구성]` → verify: `[두 오브젝트 모두 GravityBody만 opt-in하고 지형에는 추가되지 않음]`
3. `[Normal 중력 낙하 검증]` → verify: `[동일 높이에서 질량과 무관하게 같은 가속도로 낙하]`
4. `[90도 Shift 검증]` → verify: `[잠든 오브젝트가 깨어나 플레이어와 같은 방향으로 낙하]`
5. `[사격 피격 이동 연결]` → verify: `[GravityBody만 질량과 무관하게 한 발당 작은 폭으로 이동]`
6. `[정적 지형 회귀 확인]` → verify: `[바위·벽·바닥 Transform과 Collider 구성 불변]`

### Play Mode 테스트

- Player와 테스트 Cube가 같은 방향으로 떨어진다.
- 질량이 달라도 중력 가속도 차이가 발생하지 않는다.
- Cube가 잠든 상태에서 중력이 바뀌어도 새 방향으로 반응한다.
- 중력이 바뀌기 전 기존 속도가 Unity 기본 중력과 중복되어 증가하지 않는다.
- 정적 바위와 지형은 움직이지 않는다.
- Player에는 `GravityBody`가 없어 중력이 두 번 적용되지 않는다.
- 테스트 Cube는 총에 맞으면 조금씩 이동하고 정적 지형과 Player는 사격 힘을 받지 않는다.

### 완료 기준

- 플레이어와 최소 2개의 동적 오브젝트가 한 번의 90도 전환에 함께 반응한다.
- 중력 영향 대상이 컴포넌트 부착 여부로 명확히 구분된다.
- 씬 전체 Rigidbody 검색과 지형 Prefab 수정이 없다.

### 구현·검증 결과 (2026-08-24)

- `GravityBody`를 추가하고 Rigidbody 필수 계약, Unity 기본 중력 비활성화, `ForceMode.Acceleration` 기반 사용자 정의 중력, `GravityState.Changed` 구독·해제와 변경 시 `WakeUp()`을 구현했다.
- `GamePlayScene_Player/GravitySystem` 아래에 질량 `1`과 `5`, 크기 `0.4`인 Scene 전용 Cube 두 개를 Shift Trigger 직전 바닥에 배치하고 같은 `GravityState`를 연결했다. Player와 정적 지형에는 `GravityBody`를 추가하지 않았다.
- Player 사격이 비키네마틱 `GravityBody`에만 `1.5`의 질량 독립 속도 변화를 무게중심에 적용하도록 보강하고, 적용 전에 Rigidbody를 깨우도록 했다.
- `GravityManager` Inspector에 Play Mode 전용 Initial·Manual Zone 버튼과 현재 Zone 표시를 추가해 숨겨진 Context Menu 없이 Normal과 Shift를 전환할 수 있게 했다.
- Player의 마지막 사격 Collider·GravityBody Rigidbody·물리 밀기 성공 여부를 Inspector 런타임 상태로 노출했다.
- 신규 파일과 검증 경로 개선 코드를 생성 Unity C# 프로젝트에 포함한 정적 빌드는 오류 0건, 기존 경고 19건으로 완료했다.
- 사용자 Play Mode 테스트에서 Trigger 진입 즉시 중력이 전환됐고, 전환 후 Player와 질량 `1`·`5` 테스트 Cube가 모두 새 벽에 착지했으며 Player 이동도 정상 동작했다.
- 강화한 사격 이동과 새 Inspector 버튼은 Unity의 스크립트 재로드 후 Play Mode 재검증이 남아 있다.

## 11. Phase 2.5 — 중력 전환 테스트 UX와 회전 연출 기반

### 목표

Zone 수가 늘어나도 Play Mode 테스트 조작이 복잡해지지 않게 하고, `Normal → Shift` 전환을 환경의 즉시 중력 반응과 플레이어·카메라의 짧은 회전 연출로 분리한다.

환경 물체는 전환 시작 즉시 새 중력에 반응한다. 플레이어는 월드 위치와 속도를 잠시 고정하고 새 Up으로 회전하며, 카메라는 화면 기준 반시계 방향으로 Roll한다. 회전이 끝나면 플레이어를 해제해 새 중력 방향으로 낙하하거나 새 바닥에 착지시킨다.

### 범위

- 씬의 `GravityPreset`을 하나의 드롭다운에서 선택하는 Play Mode 테스트 UI
- Zone 수와 무관하게 고정된 `Apply Selected Preset`과 `Restore Initial Preset` 조작
- 현재 Zone, 실제 중력, 전환 대상, 진행률과 전환 여부의 Inspector 런타임 표시
- `GravityManager`가 소유하는 단일 전환 진행률과 `PresentationUp`
- 플레이어의 전환 Anchor 위치·속도 고정과 일반 이동 입력 일시 중지
- 플레이어 Rigidbody·VisualRoot와 카메라 Roll의 같은 진행률 사용
- 현재 시선 전방축을 기준으로 한 화면 반시계 Roll
- 전환 완료 후 Grounded/Airborne 재판정과 새 중력 적용 재개
- 전환 도중 새 Zone 요청 시 현재 표시 자세에서 최신 목표로 이어지는 취소·재시작 규칙
- Capsule 회전 공간, 위치 흔들림, Collider 겹침과 입력 복구 검증

### 하지 않을 것

- Phase 3의 Inversion·FastDown·Slow Zone 실제 구성
- Phase 4의 주기 실행과 예고 타이머
- Zero Gravity 이동 정책과 그래플 구현
- `GravityEventType` 또는 별도 enum에 방향·세기·Zone 목록 하드코딩
- Zone마다 테스트 버튼이나 직렬화된 디버그 슬롯 추가
- `GravityState` 직렬화 필드 또는 `GameFlowManager.CurrentState` 직접 덮어쓰기
- `Time.timeScale` 변경이나 모든 `GravityBody`의 전역 정지
- 근거 없이 Player Collider를 비활성화하거나 Rigidbody를 `isKinematic`으로 전환
- 카메라 흔들림, 화면 왜곡, VFX, SFX와 전환 전용 애니메이션 제작

### 책임과 전환 순서

```text
GravityManager.ApplyPreset(targetPreset)
  1. 이전 전환 취소
  2. 현재 PresentationUp과 플레이어 Anchor 확정
  3. 플레이어 GravityTransition 진입
  4. GravityState를 목표 물리값으로 한 번 변경
     └─ GravityBody는 즉시 WakeUp하고 새 방향으로 낙하
  5. 하나의 진행률로 Player와 Camera를 목표 Up까지 회전
  6. 목표 자세와 PresentationUp을 정확히 확정
  7. 플레이어 GravityTransition 해제
  8. Grounded/Airborne 재판정 후 이동·낙하 재개
```

- `GravityState`는 전환당 실제 물리값을 한 번만 변경하며 매 프레임 보간된 중력 벡터를 저장하지 않는다.
- `GravityManager`는 `IsTransitioning`, `PresentationUp`, 목표 Zone과 진행률을 소유하고 시작·완료 신호를 제공한다.
- `PlayerController`는 전환 시작 시 월드 Anchor, 선속도와 각속도를 확정하고 일반 이동·점프·달리기·웅크리기와 플레이어 중력 적용을 일시 중지한다.
- 전환 중 플레이어의 이전 속도는 보존하지 않는다. 완료 시 속도 `0`에서 새 중력에 반응하게 해 예상하지 못한 발사를 막는다.
- `ThirdPersonCameraController`는 Roll 중 Look 입력을 적용하지 않고, 완료 후 목표 Up에서 Orbit 전방을 한 번 재구성한다.
- Player와 Camera가 각각 독립 Tween을 소유하지 않는다. 보간 방식과 Ease가 달라도 `GravityManager`의 같은 정규화 진행률을 소비한다.
- 실제 게임 진행의 `Normal → Shift`는 화면 기준 반시계 Roll을 사용해 지형이 시계 방향으로 회전하는 느낌을 준다.
- Unity 회전 부호를 `+` 또는 `-`라는 이름만으로 확정하지 않고 Game View 화면 결과로 반시계 방향을 검증한다.
- `Restore Initial Preset`은 반복 테스트를 위한 명시적 초기화이며 실제 진행 Roll의 방향성 완료 기준에 포함하지 않는다. 운영 중력 적용은 동일하게 `GravityManager`를 통과한다.

### Inspector 테스트 UX

- Custom Inspector는 현재 씬의 활성·비활성 `GravityPreset`을 찾아 Hierarchy 경로가 포함된 드롭다운으로 표시한다.
- 테스트 선택은 에디터 세션 상태로 보관해 선택만으로 씬이나 Prefab에 직렬화 diff를 만들지 않는다.
- `Apply Selected Preset`은 선택한 실제 `GravityPreset`을 `ApplyPreset()`에 전달한다.
- `Restore Initial Preset`은 명시적으로 초기 중력 설정을 복구하고 진행 중 전환을 정리한다.
- 읽기 전용 Runtime 영역에는 최소한 `Current Zone`, `Direction`, `Strength`, `Is Transitioning`, `Target Zone`, `Progress`를 표시한다.
- 테스트 UI가 별도 모드, `GameFlowState` 또는 물리 방향의 두 번째 정본이 되지 않게 한다.

### 플레이어·카메라 회전 규칙

- 90도 전환의 첫 조정값은 약 `0.45~0.6초` 범위에서 시작하고 최종값은 사용자 Play Mode 체감으로 확정한다.
- 카메라는 현재 시선 전방축을 중심으로 Roll하고 전환 중 yaw·pitch 입력을 고정해 회전축이 흔들리지 않게 한다.
- 플레이어 Rigidbody와 VisualRoot는 같은 `PresentationUp`을 사용하되 물리 루트와 시각 루트가 서로 다른 최종 자세를 남기지 않는다.
- 새 요청이 진행 중 들어오면 시작 자세로 되돌리거나 완료 콜백을 중첩하지 않고 현재 `PresentationUp`에서 최신 목표로 이어간다.
- 향후 180도 Inversion에서도 회전축과 방향이 결정적이어야 하며, 최단 회전에 우연히 맡기지 않는다. 실제 Inversion 구성은 Phase 3에서 진행한다.

### 충돌과 입력 안전 원칙

- 플레이어 위치 고정은 `PlayerController`가 소유하며 매 프레임 Transform을 직접 덮어써 물리 루트와 싸우지 않는다.
- 회전 전·완료 직전에 Capsule이 목표 자세에서 정적 지형과 겹치는지 확인한다.
- 물리 Solver가 Anchor를 지속적으로 밀어 위치가 떨리면 강제 고정을 반복해 숨기지 않고 회전 공간, Trigger 위치 또는 Anchor 보정 규칙을 다시 설계한다.
- 전환 종료·중간 취소·컴포넌트 비활성화의 모든 경로에서 이동과 Look 입력이 복구돼야 한다.
- Player가 이미 새 바닥과 접촉하면 Grounded로, 지지면이 없으면 Airborne으로 재진입한다.

### 예상 변경 파일

- `Assets/_Scripts/Gravity/GravityManager.cs`
- `Assets/_Scripts/Gravity/Editor/GravityManagerEditor.cs`
- `Assets/_Scripts/Player/PlayerController.cs`
- 필요할 경우 `Assets/_Scripts/Player/PlayerMotionStateMachine.cs`
- `Assets/_Scripts/Player/ThirdPersonCameraController.cs`
- 필요할 경우 `Assets/_Custom/Prefabs/Player/Player.prefab`
- 필요할 경우 `Assets/_Custom/Prefabs/Player/ThirdPersonCameraRig.prefab`
- 필요한 참조와 테스트 값에 한해 `Assets/_Scenes/GamePlayScene_Player.unity`

### 실행 순서

1. `[기준선과 기존 사용자 변경 확인]` → verify: `[Phase 2 잔여 검증과 별개로 Original·Collider·Package·ProjectSettings 보호 범위 기록]`
2. `[Inspector Zone 선택기 개선]` → verify: `[Zone 개수와 무관하게 드롭다운과 고정 버튼만 표시되고 씬에 디버그 선택 diff가 생기지 않음]`
3. `[물리 중력과 PresentationUp 계약 분리]` → verify: `[GravityState는 한 번 변경되고 환경 GravityBody는 전환 시작 즉시 반응]`
4. `[플레이어 GravityTransition 구현]` → verify: `[위치·속도 고정, 일반 이동과 플레이어 중력 일시 중지, 런타임 상태 관찰 가능]`
5. `[카메라 화면 반시계 Roll 구현]` → verify: `[Game View에서 지형이 시계 방향으로 회전하고 Roll 중 Look 축이 흔들리지 않음]`
6. `[Player·Camera 단일 진행률 동기화]` → verify: `[같은 프레임에 목표 Up에 도착하고 최종 회전 오차가 남지 않음]`
7. `[전환 종료 후 물리 재개]` → verify: `[속도 0에서 새 방향으로 낙하하거나 새 바닥 Grounded로 진입]`
8. `[중간 재전환·초기화 처리]` → verify: `[중복 완료·입력 고착 없이 현재 자세에서 최신 목표 또는 초기 상태로 복구]`
9. `[충돌·반복 Play Mode 검증]` → verify: `[Normal → Shift를 5회 재현하고 Capsule 관통·Anchor 떨림·카메라 점프 없음]`
10. `[종료 후 diff와 문서 결과 기록]` → verify: `[보호 영역 변경 없음, 구현 증거와 사용자 체감 검증 분리 기록]`

### Play Mode 테스트

- Trigger 진입 순간 두 테스트 Cube가 새 방향으로 먼저 굴러가거나 떨어진다.
- Player의 월드 위치는 Roll 중 눈에 띄게 이동하거나 떨리지 않는다.
- Player의 선속도와 각속도는 전환 시작 시 제거되고 완료 전 다시 누적되지 않는다.
- 카메라가 화면 기준 반시계로 Roll해 지형이 시계 방향으로 회전하는 느낌을 준다.
- Player와 Camera가 서로 다른 진행률이나 반대 방향으로 회전하지 않는다.
- Roll 중 Look과 일반 이동 입력이 전환 자세를 흔들지 않는다.
- 완료 후 입력이 즉시 복구되고 Player가 새 중력 방향으로 직선 낙하하거나 새 바닥에 착지한다.
- 전환 중 새 Zone 적용, 컴포넌트 비활성화와 Play Mode 종료에서 잔여 callback과 입력 고착이 없다.
- 목표 회전 전·후 Capsule이 바닥·벽과 겹치거나 Solver에 의해 반복적으로 튀지 않는다.
- 카메라 중심 조준 Ray와 VisualRoot 전방이 완료 후 같은 기준을 사용한다.

### 완료 기준

- Zone이 늘어나도 Inspector 테스트 버튼 수가 늘어나지 않는다.
- Inspector 테스트와 Trigger가 실제 `GravityManager` 활성화 경로를 사용한다.
- 환경 물체의 중력은 즉시 바뀌고 플레이어의 물리 적용만 명시적 전환 상태에서 보류된다.
- 화면 반시계 Roll, 플레이어 Anchor 고정과 완료 후 새 방향 낙하가 한 흐름으로 재현된다.
- Normal에서 Shift로 5회 반복해도 Collider 관통, 위치 떨림, 카메라 점프와 입력 고착이 없다.
- 전환 시간과 어지러움 정도는 사용자 Play Mode 확인을 통과한다.
- Phase 3 이후 Zone은 이 전환 계약을 재사용하며 독자적인 플레이어 고정·카메라 Roll 코드를 만들지 않는다.

### 구현·자동 검증 결과 (2026-08-24)

- `GravityManager`가 물리 중력을 전환 시작 시 한 번 적용하고, 결정적인 회전축·단일 진행률·`PresentationUp`·시작/완료 신호를 소유하도록 구현했다.
- 전환 중 재요청은 현재 `PresentationUp`에서 최신 목표로 이어지고, 같은 활성 Zone의 중복 요청은 전환을 다시 시작하지 않는다.
- `PlayerController`는 전환 중 기존 Rigidbody 제약을 보존한 채 위치 제약만 임시 추가하고, 속도·점프 요청·일반 이동과 플레이어 중력 적용을 보류한다. 완료·취소·비활성화 시 원래 제약과 이동 상태를 복구한다.
- `ThirdPersonCameraController`는 별도 Tween 없이 같은 `PresentationUp`을 사용하고, 전환 중 Look을 소비하지 않으며 완료 후 목표 Up에서 Orbit 기준을 재구성한다.
- Custom Inspector는 씬의 활성·비활성 `GravityPreset`을 Hierarchy 경로 드롭다운으로 표시한다. 선택은 `SessionState`에만 보관하며 `Apply Selected Preset`과 `Restore Initial Preset` 모두 실제 `GravityManager` API를 사용한다.
- 런타임·Editor 어셈블리 빌드는 오류 0건으로 통과했다. 기존 외부 에셋과 게임플로 코드 경고 19건은 이번 변경과 무관하며 그대로 남아 있다.
- Unity MCP Play Mode에서 Shift 활성화 직후 물리 방향 `(+1, 0, 0)`, 진행률 `0`, Player 속도·각속도 `0`, 임시 `FreezePosition`과 Player 전환 상태를 확인했다.
- 전환 완료 후 `PresentationUp`, Player Up과 Camera Up이 모두 `(-1, 0, 0)`으로 일치하고, 진행률 `1`, Player 전환 해제와 Rigidbody 제약 `None` 복구를 확인했다. Initial Zone 복구 후에도 세 Up이 모두 `(0, 1, 0)`으로 일치했다.
- 진행률 `0.782`에서 Shift로 재요청했을 때 요청 전후 `PresentationUp` 차이는 `0.000000`이었고, 현재 표시 자세에서 연속 전환됐다.
- Normal과 Shift를 5회 왕복 적용한 뒤에도 최종 상태 고착, 중복 전환과 신규 Console 오류가 없었다. Play Mode 종료 후 씬은 dirty 상태가 아니었다.
- 전환 전에 `PlayerInput.AllowMovement`와 `AllowLook`을 모두 `false`로 둔 테스트에서 완료 후에도 두 값이 `false`로 유지돼, 전환이 대화·컷신 등 상위 입력 잠금을 임의로 해제하지 않음을 확인했다.
- 사용자가 Play Mode에서 중력 전환 테스트가 성공적임을 확인해 화면 Roll, 전환 체감과 실제 조작을 포함한 Phase 2.5 완료 기준을 통과했다.
- 다른 팀원도 선택한 Zone과 실행 버튼을 한 흐름에서 찾을 수 있도록 Custom Inspector를 `Gravity Zone Select` 조작 영역과 `Play Mode Zone Info` 런타임 정보 영역으로 분리했다.

### 중단 조건

- Capsule 회전 공간을 확보하지 못해 위치 고정과 충돌 안정성을 동시에 만족할 수 없으면 Collider를 끄거나 관통시켜 숨기지 않고 Anchor·Trigger 배치를 다시 협의한다.
- Player와 Camera가 단일 진행률로도 다른 목표 자세에 도착하면 보간 속도 조정보다 Up·Forward 소유권을 먼저 점검한다.
- 화면 반시계 Roll을 위해 목표 자세와 무관한 270도 회전이 필요해지면 실제 진행 전환과 테스트 초기화 정책을 분리해 다시 확정한다.
- 주기 전환 간격보다 고정 시간이 길어질 가능성이 보이면 Phase 4 구현 전에 전환 시간과 조작 중단 비율을 기획 기준으로 정한다.

## 12. Phase 3 — 고정형 Zone 상태 확장과 전환 소유권 정리

### 목표

Normal `World -Y`, Gravity Shift `World +X`, 고정 Inversion `World -X`를 같은 Zone 활성화 계약으로 처리한다.

Phase 3에서는 Shift에서 Inversion으로 넘어가는 한 번의 180도 전환을 안정화하고, 순차 진행·역행 미지원·방향 프리셋 계약을 확정해 Phase 4의 주기적 Reverse Gravity가 같은 전환 경로를 그대로 재사용할 수 있게 한다.

### 범위

- `GravityPreset` 고정형 설정 일반화
- Normal `(-Y, 9.81)`, Shift `(+X, 9.81)`, Inversion `(-X, 9.81)` 설정
- Shift `+X`에서 Inversion `-X`로 향하는 결정적 180도 전환
- `Vector3 direction`을 유지하는 월드 6축 Inspector 작성 프리셋
- 활성 Zone 추적과 중복 활성화 방지
- GameFlow 상태와 중력 상태의 일치 확인
- Trigger one-shot과 문 기반 순차 진행을 전제로 한 역행 미지원 계약

### 구역별 중력 계약

| 스테이지 구역 | 중력 설정 | 적용 Phase | 계약 |
| --- | --- | --- | --- |
| Entry / Normal Gravity | `World -Y`, `9.81` | Phase 1·3 | 초기 상태이자 명시적 Normal 복구 상태 |
| Gravity Shift | `World +X`, `9.81` | Phase 1·3 | 벽을 새 바닥으로 사용하는 고정형 방향 전환 |
| Reverse Gravity | 첫 고정 검증 `World -X`, `9.81`; 최종 `+X ↔ -X` | Phase 3·4 | Phase 3에서 180도 한 번, Phase 4에서 예고 후 주기 반복 |
| FastDown | 구성하지 않음 | MVP 제외 | Reverse Gravity 필수 기획이 아니므로 Trigger와 Zone 참조를 비활성 상태로 유지 |
| Slow | 구성하지 않음 | MVP 제외 | Zero Gravity와 혼동될 강도 상태를 만들지 않음 |
| Zero Gravity | 현재 방향·자세 유지, `Strength = 0` | Phase 6 | 중력 제거 때문에 별도 Roll을 발생시키지 않음 |
| Source | Zero Gravity 유지 | Phase 7 통합 | 엔딩 지형이 Normal 복귀를 요구할 때만 별도 Zone 추가 |

Phase 3 구현 범위는 Normal·Shift·고정 Inversion까지다. Zero Gravity와 Source 행은 이후 Phase가 따라야 할 데이터 계약을 미리 고정한 것이며 Phase 3 완료로 계산하지 않는다.

### 구현 방향

- 방향·세기는 `GravityEventType`에 하드코딩하지 않고 각 `GravityPreset`의 Inspector 데이터로 둔다.
- 같은 이벤트 타입이라도 다른 방향을 사용할 수 있게 한다.
- `GravityEventType`은 게임 진행 의미를 유지하고 물리값의 정본이 되지 않는다.
- 물리 설정 오브젝트는 `GravityPreset_Normal`, `GravityPreset_WorldPosX`, `GravityPreset_WorldNegX`로 명명해 기획 구역 의미와 분리한다.
- `GravityPreset`의 런타임 방향 enum은 추가하지 않는다. 전용 Custom Inspector의 6축 버튼은 기존 `direction`을 편집하는 작성 도구로만 동작한다.
- 방향을 테스트할 때도 `GravityManager`의 `Apply Selected Preset`을 사용한다. 여섯 방향별 영구 기획 Zone이나 방향 전용 Apply 버튼은 만들지 않는다.
- 모든 고정형 Zone은 Phase 2.5의 `환경 중력 적용 → 플레이어·카메라 전환 → 플레이어 해제` 계약을 재사용한다.
- Zone별 독자적인 플레이어 위치 고정, 카메라 Roll 또는 입력 복구 코드를 만들지 않는다.
- 새 Zone 활성화는 이전 Zone의 동작을 먼저 종료한 뒤 새 초기값을 적용한다.
- `+X → -X`는 도착 Up만으로 회전축이 결정되지 않는 180도 전환이다. 현재 `GravityManager`의 결정적 fallback 축을 먼저 실제 Game View에서 검증하고, 시각 경로가 맞지 않을 때만 최소한의 전환 축 힌트를 추가한다.
- `FastDown`·`Slow`는 이번 MVP에서 제외한다. 대응 `GravityPreset`을 만들지 않고, 플레이어가 활성 상태의 빈 참조 Trigger에 닿아 오류를 내지 않도록 해당 진행 Trigger를 비활성 상태로 둔다.
- Zero Gravity Trigger와 설정은 Phase 6 전까지 활성화하지 않는다.

### 역행과 진행 차단 계약

- 중력 상태는 마지막으로 성공한 `ApplyPreset()` 결과가 다음 적용 전까지 유지된다.
- 이전 구역으로 물리적으로 돌아가더라도 이전 중력을 복원하지 않고 `OnTriggerExit`에서도 상태를 바꾸지 않는다.
- Trigger는 최초 Enter 한 번만 게임 진행 이벤트를 발생시킨다. 같은 Trigger 재진입과 같은 Zone 중복 활성화는 새 전환을 만들지 않는다.
- Zone 순서 인덱스, 이동 방향, Trigger 진입 면과 현재 Collider 겹침 상태는 중력 시스템이 소유하지 않는다.
- 문 닫기, Zone 건너뛰기 차단과 이전 구역 비활성화는 GameFlow·레벨 진행 책임이다.
- 향후 실제 역행 플레이가 필요해지면 one-shot Trigger를 느슨하게 바꾸는 것으로 처리하지 않고 로컬 중력 볼륨·복귀 Trigger·체크포인트 계약을 별도 계획으로 설계한다.

### 예상 변경 파일

- `Assets/_Scripts/Gravity/GravityPreset.cs`
- `Assets/_Scripts/Gravity/Editor/GravityPresetEditor.cs`
- 180도 전환 축 조정이 실제로 필요한 경우에만 `Assets/_Scripts/Gravity/GravityManager.cs`
- `Assets/_Scenes/GamePlayScene_Player.unity`
- 계획 결과 기록에 한해 이 문서

### 실행 순서

1. `[기준선과 현재 Zone·Trigger 참조 확인]` → verify: `[Normal·Shift 설정 보존, Inversion·FastDown·Slow·Zero의 빈 참조와 활성 상태를 명시적으로 기록]`
2. `[고정형 Zone 공통 데이터 검증 보강]` → verify: `[방향 0, 음수 세기, 빈 참조가 Inspector/런타임 오류로 드러남]`
3. `[월드 6축 Inspector 프리셋 추가]` → verify: `[프리셋이 기존 direction만 변경하고 enum·GravityState·GameFlowState를 새 정본으로 만들지 않음]`
4. `[방향 Preset과 진행 Trigger 연결]` → verify: `[Normal -Y·World +X·World -X가 각각 9.81로 적용되고 미구현 Trigger는 비활성]`
5. `[Shift → Inversion 180도 전환 검증]` → verify: `[결정적 축으로 Player·Camera가 같은 경로를 사용하고 Capsule 관통·입력 고착 없음]`
6. `[순차 진행·중복·역행 계약 검증]` → verify: `[Exit·이전 Trigger 재진입은 중력을 바꾸지 않고 마지막 활성 Zone만 상태를 결정]`
7. `[Flow·중력 상태·최종 diff 대조]` → verify: `[GameFlowState와 적용된 GravityPreset 참조 일치, 보호 영역과 미구현 Zone 변경 없음]`

### Play Mode 테스트

- Normal `-Y` 기본값에서 Shift `+X`로 전환된다.
- Shift `+X`에서 Inversion `-X`로 한 번의 180도 전환이 완료된다.
- 180도 전환축과 회전 방향이 반복 실행에서도 바뀌지 않고 Player와 Camera가 서로 다른 경로를 선택하지 않는다.
- 같은 Zone이 중복 활성화돼도 이벤트나 초기화가 불필요하게 반복되지 않는다.
- 이전 Trigger 방향으로 돌아가거나 Trigger에서 Exit해도 마지막 활성 중력이 유지된다.
- 각 전환 후 플레이어와 GravityBody가 같은 값을 사용한다.
- Play Mode에서 선택한 실제 `GravityPreset`의 방향을 월드 `±X`, `±Y`, `±Z`로 임시 설정해 적용해도 Player·Camera·GravityBody의 기준이 깨지지 않는다. 이 검사는 영구 게임플레이 Zone 여섯 개를 만들지 않는다.
- 비활성 FastDown·Slow·Zero Trigger가 빈 Zone 참조로 실행되지 않는다.

### 완료 기준

- Normal·Shift·Inversion 고정형 Zone이 방향과 세기를 데이터로 결정한다.
- 방향 `Vector3`가 단일 정본이며 월드 6축 프리셋은 작성 편의만 제공한다.
- `GameFlowManager`에 방향 벡터와 물리 타이머가 들어가지 않는다.
- 활성 Zone 하나가 현재 중력 상태를 단독으로 결정한다.
- 역행·Exit·중복 진입이 이전 중력을 복원하거나 새 전환을 중첩하지 않는다.
- `+X → -X` 180도 전환이 사용자 Play Mode에서 방향·충돌·조작 기준을 통과한다.
- FastDown·Slow·Zero Gravity를 Phase 3에서 구현한 것으로 과장하지 않는다.

### Phase 3 구현 결과 — 2026-08-24

- `GravityPreset_WorldNegX`를 `World -X`, `9.81`의 물리 설정으로 추가하고 `Trigger_ToInversion`과 `GameFlowManager.gravityEventTriggers[1]`에 연결했다.
- `GravityPreset_Normal(-Y)`, `GravityPreset_WorldPosX(+X)`, `GravityPreset_WorldNegX(-X)`가 모두 같은 `GravityManager.ApplyPreset(GravityPreset)` 경로를 사용한다.
- `GravityPresetEditor`에 월드 `±X`, `±Y`, `±Z` 작성 버튼을 추가했다. 버튼은 기존 `direction`만 편집하며 새 방향 enum이나 런타임 상태를 만들지 않는다.
- 0/비유한 방향과 음수/비유한 세기는 Inspector 오류로 표시하고, `GravityManager`도 상태·전환을 변경하기 전에 거부한다.
- `Trigger_ToFastDown`, `Trigger_ToSlow`, `Trigger_ToZeroGravity`는 Zone 참조를 비워 둔 채 비활성화했다. Phase 3에서 FastDown·Slow·Zero Gravity는 구현하지 않았다.
- 기존 180도 fallback은 Shift 중력 `+X`에서 Inversion 중력 `-X`로 갈 때 Presentation Up을 월드 `+Y` 축으로 결정적으로 회전시킨다. 별도 Zone별 회전 코드는 추가하지 않았다.
- 자동 검증: `Assembly-CSharp.csproj`, `Assembly-CSharp-Editor.csproj` 빌드 오류 0건; 씬 재로드·AssetDatabase import 확인; 신규 YAML fileID 중복 0건; `git diff --check` 통과.
- 사용자 Play Mode 확인 대기: Shift `+X` → Inversion `-X` 180도 회전 방향, Player·Camera 동일 경로, Capsule 관통 여부, 입력 복구, 중복 적용·역행·Exit 동작. 이 확인 전에는 Phase 3의 사람 조작 완료 기준을 충족한 것으로 표시하지 않는다.

### Phase 3 책임 명칭 정리 — 2026-08-25

- 공간 Zone과 물리 설정을 구분하기 위해 설정 타입을 `GravityZone`에서 `GravityPreset`으로 변경했다.
- 물리 설정 오브젝트는 `GravityPreset_Normal`, `GravityPreset_WorldPosX`, `GravityPreset_WorldNegX`로 명명한다. 이름은 현재 물리 방향만 나타내며 Gravity Shift·Reverse Gravity 같은 기획 의미를 확정하지 않는다.
- 운영 API는 `GravityEventTrigger.Preset` → `GameFlowManager` → `GravityManager.ApplyPreset()`으로 정리하고, 런타임 상태도 `CurrentPreset`·`TargetPreset`으로 표현한다.
- 기획 구역과 Trigger는 Inspector에서 필요한 Preset을 참조한다. 기획자가 방향을 변경할 때 게임 진행 enum이나 물리 코드를 수정할 필요가 없다.

## 13. Phase 4 — 주기적 Reverse Gravity

### 목표

Reverse Gravity Zone이 활성화된 동안 `World +X`와 `World -X`를 일정 시간마다 순환하고, 다른 Zone으로 이동하면 반복과 예고가 즉시 멈추게 한다.

### 범위

- 주기형 `GravityPreset`
- 방향 목록, 간격, 첫 적용 시점과 반복 규칙
- 다음 변경을 알리는 최소 예고 이벤트 또는 디버그 표시
- 주기 동작 취소와 중복 실행 방지
- Player·Camera·GravityBody 반복 전환 검증

### 구현 방향

- `GravityManager`만 현재 주기 실행 핸들을 소유한다.
- 새 Zone 활성화, 컴포넌트 비활성화와 씬 종료 시 반복을 취소한다.
- 방향 배열이 비었거나 간격이 0 이하이면 실행하지 않고 명확한 오류를 남긴다.
- Reverse Gravity의 방향 목록은 `[World +X, World -X]` 순서로 고정한다.
- Zone 활성화 시 첫 방향 `+X`를 즉시 적용한다. 직전 Shift 상태가 이미 `+X`라면 불필요한 전환을 만들지 않고 타이머만 시작하며, 첫 예고 뒤 `-X`로 전환한다.
- VFX 자체는 구현하지 않지만 VFX/UI가 구독할 수 있는 `GravityChangeWarning` 성격의 신호를 제공한다.
- 예고 후 실제 변경까지의 순서를 한 곳에서 실행해 VFX와 물리 시간이 어긋나지 않게 한다.

### 실행 순서

1. `[주기형 Zone 데이터와 유효성 검사 구현]` → verify: `[빈 방향 목록·잘못된 시간 값이 조용히 실행되지 않음]`
2. `[GravityManager 단일 주기 실행 구현]` → verify: `[동시에 하나의 반복만 존재]`
3. `[예고 신호와 실제 변경 순서 연결]` → verify: `[방향 변경마다 예고 1회·실제 변경 1회]`
4. `[Reverse Gravity Zone 설정]` → verify: `[기획된 방향과 시간 순서가 Inspector에서 확인 가능]`
5. `[다른 Zone 진입 취소 테스트]` → verify: `[Zero·Normal 등 다음 Zone 진입 후 이전 반복이 재발하지 않음]`

### Play Mode 테스트

- 진입 후 정의된 순서와 간격으로 중력이 바뀐다.
- 플레이어, 카메라와 GravityBody가 매 변경에 함께 반응한다.
- 예고 횟수와 실제 변경 횟수가 일치한다.
- Zone 재진입 또는 중복 Trigger로 Coroutine이 두 배로 실행되지 않는다.
- 다음 Zone 진입 후 Reverse Gravity가 다시 중력을 덮어쓰지 않는다.
- 여러 번 전환한 뒤에도 플레이어가 지형을 관통하거나 영구 조작 불능에 빠지지 않는다.
- 반복 중 Play Mode 종료·컴포넌트 비활성화 시 잔여 callback 오류가 없다.

### 완료 기준

- Reverse Gravity가 독립적인 단일 주기 실행으로 동작한다.
- 주기 실행의 시작·취소·예고·실제 변경 소유자가 `GravityManager`로 명확하다.
- 5회 이상 연속 방향 전환 후에도 플레이 가능한 상태가 유지된다.

### 중단 조건

- 타이머 중복을 bool flag로만 덮어 가리게 되면 실행 핸들의 생성·취소 소유권을 다시 정리한다.
- 전환 간격보다 플레이어·카메라 정렬 시간이 길어 계속 회전 중인 상태가 되면 물리 버그로 숨기지 않고 기획 간격 또는 정렬 속도를 조정한다.

## 14. Phase 5 — 몬스터·연출 오브젝트 반응 경계 통합

### 목표

Zone별 몬스터가 필요한 중력 방향 정보만 사용하고 물리 중력은 받지 않는다는 계약을 실제 Prefab과 Play Mode에서 확인한다.

### 범위

- 지네·거미·지렁이의 `GravityState` 참조 확인
- 몬스터 Prefab의 Rigidbody·GravityBody 비참여 확인
- 중력 변경 중 몬스터 이동 중단·표면 기준 변경 여부 점검
- 적 낙하 연출이 필요할 경우 별도 물리 대체물 사용
- 공중몹 비참여 원칙 확인

### 구현 방향

- 지네는 현재 중력 반대 방향을 바닥 normal로 사용하되 실제 AddForce를 받지 않는다.
- 거미는 waypoint 표면 normal과 부착 이동을 유지한다.
- 지렁이는 공격 위치 계산 시 현재 중력 방향을 읽는다.
- 공중몹이 추가되더라도 기본적으로 중력 힘과 정렬을 구독하지 않는다.
- 살아 있는 몬스터 낙하는 구현하지 않는다. 대표 연출에는 GravityBody가 붙은 더미나 잔해를 사용한다.

### 실행 순서

1. `[몬스터 Prefab과 런타임 컴포넌트 반응 표 점검]` → verify: `[의도하지 않은 Rigidbody·GravityBody 0건]`
2. `[지네 바닥 기준 테스트]` → verify: `[필요한 Zone에서 현재 중력 방향을 읽되 물리 낙하하지 않음]`
3. `[거미 표면 이동 테스트]` → verify: `[중력 변경이 waypoint 부착 이동을 강제로 깨지 않음]`
4. `[지렁이 출현 방향 테스트]` → verify: `[현재 중력 기준 바닥 후보를 사용]`
5. `[대표 낙하 연출 대체물 검증]` → verify: `[몬스터 구조 수정 없이 적 또는 시체 대체 오브젝트가 중력 변화 표현]`

### 완료 기준

- 몬스터별 방향 참고와 실제 힘 적용 여부가 표의 계약과 일치한다.
- 몬스터 이동 구조를 Rigidbody로 재작성하지 않는다.
- 중력 대표 연출이 플레이어와 동적 잔해만으로 읽히거나, 별도 대체물이 명시적으로 사용된다.

## 15. Phase 6 — Zero Gravity 진입·복귀 기반

### 목표

Zero Gravity Zone 진입 시 이전 주기 중력을 종료하고 플레이어와 GravityBody의 중력 힘을 제거하며, 이후 그래플이 속도를 제어할 수 있는 결정적인 상태를 만든다.

### 범위

- 세기 `0` 적용
- 주기 중력 취소
- 플레이어 `ZeroGravity` 진입 1회 처리
- 무중력 진입 속도 정책
- 일반 중력 복귀
- GravityBody의 무중력 관성 확인
- 그래플 시스템을 위한 상태 인계

### 구현 방향

- Zero Gravity는 작은 임계값이 아니라 `Strength == 0`이라는 명시적 상태로 판단한다.
- Zero Gravity 진입은 직전 `GravityState.Direction`과 `PresentationUp`을 유지한 채 세기만 `0`으로 만든다. 중력이 사라진다는 이유로 Player와 Camera에 별도 Roll을 발생시키지 않는다.
- 진입 순간 플레이어의 선속도와 각속도를 한 번 초기화해 이전 낙하가 무중력 구간 끝까지 이어지지 않게 한다.
- 진입 후 매 FixedUpdate마다 속도를 0으로 덮어쓰지 않는다. 그래플이 이후 Rigidbody 속도를 소유할 수 있어야 한다.
- 무중력 동안 일반 Grounded 이동·점프·달리기·웅크리기는 활성화하지 않는다.
- GravityBody는 힘만 0이 되고 기존 관성은 유지한다. 연출상 정지가 필요하면 해당 오브젝트 배치·초기속도로 조정하고 전역 감쇠 규칙은 추가하지 않는다.
- 일반 중력 복귀 시 새 중력 방향의 Ground Probe 결과에 따라 Grounded 또는 Airborne으로 자연스럽게 전환한다.

### 예상 변경 파일

- `Assets/_Scripts/Gravity/GravityManager.cs`
- `Assets/_Scripts/Player/PlayerController.cs`
- `Assets/_Scripts/Player/PlayerMotionStateMachine.cs`
- 필요할 경우 `Assets/_Scripts/Player/PlayerAnimationController.cs`
- `Assets/_Scenes/GamePlayScene_Player.unity`

### 실행 순서

1. `[Zero Gravity Zone을 현재 방향 유지·명시적 세기 0으로 구성]` → verify: `[이전 주기 취소 후 Direction·PresentationUp 유지, Strength 정확히 0]`
2. `[Player ZeroGravity 진입 1회 처리 구현]` → verify: `[진입 순간 속도 초기화, 이후 프레임별 강제 정지 없음]`
3. `[일반 이동 상태 차단 확인]` → verify: `[무중력에서 Jump·Sprint·Crouch가 잘못 실행되지 않음]`
4. `[GravityBody 무중력 반응 확인]` → verify: `[추가 가속도 0, 기존 관성만 유지]`
5. `[일반 중력 복귀 테스트]` → verify: `[새 방향 낙하와 Grounded/Airborne 선택 정상]`
6. `[그래플 인계 계약 기록]` → verify: `[그래플이 Rigidbody 이동을 추가할 지점과 종료 시 속도 소유권이 명확]`

### Play Mode 테스트

- Reverse Gravity 도중 Zero Gravity에 진입하면 이전 주기가 즉시 멈춘다.
- 진입 직전 방향과 화면 자세가 유지되고 중력 제거만으로 불필요한 Roll이 발생하지 않는다.
- 플레이어가 진입 직전 낙하 속도로 계속 멀리 날아가지 않는다.
- 무중력 상태 진입 후 중력 힘과 지상 이동 속도 덮어쓰기가 없다.
- 사격·카메라 시야는 필요한 범위에서 계속 동작한다.
- GravityBody에 중력 가속도가 추가되지 않는다.
- 일반 중력 복귀 시 플레이어가 새 중력 방향으로 다시 낙하한다.
- Zero Gravity와 일반 중력을 반복해도 상태가 고착되지 않는다.

### 완료 기준

- 무중력 진입과 복귀가 결정적으로 재현된다.
- `ZeroGravityMotionState`가 그래플 구현을 받을 수 있는 상태로 준비된다.
- 그래플 자체, 무중력 전투와 공중몹은 구현하지 않는다.

## 16. Phase 7 — 리스폰·Zone 복구와 전체 회귀 검증

### 목표

플레이어 사망·리스폰과 Zone 연속 진행 중에도 현재 중력 상태가 체크포인트와 일치하고, Entry에서 Zero Gravity까지의 중력 흐름을 반복 재현하게 한다.

### 범위

- 활성 Zone 초기 상태 재적용
- 리스폰 시 주기 실행 재시작 또는 고정 상태 복원
- 플레이어 속도·회전·카메라 기준 복구
- Zone별 몬스터와 GravityBody 상태 점검
- 전체 진행 Play Mode 회귀 테스트

### 구현 방향

- `RespawnController`는 플레이어 위치·속도 초기화 책임을 유지한다.
- `GameFlowManager`는 현재 진행 상태에 맞는 체크포인트와 GravityPreset 복구를 조정한다.
- `GravityManager`는 전달받은 Zone의 초기 중력과 주기 규칙을 다시 활성화한다.
- 리스폰 Transform의 Up과 복구할 중력 Up이 일치하는지 검증한다.
- 체크포인트 중력 상태가 모호하면 임의로 Normal을 적용하지 않고 Zone 매핑 누락을 오류로 드러낸다.
- Source 구역은 기본적으로 Zero Gravity 상태를 유지한다. 엔딩 지형이나 진행 연출이 Normal 복귀를 실제로 요구할 때만 별도 복구 Zone을 추가한다.

### 실행 순서

1. `[GameFlowState와 GravityPreset 복구 매핑 확정]` → verify: `[각 체크포인트에 중력 초기 상태가 하나씩 대응]`
2. `[리스폰 중력 복구 연결]` → verify: `[위치·속도 초기화 후 중력·회전·카메라 기준 일치]`
3. `[고정형 Zone 사망 테스트]` → verify: `[Shift 상태 리스폰 후 올바른 벽 기준 유지]`
4. `[주기형 Zone 사망 테스트]` → verify: `[중복 Coroutine 없이 정의된 초기 순서로 재시작]`
5. `[무중력 Zone 사망 테스트]` → verify: `[Zero Gravity 상태와 속도 정책 복구]`
6. `[전체 중력 구간 3회 반복]` → verify: `[진행 불가·상태 고착·Console 오류 없이 완료]`
7. `[최종 diff와 참조 재로드 확인]` → verify: `[저장·재실행 후 동일, Original·ProjectSettings·환경 Prefab 원본 의도하지 않은 diff 없음]`

### 전체 Play Mode 검증 기준

- Normal → Shift → Reverse → Zero 순서가 기획대로 재현된다.
- 모든 Zone 전환 후 Player와 GravityBody의 중력 값이 일치한다.
- 카메라 yaw·pitch, 조준 Ray와 Player VisualRoot 전방이 현재 중력 기준에서 일치한다.
- 새 바닥에서 이동·점프·달리기·웅크리기·사격이 필요한 범위에서 동작한다.
- 정적 지형은 움직이지 않고 동적 잔해만 반응한다.
- 몬스터는 정의된 방향 정보만 읽고 의도하지 않게 낙하하지 않는다.
- 주기 실행이 다음 Zone과 리스폰 이후 남아 있지 않는다.
- 세 번 연속 진행에서 지형 관통, 영구 낙하, 카메라 뒤집힘과 조작 불능이 없다.
- 컴파일 오류, Missing Script, Missing Reference와 신규 Console 오류가 없다.

### 완료 기준

- 중력 시스템 범위의 모든 Phase 완료 기준을 충족한다.
- 그래플 구현을 시작해도 중력 상태·속도·Zone 전환 소유권을 다시 뜯어고칠 필요가 없다.
- 실제 검증 결과와 남은 제한이 문서와 활용 기록에 구분되어 남는다.

## 17. Phase 8 — 선택적 WebGL 재현 검증

이 Phase는 사용자가 실제 WebGL 빌드와 Build Settings 변경을 명시적으로 승인한 경우에만 실행한다.

### 목표

Editor Play Mode에서 확인한 방향성 중력과 무중력 전환이 WebGL 실행 환경에서도 재현되는지 확인한다.

### 범위

- 승인된 출력 경로의 WebGL 빌드
- 브라우저 입력·물리·카메라 전환 확인
- 빌드 산출물과 설정 diff 분리 확인

### 검증 기준

- 브라우저에서 별도 Editor 조작 없이 Zone 중력 전환이 발생한다.
- Player·Camera·GravityBody가 Editor와 같은 순서로 반응한다.
- 주기 타이밍이 프레임레이트 변화로 중복 실행되지 않는다.
- 빌드 산출물이 소스 자산과 분리되고 의도하지 않은 ProjectSettings 변경이 없다.

### 완료 기준

- 승인된 WebGL 빌드에서 Normal, Shift, Reverse와 Zero Gravity 전환을 한 번 이상 연속 재현한다.
- Editor와 다른 동작, 빌드 제한과 설정 diff를 결과에 명시한다.

승인이 없으면 Phase 7의 Play Mode 검증을 중력 시스템 구현 완료선으로 삼고, WebGL은 미검증 상태로 명시한다.

## 18. 예상 파일 변경 범위

### 신규 후보

- `Assets/_Scripts/Gravity/GravityManager.cs`
- `Assets/_Scripts/Gravity/GravityPreset.cs`
- `Assets/_Scripts/Gravity/GravityBody.cs`
- 필요할 경우 `Assets/_Custom/Prefabs/Gravity/GravityTestBody.prefab`

### 수정 후보

- `Assets/_Scripts/Gravity/GravityState.cs`
- `Assets/_Scripts/Gravity/Editor/GravityManagerEditor.cs`
- `Assets/_Scripts/GameFlow/Triggers/GravityEventTrigger.cs`
- `Assets/_Scripts/GameFlow/GameFlowManager.cs`
- `Assets/_Scripts/GameFlow/RespawnController.cs`
- `Assets/_Scripts/Player/PlayerController.cs`
- `Assets/_Scripts/Player/PlayerMotionStateMachine.cs`
- `Assets/_Scripts/Player/ThirdPersonCameraController.cs`
- 필요할 경우 `Assets/_Scripts/Player/PlayerAnimationController.cs`
- `Assets/_Custom/Prefabs/Player/Player.prefab`
- 필요할 경우 `Assets/_Custom/Prefabs/Player/ThirdPersonCameraRig.prefab`
- `Assets/_Scenes/GamePlayScene_Player.unity`
- 구현·검증 완료 후 `Docs/ksh/Codex_Usage_Records.md`

### 변경하지 않을 파일·영역

- `Assets/_Scenes/Original_GamePlayScene.unity`
- 팀장 지형 Collider와 환경 Prefab 원본
- 외부 에셋 원본
- 입력 public 계약
- 전투와 몬스터 체력 public 계약
- `Packages`, Build Settings와 `ProjectSettings` — 별도 승인된 최종 빌드 검증 제외

## 19. 공통 실패 케이스와 대응 원칙

- **플레이어가 90도 회전하지 않음**: Rigidbody X/Z 제약과 `MoveRotation` 충돌을 먼저 확인한다.
- **카메라만 월드 Up에 남음**: Player 회전 속도를 바꾸지 말고 카메라 yaw 축과 전방 투영을 수정한다.
- **카메라가 화면 기준 시계 방향으로 Roll함**: Unity 축의 부호 이름을 믿고 값을 뒤집지 말고 Game View에서 현재 시선 전방축과 화면 반시계 결과를 함께 확인한다.
- **전환 중 Player 위치가 떨리거나 Collider가 겹침**: 위치 강제 덮어쓰기나 Collider 비활성화로 숨기지 말고 Anchor, Capsule 회전 공간과 물리 이동 소유권을 확인한다.
- **환경 물체도 Player와 함께 멈춤**: 전체 시간 정지나 공통 GravityState 보간을 제거하고 GravityBody가 전환 시작 시 확정된 실제 중력을 즉시 받는지 확인한다.
- **전환 종료 후 입력이 돌아오지 않음**: 완료·취소·비활성화 경로가 같은 전환 해제와 입력 복구를 호출하는지 확인한다.
- **플레이어와 카메라가 서로 다른 전방을 사용함**: VisualRoot, 카메라 중심 Ray와 이동 기준의 데이터 흐름을 함께 확인한다.
- **중력이 두 배로 적용됨**: Player에 GravityBody가 붙었는지, 동적 오브젝트의 `useGravity`가 켜졌는지 확인한다.
- **주기 중력이 다음 Zone에서 재발함**: 이전 실행 핸들의 취소와 활성 Zone 소유권을 확인한다.
- **무중력 진입 후 계속 날아감**: 진입 1회 속도 초기화가 실제 상태 전환 순간에 실행됐는지 확인한다.
- **Slow가 무중력으로 처리됨**: 임의 임계값을 제거하고 `Strength == 0` 계약을 확인한다.
- **몬스터가 떨어지거나 Follow 체인이 깨짐**: 해당 Prefab의 Rigidbody·GravityBody 추가 여부를 먼저 확인한다.
- **정적 바위가 움직임**: 환경 Prefab 원본에 Rigidbody가 추가됐는지 확인하고 동적 대체물을 분리한다.
- **리스폰 후 카메라가 뒤집힘**: 리스폰 Transform Up, 복구 Zone Up과 카메라 전방 재투영 순서를 확인한다.
- **새 지형이 필요함**: `Original_GamePlayScene`을 직접 수정하지 않고 Collider 인계와 통합 경계를 다시 확인한다.

## 20. 최종 완료 기준

- 한 번의 90도 방향 전환에서 동적 잔해는 즉시 새 중력에 반응하고, Player는 위치를 고정한 채 Camera와 함께 화면 반시계 Roll을 완료한 뒤 새 방향으로 낙하한다.
- Reverse Gravity가 예고와 함께 단일 주기로 반복되고 다음 Zone에서 완전히 취소된다.
- Zero Gravity 진입·복귀가 결정적이며 그래플이 Rigidbody 이동을 이어받을 수 있다.
- 중력 영향 대상은 `GravityBody` opt-in 규칙으로 구분되고 정적 지형과 몬스터 이동 구조는 보호된다.
- 리스폰 후 체크포인트에 맞는 중력·플레이어 회전·카메라 기준이 복구된다.
- `GamePlayScene_Player`에서 중력 구간을 세 번 연속 통과해도 진행 불가 상태가 발생하지 않는다.
- 컴파일 오류, Missing Script, Missing Reference와 신규 Console 오류가 없다.
- `Original_GamePlayScene`, 지형 Collider, 외부 에셋 원본, Package와 ProjectSettings에 의도하지 않은 diff가 없다.
- 구현 결과, 사람이 확인한 항목, 미검증 WebGL과 남은 제한이 명확히 구분된다.

## 21. 문서와 작업 상태 관리

- Phase 3 세부 계약을 문서로 확정한 것만으로는 `Docs/ksh/Codex_Usage_Records.md`에 새 항목을 추가하지 않는다.
- 이 문서는 이미 `Docs/ksh/Tasks/02_in-progress`에 있으므로 Phase 3 구현을 시작할 때 위치를 바꾸지 않고 현재 상태만 갱신한다.
- 각 Phase가 끝날 때 실제 변경 파일, Play Mode 결과, 실패와 조정한 값을 해당 Phase 아래에 기록한다.
- Phase 7까지 구현·검증이 완료되면 전체 결과를 활용 기록에 하나의 완료 작업 단위로 남긴다.
- WebGL은 별도 승인이 없으면 미검증으로 기록한다.
- 모든 승인된 Phase가 완료되면 문서를 `Docs/ksh/Tasks/03_completed`로 이동한다.
- 실행 중 게임 기획, 책임 경계 또는 중력 영향 대상이 바뀌면 사용자 확인 후 `Player_Gravity_Master_Plan.md`를 갱신한다.

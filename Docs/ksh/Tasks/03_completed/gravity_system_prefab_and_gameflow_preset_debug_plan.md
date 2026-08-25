# GravitySystem 프리팹화 및 GameFlow 중력 Preset 디버그 계획

문서 작성일: 2026-08-25  
현재 상태: 완료 — Prefab화·Inspector 구현·자동 Play Mode 검증 완료

계획 프로필: `deep`

기준 문서:

- [SinkPoint MVP 게임 기획서](../../../GameDesign_MVP.md)
- [플레이어·중력 파트 마스터 계획](../../Player_Gravity_Master_Plan.md)
- [월드 6축 GravityPreset 및 Zone Trigger 매핑 실행 계획](../03_completed/world_axis_gravity_preset_zone_trigger_mapping_plan.md)

## 1. 목표

`GamePlayScene_Player`의 `/GamePlay/GravitySystem`을 재사용 가능한 connected Prefab으로 만들고, 더 이상 필요 없는 자식 테스트 바디 `GravityTestBody_Mass1`, `GravityTestBody_Mass5`를 제거한다.

또한 현재 `GravityManager` Inspector에만 있는 씬 `GravityPreset` 선택·적용 흐름을 `GameFlowManager` Inspector의 맨 아래에도 제공한다. Play Mode에서 Zone, GameFlow State와 별개로 임의 Preset을 한 곳에서 선택해 실제 `GravityManager.ApplyPreset()` 경로로 적용할 수 있어야 한다.

## 2. 범위

- `Assets/_Scenes/GamePlayScene_Player.unity`의 `/GamePlay/GravitySystem` 및 모든 운영 Preset 자식을 하나의 `GravitySystem` Prefab으로 추출
- `GravityTestBody_Mass1`, `GravityTestBody_Mass5` GameObject와 각 MeshRenderer, BoxCollider, Rigidbody, `GravityBody` 제거
- `GravitySystem` 내부의 `GravityState`, `GravityManager`, 모든 `GravityPreset`의 값과 부모 구조 보존
- GameFlowManager 전용 Custom Inspector를 추가해 기존 기본 Inspector/Odin 버튼을 보존한 뒤 Inspector **맨 아래**에 `Gravity Preset Select` 섹션 추가
- 씬 안의 `GravityPreset`을 hierarchy path 기준으로 선택하고, Play Mode에서 선택 Preset을 적용하는 버튼과 현재 Preset 읽기 전용 표시 제공
- 두 Inspector가 같은 목록 탐색·세션 선택 저장 규칙을 공유하도록 Editor 전용 helper로 중복 제거

## 3. 하지 않을 것

- `Assets/_Scenes/Original_GamePlayScene.unity`, Collider, 지형 Transform, Trigger, Zone 배치 수정
- `Packages`, `ProjectSettings`, Build Settings 또는 active build target 변경
- Preset의 Mode, direction, strength, 주기 수치나 Trigger–Preset 연결 변경
- GameFlowState·CurrentZone을 Preset 선택 버튼으로 임의 변경하거나 `ZoneId → Preset` 매핑 추가
- 새 테스트 바디, 중력 방향 enum, 두 번째 `GravityState`, test-only 직접 상태 덮어쓰기 경로 추가
- 명시적 승인 없는 WebGL 빌드

## 4. 현재 상태와 핵심 판단

- `/GamePlay/GravitySystem`은 `GravityState`, `GravityManager`, 운영 `GravityPreset` 자식을 함께 소유한다. 두 테스트 바디도 현재 이 루트의 자식이며 각각 `GravityBody`가 같은 `GravityState`를 참조한다.
- `GravityManagerEditor`는 hierarchy path로 씬 Preset을 수집하고, 선택 결과는 `SessionState`에만 저장한다. 따라서 Inspector 조작이 씬/Prefab override로 남지 않는다.
- `GameFlowManager`의 운영 중력 경로는 `GravityEventTrigger → GameFlowManager → GravityManager.ApplyPreset()`이다. 디버그 Preset 버튼도 마지막 공통 API만 호출해야 Player·Camera 전환, Periodic 정리, HUD 경고 계약을 우회하지 않는다.
- 임의 Preset 적용은 Trigger 이벤트가 아니므로 `CurrentState`와 `CurrentZone`을 바꾸지 않는다. 이는 "현재 진행 위치"와 "현재 중력"을 각각 관찰하려는 디버깅에 필요하며, 거짓 진행 상태를 만들지 않는다.
- Prefab 내부 참조(`GravityManager.gravityState`, `initialPreset`)는 Prefab 내부 연결로 유지한다. Player, Camera, `GameFlowManager`, Trigger가 가진 기존 외부 `GravityState`/`GravityManager`/Preset 참조는 Prefab 추출 후 실제 인스턴스를 계속 가리키는지 반드시 재검증한다.

## 5. 책임 경계와 Inspector 동작

```text
GameFlowManager Inspector (debug selection only)
  └─ scene GravityPreset 선택
       └─ GameFlowManager.DebugApplyGravityPreset(preset)
            └─ GravityManager.ApplyPreset(preset)  [운영 공통 API]
                 ├─ GravityState 변경
                 ├─ Player / Camera PresentationUp 전환
                 └─ Periodic / HUD 경고 상태 갱신

CurrentZone / CurrentState: 변경하지 않음
```

### `GravitySystem` Prefab

- 운영 중력 구성의 소유자다: 루트, `GravityState`, `GravityManager`, 모든 운영 Preset 자식.
- 테스트용 동적 Rigidbody는 포함하지 않는다.
- Prefab asset의 기본 Transform은 원점/identity로 두고, 현재 씬 인스턴스의 루트 위치·회전·스케일도 동일함을 확인한다.

### `GravityPresetInspectorUtility` (Editor 전용 신규 helper)

- 대상 Component와 같은 scene의 non-persistent `GravityPreset`만 찾는다.
- hierarchy path 정렬, `GlobalObjectId` 기반 session key/선택 복원을 공통으로 제공한다.
- serialized debug 슬롯을 만들지 않는다. 씬 저장 여부와 관계없이 선택은 Editor session 한정이다.

### `GravityManagerEditor`

- 현재의 선택·적용·초기 Preset 복원·현재 Preset 재시작·Play Mode 상태 표시는 유지한다.
- 목록 수집과 선택 상태 구현만 helper로 이전한다.

### `GameFlowManager` 및 전용 Editor

- runtime에는 `DebugApplyGravityPreset(GravityPreset preset)`처럼 명확한 public 진입점만 추가한다. 이 함수는 `gravityManager` 누락과 null Preset을 경고하고, 성공 여부를 `GravityManager.ApplyPreset()` 결과로 반환한다.
- `GameFlowManagerEditor`는 `DrawDefaultInspector()` 뒤에 `Gravity Preset Select`를 마지막 섹션으로 그린다. Play Mode가 아닐 때와 Preset/GravityManager가 없을 때 적용 버튼을 비활성화하고 이유를 안내한다.
- 선택 Preset의 hierarchy path와 `GravityManager.CurrentPreset`을 읽기 전용으로 보여 준다. Runtime 값 변경 시 `Repaint()`한다.
- `SetState`, `EnterZone`, `GravityEventTrigger` 구독, Trigger 배열은 변경하지 않는다.

## 6. 실행 순서

1. `[기준선과 참조 기록]` → verify: `[Git의 기존 사용자 변경을 분리하고, GravitySystem 자식 목록·두 TestBody 구성·GravityManager 내부 참조·외부 GravityState/Manager/Preset 참조를 기록]`
2. `[두 GravityTestBody 삭제]` → verify: `[Mass1/Mass5와 두 Rigidbody·GravityBody가 Hierarchy/씬 직렬화에서 사라지고, 운영 GravityBody 또는 Collider는 건드리지 않음]`
3. `[정리된 GravitySystem 전체를 Prefab asset으로 추출하고 씬 인스턴스 연결]` → verify: `[루트와 모든 운영 Preset 자식이 Connected, 내부 initialPreset/gravityState 참조 유효, asset에 TestBody가 없음]`
4. `[외부 참조 재확인·필요 시 동일 인스턴스로 재연결]` → verify: `[PlayerController, ThirdPersonCameraController, GameFlowManager, GravityEventTrigger가 새 인스턴스의 GravityState/GravityManager/각 Preset을 가리키고 Missing Reference 없음]`
5. `[Preset 선택 공통 Editor helper 추출]` → verify: `[GravityManager Inspector의 목록 순서·session-only 선택·기존 Apply/Restore/Restart 기능이 동일]`
6. `[GameFlowManager의 운영 API 기반 디버그 진입점과 맨 아래 Inspector 섹션 추가]` → verify: `[Play Mode에서 씬 Preset 선택→Apply가 CurrentPreset/Direction/Strength를 바꾸되 CurrentZone/CurrentState는 보존, null/미할당 시 안전한 경고]`
7. `[컴파일·씬 재로드·Play Mode 회귀 확인]` → verify: `[Editor/Runtime 컴파일 오류 0, Prefab Connected, 새 Console Error 0, Normal·Fixed·Periodic·ZeroGravity 선택과 복원이 운영 API 경로로 동작]`
8. `[최종 diff와 문서 상태 정리]` → verify: `[Original/Collider/설정 파일 미변경, git diff --check 통과, 이 계획을 03_completed로 이동하고 의미 있는 실행 완료 후에만 Usage Record 추가]`

## 7. 검증 기준

### 정적·직렬화 검증

- `GravitySystem` Prefab asset에는 `GravityState`, `GravityManager`, 기존 운영 Preset만 있고 TestBody 이름·`GravityBody`가 없다.
- `GamePlayScene_Player`의 GravitySystem은 connected instance이며, 내부와 외부 참조에 Missing Object/Missing Script가 없다.
- `GravityManager.initialPreset`은 기존 Normal Preset을 계속 참조하고, 모든 Trigger의 Preset 참조가 기존 대상과 동일하다.
- `GameFlowManager` Inspector의 기존 Zone/State/Barrier/Odin 제어는 사라지지 않고 Preset 섹션이 마지막에 있다.

### Play Mode 검증

1. GameFlowManager Inspector에서 `GravityPreset_Normal`을 적용해 `CurrentPreset`, direction `(0,-1,0)`, strength `9.81`을 확인한다.
2. Fixed Preset 하나를 적용해 Player와 Camera가 함께 전환되고, `CurrentZone`/`CurrentState` 값은 적용 전과 같음을 확인한다.
3. Periodic Preset을 적용해 `IsPeriodicRunning`, 다음 방향, 경고 상태가 GravityManager Inspector와 일치함을 확인한다.
4. Zero Gravity Preset을 적용해 strength가 `0`이 되고 Periodic routine이 정리되는지 확인한다.
5. `Restore Initial Preset`과 Trigger 기반 중력 전환을 각각 실행해 기존 정상 경로가 유지되는지 확인한다.
6. 새 `NullReferenceException`, Missing Reference, 중복 `GravityManager` 또는 TestBody 관련 로그가 없는지 확인한다.

### 사용자 확인 항목

- GameFlowManager 하나에서 Zone/State 기존 디버그 제어와 Preset 선택을 오가며 빠르게 상태를 재현할 수 있는지.
- 임의 Preset 테스트 뒤 실제 Trigger를 통과했을 때 게임 진행과 중력 전환이 정상으로 계속 이어지는지.

## 8. 완료 기준

- 두 `GravityTestBody`는 씬과 새 Prefab 어느 곳에도 없다.
- `/GamePlay/GravitySystem`은 운영 구성 전체를 포함하는 connected Prefab이다.
- GravityManager와 GameFlowManager Inspector에서 동일한 씬 Preset 목록을 선택할 수 있다.
- GameFlowManager의 선택 적용은 `GravityManager.ApplyPreset()`만 통하며, GameFlow Zone/State를 변경하지 않는다.
- 컴파일, 씬 재로드, Play Mode 검증에서 새 오류가 없고 보호 범위의 변경이 없다.

## 9. 문서와 작업 상태 관리

- 구현 시작 시 `02_in-progress`로 이동했다.
- Prefab화와 Inspector 기능, 자동 Play Mode 검증을 완료했으므로 `03_completed`로 이동하고 완료 기록을 남긴다.

## 10. 구현·검증 기록 — 2026-08-25

- `/GamePlay/GravitySystem`에서 `GravityTestBody_Mass1`, `GravityTestBody_Mass5`를 제거한 뒤 남은 운영 구성 전체를 `Assets/_Custom/Prefabs/Gravity/GravitySystem.prefab`으로 추출했다. 씬 인스턴스는 connected 상태로 유지된다.
- `GravityManager.gravityState`와 `initialPreset`, Player·Camera의 `GravityState`, GameFlowManager의 `GravityManager` 참조가 모두 새 씬 인스턴스를 계속 가리키는 것을 재조회했다.
- `GravityPresetSceneSelector` Editor helper로 hierarchy-path Preset 목록과 session-only 선택 상태를 공유했다. 기존 GravityManager Inspector는 같은 동작을 유지하고, Odin 기반 GameFlowManager Inspector의 맨 아래에 동일한 `Gravity Preset Select`와 현재 Preset 표시를 추가했다.
- `GameFlowManager.DebugApplyGravityPreset()`은 null/미할당을 안전하게 거부하고 `GravityManager.ApplyPreset()`만 호출한다. Zone과 State를 변경하지 않는다.
- Unity 재컴파일은 `failed=false`, 오류 0건이었다. 새 Play Mode에서 Periodic Z 적용 시 `IsPeriodicRunning = true`, Normal 적용 시 routine 정리와 strength `9.81`, Zero Gravity 적용 시 strength `0`과 routine 정리를 확인했다. Periodic 적용 전후 `CurrentZone = Zone01_Entry`, `CurrentState = Entry`가 유지됐고, 새 Console error는 없었다.
- 실제 Inspector를 사용한 수동 선택·조작감과 Trigger 통과 후 연속 진행은 사용자 Play Mode 확인이 남아 있다.

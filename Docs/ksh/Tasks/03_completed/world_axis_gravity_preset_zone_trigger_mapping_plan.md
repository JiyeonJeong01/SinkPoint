# 월드 6축 GravityPreset 및 Zone Trigger 매핑 실행 계획

문서 작성일: 2026-08-25  
현재 상태: 완료 — Phase 1~3 구현·자동 Play Mode 검증·사용자 연속 조작 확인 완료

계획 프로필: `deep`

기준 문서:

- [SinkPoint MVP 게임 기획서](../../../GameDesign_MVP.md)
- [플레이어·중력 파트 마스터 계획](../../Player_Gravity_Master_Plan.md)
- [완료된 Zone 기반 중력 시스템 구현 계획](../03_completed/gravity_zone_system_implementation_plan.md)

## 1. 목표

`GamePlayScene_Player`의 고정 방향 중력을 월드 6축으로 선택할 수 있게 하고, 맵의 실제 착지면과 진행 동선에 맞는 `GravityPreset`을 각 중력 전환 Trigger에 명시적으로 연결한다.

작업은 세 Phase로 분리한다.

1. **Phase 1 — 월드 6축 고정형 Preset 라이브러리 완성**
2. **Phase 2 — Zone 진행 경계별 Trigger–Preset 매핑과 실제 동선 검증**
3. **Phase 3 — Zone 04 주기 중력·Zone 05 무중력 통합**

핵심 완료 상태는 다음과 같다.

```text
맵의 착지면 확인
  → 해당 면을 향하는 월드 축 Preset 선택
  → 진행 경계의 Trigger 하나에 Preset 하나만 연결
  → Player·Camera가 함께 전환
  → 목표 벽/천장/바닥에 착지
  → 다음 Zone 진입과 바리게이트 진행 성공
```

## 2. 범위

### Phase 1 범위

- 기존 `GravityPreset_Normal(-Y)`, `GravityPreset_WorldPosX(+X)`, `GravityPreset_WorldNegX(-X)` 보존
- `GravityPreset_WorldPosY(+Y)` 추가
- `GravityPreset_WorldPosZ(+Z)` 추가
- `GravityPreset_WorldNegZ(-Z)` 추가
- 모든 고정형 Preset의 `Fixed`, 방향 단위 벡터, 세기 `9.81` 검증
- 기존 Inspector의 `World ±X`, `World ±Y`, `World ±Z` 작성 버튼과 실제 Preset 값 대조
- 6축을 `GravityManager.ApplyPreset(GravityPreset)` 공통 경로로 수동 적용하는 Play Mode 검사

### Phase 2 범위

- `GamePlayScene_Player`의 Zone 02 → Zone 03 → Zone 04 진행 경계 조사
- 보라색 `MapBoxCollider`와 암석 `MeshCollider`를 근거로 각 경계의 목표 착지면 식별
- 목표 표면의 바깥쪽 normal을 `n`이라 할 때 중력 방향을 `-n`으로 선택
- `Trigger_ToShfitGravity`에 Zone 03 진입용 고정형 Preset 연결
- Zone 04 진입 경계에 Inversion용 고정형 Preset 연결
- 겹쳐 있는 기존 Shift/Inversion Trigger가 같은 진입에서 연속 적용되지 않도록 단일 활성 경로 구성
- Zone 진입, 몬스터 완료, 바리게이트 개방과 현재 Preset의 일치 검증
- 선택한 최종 방향과 Play Mode 근거를 이 문서의 매핑 표에 기록

### Phase 3 범위

- Zone 04 `Reverse Gravity`를 고정 반전이 아니라 `World -Z ↔ World +Z` 주기 중력으로 구현
- Zone 04 진입 즉시 첫 방향 `World -Z`를 적용하고 이후 설정 간격마다 `+Z`, `-Z`를 반복
- Zone 04의 `CurrentState = Inversion`, `CurrentZone = Zone04_Inversion` 계약은 유지하고 `CurrentPreset`은 운영 Periodic Preset으로 연결
- 기존 `GravityChangeWarning` 이벤트를 사용해 다음 중력 변경을 플레이어가 볼 수 있는 최소 HUD 경고로 표시
- Zone 05 진입 경계에 `GravityEventTrigger(ZeroGravity)`와 운영 Zero Gravity Preset 연결
- Zone 05 진입 시 `CurrentZone = Zone05_ZeroGravitySource`, `CurrentState = ZeroGravity`, `CurrentPreset = GravityPreset_ZeroGravity`가 같은 경계 뒤 일치하는지 검증
- Zero Gravity 진입 시 그 순간의 Periodic 방향과 `PresentationUp`을 유지하고 `Strength = 0`만 적용되는지 검증
- 기존 별도 `Trigger_ToZeroGravity`의 중복 운영 경로 제거
- Zone 04 → Zone 05 연속 진행, 무중력 진입, 사격 반작용과 리스폰 회귀 검증

## 3. 하지 않을 것

- 팀장 소유 `Assets/_Scenes/Original_GamePlayScene.unity` 수정
- `GamePlayScene_Player`의 기존 Collider 위치, 회전, 크기, 활성 상태 또는 물리 재질 변경
- 바위·벽·바닥·기둥의 Transform이나 지형 배치 변경
- Build Settings, `Packages`, `ProjectSettings` 또는 active build target 변경
- 방향 enum, 축별 `GameFlowState` 또는 두 번째 중력 상태 정본 추가
- 런타임 Raycast로 주변 벽을 자동 추측하여 중력 방향을 결정하는 시스템
- 여러 Zone이 동시에 서로 다른 중력을 유지하는 로컬 중력장
- 역행 시 이전 중력을 복원하는 양방향 Trigger
- 몬스터 이동, 표면 부착, 전투 패턴 또는 Collider 수정
- 그래플·로프 물리 구현 및 최종 Source 스캔/Ending 연결
- 무중력 사격 반작용 코드나 수치 재설계
- 명시적 승인 없는 WebGL 빌드

무중력 사격 반작용 자체는 완료된 `zero_gravity_weapon_recoil_implementation_plan.md`에서 다뤘다. Phase 3은 그 코드를 변경하지 않고 Zone 05 진입 경계에 기존 Zero Gravity 런타임을 연결하는 레벨 통합만 담당한다. Source 스캔과 Ending은 후속 작업으로 남긴다.

## 4. 현재 상태와 문제 근거

### 4.1 현재 고정형 Preset

| 씬 오브젝트 | Mode | Direction | Strength | 용도 |
|---|---|---:|---:|---|
| `GravityPreset_Normal` | `Fixed` | `(0, -1, 0)` | `9.81` | 기본 바닥 |
| `GravityPreset_WorldPosX` | `Fixed` | `(1, 0, 0)` | `9.81` | 기존 Shift |
| `GravityPreset_WorldNegX` | `Fixed` | `(-1, 0, 0)` | `9.81` | 기존 Inversion |
| `GravityPreset_WorldPosY` | `Fixed` | `(0, 1, 0)` | `9.81` | Zone 03 Shift |
| `GravityPreset_WorldPosZ` | `Fixed` | `(0, 0, 1)` | `9.81` | Zone 03 Shift |
| `GravityPreset_WorldNegZ` | `Fixed` | `(0, 0, -1)` | `9.81` | 수동 단일축 검사 |
| `GravityPreset_TestPeriodicX` | `Periodic` | `+X ↔ -X` | `9.81` | Inspector 검증용, Phase 3에서 운영 Z축 Preset으로 승격 예정 |

Phase 1에서 월드 6축 Fixed Preset 라이브러리를 완성했다. `-Y`는 기존 `GravityPreset_Normal`이 계속 담당한다.

### 4.2 Phase 2 실행 전 Trigger 문제

- `Trigger_ToShfitGravity`는 `GravityPreset_WorldPosX`를 참조한다.
- `Trigger_ToInversion`은 `GravityPreset_WorldNegX`를 참조한다.
- 두 Trigger는 `/Environment/Triggers` 아래에서 거의 같은 위치와 겹치는 BoxCollider를 사용한다.
- 둘 다 활성 상태이고 `GameFlowManager.gravityEventTriggers`에 등록되어 있다.
- 독립적인 `OnTriggerEnter`가 서로 반대인 Preset을 적용할 수 있으므로 하나의 진행 경계가 현재 Preset을 단독으로 결정한다는 계약이 깨질 수 있다.
- 실제 플레이에서는 Shift 이벤트 뒤 Zone 03 진입이 누락되어, 거미가 죽어도 `CurrentZone`이 Zone 02에 남고 `3→4` 바리게이트가 열리지 않았다.

### 4.3 시스템 지원 상태

- `GravityPreset.direction`은 축 enum이 아니라 정규화된 `Vector3`가 단일 정본이다.
- `GravityManager`, `GravityState`, `PlayerController`, `ThirdPersonCameraController`는 특정 X축이 아니라 현재 방향과 `PresentationUp`을 사용한다.
- 180도 전환에는 결정적 fallback 축이 존재한다.
- 기존 사용자 Play Mode 확인은 주로 `-Y ↔ ±X`에 한정되어 있으므로 `+Y`, `±Z`는 구현 가능 상태와 실제 동선 검증 완료 상태를 구분해야 한다.

### 4.4 Phase 3 실행 전 Zone–State–Preset 불일치

- 사용자가 실제 조작으로 Zone 04 Inversion과 Zone 05 `ZeroGravitySource` 진입까지 성공했다. 따라서 Phase 2의 Zone 03 `World +Z` 경로와 Zone 진행 연결은 통과로 판정한다.
- Zone 04 진입은 `CurrentZone = Zone04_Inversion`, `CurrentState = Inversion`까지 맞지만 `CurrentPreset = GravityPreset_Normal(-Y)`이므로 기획된 주기 중력이 시작되지 않는다.
- `/Environment/Zone05_EntryTrigger`에는 `ZoneEntryTrigger(Zone05_ZeroGravitySource)`만 있어 Zone은 05로 바뀌지만 State와 Preset은 Zone 04 값에 남는다.
- `/Environment/Zone_04_Inversion/Triggers/Trigger_ToZeroGravity`는 `eventType = ZeroGravity`지만 Preset이 `null`이고 `GameFlowManager.gravityEventTriggers`에도 등록되지 않아 운영 이벤트로 동작하지 않는다.
- `GravityPreset_TestPeriodicX`는 `Periodic`, `[+X, -X]`, 간격 `4초`, 예고 `1초`로 구현돼 있으나 운영 Zone에 연결되지 않은 Inspector 검증용이다. Phase 3에서는 이 오브젝트를 `GravityPreset_PeriodicZ`로 승격하고 방향을 `[-Z, +Z]`로 바꾼다.
- `GravityPreset_TestZero`는 이미 `ZeroGravity` Mode를 가지지만 테스트 이름이다. Phase 3에서는 새 Preset을 중복 생성하지 않고 이 오브젝트를 `GravityPreset_ZeroGravity`로 승격·이름 변경해 운영 참조로 사용한다.

## 5. 필요한 가정

- 맵 Collider가 레벨 설계의 정본이며, 중력 프리셋과 Trigger 연결을 맵에 맞춘다.
- 고정형 중력의 기본 세기는 전 축에서 `9.81`로 통일한다.
- `GravityPreset_Normal`은 이름을 유지한 채 `World -Y` 역할을 한다. 참조 안정성을 위해 별도 `GravityPreset_WorldNegY`를 중복 생성하거나 기존 오브젝트를 이름 변경하지 않는다.
- 각 진행 경계에서는 활성 `GravityEventTrigger` 하나가 `GravityPreset` 하나만 결정한다.
- Trigger 이름의 `Shift`나 `Inversion`보다 실제 착지 가능성과 다음 Zone 진행을 우선한다.
- Zone 04 `Inversion`은 GameFlow 상태 이름이고 실제 중력 동작은 `Periodic` Preset이 담당한다. 별도 `Periodic` GameFlowState를 추가하지 않는다.
- Periodic 방향 배열은 `[-Z, +Z]` 순서로 둔다. 기존 구현이 배열의 첫 방향을 즉시 적용하므로 Zone 03 `+Z`에서 Zone 04 진입 시 `-Z` 반전이 먼저 발생한다.
- 기존 테스트 값인 변경 간격 `4초`, 예고 `1초`로 시작했으나 사용자 Play Mode 체감에 따라 변경 간격을 `10초`로 조정했다. 예고는 `1초`를 유지하며 둘 다 Inspector 조정값으로 둔다.
- Zero Gravity Preset은 새 방향을 만들지 않고 Zone 05 진입 순간의 Periodic 방향과 `PresentationUp`을 보존한 채 `Strength = 0`으로 전환한다.
- 자동 입력이나 Inspector 수동 적용만으로 실제 동선 완료를 주장하지 않는다. 최종 완료는 사용자가 실제 이동으로 확인한다.

## 6. 책임 경계와 데이터 흐름

### `GravityPreset`

- Mode, 방향, 세기만 소유한다.
- Zone 의미, Trigger 위치, 진행 순서를 소유하지 않는다.
- `direction`을 유일한 물리 방향 정본으로 유지한다.

### `GravityEventTrigger`

- 플레이어 첫 접촉과 one-shot 판정을 소유한다.
- Inspector에서 선택한 `GravityPreset` 참조를 제공한다.
- 직접 `GravityState`를 덮어쓰지 않는다.

### `GameFlowManager`

- 씬에서 사용 중인 `GravityEventTrigger`만 구독한다.
- Trigger 이벤트를 `GravityManager.ApplyPreset()`으로 전달한다.
- Zone 진입, 몬스터 활성화·완료와 바리게이트 진행을 조정한다.
- 방향 벡터를 하드코딩하지 않는다.
- `CurrentZone`은 `ZoneEntryTrigger`, `CurrentState`는 `GravityEventTrigger`가 각각 변경하는 독립 상태임을 유지한다.
- 같은 진행 경계가 두 값을 함께 바꿔야 할 때 동일 GameObject의 두 Trigger 결과와 `CurrentPreset`이 최종적으로 일치하는지 검증한다.

### `GravityManager`

- Preset 적용, 이전 동작 취소, `PresentationUp`, Player·Camera 전환을 소유한다.
- Trigger나 Zone별 방향을 추측하지 않는다.
- Periodic 실행, 다음 방향·남은 시간 계산과 `GravityChangeWarning` 이벤트 발생을 소유한다.

### `InGameHudCanvas`

- `GravityChangeWarning`을 구독해 예고 시간 동안 다음 중력 방향을 간단한 HUD 문구로 표시한다.
- 주기 계산이나 방향 변경을 소유하지 않으며, `GravityManager`의 이벤트와 읽기 전용 값을 표현만 한다.
- 화려한 전용 VFX를 새로 만들지 않고 기존 HUD에 경고 Text 하나를 추가하는 최소 MVP로 구현한다.

```text
맵 Collider와 실제 동선
  └─ 사람이 목표 착지면과 월드 축을 판정
        ↓
GravityPreset(direction, strength)
        ↓ Inspector reference
GravityEventTrigger
        ↓ Triggered
GameFlowManager
        ↓ ApplyPreset
GravityManager
  ├─ GravityState
  ├─ PlayerController
  ├─ ThirdPersonCameraController
  └─ GravityBody
```

## 7. Trigger 매핑 방식 결정

새 Zone별 방향 enum이나 `ZoneId → Vector3` 코드를 만들지 않고 기존 `GravityEventTrigger.Preset` 참조를 그대로 사용한다.

### Zone 03 전환

- 중력 변경이 Zone 03 착지 전에 필요하므로 기존 선행 Trigger인 `Trigger_ToShfitGravity`를 유지한다.
- Phase 1의 6축 Preset 중 실제 목표 표면으로 안정적으로 떨어지는 하나를 연결한다.
- 중앙 진입, 좌우 가장자리 진입 모두 같은 목표면에 도달하지 못하면 해당 방향은 실패로 판정한다.

### Zone 04 전환

- 현재 Zone 03 입구와 겹친 `Trigger_ToInversion`의 `GravityEventTrigger`는 운영 경로에서 제외한다.
- 기존 Collider Transform은 변경하지 않는다.
- `Zone04_EntryTrigger`의 기존 BoxCollider를 진행 경계로 재사용하고, 같은 GameObject에 `GravityEventTrigger`를 추가해 선택한 Preset을 연결한다.
- 새 컴포넌트를 `GameFlowManager.gravityEventTriggers`에 등록한다.
- 같은 Collider의 `ZoneEntryTrigger`와 `GravityEventTrigger`는 같은 진입 프레임에 각각 Zone 진행과 중력 적용을 알리며, 어느 호출 순서에서도 최종 `CurrentZone`, `CurrentState`, `CurrentPreset`이 일치해야 한다.
- Phase 3에서는 `eventType = Inversion`을 유지하면서 Preset만 `GravityPreset_PeriodicZ`로 교체한다.
- `GravityPreset_PeriodicZ`는 `[-Z, +Z]`, Strength `9.81`, 변경 간격 `10초`, 예고 `1초`를 사용한다.
- Zone 04 진입 즉시 `-Z`를 적용하고, 9초 뒤 경고를 표시한 다음 1초 후 `+Z`로 바뀌는 첫 사이클을 확인한다.

### Zone 05 전환

- `/Environment/Zone05_EntryTrigger`의 기존 BoxCollider와 `ZoneEntryTrigger`를 그대로 사용한다.
- 같은 GameObject에 `GravityEventTrigger(ZeroGravity, GravityPreset_ZeroGravity)`를 추가하고 `GameFlowManager.gravityEventTriggers`에 한 번만 등록한다.
- Zone 05 진입 뒤 최종 상태는 `Zone05_ZeroGravitySource / ZeroGravity / GravityPreset_ZeroGravity`여야 한다.
- 기존 `GravityPreset_TestZero` GameObject는 Mode와 fileID를 보존한 채 `GravityPreset_ZeroGravity`로 이름만 변경해 운영 Preset으로 승격한다.
- 기존 별도 `Trigger_ToZeroGravity` GameObject, BoxCollider와 MeshRenderer는 보존하되 `GravityEventTrigger` 컴포넌트는 비활성화하고 GameFlow 구독 배열에 포함하지 않는다.
- 동일 진입에서 Zone과 Zero Gravity 적용 순서가 달라도 최종 상태가 같아야 하며, 중력 이벤트 실패 때문에 Zone만 05로 진행하는 부분 성공 상태를 허용하지 않는다.

### 겹친 기존 Inversion Trigger 처리

- 기존 `Trigger_ToInversion` GameObject, BoxCollider와 MeshRenderer는 삭제·이동·크기 변경하지 않는다.
- 해당 GameObject의 `GravityEventTrigger` 컴포넌트만 비활성화하고 `GameFlowManager.gravityEventTriggers` 배열에서 제외한다.
- 실행 후 Console에 기존 입구에서 `Inversion` 로그가 발생하지 않는지 확인한다.

## 8. 방향 선택 기준

각 Zone의 최종 방향은 계획 단계에서 추측으로 확정하지 않고 Phase 2 실행 중 다음 근거로 결정한다.

1. Scene View에서 목표 표면의 world normal과 Trigger 통과 방향을 확인한다.
2. 후보 중력 방향을 `-surfaceNormal`에 가장 가까운 월드 축으로 좁힌다.
3. Gravity Manager Inspector의 운영 API로 후보 Preset을 수동 적용한다.
4. Player가 Trigger 중앙선에서 추가 공중 조작 없이 목표면에 닿는지 확인한다.
5. 착지 후 Grounded, 이동, 카메라 Look, 점프와 다음 Zone 접근을 확인한다.
6. 경계 가장자리에서도 잘못된 먼 벽이나 이전 구역으로 떨어지지 않는지 확인한다.
7. 통과 가능한 후보가 둘 이상이면 다음 Zone까지 더 짧고 명확한 동선을 우선한다.

자동 Raycast 선택은 도입하지 않는다. 장식 MeshCollider나 이전 Zone 표면을 잘못 선택할 수 있고 결과가 플레이어 위치에 따라 달라져 진행 정본이 불안정해지기 때문이다.

## 9. Phase 1 — 월드 6축 고정형 Preset 라이브러리

### 변경 방향

`/GamePlay/GravitySystem` 아래에 기존 고정형 Preset과 같은 컴포넌트 구조로 다음 세 오브젝트를 추가한다.

| 신규 오브젝트 | Direction | Strength |
|---|---:|---:|
| `GravityPreset_WorldPosY` | `(0, 1, 0)` | `9.81` |
| `GravityPreset_WorldPosZ` | `(0, 0, 1)` | `9.81` |
| `GravityPreset_WorldNegZ` | `(0, 0, -1)` | `9.81` |

### 실행 순서

1. `[기존 GravitySystem과 Preset 기준선 확인]` → verify: `[Normal -Y, World +X, World -X와 테스트 Preset의 이름·참조·값 기록]`
2. `[World +Y, +Z, -Z 고정형 Preset 생성]` → verify: `[각 오브젝트가 Fixed, 단위 방향, 9.81이며 다른 Preset 참조를 변경하지 않음]`
3. `[Inspector와 직렬화 값 검증]` → verify: `[World Axis Presets 버튼 결과와 저장된 direction 일치, TryValidate 오류 없음]`
4. `[6축 운영 API 수동 적용]` → verify: `[각 Preset이 GravityManager.ApplyPreset 경로로 적용되고 CurrentPreset·Direction·Strength 갱신]`
5. `[Player·Camera·GravityBody 축별 회귀 확인]` → verify: `[Presentation Up = -Direction, Player Up과 Camera Up 일치, 전환 종료 후 입력 잠금 해제]`
6. `[Phase 1 diff와 보호 영역 확인]` → verify: `[GamePlayScene_Player의 GravitySystem 추가만 존재하고 Original 씬·Collider·설정 파일 변경 없음]`

### Phase 1 완료 기준

- 월드 `±X`, `±Y`, `±Z`를 대표하는 Fixed Preset 여섯 개를 즉시 선택할 수 있다.
- `-Y`는 기존 `GravityPreset_Normal`이 계속 담당한다.
- 방향 enum이나 축별 분기 코드가 추가되지 않는다.
- 여섯 방향이 같은 `ApplyPreset()` 경로를 사용한다.
- 컴파일 오류가 없고 새 Console Error가 없다.
- 아직 Zone에 연결하지 않은 새 Preset을 게임플레이 완료로 과장하지 않는다.

### Phase 1 구현·검증 기록 — 2026-08-25

- `GamePlayScene_Player`의 `/GamePlay/GravitySystem` 아래에 `GravityPreset_WorldPosY`, `GravityPreset_WorldPosZ`, `GravityPreset_WorldNegZ`를 추가했다.
- 신규 Preset 세 개는 모두 `Fixed`, 방향 단위 벡터 `(0, 1, 0)`, `(0, 0, 1)`, `(0, 0, -1)`, 세기 `9.81`로 저장했다.
- 기존 `GravityPreset_Normal(-Y)`, `GravityPreset_WorldPosX(+X)`, `GravityPreset_WorldNegX(-X)`, 테스트 Preset, Trigger 참조는 변경하지 않았다.
- Play Mode에서 여섯 Fixed Preset을 각각 `GravityManager.ApplyPreset()`으로 적용했다. 모든 방향에서 `CurrentPreset`, `Direction`, `Strength = 9.81`이 일치했고 전환 종료 후 `IsTransitioning = false`였다.
- 모든 방향에서 목표 Up과 `PresentationUp`·Player Up·Camera Up의 내적이 약 `1.0`이었다. 검사 종료 전 `GravityPreset_Normal`을 다시 적용해 `World -Y`로 복원했다.
- 기존 `GravityTestBody_Mass1`과 `GravityTestBody_Mass5`를 Play Mode에서만 충돌 없는 위치에 두고 여섯 방향을 적용했다. 두 바디의 속도 방향은 매번 현재 중력 방향과 내적 `1.0`이었고, 질량 `1`과 `5`의 속력 차이는 `0`으로 `ForceMode.Acceleration` 계약을 유지했다.
- 신규 Console Error는 없었다. 기존 Console 버퍼에는 이전 도구 검사 오류만 남아 있으며 이번 Phase 1 검사 이후 새 오류는 추가되지 않았다.
- `Assembly-CSharp.csproj`와 `Assembly-CSharp-Editor.csproj`의 `--no-restore` 빌드는 각각 경고 0개, 오류 0개로 통과했다.
- Phase 2의 Trigger–Preset 연결, Collider, Zone 진행 매핑은 변경하지 않았다. 실제 Zone 03·04 동선 완료는 아직 검증하지 않았다.

## 10. Phase 2 — Zone Trigger 매핑과 실제 동선 검증

### 실행 전 매핑 표

| 진행 경계 | 현재 Trigger | 현재 Preset | 최종 후보 | 계획 상태 |
|---|---|---|---|---|
| 시작/Zone 02 | 초기 Preset | `Normal(-Y)` | `Normal(-Y)` | 유지 |
| Zone 02 → Zone 03 | `Trigger_ToShfitGravity` | `World +X` | `World +Z` | 바닥형 `Zone03_EntryTrigger` 방향으로 연속 착지·Zone 판정 재검증 |
| Zone 03 → Zone 04 | `Zone04_EntryTrigger` | 겹친 입구의 `World -X` | `Normal(-Y)` | 경계 교체·정확한 반대축·동일 바닥면 안착 검증 |
| Zone 04 → Zone 05 | `Zone05_EntryTrigger` + 별도 `Trigger_ToZeroGravity` | Preset 미연결 | `GravityPreset_ZeroGravity` | Phase 2 제외, Phase 3 통합 대상 |

### 실행 순서

1. `[Zone 03·04 Collider와 Trigger 기준선 캡처]` → verify: `[각 Trigger 위치·회전·크기, 후보 착지면 normal, 현재 중복 Trigger 상태 기록]`
2. `[Zone 03 후보 방향 수동 적용 비교]` → verify: `[중앙·가장자리 진입의 착지면, Grounded, 다음 진입 Trigger 접근 가능 여부 기록]`
3. `[Zone 03 Preset 확정 및 Shift Trigger 연결]` → verify: `[Trigger_ToShfitGravity가 선택된 Preset 하나만 참조하고 1회 적용]`
4. `[기존 겹친 Inversion 운영 경로 제거]` → verify: `[컴포넌트 비활성, GameFlow 구독 배열 제외, Collider Transform 무변경]`
5. `[Zone 04 후보 방향 수동 적용 비교]` → verify: `[Zone 03 완료 후 목표 표면에 착지하고 Zone 04 진행 경로가 유지됨]`
6. `[Zone04_EntryTrigger에 Inversion 이벤트 연결]` → verify: `[기존 BoxCollider 재사용, 신규 GravityEventTrigger와 선택 Preset 등록, 중복 적용 없음]`
7. `[Zone 진행과 Preset 상태 대조]` → verify: `[CurrentZone·CurrentState·CurrentPreset·GravityState.Direction이 각 경계 후 합의된 값으로 일치]`
8. `[Entry부터 Zone 04까지 연속 Play Mode 검증]` → verify: `[Zone03 진입, 거미 처치, 3→4 문 개방, Zone04 진입과 다음 중력 전환 성공]`
9. `[최종 매핑 표·문서·diff 갱신]` → verify: `[선택 방향과 탈락 후보 근거 기록, 보호 영역·사용자 기존 변경 미침범]`

### Phase 2 실패 케이스

- 같은 입구에서 Shift와 Inversion이 모두 발생한다.
- Trigger 중앙으로 통과했는데 목표가 아닌 먼 표면에 착지한다.
- 착지했지만 Ground Probe가 지면을 찾지 못한다.
- Player와 Camera의 Up이 서로 다르거나 전환 종료 후 Look이 잠긴다.
- Zone 03에 도달했지만 `CurrentZone`이 Zone 02에 남는다.
- 거미를 처치했지만 `3→4_MapBoxBarrier`가 열리지 않는다.
- Zone 04 진입 시 Preset은 바뀌지만 `GameFlowState`가 이전 상태에 남는다.
- 리스폰 후 현재 Zone의 Preset과 Player 자세가 복구되지 않는다.

### Phase 2 완료 기준

- Zone 03과 Zone 04의 최종 중력 방향이 맵 착지면 근거와 함께 문서화된다.
- 하나의 진행 경계에서 활성 Preset 하나만 적용된다.
- 기존 Collider의 위치·회전·크기를 변경하지 않고 실제 동선이 연결된다.
- Entry에서 Zone 04 진입까지 에디터 조작 없이 진행할 수 있다.
- Zone 03 진입 후 거미 처치가 현재 Zone 완료로 인정되고 `3→4` 문이 열린다.
- Player·Camera·GravityBody가 각 전환 후 같은 방향과 `PresentationUp`을 사용한다.
- 사용자 Play Mode 확인을 최종 완료 기준으로 삼는다.

### Phase 2 구현·자동 Play Mode 검증 기록 — 2026-08-25

- Phase 1 결과를 사용자가 문제없다고 확인했다.
- 최초 자동 검사에서는 `Trigger_ToShfitGravity`를 `World +Y`에 연결해 상부 암석면 안착 안정성만 확인했다. 그러나 이 검사는 착지 뒤 `Zone03_EntryTrigger` 진입을 별도 텔레포트로 처리해 실제 진행 동선을 증명하지 못했다.
- 사용자 연속 조작과 Scene 기준선 재확인 결과, `/Environment/Zone03_EntryTrigger`는 Z축 두께가 가장 얇은 XY 평면이고 Shift Trigger보다 `+Z` 쪽에 있다. 따라서 Zone 03의 의도 방향을 `World +Z`로 정정하고 `Trigger_ToShfitGravity`를 `GravityPreset_WorldPosZ`에 연결했다.
- `World +Y`는 옆 암석면에 서는 비의도 결과로 제외한다. 기존 `World +X` 실패 기록은 해당 자동 낙하 위치의 결과로만 보존하며, 최종 후보 판단은 바닥형 Zone Trigger까지의 연속 진행을 우선한다.
- 정정 뒤 운영 Shift 이벤트 경로에서 `CurrentPreset = GravityPreset_WorldPosZ`, `GravityState.Direction = (0, 0, 1)` 적용을 확인했다.
- Shift Trigger 중앙에서 이동 입력 없이 시작한 자동 직선 낙하는 중간 `/Environment/Zone_03_GravityShift/SideRock1 (9)`에 걸려 `Zone03_EntryTrigger`까지 도달하지 못했다. 실제 플레이에서는 게이트 통과 이동을 계속해 바닥형 보라색 Trigger에 안착하는 연속 동선을 사용자 조작으로 확인해야 한다.
- 기존 Zone 02 입구의 `Trigger_ToInversion`은 GameObject·BoxCollider·MeshRenderer를 보존한 채 `GravityEventTrigger`만 비활성화하고 `GameFlowManager.gravityEventTriggers`에서 제외했다.
- `Zone04_EntryTrigger`의 기존 BoxCollider를 그대로 사용해 `GravityEventTrigger(Inversion, GravityPreset_Normal)`을 추가하고 GameFlow 구독 배열에 등록했다.
- Zone 04는 Zone 03의 정정된 `World +Z`에서 `Normal(-Y)`로 전환한다. 기존 검사에서 중앙·양 가장자리 모두 `/Environment/Zone_04_Inversion/FlatRock4 (1)`에 속력 `0.012` 이하로 안착했지만, Zone 03 방향 정정 뒤 실제 연속 진입 기준으로 다시 확인한다.
- 실제 Trigger 콜백 경로에서 Shift는 한 번만 발동하고 기존 Inversion은 비활성·미발동이었다. Zone 04에서는 같은 Collider의 `ZoneEntryTrigger`와 새 `GravityEventTrigger`가 같은 진입에 동작해 `CurrentZone = Zone04_Inversion`, `CurrentState = Inversion`, `CurrentPreset = GravityPreset_Normal`로 일치했다.
- Zone 03 진입 후 클리어 API로 몬스터 처치 완료 신호를 대체해 `3->4_MapBoxBarrier`가 열리고 Zone 04가 준비되는 흐름을 확인했다. 실제 거미 전투 자체는 이번 자동 검사에서 재현하지 않았다.
- Zone 04 Trigger 재진입과 Exit 뒤에도 Preset이 복원되지 않았고 one-shot 상태가 유지됐다. 사망 처리 후 `Zone04_Inversion_Respawn`으로 복귀했으며 Preset·방향·Player Up·Camera Up이 모두 `World -Y` 기준으로 일치했다.
- 새 Play Mode Console Error는 0건이었다.
- 사용자가 실제 조작으로 Zone 03을 통과하고 Zone 04 Inversion과 Zone 05 `ZeroGravitySource` 진입까지 성공했다고 확인했다. Phase 2의 연속 진행 기준은 통과했으며, 문서는 새 Phase 3가 남아 있어 진행 중 상태를 유지한다.

## 11. Phase 3 — Zone 04 주기 중력·Zone 05 무중력 통합

### 목표 매핑

| 진입 경계 | CurrentZone | CurrentState | CurrentPreset | GravityState |
|---|---|---|---|---|
| Zone 03 진입 후 | `Zone03_GravityShift` | `GravityShift` | `GravityPreset_WorldPosZ` | Direction `+Z`, Strength `9.81` |
| Zone 04 진입 후 | `Zone04_Inversion` | `Inversion` | `GravityPreset_PeriodicZ` | `-Z` 즉시 적용 후 `-Z ↔ +Z`, Strength `9.81` |
| Zone 05 진입 후 | `Zone05_ZeroGravitySource` | `ZeroGravity` | `GravityPreset_ZeroGravity` | 진입 순간 Direction 유지, Strength `0` |

Zone 04에서는 State와 Zone이 이미 맞으므로 `Zone04_EntryTrigger`의 Preset 참조를 `GravityPreset_Normal`에서 운영 `GravityPreset_PeriodicZ`로 바꾼다. `Inversion`은 Zone 04의 GameFlow 상태 이름으로 유지하고, 실제 반복 동작은 Preset Mode `Periodic`이 담당한다. Zone 05에서는 기존 Zone 진입 판정에 Zero Gravity 이벤트와 Preset 적용을 추가해 실행 중인 Periodic을 종료한다.

### 실행 순서

1. `[Periodic 운영 Preset 승격]` → verify: `[GravityPreset_TestPeriodicX를 GravityPreset_PeriodicZ로 이름 변경, Mode = Periodic, directions = [-Z, +Z], strength = 9.81, interval = 10, warning = 1]`
2. `[Zero Gravity 운영 Preset 승격]` → verify: `[GravityPreset_TestZero를 GravityPreset_ZeroGravity로 이름 변경하고 Mode = ZeroGravity, 런타임 Strength = 0 확인]`
3. `[Zone 04 Inversion에 Periodic 연결]` → verify: `[Zone04_EntryTrigger의 eventType = Inversion 유지, Preset만 PeriodicZ로 변경, CurrentZone·CurrentState 불변]`
4. `[첫 반전과 주기 사이클 검증]` → verify: `[진입 즉시 -Z, 9초 뒤 Warning, 1초 뒤 +Z, 다음 10초 뒤 -Z 반복]`
5. `[최소 중력 변경 예고 UI 연결]` → verify: `[GravityChangeWarning 동안 기존 HUD에 다음 방향과 변경 임박 문구 표시, 종료 뒤 숨김]`
6. `[Zone 04 실제 진행 검증]` → verify: `[두 방향의 목표면 안착, 이동·카메라·점프 정상, 주기 중에도 4→5 경로 접근 가능]`
7. `[Zone 05 진입 경계에 Zero Gravity 연결]` → verify: `[Zone05_EntryTrigger에 GravityEventTrigger(ZeroGravity, GravityPreset_ZeroGravity) 추가]`
8. `[GameFlow 운영 구독 정리]` → verify: `[Shift, Zone04 Entry, Zone05 Entry만 각각 한 번 등록되고 Preset null 없음]`
9. `[기존 별도 Zero Gravity Trigger 중복 제거]` → verify: `[GameObject·Collider·Renderer 보존, GravityEventTrigger 비활성, 구독 배열 제외]`
10. `[Zone 05 최종 상태 정합성 검증]` → verify: `[CurrentZone = Zone05_ZeroGravitySource, CurrentState = ZeroGravity, CurrentPreset = GravityPreset_ZeroGravity, Strength = 0, Periodic Running = false]`
11. `[Zero Gravity 런타임 회귀 검증]` → verify: `[진입 순간 Direction·PresentationUp 유지, Player ZeroGravity 상태 진입, 무기 반작용 동작]`
12. `[재진입·리스폰·Console 검증]` → verify: `[one-shot 중복 없음, Zone05 리스폰 뒤 Zero Gravity 복구, 신규 Error·NullReferenceException 없음]`
13. `[사용자 연속 Play Mode 확인]` → verify: `[Zone04 주기 전환 예고·양방향 착지부터 Zone05 무중력 진입과 반작용 이동까지 에디터 조작 없이 성공]`

### 실패 케이스

- Zone 04에서 `CurrentState = Inversion`이지만 CurrentPreset이 `GravityPreset_PeriodicZ`가 아니거나 Periodic 실행이 시작되지 않는다.
- 첫 방향이 `-Z`가 아니거나 이후 `+Z`, `-Z` 순환이 지정 간격대로 반복되지 않는다.
- 경고 이벤트가 없거나 실제 방향 변경 뒤에 표시되어 플레이어가 전환을 예측할 수 없다.
- `-Z` 또는 `+Z` 전환 후 목표면에 착지하지 못하거나 Zone 05 경로에 접근할 수 없다.
- Zone 05에서 `CurrentZone`만 바뀌고 State가 `Inversion`에 남는다.
- Zone 05 State는 `ZeroGravity`지만 `CurrentPreset`이 null이거나 이전 Fixed Preset에 남는다.
- Zero Gravity 진입 순간 방향 또는 `PresentationUp`이 임의의 축으로 바뀌어 Player·Camera가 추가 회전한다.
- Zone 05 진입 뒤에도 Periodic Coroutine이 살아 있어 Zero Gravity 상태에서 방향 전환이 다시 발생한다.
- 신규 Zone05 Entry와 기존 `Trigger_ToZeroGravity`가 모두 발동해 중복 이벤트가 발생한다.
- Zero Gravity에서 발사 반작용이 적용되지 않거나 Fixed 중력 상태에서도 반작용이 적용된다.
- Zone05 리스폰 뒤 Strength가 0이 아니거나 이전 Zone의 Preset으로 돌아간다.

### 완료 기준

- Zone 03·04·05의 `CurrentZone`, `CurrentState`, `CurrentPreset`, Direction과 Strength가 목표 매핑 표와 일치한다.
- Zone 04 진입 즉시 `World +Z → World -Z`로 반전한 뒤 `-Z ↔ +Z` 주기를 지속하고 실제 진행 동선을 보존한다.
- 각 변경 전에 최소 HUD 경고가 표시되고 `NextPeriodicDirection`과 일치한다.
- Zone 05 진입 한 번으로 Zone과 State가 함께 바뀌고 Zero Gravity Preset이 한 번만 적용된다.
- Zero Gravity는 Zone 05 진입 순간의 방향과 `PresentationUp`을 보존하면서 Strength만 0으로 만들고 Periodic 실행을 종료한다.
- 기존 Collider Transform과 필드는 변경하지 않는다.
- 사용자 Play Mode에서 Zone 04 주기 전환과 예고부터 Zone 05 무중력 반작용 이동까지 연속 확인한다.

### Phase 3 구현·자동 Play Mode 검증 기록 — 2026-08-25

- `GravityPreset_TestPeriodicX`를 `GravityPreset_PeriodicZ`로 승격하고 Periodic 방향을 `[-Z, +Z]`, Strength `9.81`, 변경 간격 `10초`, 예고 `1초`로 구성했다.
- 최초 `4초`였던 변경 간격은 사용자 Play Mode 체감상 너무 빨라 `10초`로 조정했다.
- `GravityPreset_TestZero`를 `GravityPreset_ZeroGravity`로 승격했다. Zero Gravity Mode의 런타임 Strength는 `0`이며 진입 순간 방향과 `PresentationUp`을 유지한다.
- `Zone04_EntryTrigger`의 `eventType = Inversion`은 유지하고 Preset을 `GravityPreset_Normal`에서 `GravityPreset_PeriodicZ`로 교체했다.
- `Zone05_EntryTrigger`의 기존 BoxCollider와 `ZoneEntryTrigger`를 보존한 채 `GravityEventTrigger(ZeroGravity, GravityPreset_ZeroGravity)`를 추가했다.
- `GameFlowManager.gravityEventTriggers`를 Shift, Zone04 Entry, Zone05 Entry 세 항목으로 구성했다. 기존 별도 `Trigger_ToZeroGravity`의 GameObject·Collider·Renderer는 보존하고 `GravityEventTrigger`만 비활성화했다.
- `InGame HUD Canvas`에 비활성 기본 상태의 `Gravity Warning Text`를 추가했다. `InGameHudCanvas`가 `GravityChangeWarning`을 구독해 `GRAVITY SHIFT → +Z/-Z`를 표시하고 경고 종료 또는 Periodic 중단 시 숨긴다.
- 운영 이벤트 경로에서 Zone04 진입 직후 `CurrentZone / CurrentState / CurrentPreset = Zone04_Inversion / Inversion / GravityPreset_PeriodicZ`, Direction `-Z`, Strength `9.81`, `IsPeriodicRunning = true`를 확인했다.
- 첫 주기에서 다음 방향 `+Z` 경고와 HUD 표시 후 Direction `+Z` 전환, 다음 주기에서 `-Z` 경고와 HUD 표시를 확인했다.
- `-Z` 전환 경고 중 Zone05 이벤트를 적용했을 때 최종 `Zone05_ZeroGravitySource / ZeroGravity / GravityPreset_ZeroGravity`, Strength `0`, `IsPeriodicRunning = false`, Warning false, Next Direction zero, HUD 비활성으로 정리됐다.
- Zone05 사망 처리 후에도 Zero Gravity Preset·Direction·Strength와 Zone/State가 복구됐고, 무중력 반작용 API가 `true`를 반환하며 정지 상태 속도를 `0.3`만큼 변경했다.
- Unity Play Mode Console Error는 0건이었다. `Assembly-CSharp` 빌드는 오류 0건과 기존 외부 에셋·타 시스템 경고 28건, Editor 어셈블리는 경고 0건·오류 0건으로 통과했다.
- 자동 검증은 운영 Trigger 콜백과 GameFlow 공개 API를 호출한 하네스다. 이후 사용자 연속 Play Mode로 실제 맵 이동 중 양방향 착지, HUD 가독성, Zone05 경계 진입과 반작용 조작감을 확인해 Phase 3 완료 기준을 충족했다.

## 12. 예상 변경 파일

필수:

- `Assets/_Scenes/GamePlayScene_Player.unity`
- `Assets/_Custom/Prefabs/UI/InGame HUD Canvas.prefab`
- `Assets/_Scripts/UI/InGameHudCanvas.cs`
- `Docs/ksh/Tasks/02_in-progress/world_axis_gravity_preset_zone_trigger_mapping_plan.md`

필요한 경우에만:

- Periodic·Zero Gravity 실행 로직은 기존 코드를 재사용하고 Scene의 Preset·Trigger·GameFlow 직렬화 참조만 연결한다.
- `InGameHudCanvas`는 기존 `GravityChangeWarning` 이벤트를 표시하는 최소 구독·표시 책임만 추가한다.
- 동일 Collider의 `ZoneEntryTrigger`와 `GravityEventTrigger` 호출 순서 때문에 상태 불일치가 재현될 때만 `Assets/_Scripts/GameFlow/GameFlowManager.cs`에 최소 조정을 검토한다.

수정 금지:

- `Assets/_Scenes/Original_GamePlayScene.unity`
- 기존 Collider가 소속된 지형·트리거 Transform과 Collider 필드
- `Packages/`
- `ProjectSettings/`
- Build Settings와 active build target

## 13. 검증 기준

### 정적·직렬화 검증

- 신규 Fixed Preset 세 개의 이름, Mode, 방향, 세기 확인
- 모든 운영 `GravityEventTrigger`의 Preset null 여부 확인
- `GameFlowManager.gravityEventTriggers`에 활성 운영 Trigger만 한 번씩 등록됐는지 확인
- 기존 입구의 Shift/Inversion 중복 활성 경로 제거 확인
- `GravityPreset_PeriodicZ`의 Mode, `[-Z, +Z]`, Strength `9.81`, 간격 `10초`, 예고 `1초` 확인
- Zone 04 Inversion Trigger가 `GravityPreset_PeriodicZ`를 참조하는지 확인
- Zone 05 Entry의 Zero Gravity Trigger와 `GravityPreset_ZeroGravity` 참조 확인
- 운영 `gravityEventTriggers` 배열이 Shift, Zone04 Entry, Zone05 Entry 세 항목으로만 구성되는지 확인
- HUD 경고 Text 참조와 `GravityManager` 런타임 바인딩 확인
- YAML fileID 중복과 missing script 없음
- `git diff --check`
- 런타임 및 Editor 어셈블리 컴파일 오류 0건

### Play Mode 검증

- 6축 Preset 수동 적용 시 Player·Camera·GravityBody 기준 일치
- Zone 03 진입 후보별 착지면 비교
- 확정 방향으로 중앙·가장자리 진입 성공
- Zone 03 진입 로그와 `CurrentZone = Zone03_GravityShift` 확인
- 거미 처치 후 `3→4_MapBoxBarrier = Open` 확인
- Zone 04 진입 시 선택 Preset 한 번 적용
- Zone 04 진입 후 `CurrentZone / CurrentState / CurrentPreset = Zone04_Inversion / Inversion / GravityPreset_PeriodicZ` 확인
- 진입 즉시 `-Z`, 예고 뒤 `+Z`, 다음 예고 뒤 `-Z` 적용과 `IsPeriodicRunning = true` 확인
- 경고 표시의 다음 방향과 `NextPeriodicDirection` 일치 확인
- Zone 05 진입 후 `CurrentZone / CurrentState / CurrentPreset = Zone05_ZeroGravitySource / ZeroGravity / GravityPreset_ZeroGravity` 확인
- Zero Gravity 진입 전후 당시 Direction과 `PresentationUp` 유지, Strength `0`, `IsPeriodicRunning = false` 확인
- Zero Gravity 무기 반작용과 최대 속력 제한 회귀 확인
- 같은 Trigger 재진입과 `OnTriggerExit`에서 중력 복원 없음
- 사망·리스폰 후 현재 Preset과 Player Up 복구
- 새 Console Error와 `NullReferenceException` 없음

### 최종 사용자 확인

사용자가 실제 조작으로 다음을 확인해야 완료한다.

1. Zone 02에서 방향 전환 Trigger 중앙으로 진입한다.
2. 의도한 벽 또는 천장에 자연스럽게 착지한다.
3. Zone 03 진입 판정 후 거미를 처치한다.
4. `3→4` 문이 열리고 다음 구역으로 이동한다.
5. Zone 04 진입 즉시 `World -Z`로 반전되는지 확인한다.
6. 경고 표시 뒤 `World +Z`, 다시 `World -Z`로 주기 전환되는지 확인한다.
7. 양방향 목표면에서 진행 가능하고 Zone 05 경계까지 이동할 수 있는지 확인한다.
8. Zone 05 진입 즉시 주기 전환이 멈추고 중력이 0이 되며 Player·Camera가 불필요하게 추가 회전하지 않는지 확인한다.
9. 무중력에서 사격 반작용으로 이동·조향·제동할 수 있는지 확인한다.
10. 각 중력 전환 뒤 이동·카메라·점프가 정상인지 확인한다.

## 14. 문서와 작업 상태 관리

- 사용자 실행 승인 전에는 이 문서를 `Docs/ksh/Tasks/01_planned`에 둔다.
- 실행을 시작할 때 `02_in-progress`로 이동한다.
- Phase 1만 완료된 상태에서는 전체 계획을 완료 처리하지 않는다.
- Phase 2의 사용자 연속 Play Mode 확인은 완료됐다.
- Phase 3의 구현·자동 검증과 사용자 연속 Play Mode 확인까지 완료했으므로 이 문서를 `03_completed`로 이동한다.
- 계획만 작성한 이번 단계에서는 `Docs/ksh/Codex_Usage_Records.md`에 완료 기록을 추가하지 않는다.
- 구현·검증이 완료되면 세 Phase를 하나의 의미 있는 완료 작업 단위로 기록한다.

# AudioSystem Prefab 기반 플레이어 환경음 관리 계획서

- 상태: 계획됨
- 작성일: 2026-08-25
- 계획 프로필: deep — 현재 씬 소유 컴포넌트를 재사용 가능한 Prefab으로 옮기면서, 공통 음향 설정과 씬 의존 참조의 소유권을 분리한다.

## 목표

현재 `GamePlayScene_Player`의 `GameFlowManager`에 붙어 있는 `AudioEnvironmentController`를 독립 `AudioSystem` Prefab으로 구성한다. 이를 통해 Entry/Cave Snapshot, 리버브 전환 시간, 동굴 강도 같은 공통 환경음 설정은 하나의 Prefab Inspector에서 관리하고, 각 씬은 자신의 `GameFlowManager`와 Player `AudioReverbFilter`만 연결한다.

이 구조는 향후 몬스터·월드 SFX를 `SFX` 하위 Mixer 그룹에 추가해도 환경 전환의 소유자를 바꾸지 않게 하는 기반이다.

## 범위

### 포함

- `Assets/_Custom/Prefabs/Audio/AudioSystem.prefab` 생성
- `AudioEnvironmentController`의 공통 설정과 씬 참조의 책임 분리
- `GamePlayScene_Player`에서 기존 GameFlowManager 부착 컨트롤러를 AudioSystem Prefab 인스턴스로 교체
- 기존 Entry/Cave 전환, Player 리버브, 사격음·발소리 라우팅의 동작 보존
- Prefab Inspector에서 Cave 조절값을 찾을 수 있는 구성 및 오류 메시지 정리

### 제외

- `Original_GamePlayScene` 수정·이관
- 몬스터, 월드, UI, BGM의 실제 SFX 라우팅
- 새 AudioSource, 새 Mixer 그룹, 볼륨 UI, 저장 옵션 추가
- 플러그인 도입, Packages·ProjectSettings·Build Settings 변경, WebGL 빌드
- AudioSystem을 씬 전환 간 `DontDestroyOnLoad` 싱글턴으로 전환

## 현재 상태와 문제

현재 `AudioEnvironmentController`는 `GameFlowManager`와 같은 씬 오브젝트에 있으며 다음 네 가지를 직렬화 참조한다.

- `GameFlowManager`
- `Entry` Snapshot
- `Cave` Snapshot
- Player 루트의 `AudioReverbFilter`

이 배치는 동작에는 문제가 없지만, 공통 음향 조절값과 특정 씬·플레이어를 가리키는 참조가 같은 컴포넌트에 섞여 있다. Prefab으로 옮길 때 Player 또는 GameFlowManager를 Prefab Asset에 직접 저장하려 하면 씬 오브젝트 참조를 보관할 수 없으므로, 공통 설정은 Prefab Asset에 두고 씬 참조는 Prefab 인스턴스의 바인딩 값으로 남겨야 한다.

## 목표 구조와 책임

```text
Assets/_Custom/Prefabs/Audio/AudioSystem.prefab
└─ AudioSystem
   ├─ AudioEnvironmentController
   │  └─ 공통 설정: Entry/Cave Snapshot, 전환 시간, Entry/Cave Reverb Level
   └─ AudioSystemSceneBindings
      └─ 씬 참조: GameFlowManager, Player AudioReverbFilter

GamePlayScene_Player
├─ GameFlowManager
│  └─ CurrentZoneChanged 이벤트 제공 (진행 상태는 계속 소유)
├─ Player Prefab 인스턴스
│  └─ AudioReverbFilter (플레이어 SFX에 적용될 대상)
└─ AudioSystem Prefab 인스턴스
   └─ 두 씬 참조를 바인딩하고 환경음 상태만 갱신
```

| 대상 | 소유 책임 |
| --- | --- |
| `SinkPointSfx.mixer` | `Master > SFX > Player` 경로와 Entry/Cave Snapshot 자산 |
| `AudioSystem.prefab` | 환경음 전환 정책과 조절 가능한 공통 수치 |
| `AudioSystemSceneBindings` | 해당 씬의 GameFlowManager와 Player 필터를 컨트롤러에 전달 |
| `GameFlowManager` | 현재 Zone과 Zone 변경 이벤트. 오디오 상태는 변경하지 않음 |
| Player Prefab | 발소리·사격음 Source와 공통 Reverb Filter. Zone 판정은 하지 않음 |

`AudioSystem`은 AudioSource를 새로 만들거나 Player 오디오를 복제하지 않는다. 현재의 Player AudioSource 두 개와 Mixer 라우팅은 그대로 둔다.

## 설계 상세

### 1. Prefab의 공통 환경 설정

`AudioEnvironmentController`는 다음처럼 모든 씬에서 동일한 값만 보관한다.

- `Entry Snapshot`, `Cave Snapshot`
- `Zone Transition Duration` (현재 의도 `0.4초`)
- `Entry Reverb Level`, `Cave Reverb Level`
- Entry와 동굴을 구분하는 `ZoneId` 정책

현재 `GamePlayScene_Player` 인스턴스에 적용된 리버브 조절값을 Prefab 기본값으로 옮긴다. 그러면 동굴 강도 조절은 `AudioSystem.prefab > AudioEnvironmentController` Inspector에서 이뤄지고, 씬 인스턴스에는 서로 다른 씬 참조만 Override로 남는다.

### 2. 씬 바인딩 API

새 `AudioSystemSceneBindings` 컴포넌트를 `AudioSystem` Prefab에 둔다. 이 컴포넌트는 다음 두 필드를 Inspector에 노출한다.

- `GameFlowManager gameFlowManager`
- `AudioReverbFilter playerReverbFilter`

`AudioEnvironmentController`에는 바인딩을 위한 명시적 메서드를 추가한다.

```csharp
public void Configure(GameFlowManager gameFlowManager, AudioReverbFilter playerReverbFilter)
```

`AudioSystemSceneBindings`는 `Awake`에서 이 메서드를 호출한다. 컨트롤러는 `Start`에서 바인딩 유효성을 검사하고 구독한 뒤, 현재 Zone을 즉시 적용한다. 이 순서는 Prefab 인스턴스가 활성화되는 시점에 구독이 중복되거나 초기 Zone 적용이 누락되는 일을 막는다.

바인딩 대상이 누락되면 다음을 보장한다.

- 어떤 필드가 비었는지 이름이 포함된 명확한 `Debug.LogError`를 남긴다.
- 오디오 컨트롤러만 비활성화한다.
- `GameFlowManager`, Player 이동·전투·사격 판정은 계속 동작한다.

`OnDisable`에서는 현재 구독된 `GameFlowManager.CurrentZoneChanged`만 정확히 해제한다. 재바인딩이 필요해질 경우에도 기존 이벤트를 먼저 해제하고 새 대상에 한 번만 구독한다.

### 3. Zone과 리버브 전환 보존

Zone 판정은 현재 정책을 유지한다.

| Zone | 환경 |
| --- | --- |
| `Zone01_Entry` | `Entry` Snapshot, 건조한 Reverb Level |
| `Zone02_Normal` | `Cave` Snapshot, 동굴 Reverb Level |
| `Zone03_GravityShift` | `Cave` Snapshot, 동굴 Reverb Level |
| `Zone04_Inversion` | `Cave` Snapshot, 동굴 Reverb Level |
| `Zone05_ZeroGravitySource` | `Cave` Snapshot, 동굴 Reverb Level |

Snapshot은 Mixer 전환을 담당하고, Player `AudioReverbFilter.reverbLevel`은 같은 시간 동안 보간한다. Cave 값은 Filter Inspector의 고정 preset 수치가 아니라 `AudioSystem`의 컨트롤러가 Zone 전환마다 Filter에 적용하는 런타임 목표값임을 Inspector 설명으로 분명히 한다.

## 구현 순서

1. `[현재 참조와 값 기록]` `GamePlayScene_Player`의 AudioEnvironmentController, Mixer Snapshot, Player Reverb Filter와 현재 Inspector Override를 확인한다.
   → verify: Entry/Cave Snapshot, 전환 시간, Cave 강도 및 Player Filter 대상이 모두 식별된다.

2. `[Prefab과 바인더 추가]` `Assets/_Custom/Prefabs/Audio/AudioSystem.prefab`을 만들고 `AudioEnvironmentController`, `AudioSystemSceneBindings`를 같은 루트에 추가한다. Mixer/Snapshot/환경 수치는 Prefab 기본값으로 저장한다.
   → verify: Prefab Asset에는 씬 오브젝트 참조가 없고, 공통 음향 수치가 Inspector에서 편집 가능하다.

3. `[컨트롤러 바인딩 정리]` `AudioEnvironmentController`가 명시적 `Configure` 호출 뒤에만 구독·초기 적용하게 수정하고, 바인더는 Awake에서 씬 참조를 전달하게 한다. 기존 `GameFlowManager` 동위 오브젝트 탐색에 의존하지 않도록 한다.
   → verify: 유효 참조에서는 시작 Zone이 즉시 적용되고, 누락 참조에서는 오디오 컨트롤러만 명확한 오류와 함께 중지된다.

4. `[Player 씬 이관]` `GamePlayScene_Player`에 AudioSystem Prefab 인스턴스를 하나 배치하고 GameFlowManager 및 Player의 AudioReverbFilter를 바인더에 연결한다. GameFlowManager에 붙어 있던 기존 AudioEnvironmentController는 제거해 이벤트 구독자가 둘이 되지 않게 한다.
   → verify: Hierarchy에 AudioSystem은 하나이며 AudioEnvironmentController도 하나이고, `Original_GamePlayScene`에는 변경이 없다.

5. `[회귀 검증과 청감 조절]` Entry와 Zone02~05 전환, 발소리, 사격음, 장시간 연사를 확인한다. 필요하면 AudioSystem Prefab의 Cave 설정만 조절한다.
   → verify: 0.4초 전환, Entry 복귀, 실제 발사별 사격음, 새 Console 오류 없음 및 청감상 과도한 누적 없음.

6. `[기록]` 구현과 Play Mode 확인이 완료된 경우에만 `Docs/ksh/Codex_Usage_Records.md`에 Prefab 구조 전환 결과를 한 항목으로 기록한다.
   → verify: 계획 문서와 완료 기록의 범위·검증 결과가 일치한다.

## 완료 기준

- `AudioSystem.prefab` 하나에서 Entry/Cave 환경 수치를 조절할 수 있다.
- 각 씬은 Prefab 인스턴스에서 GameFlowManager와 Player Filter만 연결한다.
- AudioEnvironmentController가 Zone 변경 이벤트를 중복 구독하지 않는다.
- `GamePlayScene_Player`의 Entry/Cave 전환, 발소리·사격음 및 0.15초 사격 처리 동작이 보존된다.
- Missing Script·참조 누락·새 Console 오류 없이 Play Mode를 확인한다.
- `Original_GamePlayScene`, Packages, ProjectSettings, Build Settings에는 변경이 없다.

## 가정과 후속 확장 경계

- 이번에는 `GamePlayScene_Player`에 AudioSystem 인스턴스 하나만 둔다. 씬 전환 간 영속화가 필요해질 때만 중복 방지 정책과 `DontDestroyOnLoad`를 별도 설계한다.
- 다음 SFX 범주(몬스터·월드 등)는 각 AudioSource를 `SFX` 하위 그룹으로 라우팅하는 별도 작업에서 추가한다. AudioSystem은 그 전에 환경 전환 정책만 제공한다.
- 현재 Player 루트의 하나의 Reverb Filter가 발소리와 사격음 모두에 적용되는 정책은 유지한다. 무기별 독립 필터나 거리별 리버브는 이번 계획의 범위를 벗어난다.

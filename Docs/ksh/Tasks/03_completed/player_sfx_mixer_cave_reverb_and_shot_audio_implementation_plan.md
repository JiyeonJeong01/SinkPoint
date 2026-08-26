# 플레이어 SFX Mixer·동굴 리버브·사격음 통합 완료 계획서

- 상태: 구현 완료 (자동 검증 완료, 실제 청감 확인 일부 남음)
- 작성일: 2026-08-25
- 계획 프로필: deep — Player Prefab, 씬의 Zone 흐름, AudioMixer 자산이 함께 연결되는 변경이다.

## 목표

`GamePlayScene_Player`에서 플레이어 발소리와 사격음을 공통 `SFX/Player` 경로로 라우팅하고, 현재 Zone에 따라 Entry의 건조한 소리와 동굴 Zone의 강한 울림을 전환한다. 사격음은 기존 `0.15초` 발사 간격의 실제 발사마다 재생하되, 연사 중 원본 보이스가 누적되지 않게 한다.

## 범위와 제외 범위

### 포함

- `SinkPointSfx` AudioMixer의 `Master > SFX > Player` 그룹과 `Entry`·`Cave` Snapshot
- Player Prefab의 발소리·사격음 AudioSource 라우팅 및 공통 `AudioReverbFilter`
- `PlayerCombatController`의 사격음 재생 연결
- `AudioEnvironmentController`의 Zone 기반 Snapshot·리버브 레벨 전환
- `GamePlayScene_Player`의 `GameFlowManager` 연결

### 제외

- `Original_GamePlayScene` 수정 또는 수동 이관
- 몬스터, 월드, UI, BGM의 실제 AudioMixer 라우팅
- Steam Audio 등 외부 플러그인 도입
- Packages, ProjectSettings, Build Settings 수정 및 WebGL 빌드
- 독립 `AudioSystem` Prefab으로의 구조 전환

현재 구현은 Zone 흐름을 소유한 `GameFlowManager`와 직접 연결해야 하므로 씬의 GameFlowManager에 컨트롤러를 둔다. 향후 여러 씬에서 같은 구조를 재사용할 필요가 생기면, 컨트롤러를 담은 독립 `AudioSystem` Prefab을 만들고 각 씬에서 GameFlowManager와 Player Reverb Filter만 연결하는 방식으로 분리한다. 이는 이번 완료 범위에는 포함하지 않는다.

## 완료된 구조

```text
PlayerFootsteps
  -> Player 루트 AudioSource
  -> SinkPointSfx / Master / SFX / Player

PlayerCombatController.FireShot()
  -> 사격 전용 AudioSource.Stop() + Play()
  -> AutoGun_3p_01.wav (Volume 0.7)
  -> SinkPointSfx / Master / SFX / Player

GameFlowManager.CurrentZoneChanged
  -> AudioEnvironmentController
  -> Entry 또는 Cave Snapshot 전환 (0.4초)
  -> Player 루트 AudioReverbFilter의 Reverb Level 보간
```

### 자산과 책임

| 대상 | 책임 |
| --- | --- |
| `Assets/_Custom/Audio/SinkPointSfx.mixer` | Player를 시작점으로 확장 가능한 `Master > SFX > Player` 라우팅과 Entry/Cave Snapshot 보관 |
| `Assets/_Custom/Prefabs/Player/Player.prefab` | 발소리 AudioSource, 사격 전용 AudioSource, Player 공통 AudioReverbFilter 보관 |
| `Assets/_Scripts/Player/PlayerCombatController.cs` | 실제 사격이 성립한 직후 사격음을 1회 재생 |
| `Assets/_Scripts/Audio/AudioEnvironmentController.cs` | GameFlow Zone을 AudioMixer Snapshot과 Player 리버브 상태로 변환 |
| `Assets/_Scenes/GamePlayScene_Player.unity` | GameFlowManager와 AudioEnvironmentController의 씬 참조 연결 |

`AudioEnvironmentController`는 진행 상태를 변경하지 않고 `GameFlowManager.CurrentZoneChanged`를 읽기만 한다. Player Prefab은 재사용 가능한 출력 장치와 필터만 소유하고, 어느 Zone인지 판정하는 씬 의존 로직은 소유하지 않는다.

## 구현 내용

1. `[AudioMixer 구성]` `SinkPointSfx`에 `Master > SFX > Player` 그룹과 `Entry`, `Cave` Snapshot을 만들고 Player 두 AudioSource를 `SFX/Player`로 라우팅했다.
   → verify: Mixer 그룹 참조가 유효하며, 발소리와 사격음 모두 Player 그룹을 출력 대상으로 가진다.

2. `[Player 오디오 구성]` 기존 루트 AudioSource는 발소리 전용으로 유지하고, 사격 전용 AudioSource를 추가했다.
   - Clip: `Assets/PostApocalypseGunsDemo/AssaultRifles/AutoGun_3p_01.wav`
   - Volume: `0.7`
   - Play On Awake: 꺼짐
   - 위치 기반 3D 설정과 Mixer 효과 경로 유지
   - Player 루트의 `AudioReverbFilter`가 두 Source에 공통 적용
   → verify: Prefab에서 Clip, Volume, Mixer 출력 및 필터 참조가 연결됨을 확인했다.

3. `[사격 처리 연결]` `PlayerCombatController.FireShot()`에서 Ray·피해·반작용 처리 후 `PlayShotAudio()`를 호출하게 했다. 재생 전 기존 직접 재생을 `Stop()`하므로, 0.15초 간격 연사 중 같은 원본 클립 보이스가 겹치지 않는다. AudioSource 또는 Clip이 없으면 경고만 남기고 발사 판정은 유지한다.
   → verify: Play Mode에서 사격 AudioSource의 Clip `AutoGun_3p_01`, Volume `0.7`, `isPlaying=True` 상태를 확인했다.

4. `[환경 전환]` `AudioEnvironmentController`를 GameFlowManager에 추가했다.
   - `Zone01_Entry` → `Entry` Snapshot, Reverb Level `-10000 mB`
   - `Zone02_Normal`, `Zone03_GravityShift`, `Zone04_Inversion`, `Zone05_ZeroGravitySource` → `Cave` Snapshot, Reverb Level `+600 mB`
   - Zone 변경 시 `0.4초` 보간, 시작 시 현재 Zone 즉시 반영
   - 동굴 필터 성향: Decay Time `2.4초`, Reflections Level `0 mB`, Diffusion/Density `100`
   → verify: Play Mode에서 Entry의 `-10000 mB`에서 Zone02의 `+600 mB`로 전환되고, Decay Time `2.4초` 및 Reflections Level `0 mB` 상태를 확인했다.

## 검증 결과

- Unity 스크립트 컴파일 오류 없음.
- 수정 후 새 Play Mode 실행에서 Console Error `0` 확인.
- Entry와 Zone02 사이의 `AudioReverbFilter.Reverb Level` 런타임 전환 확인.
- 사격음 AudioSource의 Clip·Volume·재생 상태 확인.
- `Original_GamePlayScene`, Packages, ProjectSettings, Build Settings는 이번 변경 대상에서 제외했으며 수정하지 않았다.

## 남은 수동 청감 확인

자동 검증은 오디오 출력이 실제로 들리는지와 혼합 밸런스를 보장하지 않는다. Unity Editor에서 다음을 직접 확인한다.

1. Entry에서 발소리와 사격음이 건조하게 들리는지 확인한다.
2. Zone02~05에서 두 소리에 동굴 울림이 분명히 들리고, Entry 복귀 시 제거되는지 확인한다.
3. 발사 버튼을 계속 눌러 0.15초 연사에서 사격음이 매 발사마다 시작되며, 원본 클립의 다중 중첩으로 과도하게 커지지 않는지 확인한다.
4. 청감이 과하면 GameFlowManager의 `AudioEnvironmentController > Cave Reverb Level`을 낮춰 조절한다. `Cave` 값은 AudioReverbFilter Inspector의 독립 preset 값이 아니라, Zone 전환 때 컨트롤러가 해당 Filter에 적용하는 런타임 값이다.

## 완료 기준

- [x] 플레이어 발소리와 사격음이 같은 `SFX/Player` Mixer 경로를 사용한다.
- [x] 실제 사격 1회와 사격음 재생 1회가 연결되고, 직접 재생 보이스는 누적되지 않는다.
- [x] Entry/Cave Zone 전환이 Snapshot과 Player 리버브 상태를 전환한다.
- [x] 컴파일 및 새 Play Mode에서 새 Console 오류가 없다.
- [ ] Editor의 실제 청감으로 동굴 강도와 연사 밸런스를 최종 확정한다.

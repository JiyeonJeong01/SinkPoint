# Player Footstep Animation Event 실행 계획

> 상태: 완료 — 사용자 Play Mode 청취 확인 완료
>
> 작성일: 2026-08-25
>
> 프로필: deep — 외부 애니메이션 원본 보호, Animator Blend Tree 참조 교체, 중첩 프리팹 이벤트 수신, Play Mode 검증을 함께 다룬다.

## 1. 목표

`Player`가 이동 입력으로 걷거나 전진 Sprint할 때 애니메이션 보행 주기에 맞춰 `Ground_Step0`~`Ground_Step4` 중 하나를 재생한다. 소리 재생 시점은 동기화된 애니메이션 이벤트가 제공하고, `PlayerController`는 이동 의도만 제공한다.

## 2. 현재 확인된 기준

- Player Animator Controller는 `Assets/_Custom/Animations/Player/Player.controller`이다.
- Base Layer의 일반 이동은 `Locomotion Blend Tree`, 전진 달리기는 별도 `Sprint` 상태의 `machinegun_sprint` 모션을 사용한다.
- 외부 Toon Soldiers에서 온 걷기·달리기 Animation Clip은 Animation 창에서 읽기 전용이다.
- `Player.prefab`에는 Player 루트 `AudioSource`, 모델 자식 `VisualRoot/TS-Armies_Recon_B`의 `PlayerFootsteps`, `Ground_Step0`~`Ground_Step4` 참조가 구성돼 있다.
- 최초 `PlayerFootsteps.PlayFootstep()`은 Grounded만 이동 가능 조건으로 사용해 Idle·경사 미끄러짐에서도 낮은 가중치 이동 Clip 이벤트를 허용하고, 서로 다른 방향 Clip의 이벤트 주기가 섞이는 문제가 있었다.

## 3. 범위

- 실제 Locomotion Blend Tree와 Sprint가 참조하는 걷기·달리기 원본 Clip을 식별한다.
- 그 Clip만 Player 소유의 편집 가능한 `.anim` 자산으로 추출한다.
- 추출본의 발 착지 프레임에 `PlayFootstep` Animation Event를 추가한다.
- `Player.controller`가 원본이 아닌 추출본을 사용하도록 해당 모션 참조만 교체한다.
- `Player.prefab`의 AudioSource와 `PlayerFootsteps` 참조, Play Mode 재생 여부를 확인한다.

## 4. 하지 않을 것

- Toon Soldiers 원본 Prefab, FBX, FBX `.meta`를 수정하지 않는다.
- `Original_GamePlayScene`, Collider, Packages, ProjectSettings, Build Settings를 변경하지 않는다.
- 표면별 발소리, 착지/점프/웅크리기 전용 소리, Audio Mixer, VFX, Timeline은 이번 범위에 넣지 않는다.
- 기존 이동 물리, 입력, Animator 파라미터·전이 조건을 재설계하지 않는다.

## 5. 설계와 책임 경계

| 항목 | 소유자 | 책임 |
| --- | --- | --- |
| 원본 애니메이션 | Toon Soldiers 외부 에셋 | 읽기 전용 동작 기준 보존 |
| 편집용 locomotion Clip | `Assets/_Custom/Animations/Player` | 원본 Curve를 유지한 이벤트 전용 사본 |
| 발 접지 시점 | Animation Event | 각 발이 바닥에 닿는 시간에 `PlayFootstep` 호출 |
| 재생 필터·랜덤 선택 | `PlayerFootsteps` | 이동 의도·이벤트 Clip의 유효 Blend Weight 확인, Step0~4 랜덤 선택, 동시 이벤트 중복 제한 |
| 실제 소리 출력 | Player 루트 `AudioSource` | `PlayOneShot` 출력 |
| 이동 의도 정본 | `PlayerController` | `PlayerInput.Move` 기반 이동 의도 제공; Rigidbody 미끄러짐을 이동 의도로 취급하지 않음 |

Animation Event 수신기는 Animator가 있는 `TS-Armies_Recon_B`에 유지한다. 해당 컴포넌트는 부모의 Player AudioSource와 PlayerController를 직렬화 참조하므로, 중첩 모델에서 이벤트를 받아도 Player의 공통 오디오 출력을 사용한다.

## 6. 필요한 가정

- 현재 `Locomotion Blend Tree`와 `Sprint`에서 쓰는 원본 Clip은 Unity에서 Curve를 보존한 편집용 `.anim`으로 추출할 수 있다.
- 걷기/달리기 애니메이션이 실제로 발 접지 동작을 포함하며, Preview에서 두 착지 시점을 식별할 수 있다.
- `GamePlayScene_Player`의 Player는 `Assets/_Custom/Prefabs/Player/Player.prefab` 연결 인스턴스다.

추출이 불가능하거나 Clip이 다른 모델 Importer에 묶여 있다면, 원본 Import Settings를 변경하지 않는다. 그때는 원본과 동일한 Avatar·Curve를 유지하는 Player 소유 복사본을 만드는 구체적 방법을 먼저 확인하고 실행을 멈춘다.

## 7. 실행 단계

1. `[정본 Motion 식별]` Player.controller의 Locomotion Blend Tree 자식과 Sprint 모션을 읽어 실제 걷기·달리기 Clip 경로, 방향별 공유 여부, 재생 길이를 기록한다. → verify: 대상 Clip 목록이 원본 경로·사용 상태와 함께 확정되고, 점프·발사·웅크리기 Clip은 목록에서 제외된다.

2. `[편집용 Clip 추출]` 대상 Clip만 `Assets/_Custom/Animations/Player/Footsteps/` 아래에 원본 Curve·Loop 설정을 유지한 새 `.anim`으로 추출한다. → verify: 새 Clip의 길이·Loop·Curve 바인딩이 원본과 일치하고, Toon Soldiers 원본 파일과 `.meta`에는 diff가 없다.

3. `[착지 이벤트 배치]` 각 편집용 Clip의 실제 접지 프레임에 `PlayFootstep` 이벤트를 추가한다. Sprint의 이벤트 간격은 원본 Curve를 기준으로 별도 배치한다. → verify: 0.833초 방향 이동 Clip에는 좌·우 접지 이벤트가 2개씩, 두 보행 주기를 포함한 1.333초 Sprint Clip에는 이벤트가 4개 존재하며, 함수명은 정확히 `PlayFootstep`이고 점프·공중·발사 Clip에는 이벤트가 없다.

4. `[Animator 참조 최소 교체]` 기존 Blend Tree/Sprint가 사용하는 대상 모션만 편집용 Clip으로 교체한다. 파라미터, 상태, 전이, Blend 좌표와 Root Motion 설정은 유지한다. → verify: Controller diff가 대상 Motion 참조 변경으로 제한되고, 기존 Locomotion·Sprint·Jump·Crouch 전이가 동일하다.

5. `[Prefab 구성 확인]` Player.prefab에서 Player 루트 AudioSource의 Mute 해제·Volume·3D 설정·`Play On Awake` 비활성화를 확인하고, 모델 자식 `PlayerFootsteps`의 AudioSource·PlayerController·다섯 Clip 참조를 재확인한다. → verify: Missing Script 0개, 모든 Clip 참조 유효, 외부 모델 원본 Prefab에는 변경 없음.

6. `[컴파일과 Play Mode 검증]` Unity 재컴파일 후 `GamePlayScene_Player`에서 걷기, Sprint, 점프 중 공중, 무중력, 멈춤, 걷기↔Sprint 전환을 확인한다. → verify: 지상 걷기·Sprint에서 발소리가 접지와 동기화되고, 공중/무중력/정지에는 재생되지 않으며, 전환 시 눈에 띄는 이중 재생·Console 오류가 없다.

7. `[최종 변경 검토]` Player 소유 `.anim`, Player.controller, Player.prefab, PlayerFootsteps와 필요한 `.meta`만 확인한다. → verify: `git diff --check`, Original 씬·외부 Toon Soldiers 원본·Packages·ProjectSettings·Build Settings 무변경 확인. 사용자의 Play Mode 체감 확인을 완료 기준으로 보고, 실제 WebGL 빌드는 별도 승인 없이는 실행하지 않는다.

## 8. 실패·복구 기준

- 이벤트가 Blend 전환 중 이중 호출되면 각 이동 Clip의 이벤트 위상과 이벤트 발신 Clip의 Blend Weight를 먼저 확인한다. `minimumInterval`은 같은 위상의 동시 호출을 합치는 범위에서만 조절하고, 정상 보행 주기를 만드는 용도로 늘리지 않는다.
- Clip 복사 후 발 동작·Root Motion·Avatar 바인딩이 달라지면 Animator 참조 교체를 되돌리고 원인을 확인한다.
- AudioSource는 호출되지만 소리가 나지 않으면 Clip 참조, Mute/Volume, Audio Listener, Spatial Blend와 Console을 순서대로 확인한다.
- 원본 외부 에셋 또는 Original 씬에 변경이 생기면 그 변경을 이번 작업에서 저장·적용하지 않고 Player 소유 경로로 되돌린다.

## 9. 완료 기준

- 걷기와 Sprint의 발 착지마다 Step0~4 중 하나가 재생된다.
- 달리기 속도는 걷기보다 자연스럽게 빠른 발소리 리듬을 가진다.
- 점프·낙하·무중력·정지에서는 발소리가 나지 않는다.
- Player.prefab을 사용하는 씬에서 동일하게 동작하며, 외부 Toon Soldiers 원본과 `Original_GamePlayScene`은 변경되지 않는다.
- Unity 컴파일 오류와 새 Console 오류가 없고, 사용자가 Play Mode에서 실제 소리를 확인한다.

## 10. 구현 결과

- `infantry_combat_run`, `run_back`, `run_left`, `run_right`, `machinegun_sprint`의 Curve와 Loop 설정을 보존한 Player 소유 `.anim` 5개를 `Assets/_Custom/Animations/Player/Footsteps`에 생성했다.
- 방향 이동 Clip에는 원본 발 Curve의 접지 최저점을 기준으로 `PlayFootstep` 이벤트를 2개씩 배치했다. Sprint 원본은 1.333초 동안 두 보행 주기를 포함하므로 4개를 배치했다.
- `Player.controller`에서는 Locomotion Blend Tree의 네 방향 이동과 Sprint 모션 참조만 추출본으로 교체했다. Idle, Crouch, Jump, Upper Body 모션과 파라미터·전이·Blend 좌표는 유지했다.
- `Player.prefab`의 기존 `PlayerFootsteps`, 다섯 AudioClip 참조와 루트 AudioSource를 유지하고 `Play On Awake`를 비활성화했다.
- Unity 스크립트 재컴파일과 편집용 Clip의 길이·Frame Rate·Loop·이벤트 개수, Controller 참조 diff를 확인했다. 사용자가 Play Mode에서 정지·경사 미끄러짐의 무음과 걷기·Sprint의 발소리 재생이 정상임을 확인했다.

## 11. Animation Event 필터 재설계

- 경사 미끄러짐과 물리 진동은 Rigidbody 속도를 만들 수 있으므로 실제 속도나 Animator `MoveSpeed`를 재생 조건으로 사용하지 않는다. `PlayerController.HasMoveIntent`는 이동 입력 허용, `PlayerInput.Move` dead zone `0.1`, 중력 전환 비활성만으로 이동 의도를 제공한다.
- Grounded는 착지 직후 물리 상태 갱신이 애니메이션보다 늦어 정상 접지 이벤트를 누락시켰으므로 재생 조건에서 제외한다.
- 가장 높은 Weight의 대표 Clip 하나만 허용하는 방식은 대각선·방향 전환·Locomotion↔Sprint 전환에서 정상 이벤트를 누락시켰으므로 제거한다. `PlayerFootsteps.PlayFootstep(AnimationEvent)`은 이동 의도와 이벤트 Clip의 Blend Weight `0.01` 초과만 요구한다.
- 0.833초 네 방향 Locomotion Clip의 이벤트를 `0.1 / 0.5166667`로 통일해 반 주기 간격으로 정렬한다. 1.333초 Sprint Clip은 `0.0333 / 0.3666667 / 0.7 / 1.0333`으로 4등분 주기를 사용한다.
- 동일 위상에서 여러 Blend Clip 이벤트가 들어오면 첫 호출만 `minimumInterval`을 통과한다. 이 값은 정상 보행 주기를 만들지 않고 동시 이벤트만 합치는 최종 중복 방지에 사용한다.
- Unity 재컴파일은 오류 없이 통과했다. 사용자가 Play Mode에서 기존 오재생과 이동 중 무음이 사라지고 발소리가 정상적으로 재생됨을 확인해 완료로 판단했다.

# TPS 플레이어 시각 회전 분리·떨림 개선 실행 계획

문서 작성일: 2026-08-23
현재 상태: 완료 (2026-08-23)
계획 프로필: `standard`

## 목표

현재 `MvpPlayerController.FixedUpdate`에서 Rigidbody 전체가 카메라 정면을 추격하면서 보이는 플레이어 모델이 물리 주기 단위로 떨리는 현상을 제거한다.

슈팅 TPS의 현재 요구에 맞춰 플레이어 모델은 카메라의 중력 평면상 정면을 같은 렌더 프레임에 즉시 바라보게 하되, Rigidbody·CapsuleCollider 물리 루트의 회전과 화면에 보이는 모델 회전을 분리한다. 이후 Animator, 상·하체 분리, Aim Blend Tree, Animation Rigging과 총구 보정을 추가할 때 기존 모델 계층과 뼈를 다시 뜯지 않아도 되는 구조를 만든다.

이번 완료는 카메라 방향에 따른 플레이어의 수평 시각 회전 안정화만 의미한다. 실제 조준 판정, 상하체 애니메이션과 총구 방향 제어는 후속 작업으로 남긴다.

## 범위

- 현재 플레이어 회전 떨림의 Play Mode 기준선과 재현 조건 기록
- `MvpPlayer` 물리 루트와 캐릭터 모델 사이에 회전 전용 `VisualRoot` 추가
- 기존 `TS-Armies_Recon_B` nested Prefab을 `VisualRoot` 아래로 이동하면서 연결·로컬 배치 보존
- `MvpPlayerController`에서 물리 루트의 카메라 yaw 추격 제거
- 물리 루트에는 현재 중력 방향에 대한 up축 정렬만 유지
- `VisualRoot`가 카메라 정면을 중력 평면에 투영한 방향으로 같은 렌더 프레임에 즉시 회전
- 기존 카메라 기준 WASD 이동, 중력, 지면 판정, 충돌과 씬 참조 보존
- Unity 재컴파일, Prefab·씬 참조, Play Mode와 프레임별 할당 검증

## 하지 않을 것

- `MvpThirdPersonCamera`, CameraRig 또는 별도 카메라 계획 내용 수정
- 카메라 충돌 방지, 거리 조정, DOTween 보간 구현
- Animator Controller, Avatar Mask, Blend Tree 또는 애니메이션 상태 추가
- 상·하체 분리, aim up/down, 사격·재장전 애니메이션 연결
- Animation Rigging 패키지 설치, Multi-Aim Constraint 또는 Two Bone IK 추가
- 카메라 중앙 Raycast, 총구 Raycast, 탄환·그래플 판정 구현
- 총기·손·척추·목·머리 뼈 직접 회전
- Root Motion 정책이나 원본 Toon Soldiers 애셋 수정
- 카메라 방향에 따라 Rigidbody yaw를 다시 맞추는 별도 보간 추가
- `Assets/_Scenes/Original_GamePlayScene.unity` 또는 팀장 지형 Collider 수정
- 현재 작업 트리의 씬·DOTween·Resources·ProjectSettings·솔루션 관련 기존 변경 정리 또는 되돌리기

## 현재 상태와 필요한 가정

- `MvpThirdPersonCamera`는 `LateUpdate`에서 CameraRig yaw와 Pivot pitch를 갱신한다.
- `MvpPlayerController`는 `FixedUpdate`에서 Main Camera forward를 중력 평면에 투영해 이동 기준과 Rigidbody 목표 회전에 함께 사용한다.
- 현재 물리 고정 주기는 `0.02초`, 50Hz이며 `rotationSpeed`는 `720도/초`다. 플레이어가 카메라의 계속 움직이는 목표를 물리 프레임마다 최대 `14.4도`씩 추격하는 구조다.
- Player Rigidbody의 Interpolate는 활성화되어 있지만 렌더 프레임과 물리 프레임 사이의 회전 목표 갱신 차이까지 제거하지는 않는다.
- `MvpPlayer` 루트는 Rigidbody, CapsuleCollider, 입력과 컨트롤러를 소유하고, `TS-Armies_Recon_B` 모델은 별도 nested Prefab 자식이다.
- 현재 모델 로컬 위치는 `(0, -0.235, 0)`, 로컬 회전은 identity, 로컬 스케일은 `(0.3, 0.3, 0.3)`이며 이 값을 보존한다.
- 모델 Animator에는 현재 Controller가 연결되어 있지 않다. Aim·사격 애니메이션 파일과 Humanoid 뼈는 존재하지만 이번 계획에서는 연결하지 않는다.
- CapsuleCollider는 로컬 up축을 중심으로 대칭이므로 카메라 yaw를 물리 루트에 계속 적용하지 않아도 현재 충돌 형상은 유지된다.
- 방향성 중력을 위해 물리 루트의 up축 정렬 책임은 제거하지 않는다.
- 현재 Player Rigidbody는 X/Z 회전이 고정되어 있으므로 이번 완료 기준은 일반 중력의 시각 회전 안정화로 한정한다. 방향성 중력 정렬과 Rigidbody 제약 재설계는 후속 작업에서 함께 검증한다.
- 현재 프로젝트 스크립트에는 Player 루트 `transform.forward`를 사격·그래플 조준 정본으로 사용하는 소비자가 없다.
- 구현 후 `MvpPlayer.transform.forward`는 조준 방향을 의미하지 않는다. 후속 조준 시스템은 카메라 중앙 방향을 정본으로 사용해야 한다.
- 카메라와 플레이어 시각 회전 사이의 한 프레임 수준 지연은 안정적인 갱신 순서를 위해 허용하며, 실제 체감은 Play Mode에서 확인한다.

## 마스터 플랜 정합성

| 마스터 플랜 기준 | 이번 계획의 대응 | 판단 |
| --- | --- | --- |
| 3인칭 플레이어 이동과 카메라는 담당 범위 | 기존 이동 기준은 유지하고 플레이어 시각 회전만 안정화 | 일치 |
| Rigidbody 기반 사용자 정의 중력 | Rigidbody 루트가 중력 up축 정렬과 충돌을 계속 소유 | 일치 |
| 카메라 중심 Raycast를 후속 그래플 기준으로 사용 | Player 루트 forward를 조준 정본으로 만들지 않음 | 일치 |
| 완성형 애니메이션과 고급 연출 제외 | Animator·Aim·IK를 추가하지 않고 확장 경계만 보존 | 일치 |
| 핵심 루프와 플레이 가능 상태 우선 | 현재 회전 떨림 제거와 이동·충돌 회귀 방지에 한정 | 일치 |
| 구체 API와 수치는 구현에서 검증 후 확정 | 카메라 이후의 시각 회전 실행 순서를 코드로 고정 | 일치 |

이번 작업은 마스터 플랜의 범위나 핵심 기술 방향을 바꾸지 않는다. 새로운 팀 합의가 발생하지 않는 한 `Player_Gravity_Master_Plan.md`는 수정하지 않는다.

## 책임 경계

```text
MvpPlayer
├─ Rigidbody / CapsuleCollider
│  ├─ 이동과 충돌
│  └─ 현재 중력에 대한 up축 정렬
│
├─ VisualRoot
│  └─ 카메라의 중력 평면상 정면으로 부드러운 시각 회전
│     └─ TS-Armies_Recon_B nested Prefab
│        └─ 후속 Animator / Upper Body Aim / IK 영역
│
└─ Point Light
```

- 게임플레이 이동 방향: Main Camera forward/right를 중력 평면에 투영한 기존 값
- 물리 회전: Player Rigidbody의 현재 up을 중력 up으로 정렬
- 시각적 수평 방향: VisualRoot가 카메라의 중력 평면상 forward를 추격
- 후속 조준 판정: 카메라 중심 Raycast
- 후속 애니메이션과 IK: `TS-Armies_Recon_B` 내부 Animator와 뼈

`VisualRoot`는 모델 Prefab과 물리 루트 사이의 프레젠테이션 경계다. 이번 구현은 모델 내부 Transform이나 뼈를 직접 수정하지 않는다.

## 구현 방향

### 1. Prefab 계층 분리

- `MvpPlayer` 아래에 identity Transform인 `VisualRoot`를 새로 만든다.
- 기존 `TS-Armies_Recon_B` nested Prefab 인스턴스를 `VisualRoot` 아래로 이동한다.
- 모델의 기존 로컬 위치 `(0, -0.235, 0)`, 로컬 회전 identity와 로컬 스케일 `0.3`을 보존한다.
- 모델 nested Prefab은 Connected 상태를 유지하고 unpack하지 않는다.
- Point Light는 이번 시각 회전 대상에 포함하지 않고 기존 Player 루트 자식으로 유지한다.
- `VisualRoot`에는 Animator, Collider, Rigidbody 또는 조준용 추가 컴포넌트를 붙이지 않는다.

### 2. 물리 루트의 회전 책임 축소

- `FixedUpdate`의 이동 방향 계산과 `body.linearVelocity`, 지면 판정, 중력 적용은 변경하지 않는다.
- 현재 `Quaternion.LookRotation(cameraForward, up)`으로 카메라 yaw까지 포함해 Rigidbody를 회전하는 코드를 제거한다.
- 물리 루트의 목표 회전은 현재 Rigidbody up을 중력 up에 맞추는 회전으로 한정한다.
- 후보 계산은 `Quaternion.FromToRotation(body.rotation * Vector3.up, up) * body.rotation`처럼 현재 회전에 필요한 up 정렬만 합성하는 방식을 사용한다.
- `Quaternion.RotateTowards`와 `body.MoveRotation`은 후속 방향성 중력 작업에서 재사용할 물리 정렬 경로로 유지하되, 이번 작업에서는 일반 중력에서 카메라 yaw를 추격하지 않는지만 검증한다.
- 기존 `rotationSpeed`는 실제 역할에 맞게 `gravityAlignmentSpeed`로 명확히 변경한다.
- 직렬화 값 손실을 막기 위해 이름 변경 시 `[FormerlySerializedAs("rotationSpeed")]`를 사용하고 Prefab 값 `720`이 유지되는지 재조회한다.
- 일반 중력에서 이미 up이 일치하면 물리 루트는 카메라를 움직여도 yaw 회전하지 않아야 한다.

### 3. VisualRoot의 카메라 방향 즉시 동기화

- `MvpPlayerController`에 Prefab 내부 `visualRoot` 참조를 추가한다.
- 기존 `cameraTransform`과 `gravityState` 참조를 재사용해 scene reference를 중복 추가하지 않는다.
- 카메라가 갱신된 뒤 실행되는 `LateUpdate`에서 아래 순서로 목표 방향을 계산한다.
  1. `up = -gravityState.Direction`
  2. Main Camera forward를 up에 수직인 평면으로 투영
  3. 정규화 가능한 경우 `Quaternion.LookRotation(facingForward, up)` 생성
  4. VisualRoot world rotation에 목표 world rotation을 즉시 적용
- 카메라 forward가 up과 거의 평행해 투영 결과가 0에 가까우면 현재 VisualRoot forward를 같은 평면에 투영해 fallback한다.
- fallback도 유효하지 않으면 그 프레임의 시각 회전을 건너뛰며 잘못된 Quaternion을 만들지 않는다.
- 지수 보간과 sharpness 튜닝은 제거하여 카메라 조작과 모델 방향 사이의 의도적인 지연을 만들지 않는다.
- 매 프레임 Tween, 코루틴, 새 배열 또는 새 객체를 만들지 않는다.
- `MvpPlayerController`에 `[DefaultExecutionOrder(100)]`을 지정하여 기본 순서인 `MvpThirdPersonCamera.LateUpdate` 뒤에 시각 회전이 실행되게 한다.
- Project Settings의 Script Execution Order는 변경하지 않는다.

### 4. 초기화와 참조 검증

- `Start`의 필수 참조 검증에 `visualRoot`를 포함한다.
- 필수 참조가 빠졌으면 기존 방식처럼 명확한 오류를 기록하고 컨트롤러를 비활성화한다.
- 시작과 이후 모든 유효 프레임에서 VisualRoot를 현재 카메라 평면 방향에 즉시 정렬한다.
- 리스폰 시 Rigidbody 회전이 바뀌더라도 VisualRoot는 다음 유효 렌더 프레임부터 카메라 방향으로 복귀한다.
- 이번 범위에서는 `RespawnController`에 새 public API나 콜백을 추가하지 않는다. 리스폰 후 장시간 회전이 보일 때만 별도 연동을 후속 판단한다.

## 후속 애니메이션과 조준을 위한 보존 계약

- `VisualRoot`는 전체 모델의 수평 방향과 중력 up 정렬을 위한 외부 래퍼로 유지한다.
- Animator Controller, Avatar Mask, Aim Blend Tree와 Animation Rigging은 `TS-Armies_Recon_B` 내부에서 동작해야 한다.
- 후속 상체 pitch는 VisualRoot를 기울이지 않고 Spine 이상의 애니메이션 또는 Rig가 담당한다.
- 실제 사격·그래플 판정은 Player 루트 또는 VisualRoot forward가 아니라 카메라 중심 방향을 사용한다.
- 총구와 손 위치는 후속 애니메이션·IK 단계에서 조준점에 맞추며 이번 코드가 해당 뼈를 선점하지 않는다.
- 후속 Animator 연결 시 Rigidbody 이동과 충돌을 유지하려면 Apply Root Motion 정책을 별도로 검토한다. 이번 계획에서는 현재 Animator 값을 변경하지 않는다.
- 모델 교체 시에도 새 모델을 VisualRoot 아래에 배치하고 `visualRoot` 참조는 그대로 유지할 수 있어야 한다.

## 초기 튜닝 후보

| 항목 | 초기값 | 목적 |
| --- | ---: | --- |
| 중력 정렬 속도 | `720도/초` | 기존 직렬화 값 보존과 방향성 중력 정렬 경로 유지 |
| 물리 고정 주기 | `0.02초` | 전역 설정을 바꾸지 않고 현재 기준 유지 |
| 플레이어 실행 순서 | `100` | 기본 순서 카메라의 LateUpdate 이후 즉시 시각 회전 |

중력 정렬 속도는 Inspector에서 조정 가능한 직렬화 값으로 유지한다. 시각 회전에는 튜닝 수치를 두지 않고 느린 마우스 이동, 빠른 180도 회전과 이동 조합에서 즉시 반응과 떨림 제거를 함께 확인한다.

## 실행 계획

1. `[Git·Unity·회전 떨림 기준선 기록]` → verify: `[기존 미커밋 변경 목록 분리, 대상 스크립트·Prefab hash 기록, 정지 상태의 느린·빠른 마우스 yaw에서 모델 떨림 재현, 기존 Console 오류 기록]`
2. `[MvpPlayer Prefab에 VisualRoot 경계 추가]` → verify: `[VisualRoot identity, 모델 로컬 위치·회전·스케일 불변, Toon Soldiers nested Prefab Connected, Point Light 기존 부모 유지]`
3. `[MvpPlayerController의 물리 회전을 중력 up 정렬로 축소]` → verify: `[일반 중력에서 마우스 yaw만으로 Rigidbody yaw가 변하지 않고, 카메라 기준 이동·중력·지면 판정 코드는 그대로 유지]`
4. `[VisualRoot의 LateUpdate 즉시 동기화 구현]` → verify: `[카메라 forward의 중력 평면 투영, 무효 벡터 fallback, 실행 순서 100, 현재 프레임 목표 방향 즉시 적용]`
5. `[직렬화 이름과 Prefab 참조 보존]` → verify: `[FormerlySerializedAs로 기존 720 값 유지, visualRoot 내부 참조 유효, 기존 cameraTransform·gravityState scene override 유지]`
6. `[Unity 재컴파일과 저장·재로드]` → verify: `[컴파일 오류·Missing Script·Missing Reference 0건, 재로드 후 nested Prefab과 모든 참조 유지]`
7. `[Play Mode 정지 회전 검증]` → verify: `[느린 yaw, 빠른 좌우 왕복과 180도 회전에서 모델 떨림과 조작 지연 없음, 카메라 입력은 기존과 동일, VisualRoot가 같은 프레임에 즉시 동기화]`
8. `[Play Mode 이동·물리 회귀 검증]` → verify: `[WASD 전후좌우·대각선 이동 방향, 정지·이동 중 충돌, 지면 유지와 중력 적용이 기존과 동일]`
9. `[일반 중력·리스폰 경계 검증]` → verify: `[일반 중력에서 Rigidbody yaw 고정, 리스폰 후 모델의 과도한 회전·위치 이탈 없음, 방향성 중력 정렬은 후속 작업으로 기록]`
10. `[성능·프레임 변화 검증]` → verify: `[안정 상태 LateUpdate/FixedUpdate의 반복 GC Alloc 0 B, 낮은·높은 렌더 프레임에서 추가 회전 지연 없음]`
11. `[최종 diff와 범위 검증]` → verify: `[변경은 MvpPlayerController와 MvpPlayer Prefab 중심, 카메라 파일·Original 씬·Animator·애니메이션·패키지·ProjectSettings의 의도하지 않은 diff 없음]`
12. `[결과 기록과 계획 상태 갱신]` → verify: `[즉시 동기화 방식·검증 결과·사람이 판단한 체감을 Codex 활용 기록에 남기고 완료 시 계획서를 03_completed로 이동]`

## 검증 기준

- 정지 상태에서 마우스를 천천히 또는 빠르게 움직여도 보이는 플레이어 모델이 50Hz 간격으로 떨리거나 따라잡는 현상이 없다.
- 플레이어 모델은 카메라의 중력 평면상 정면을 같은 렌더 프레임에 즉시 바라본다.
- 플레이어 Rigidbody yaw는 일반 중력에서 카메라 yaw 입력만으로 계속 회전하지 않는다.
- Player Rigidbody와 CapsuleCollider는 기존 이동·충돌·중력 책임을 유지한다.
- 물리 루트의 up축 정렬 코드 경로는 유지하지만, 현재 X/Z 회전 제약 아래의 방향성 중력 정렬 성공을 이번 완료 결과로 주장하지 않는다.
- 카메라 기준 WASD 전후좌우·대각선 이동 방향과 이동 속도가 기존과 같다.
- VisualRoot 아래 모델의 위치, 회전 기준, 스케일과 nested Prefab 연결이 저장·재로드 후 유지된다.
- Point Light와 모델 외 Player 자식은 VisualRoot 회전에 불필요하게 포함되지 않는다.
- 기존 CameraRig와 `MvpThirdPersonCamera`의 코드·Prefab·scene override를 변경하지 않는다.
- Animator Controller, Avatar Mask, Aim Animation, Animation Rigging 또는 총구 보정이 새로 추가되지 않는다.
- 모델 내부 뼈를 코드에서 직접 회전하지 않으므로 후속 상·하체 애니메이션과 IK가 같은 뼈를 소유할 수 있다.
- 후속 조준 시스템이 사용할 카메라 중심 방향과 이번 VisualRoot 표현 방향의 책임이 문서상 구분된다.
- 시작과 리스폰 후 모델이 위치에서 벗어나거나 장시간 제자리 회전하지 않는다.
- 안정 상태에서 새 managed allocation, Tween 또는 코루틴이 프레임마다 생성되지 않는다.
- 컴파일 오류, Missing Script, Missing Reference와 신규 Console 오류가 없다.
- `Original_GamePlayScene`, 카메라 계획 파일, Toon Soldiers 원본 애셋, 패키지와 ProjectSettings를 수정하지 않는다.

## 중단 및 복구 조건

- VisualRoot 삽입 과정에서 Toon Soldiers nested Prefab이 Unpacked되거나 모델의 로컬 배치·스케일이 달라지면 저장하지 않고 Prefab 계층 변경만 복구한다.
- 기존 `cameraTransform`, `gravityState` scene override 또는 Player Prefab 연결이 끊기면 Play Mode로 진행하지 않는다.
- Rigidbody yaw 제거로 현재 코드 밖의 시스템이 Player 루트 forward를 조준·상호작용 정본으로 사용한다는 새 근거가 발견되면 해당 소비자를 먼저 보고하고 임의로 의미를 바꾸지 않는다.
- 후속 방향성 중력 작업에서는 현재 X/Z 회전 제약을 먼저 재검토하고, 뒤집힘이나 불안정한 회전축이 확인되면 카메라 yaw를 되살리지 않고 중력 정렬 알고리즘과 제약만 별도 보완한다.
- 실행 순서 `100`의 `LateUpdate` 즉시 회전이 새 떨림을 만들면 Project Settings를 바꾸지 않고 Player와 Camera의 실제 프레임 순서를 먼저 측정해 가장 좁은 코드 변경으로 조정한다.
- 후속 Animator 또는 IK 없이는 만족할 수 없는 상하 조준·손 위치 문제가 발견돼도 이번 회전 안정화 계획에 섞지 않고 후속 작업으로 기록한다.
- 회귀가 발생하면 신규 VisualRoot와 플레이어 회전 코드만 되돌릴 수 있어야 하며 기존 씬·카메라·DOTween·사용자 변경을 복구 대상으로 삼지 않는다.

## 완료 후 기록

- 구현과 검증 결과는 `Docs/ksh/Codex_Usage_Records.md`에 한 항목으로 남긴다.
- 최종 즉시 동기화 방식, 실행 순서 `100`과 물리 중력 정렬 속도를 기록한다.
- 사람이 직접 확인한 느린 yaw, 빠른 왕복, 180도 회전과 이동 중 체감 결과를 구분해 기록한다.
- 마스터 플랜의 범위나 핵심 기술 방향이 바뀌지 않으면 `Player_Gravity_Master_Plan.md`는 수정하지 않는다.
- 이번 완료를 상·하체 애니메이션, Aim Offset, 총구 정렬 또는 사격 시스템 완료로 기록하지 않는다.
- 사용자가 Play Mode에서 회전 떨림 제거와 카메라 방향 추격 감각을 직접 확인했고, 계획서를 `Docs/ksh/Tasks/03_completed`로 이동했다.

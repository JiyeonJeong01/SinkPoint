# TPS 기본 이동·카메라 구현 실행 계획

문서 작성일: 2026-08-23  
현재 상태: 구현 및 검증 완료  
계획 프로필: `deep`

## 목표

`GamePlayScene_Player`에서 WASD 기반 Rigidbody 이동, 마우스 TPS 카메라 회전, 지면·경사·벽 충돌을 실제 Play Mode로 검증한다.

카메라는 별도 스크립트와 별도 Prefab으로 분리한다. 사용자 승인 전에는 이 문서만 작성하고, 승인 후 계획서를 `02_in-progress`로 이동한 다음 MCP 기반 구현을 시작한다.

## 범위

- `MvpPlayer.prefab`과 `MvpThirdPersonCameraRig.prefab` 생성
- Rigidbody 기반 카메라 상대 이동과 카메라 방향 캐릭터 회전
- 마우스 yaw·pitch 카메라 회전과 cursor lock
- CapsuleCollider 기반 지형 충돌
- 지면 법선과 경사 제한을 사용하는 지면 판정
- `/GamePlay/GravitySystem`의 최소 공통 중력 상태와 Player 연결
- `GamePlayScene_Player`의 두 Prefab 인스턴스 배치와 참조 연결
- MCP를 이용한 Unity 컴포넌트·Hierarchy·Prefab·씬 구성 및 Play Mode 검증

## 하지 않을 것

- 점프, 달리기, 대쉬와 웅크리기
- 중력 방향 전환, 구역 전환과 주변 Rigidbody의 공통 중력 반응
- 무중력과 그래플
- 애니메이션 파라미터와 Blend Tree 연동
- 카메라 벽 충돌, 줌, 어깨 전환과 흔들림
- Cinemachine 또는 다른 dependency 추가
- `Original_GamePlayScene`, 지형 Collider, Build Settings와 입력 액션 자산 변경
- 기존 미커밋 변경의 커밋 또는 되돌리기

## 현재 상태와 가정

- 활성 씬은 `Assets/_Scenes/GamePlayScene_Player.unity`다.
- `/GamePlay/Player`에는 현재 Transform만 있고 Rigidbody, Collider와 플레이어 스크립트가 없다.
- Player 자식의 `Main Camera`는 Camera, AudioListener와 URP 카메라 데이터를 가지고 있다.
- `/GamePlay/GravitySystem`에는 현재 Transform만 있고 중력 컴포넌트가 없다.
- 캐릭터 모델은 `TS-Armies_Recon_B` Prefab 인스턴스이며 현재 Player 자식으로 배치돼 있다.
- `Assets/_Custom/Prefabs/Player`에는 아직 전용 플레이어 Prefab이 없다.
- 입력 정본은 `Assets/_Scripts/Input/MvpPlayerInput.cs`이며 기존 public 계약을 유지한다.
- 이동 방식은 카메라 수평 방향을 바라보는 무장 TPS 방식으로 확정한다.
- 카메라 Rig는 플레이어와 별도 Prefab으로 만들고 씬에서 형제 인스턴스로 둔다.
- 이번 변경은 마스터 플랜의 단계 1을 분할한 첫 변경이며 점프는 다음 변경으로 미룬다.
- 달리기, 대쉬와 웅크리기도 후속 변경에서 다루며 이번 완료 여부에 포함하지 않는다.
- 현재 미커밋 MCP 검증 변경은 그대로 보존하고, 구현 시작 시 Git 상태와 대상 파일 해시를 기준선으로 기록한다.

## 구현 방향

### 플레이어

- `Assets/_Custom/Prefabs/Player/MvpPlayer.prefab`을 생성한다.
- 루트에 `Rigidbody`, `CapsuleCollider`, `MvpPlayerInput`, 신규 `MvpPlayerController`를 둔다.
- 초기 CapsuleCollider는 높이 `0.9`, 반지름 `0.22`, 중심 Y `0.05`로 설정한다.
- Rigidbody는 질량 `1`, 보간 `Interpolate`, 충돌 감지 `ContinuousDynamic`, X/Z 회전 고정으로 설정한다.
- Unity 기본 중력은 끄고 컨트롤러가 `MvpGravityState`의 현재 방향과 세기를 적용한다.
- 이동 속도는 `3m/s`, 회전 속도는 `720°/s`, 최대 지면 각도는 `50°`, 지면 탐색 여유는 `0.15m`로 설정한다.
- 카메라의 수평 전방·우측으로 이동 벡터를 만들고 대각선 입력을 정규화한다.
- 지면에서는 이동 벡터를 지면 법선 평면에 투영하고, 정지 시 경사면 미끄러짐을 억제한다.
- 플레이어 루트는 카메라 수평 방향을 바라보며 카메라 Rig 회전에는 영향을 주지 않는다.

### 중력 상태

- `Assets/_Scripts/Gravity/MvpGravityState.cs`를 생성한다.
- `MvpGravityState`는 현재 중력 방향과 세기의 단일 입구이며 `/GamePlay/GravitySystem`에 배치한다.
- 이번 변경에서는 아래 방향과 `9.81m/s²`만 제공한다. 방향 전환, 구역 감지와 주변 Rigidbody 반응은 후속 단계에서 확장한다.
- `MvpPlayerController`는 중력 값을 직접 소유하거나 Unity 기본 중력을 사용하지 않고 `MvpGravityState`를 직렬화 참조로 받는다.

### 카메라

- `Assets/_Custom/Prefabs/Player/MvpThirdPersonCameraRig.prefab`을 생성한다.
- Hierarchy는 `MvpThirdPersonCameraRig/CameraPivot/Main Camera`로 구성한다.
- 신규 `MvpThirdPersonCamera`는 직렬화 참조로 받은 `MvpPlayerInput.Look`만 읽고 다른 입력 API를 직접 호출하지 않는다.
- 현재 구도를 기준으로 Pivot 높이 `1.03`, 카메라 거리 `1.605`, 초기 pitch 약 `14.29°`를 사용한다.
- 마우스 감도 `2`, pitch 범위 `-40°~70°`, 자유 yaw를 사용하며 roll은 허용하지 않는다.
- Rig는 `LateUpdate`에서 플레이어 위치를 따라가고 회전은 플레이어 루트와 독립적으로 유지한다.
- Play Mode 진입 시 커서를 잠그고 숨기며 컴포넌트 비활성화 시 복원한다.

### 컴포넌트 계약과 씬 연결

- 신규 코드는 `Assets/_Scripts/Player/MvpPlayerController.cs`, `MvpThirdPersonCamera.cs`와 `Assets/_Scripts/Gravity/MvpGravityState.cs`로 제한한다.
- `MvpPlayerController`는 `MvpPlayerInput`, 이동 기준 Camera Transform과 `MvpGravityState`를 직렬화 참조로 받는다.
- `MvpThirdPersonCamera`는 대상 Player Transform과 Player 루트의 `MvpPlayerInput`을 각각 직렬화 참조로 받는다.
- 두 Prefab에는 공용 컴포넌트와 기본값만 저장한다.
- Player·CameraRig·GravitySystem 사이의 참조와 스폰 위치는 `GamePlayScene_Player`의 Prefab instance override로 둔다.
- Player와 CameraRig는 `/GamePlay` 아래 형제 인스턴스로 배치한다.

## 실행 계획

1. `[현재 씬·Git 기준선과 Console 상태 기록]` → verify: `[GamePlayScene_Player active·dirty false, Original diff 없음, 현재 미커밋 파일 해시 기록]`
2. `[Player·Gravity 스크립트 폴더와 세 C# 컴포넌트 작성]` → verify: `[MCP recompile 완료, Console 컴파일 오류 0건]`
3. `[MCP로 GravitySystem 중력 상태, CameraRig/Pivot, Main Camera 재배치와 Player 물리·스크립트 컴포넌트 구성]` → verify: `[Hierarchy와 serialized property가 계획값과 일치]`
4. `[MCP create_prefab으로 Player와 CameraRig를 각각 Prefab화하고 씬 참조 연결]` → verify: `[씬 오브젝트가 두 Prefab의 connected instance이며 참조 누락 없음]`
5. `[GamePlayScene_Player만 저장하고 Play Mode 실행]` → verify: `[이동·회전·지면·벽 충돌 수동 검증 및 Console 오류 확인]`
6. `[Play Mode 종료 후 diff와 자산 상태 정리]` → verify: `[Original·ProjectSettings 변경 없음, Prefab·스크립트·Player 씬·문서만 변경]`
7. `[결과를 활용 기록에 남기고 계획서를 completed로 이동]` → verify: `[검증 성공·사람이 확인한 항목·남은 제한이 문서화됨]`

## 검증 기준

- W/S가 카메라 전후, A/D가 카메라 좌우로 움직이고 대각선이 더 빠르지 않다.
- 마우스 좌우 회전은 제한 없이 가능하고 상하 회전은 지정 범위에서 멈춘다.
- 플레이어가 카메라의 수평 방향을 바라보며 카메라가 플레이어 회전에 중복 회전하지 않는다.
- Entry 주변 BoxCollider 바닥과 경사를 오르내릴 수 있고 정지 시 과도하게 미끄러지지 않는다.
- 벽을 통과하거나 바닥 아래로 빠지지 않으며 충돌 시 비정상적으로 튀지 않는다.
- MCP가 Play Mode와 Console을 감시하고 사용자가 실제 WASD·마우스 조작을 직접 확인한다.
- 컴파일 오류와 신규 Console 오류가 없고 두 Prefab 및 씬 참조가 재실행 후 유지된다.
- Player와 CameraRig가 같은 `MvpPlayerInput`을 참조하고, Player가 `/GamePlay/GravitySystem`의 `MvpGravityState`를 참조한다.
- Unity 기본 중력이 꺼진 Player가 `MvpGravityState`의 아래 방향 `9.81m/s²`에 따라 낙하한다.
- `Original_GamePlayScene`, 지형 Collider, ProjectSettings와 Build Settings에 의도하지 않은 diff가 없다.

## 완료 후 기록

- 구현 결과와 검증 내용을 `Docs/ksh/Codex_Usage_Records.md`에 한 항목으로 남긴다.
- 마스터 플랜의 범위나 핵심 방향은 바뀌지 않으므로 이번 작업에서 `Player_Gravity_Master_Plan.md`는 수정하지 않는다.
- 이번 계획 완료는 기본 이동·카메라와 기본 중력 연결 완료만 의미하며, 점프를 포함한 마스터 플랜 단계 1 전체 완료로 기록하지 않는다.
- Play Mode에서 실제 입력 검증이 완료되면 계획서를 `Docs/ksh/Tasks/03_completed`로 이동한다.

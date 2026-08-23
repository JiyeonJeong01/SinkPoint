# GamePlayScene_Player 최신 Original 동기화 실행 계획

문서 작성일: 2026-08-23  
현재 상태: 완료  
계획 프로필: `standard`

## 목표

팀장 기준본인 최신 `Original_GamePlayScene`의 지형·Collider·Zone·게임 흐름 변경을 `GamePlayScene_Player`에 반영하고, 우리 파트의 Player·TPS CameraRig·기본 중력 연결을 복구한다.

실제 씬 변경은 `Assets/_Scenes/GamePlayScene_Player.unity`로 제한한다. `Original_GamePlayScene`은 읽기 전용 정본으로 사용하며 직접 저장하거나 수정하지 않는다.

## 범위

- 최신 `Original_GamePlayScene`의 Hierarchy와 직렬화 상태를 `GamePlayScene_Player`의 새 기준으로 반영
- 최신 Original의 Player 배치 위치를 유지하면서 `MvpPlayer.prefab` 인스턴스 연결
- `MvpThirdPersonCameraRig.prefab` 인스턴스 연결과 기존 Main Camera 교체
- 기존 활성 `GravitySystem`에 `MvpGravityState` 연결
- Player·CameraRig·GravitySystem 사이의 씬 직렬화 참조 재구성
- 최신 `GameSystem`, `Triggers`, `Environment`, Zone, Collider와 마지막 Zone 배치 보존
- 씬 저장·재로드, Play Mode와 Git path-scoped diff를 통한 검증

## 하지 않을 것

- `Assets/_Scenes/Original_GamePlayScene.unity` 또는 해당 `.meta` 수정
- `GamePlayScene_Player.unity.meta`의 GUID 변경
- Player·CameraRig Prefab, C# 스크립트, 입력 자산, ProjectSettings와 Build Settings 수정
- 기존 `GamePlayScene_Player`의 지형 변경을 최신 Original 위로 다시 이식
- 충돌 후보를 자동 선택하는 Unity YAML 전체 병합
- 비활성 EditorOnly `//GravitySystem` 오브젝트 재이식
- DOTween을 이용한 카메라 연출·흔들림·전환 추가
- 점프, 달리기, 대쉬, 웅크리기, 방향성 중력, 무중력과 그래플 구현
- 현재 작업 트리의 DOTween·Resources·솔루션 관련 기존 변경 정리 또는 되돌리기

## 현재 상태와 필요한 가정

- Git 이력과 씬 직렬화 비교상 공통 기준은 `384c86d`이며, 우리 기본 플레이어 작업은 `1cbdfa3`, 최신 Original 지형 배치는 `46f70ad`에 반영됐다.
- `731e9eb`은 `Original_GamePlayScene`의 Point Light 세기를 `100`에서 `5`로 변경했으며, 최신 Original 값 `5`를 그대로 보존한다.
- 공통 기준 대비 현재 Player 씬에는 Player·CameraRig Prefab 인스턴스와 `MvpGravityState` 연결이 있고, Original에는 마지막 Zone과 Collider를 포함한 대규모 지형 변경이 있다.
- 같은 Unity 오브젝트 ID를 양쪽에서 다르게 변경한 후보가 있어 자동 YAML 병합 결과를 정본으로 사용할 수 없다.
- 최신 Original의 기존 Player Transform은 우리 씬 분기 이후 이동됐으므로, 스폰 위치는 기존 Player 씬 값이 아니라 최신 Original 값을 따른다.
- `MvpPlayer.prefab`, `MvpThirdPersonCameraRig.prefab`, `MvpPlayerController`, `MvpThirdPersonCamera`, `MvpGravityState`는 현재 상태 그대로 재사용할 수 있다고 가정한다.
- 실행 전에 열린 Player 씬에 미저장 변경이 없음을 사용자가 확인하고 Unity Editor를 종료한다.
- 실행 중 문제가 생기면 현재 커밋의 `GamePlayScene_Player`를 복구 기준으로 사용한다. 별도 씬 백업 파일은 생성하지 않는다.

## 핵심 변경 방향

### 1. 최신 Original을 목적지의 새 기준으로 사용

- Unity Editor에서 최신 `Original_GamePlayScene`을 읽기 전용 기준으로 확인한다.
- 최신 Original 직렬화 내용을 `GamePlayScene_Player`의 기반으로 사용하고, 기존 Player 하위 전체를 제거한 뒤 검증된 Player·CameraRig Prefab 인스턴스와 Gravity 참조 블록만 제한적으로 이식한다.
- 작업 과정에서 Original을 저장하지 않고, `GamePlayScene_Player.unity.meta`는 유지한다.
- 기존 Player 씬의 전체 YAML을 최신 Original과 줄 단위로 합치지 않는다.

### 2. 최신 Player 위치에 우리 Prefab 재연결

- 최신 Original의 Player Transform 위치·회전·부모를 먼저 기록한다.
- 기존 Player 오브젝트를 `MvpPlayer.prefab`의 connected instance로 교체한다.
- Prefab 인스턴스에는 최신 Original에서 기록한 위치·회전을 scene override로 적용한다.
- 기존 Player 모델, Rigidbody, Collider와 입력 컴포넌트가 중복으로 남지 않았는지 확인한다.

### 3. CameraRig와 중력 참조 재구성

- 기존 Main Camera와 AudioListener를 제거한 뒤 `MvpThirdPersonCameraRig.prefab`을 `/GamePlay` 아래 Player의 형제로 배치한다.
- 기존 활성 `/GamePlay/GravitySystem`에 `MvpGravityState`를 연결하고 아래 방향 `9.81m/s²` 기본값을 확인한다.
- `MvpPlayerController.cameraTransform`은 새 Main Camera Transform을 참조한다.
- `MvpPlayerController.gravityState`는 활성 GravitySystem의 `MvpGravityState`를 참조한다.
- `MvpThirdPersonCamera.input`은 Player의 `MvpPlayerInput`, `target`은 Player Transform, `cameraPivot`은 CameraRig의 Pivot을 참조한다.
- 활성 MainCamera 태그와 AudioListener는 각각 하나만 존재해야 한다.

### 4. Original 변경 보존과 범위 통제

- `GameSystem`, `Triggers`, `Environment`, 각 Zone과 Collider 계층은 최신 Original 상태를 유지한다.
- 기존 Player 씬에서 제거됐던 `GameSystem`과 `Triggers`를 다시 제거하지 않는다.
- Player·CameraRig·Gravity 연결과 무관한 Prefab instance position override를 이전 Player 씬에서 가져오지 않는다.
- DOTween은 설치 상태만 유지하고 이번 씬 동기화에는 컴포넌트나 Sequence를 추가하지 않는다.

## 실행 계획

1. `[Git·Unity 기준선과 복구 지점 기록]` → verify: `[두 씬 dirty false, Original·Player 씬과 각 meta의 Git 상태·해시·GUID 기록, 현재 작업 트리의 기존 변경 목록 분리]`
2. `[최신 Original의 Player 위치와 핵심 Hierarchy 기록]` → verify: `[GameSystem·Triggers·Environment·Zone·Collider·GravitySystem·Player의 경로와 Player Transform 값을 기록]`
3. `[최신 Original 내용을 GamePlayScene_Player의 새 기준으로 반영]` → verify: `[Player 씬 저장 후 Original과 핵심 Hierarchy가 일치, Original hash와 Player meta GUID 불변]`
4. `[최신 위치를 유지해 MvpPlayer connected instance 배치]` → verify: `[Player 위치·회전·부모가 기록값과 일치하고 Rigidbody·Collider·MvpPlayerInput·MvpPlayerController가 각각 한 개]`
5. `[기존 카메라를 CameraRig connected instance로 교체]` → verify: `[CameraRig/Pivot/Main Camera 계층, MainCamera 태그와 AudioListener 각 한 개, 카메라 target·input·pivot 참조 누락 없음]`
6. `[기존 활성 GravitySystem에 MvpGravityState 연결하고 Player 참조 구성]` → verify: `[중력 방향 (0, -1, 0), 세기 9.81, Player의 gravityState·cameraTransform 참조 누락 없음]`
7. `[GamePlayScene_Player만 저장하고 씬 재로드]` → verify: `[Prefab 연결 상태가 Connected로 유지되고 Missing Script·Missing Reference·직렬화 오류 없음]`
8. `[Play Mode에서 기본 이동·카메라·충돌·중력 검증]` → verify: `[최신 Entry 스폰, WASD·마우스 조작, 바닥 충돌과 낙하 안정성, 신규 Console 오류 0건]`
9. `[최종 path-scoped diff와 Original 무변경 확인]` → verify: `[의도한 씬 변경은 GamePlayScene_Player에 한정, Original·두 meta·Prefab·스크립트·ProjectSettings diff 없음]`
10. `[검증 결과 기록과 계획 상태 갱신]` → verify: `[성공·실패·사람이 확인한 조작·남은 제한을 활용 기록에 남기고 완료 시 계획서를 03_completed로 이동]`

## 검증 기준

- `Original_GamePlayScene.unity`와 `.meta`의 작업 전후 해시가 같다.
- `GamePlayScene_Player.unity.meta`의 GUID와 파일 내용이 바뀌지 않는다.
- 최신 Original의 `GameSystem`, `Triggers`, `Environment`, Zone, Collider와 마지막 Zone 배치가 Player 씬에 존재한다.
- Player가 최신 Original의 스폰 위치와 회전을 유지한다.
- Player와 CameraRig가 각각 기존 Prefab의 connected instance다.
- Player에 Rigidbody, CapsuleCollider, MvpPlayerInput과 MvpPlayerController가 중복 없이 존재한다.
- 활성 GravitySystem의 `MvpGravityState`가 아래 방향 `9.81m/s²`를 제공한다.
- PlayerController의 Camera Transform·Gravity State와 CameraRig의 Input·Target·Pivot 참조가 모두 유효하다.
- 활성 MainCamera 태그와 AudioListener가 각각 하나다.
- 씬 저장·닫기·재열기 후에도 Hierarchy, Prefab 연결과 직렬화 참조가 유지된다.
- Play Mode에서 WASD 이동, 마우스 카메라 회전, 지형 충돌과 사용자 정의 중력이 재현된다.
- 컴파일 오류, Missing Script, Missing Reference와 신규 런타임 오류가 없다.
- 현재 존재하는 DOTween·Resources·솔루션 관련 사용자 변경은 수정하거나 되돌리지 않는다.

## 중단 및 복구 조건

- 목적지 갱신 과정에서 Original이 dirty가 되면 저장하지 않고 작업을 중단한다.
- Player meta GUID 변경, Prefab 연결 손실 또는 핵심 Original Hierarchy 누락이 확인되면 Play Mode로 진행하지 않는다.
- 최신 Original의 Player가 게임 흐름·전투 시스템의 추가 컴포넌트나 참조를 소유해 단순 교체할 수 없으면 임의로 삭제하지 않고 차이를 보고한다.
- 자동 처리 결과가 다섯 개 충돌 후보 중 어느 쪽을 선택했는지 확인할 수 없으면 현재 커밋의 Player 씬으로 복구하고 수동 재구성 방식으로 전환한다.
- 복구 후에도 Original과 Player meta는 작업 전 해시와 GUID가 같아야 한다.

## 완료 후 기록

- 구현과 검증 결과는 `Docs/ksh/Codex_Usage_Records.md`에 한 항목으로 남긴다.
- 마스터 계획의 범위나 핵심 기술 방향은 바뀌지 않으므로 `Player_Gravity_Master_Plan.md`는 수정하지 않는다.
- 이번 계획 완료는 최신 팀 씬과 기본 Player·Camera·Gravity 연결의 동기화만 의미한다. 카메라 연출과 마스터 계획의 후속 기능 완료로 기록하지 않는다.
- 씬 동기화와 자동 검증 후 사용자가 WASD 이동·마우스 카메라 조작에 문제가 없음을 확인해 계획을 완료했다.

## 실행 결과

- 최신 Original의 Player 위치 `(113.51, -120.78, 30.24)`와 회전을 Player·CameraRig에 적용했다.
- 기존 Player 하위의 BoxCollider·Rigidbody·직접 Main Camera·모델·Point Light를 제거하고 `MvpPlayer`와 `MvpThirdPersonCameraRig` connected instance로 교체했다.
- Player 태그, `GameFlowManager/RespawnController.playerRoot`, CameraRig 입력·대상·Pivot, PlayerController 카메라·중력 참조를 Unity에서 재조회했다.
- Unity 씬은 active·loaded·dirty false이며 재컴파일은 up-to-date, Play Mode 5초 동안 Console 오류 0건이었다.
- Original 씬과 두 meta, Player meta, Prefab, 스크립트는 변경하지 않았다.
- 사용자가 WASD 이동·마우스 카메라 조작에 문제가 없음을 확인했다.

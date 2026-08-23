# 플레이어 조준·사격·달리기·웅크리기 실행 계획

문서 작성일: 2026-08-23  
현재 상태: 보완 검증 대기  
계획 프로필: `deep`

## 목표

기존 Rigidbody 이동 FSM과 Animator 표현 구조를 유지하면서 Left Shift 전진 달리기, Left Ctrl 홀드 웅크리기, 카메라 상하 조준, 기관총 연사 애니메이션과 Raycast 발사 판정을 연결한다.

적·체력·피해 처리는 팀장 시스템이 전달된 뒤 연결한다. 이번 작업은 플레이어가 실제 조준하고 발사 판정을 만들 수 있는 경계까지만 담당한다.

## 구현 방향

1. `MvpPlayerInput`의 Shift 입력을 `SprintHeld`로 명확히 하고 `CrouchHeld`를 추가한다.
2. Sprint는 Grounded 상태의 전진 속도 modifier, Crouch는 Standing/Crouching stance로 둔다.
3. Crouch는 CapsuleCollider 높이와 중심을 함께 변경하고, 기립 공간이 막히면 작은 Collider를 유지한다.
4. Animator에는 Sprint·Crouch locomotion과 Upper Body 사격 레이어를 추가한다. 이동의 정본은 계속 Rigidbody이며 Root Motion은 사용하지 않는다.
5. Animator 평가 뒤 Spine에 카메라 pitch를 적용해 이동·사격 중에도 상체와 총구가 카메라 상하 방향을 따른다.
6. 카메라 중앙 Raycast로 조준점을 구한 뒤 Muzzle에서 조준점까지 다시 검사해 총구 앞 장애물을 실제 적중으로 우선한다.
7. Player·CameraRig Prefab과 `GamePlayScene_Player`만 연결하고 `Original_GamePlayScene`과 Toon Soldiers 원본은 수정하지 않는다.

## 기본값과 범위

- 보행 속도 `3`, Sprint 속도 `5`, Crouch 속도 `1.5`
- Sprint는 Grounded·Standing에서 전진 입력이 있을 때만 허용
- Crouch는 Ctrl을 누르는 동안 유지하며 Crouch 중 점프 금지
- Crouch Capsule 높이는 standing 높이의 `65%`
- 연사 간격 `0.1초`, 사거리 `100m`
- 임시 조준 UI는 화면 중앙 `4x4` 흰색 점
- 제외: 적 피해 호출, 체력, 탄약, 재장전, 반동, 총구 섬광, 사운드, 정식 HUD, WebGL 빌드

## 검증 기준

- Shift+전진에서만 Sprint 속도와 애니메이션이 적용된다.
- Crouch의 속도·Collider·애니메이션이 함께 바뀌고 낮은 천장 아래에서 기립하지 않는다.
- 기존 걷기·점프·착지·ZeroGravity 상태가 회귀하지 않는다.
- 카메라 pitch에 따라 상체와 Muzzle이 움직이며 하체 locomotion은 유지된다.
- 최초 탄은 즉시 발사되고, 유지 시 `0.1초` 간격으로만 판정된다.
- 카메라 적중·빗나감·총구 앞 장애물·자기 Collider 제외가 확인된다.
- Unity 재컴파일과 C# 빌드 오류가 없고 Play Mode Console에 신규 오류가 없다.

## 문서 정합성

- 마스터 플랜에는 전진 Sprint와 Ctrl Crouch 결정을 확정하고, 플레이어 담당 사격을 조준·애니메이션·판정까지로 기록한다.
- 게임 기획서의 오래된 Shift 단일 해석과 입력 파일 경로를 실제 구현에 맞춘다.

## 실행 결과

- 전진 Sprint `5`, Crouch `1.5`, Crouch Capsule 높이 `0.585`와 기립 공간 차단·복귀를 Play Mode에서 확인했다.
- 중앙 4x4 점, Upper Body 사격 레이어, 카메라 pitch 기반 Spine 조준과 Muzzle을 연결했다.
- 연사 간격, 카메라 조준 표면 적중, 총구 앞 장애물 우선과 자기 Collider 제외를 런타임 테스트 오브젝트로 확인했다.
- 3개 뼈 분배는 자식 world rotation이 부모 보정을 일부 상쇄해 Spine 단일 보정으로 축소했고, 위·아래 조준에서 Muzzle과 카메라 Ray 내적 `0.9998` 이상을 확인했다.
- C# 빌드는 기존 애셋 경고 17건과 함께 오류 0건으로 성공했고 신규 Play Mode 오류는 없었다.

## Phase 2 — 달리기 반응성 및 발사 궤적 보완

계획 프로필: `standard`

### 목표

- Sprint 입력부터 Animator 전이까지의 불필요한 프레임 지연을 줄인다.
- Shift를 유지한 점프 착지에서 짧은 착지 모션 뒤 Sprint로 직접 복귀한다.
- 기존 Hitscan 판정의 실제 Muzzle-to-hit 경로를 Game View에서 확인할 수 있게 한다.

### 구현 방향

1. 이동·접지·Sprint·Crouch Animator 파라미터는 `Update`에서 전달하고, 사격 상태·카메라 pitch·Spine 보정은 `LateUpdate`에 유지한다.
2. `Locomotion ↔ Sprint` 전이 시간은 `0.05초`로 줄인다.
3. `JumpLand → Sprint` 전이는 `IsGrounded && IsSprinting`, Exit Time `0.2`, 전이 시간 `0.05초`로 추가한다.
4. Muzzle의 `LineRenderer` 하나를 재사용해 실제 `shotEnd`까지 `0.05초`간 표시한다.
5. `showShotTracer`가 꺼져 있으면 시각화만 생략하고 발사 판정과 애니메이션은 유지한다.

### 기본값과 범위

- 트레이서 폭 `0.015`, World Space, View 정렬, 그림자 비활성
- 명중 트레이서 빨강, 빗나감 트레이서 청록
- 개발 중 `showShotTracer = true`, 릴리즈 시 Player Prefab에서 false로 전환
- 제외: 탄환 물리, 피해, 피격 이펙트, 총구 섬광, WebGL 빌드

### 검증 기준

- Shift 입력 후 실제 속도는 다음 Fixed Tick에 바뀌고 Animator에는 불필요한 추가 프레임 지연 없이 전달된다.
- Shift 유지 점프는 약 `0.1초`의 JumpLand 뒤 Locomotion을 거치지 않고 Sprint로 복귀한다.
- Shift 해제 착지와 Crouch 착지는 기존 전이를 유지한다.
- 명중·빗나감·총구 앞 장애물에서 트레이서 종점이 실제 `shotEnd`와 일치한다.
- `showShotTracer`를 끄면 트레이서만 사라지고 연사 간격과 Raycast 결과는 유지된다.
- 기존 걷기·점프·웅크리기·ZeroGravity가 회귀하지 않고 Play Mode Console에 신규 오류가 없다.

### Phase 2 실행 결과

- 이동·접지·Sprint·Crouch Animator 파라미터를 `Update`로 분리하고 `Locomotion ↔ Sprint` 전이를 `0.05초`로 단축했다.
- `JumpLand → Sprint`에 `IsGrounded && IsSprinting`, Exit Time `0.2`, 전이 시간 `0.05초`를 적용했다.
- Muzzle에 재사용 가능한 `LineRenderer`와 `Sprites/Default` 재질을 연결하고 `showShotTracer` 토글을 추가했다.
- C# 빌드는 기존 애셋 경고 17건과 함께 오류 0건으로 성공했다.
- Unity 도메인 리로드 뒤 편집기 tick 중단으로 MCP가 시간 초과되어 Play Mode 체감·Game View 검증은 대기 중이다.

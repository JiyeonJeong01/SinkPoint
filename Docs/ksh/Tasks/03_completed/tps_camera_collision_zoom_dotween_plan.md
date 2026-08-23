# TPS 카메라 충돌·거리 조정·DOTween 보간 실행 계획

문서 작성일: 2026-08-23  
현재 상태: 완료 (2026-08-23)  
계획 프로필: `standard`

## 목표

기존 `MvpThirdPersonCameraRig`의 즉각적인 마우스 회전 감각과 Player 추적 구조를 유지하면서, `GamePlayScene_Player`에서 지형이 카메라와 Player 사이를 가리지 않도록 카메라 거리를 안전하게 줄이고 다시 확보한다.

마우스 휠로 사용자가 시야 거리를 조정할 수 있게 하며, DOTween은 줌과 장애물 이탈 후 거리 복귀에만 제한적으로 사용한다. 카메라 충돌 안전성은 Tween보다 우선한다.

## 범위

- `MvpPlayerInput`을 통한 마우스 휠 거리 입력
- `MvpThirdPersonCamera`의 사용자 목표 거리, 현재 표시 거리와 충돌 제한 거리 관리
- Camera Pivot에서 목표 카메라 위치까지의 `SphereCastNonAlloc` 기반 장애물 검사
- Player 자신의 Collider와 Trigger를 충돌 후보에서 제외
- 장애물 접근 시 즉시 거리 축소
- 장애물 이탈과 사용자 줌 변경 시 DOTween easing 적용
- `MvpThirdPersonCameraRig.prefab`의 Main Camera 참조와 기본 튜닝값 구성
- Unity 재컴파일, Prefab·씬 참조, Play Mode와 WebGL 빌드 검증

## 하지 않을 것

- `Assets/_Scenes/Original_GamePlayScene.unity` 수정
- 지형 Collider 추가·교체 또는 별도 카메라 충돌 지형 제작
- Cinemachine 도입이나 카메라 시스템 전면 교체
- DOTween Animation·DOTween Path를 이용한 시네마틱 카메라 연출
- 카메라 흔들림, 피격 연출, 조준 FOV, 어깨 교체와 자동 시점 복귀
- 방향성 중력에 맞춘 Camera Rig up축 전환
- 카메라 상태 머신 또는 범용 카메라 프레임워크 추가
- 새 Physics Layer·Tag 또는 입력 패키지 추가
- DOTween 플러그인 파일·설정의 재설치, 업그레이드 또는 기존 사용자 변경 정리
- 현재 작업 트리의 씬·ProjectSettings·솔루션·활용 기록 변경 되돌리기

## 현재 상태와 필요한 가정

- 기존 Camera Rig는 `MvpThirdPersonCameraRig/CameraPivot/Main Camera` 계층이며 Rig가 yaw와 Player 추적, Pivot이 pitch, Main Camera의 로컬 Z가 거리를 담당한다.
- 현재 Pivot 높이는 `1.03`, Main Camera 로컬 Z는 `-1.605`, Camera FOV는 `60`, near clip plane은 `0.3`이다.
- `MvpThirdPersonCamera`는 `LateUpdate`에서 `MvpPlayerInput.Look`을 읽어 Rig 위치와 yaw, Pivot pitch를 갱신한다.
- Player Rigidbody는 `Interpolate`를 사용하므로 기존 LateUpdate 추적 순서를 유지한다.
- CameraRig의 `input`과 `target`은 씬 override, `cameraPivot`과 신규 Main Camera Transform 참조는 Prefab 내부 참조로 유지한다.
- 지형 충돌 정본은 최신 `GamePlayScene_Player`에 반영된 팀장 소유 BoxCollider다.
- 전용 Player 또는 CameraCollision Layer가 없으므로 `collisionMask` 기본값과 대상 Player 계층 제외 검사를 함께 사용한다.
- DOTween, DOTween Pro와 `Assets/Resources/DOTweenSettings.asset`이 현재 프로젝트에 존재한다. 구현 시작 시 Unity 재컴파일로 실제 참조 가능 상태를 다시 확인한다.
- DOTween의 코드 API를 사용하며 Pro 전용 시각적 컴포넌트는 이번 반응형 카메라에 사용하지 않는다.
- 초기 수치는 출발점이며 실제 Entry·모서리·좁은 통로 Play Mode 결과에 따라 Inspector 값만 좁게 조정한다.

## 마스터 플랜 정합성

| 마스터 플랜 기준 | 이번 계획의 대응 | 판단 |
| --- | --- | --- |
| 3인칭 플레이어 이동과 카메라는 담당 범위 | 기존 TPS Camera Rig에 충돌 방지와 거리 조정을 추가 | 일치 |
| 입력은 `MvpPlayerInput`을 단일 입구로 사용 | 마우스 휠도 입력 래퍼에서 읽고 카메라는 값만 소비 | 일치 |
| 지형은 팀장 배치 BoxCollider를 사용 | 별도 지형을 만들지 않고 기존 Collider를 SphereCast 대상으로 사용 | 일치 |
| 핵심 루프와 플레이 가능 상태 우선 | 관통 방지와 시야 확보를 안전성 기능으로 한정 | 일치 |
| 완성형 애니메이션과 고급 카메라 연출 제외 | DOTween을 짧은 거리 보간에만 사용하고 연출 기능은 제외 | 일치 |
| 구체 API와 수치는 구현 변경에서 검증 후 확정 | 공개 API를 만들지 않고 직렬화 수치는 Play Mode에서 확정 | 일치 |
| 방향성 중력은 후속 단계 | 이번 변경은 현재 world-up 카메라 동작을 보존하고 up축 전환은 확장하지 않음 | 범위 분리 |

이번 작업은 마스터 플랜의 범위나 핵심 기술 방향을 바꾸지 않는다. 따라서 구현 전후에 정합성 차이가 새로 발견되지 않는 한 `Player_Gravity_Master_Plan.md`는 수정하지 않는다.

## 구현 방향

### 1. 입력 계약

- `MvpPlayerInput`에 해당 프레임의 카메라 거리 입력을 나타내는 읽기 전용 값을 추가한다.
- 마우스 휠은 `MvpPlayerInput.Update` 안에서만 `Input.mouseScrollDelta.y`로 읽는다.
- `allowLook`이 꺼지면 Look과 함께 거리 입력도 `0`으로 초기화하여 대화·컷신 중 거리 변경을 막는다.
- 휠 위쪽은 거리를 줄이고 휠 아래쪽은 거리를 늘린다.
- 기존 Move, Look과 액션 입력의 이름·의미·차단 동작은 변경하지 않는다.

### 2. 카메라 거리 상태

- 사용자 설정 범위는 `defaultDistance`, `minDistance`, `maxDistance`로 관리한다.
- 사용자 요청 거리와 실제로 화면에 적용되는 거리를 분리한다. 충돌 때문에 실제 거리는 사용자 최소 거리보다 더 짧아질 수 있다.
- 초기 기본 거리는 현재 구도를 보존하는 `1.605`를 사용한다.
- 초기 사용자 최소·최대 거리는 각각 `0.6`, `3.0`, 휠 한 단계 변화량은 `0.25`를 후보값으로 둔다.
- 카메라 최종 위치는 Main Camera Transform의 `localPosition`에서 기존 X·Y를 보존하고 Z만 음수 거리로 갱신한다.

### 3. 충돌 거리 계산

- yaw와 pitch를 먼저 적용한 뒤 Pivot의 월드 위치에서 사용자가 요청한 카메라 중심 위치 방향으로 구체를 Cast한다.
- `Physics.SphereCastNonAlloc`과 재사용 배열을 사용하여 안정 상태의 프레임별 GC 할당을 만들지 않는다.
- `QueryTriggerInteraction.Ignore`를 사용하고, 반환 순서에 의존하지 않고 모든 결과 중 가장 가까운 유효 충돌을 찾는다.
- `hit.collider.transform`이 대상 Player Transform과 같거나 그 자식이면 제외한다.
- 충돌 반경 `0.2`, 벽 여유 거리 `0.05`를 초기 후보로 두고 최종 안전 거리는 `hit.distance - collisionPadding`으로 계산한다.
- 안전 거리는 `0` 이상과 사용자 요청 거리 이하로 제한한다. 사용자 최소 줌 거리는 충돌 안전 거리의 하한으로 사용하지 않는다.
- `collisionMask`를 직렬화 필드로 제공하되 새 Layer는 만들지 않고 현재 지형을 포함하는 기본값으로 시작한다.
- `collisionMask`의 기본값은 모든 레이어(`~0`)로 명시하며, 재사용 hit 배열은 16개로 시작하고 포화 시 개발 환경에서 한 번 경고한다.

### 4. 즉시 충돌 반응과 DOTween 경계

- 새 안전 거리가 현재 표시 거리보다 짧으면 활성 거리 Tween을 중단하고 같은 프레임에 즉시 앞으로 당긴다.
- 장애물에 막힌 상태에서 안전 거리가 조금씩 증가하면 새 Tween을 매 프레임 만들지 않고 제한된 속도로 바깥쪽 거리를 회복한다.
- 충돌 중 바깥쪽 회복 속도는 초기 `8m/s`를 사용한다.
- 장애물 상태가 해제되면 현재 거리에서 사용자 요청 거리까지 단일 Tween을 시작한다.
- 사용자 휠 입력으로 목표 거리가 바뀌면 기존 거리 Tween 하나를 교체한다.
- 초기 복귀 시간은 `0.2초`, 줌 시간은 `0.15초`, easing은 `Ease.OutCubic`을 사용한다.
- `Tween` 참조를 필드로 보관하고 `OnDisable` 또는 `OnDestroy`에서 `Kill`한다.
- `LateUpdate`마다 Tween을 생성하거나 `DOTween.Kill` 전역 호출을 사용하지 않는다.
- yaw와 pitch에는 Tween을 적용하지 않아 조작 지연을 만들지 않는다.

### 5. Camera Rig Prefab

- `MvpThirdPersonCameraRig.prefab`의 기존 세 단계 계층, MainCamera 태그, Camera, AudioListener와 URP 카메라 데이터를 유지한다.
- `MvpThirdPersonCamera`가 Prefab 내부 Main Camera Transform을 직렬화 참조로 받게 한다.
- `input`과 `target`의 기존 씬 override는 유지하고 새 필드 추가로 참조가 끊기지 않았는지 재조회한다.
- Prefab 변경만으로 씬 인스턴스에 반영 가능한 상태를 우선하며, 신규 scene override가 필요하지 않으면 `GamePlayScene_Player`를 저장하지 않는다.

## 초기 튜닝 후보

| 항목 | 초기값 | 목적 |
| --- | ---: | --- |
| 기본 거리 | `1.605` | 기존 구도 보존 |
| 사용자 최소 거리 | `0.6` | 과도한 근접 줌 제한 |
| 사용자 최대 거리 | `3.0` | 좁은 레벨에서 과도한 원거리 시야 제한 |
| 휠 변화량 | `0.25` | 단계가 느껴지되 급격하지 않은 조정 |
| 충돌 반경 | `0.2` | Ray 한 줄보다 모서리 관통에 강한 검사 |
| 충돌 여유 | `0.05` | 카메라가 표면에 정확히 붙는 현상 방지 |
| 줌 시간 | `0.15초` | 입력 지연 없이 짧은 easing |
| 장애물 이탈 복귀 | `0.2초` | 벽을 벗어날 때 튀는 현상 완화 |
| Ease | `OutCubic` | 초반에 빠르게 이동하고 끝에서 안정화 |

## 실행 계획

1. `[Git·Unity·DOTween 기준선 기록]` → verify: `[기존 미커밋 변경 목록 분리, 대상 스크립트·Prefab hash 기록, Unity Console의 기존 오류와 DOTween 참조 가능 상태 확인]`
2. `[MvpPlayerInput에 거리 입력값 추가]` → verify: `[allowLook 활성 시 휠 값 전달, 비활성 시 Look과 거리 입력 모두 0, 기존 입력 계약 변화 없음]`
3. `[MvpThirdPersonCamera에 거리 상태와 Main Camera 참조 추가]` → verify: `[기본 거리 1.605에서 기존 yaw·pitch·추적 구도가 동일하고 필수 참조 누락 시 명확히 비활성화]`
4. `[SphereCastNonAlloc 충돌 거리 계산 구현]` → verify: `[Player·Trigger 제외, 비정렬 hit 중 최근접 유효 Collider 선택, 충돌 시 같은 프레임에 안전 거리 적용]`
5. `[DOTween 줌과 장애물 이탈 복귀 연결]` → verify: `[접근은 즉시, 이탈과 휠 변경만 easing, 프레임마다 Tween을 생성하지 않고 비활성화 시 잔여 Tween 없음]`
6. `[CameraRig Prefab 내부 참조와 초기값 적용]` → verify: `[Prefab Connected 상태, Main Camera 내부 참조 유효, 기존 input·target scene override와 MainCamera·AudioListener 각 한 개 유지]`
7. `[Unity 재컴파일과 씬 재로드]` → verify: `[컴파일 오류·Missing Script·Missing Reference 0건, 저장·재로드 후 Prefab 값과 씬 참조 유지]`
8. `[Play Mode 기능 검증]` → verify: `[개방 공간 회전·이동, 벽 후진 접근, 모서리 회전, 좁은 통로, 휠 최소·최대 거리와 입력 차단을 직접 확인]`
9. `[성능과 회귀 검증]` → verify: `[안정 상태 LateUpdate의 반복 GC Alloc 0 B, Player 이동 방향·카메라 기준 그래플 Raycast·Cursor lock 회귀 없음]`
10. `[WebGL 빌드와 최종 diff 확인]` → verify: `[WebGL 컴파일 성공과 핵심 카메라 조작 재현, Original 씬·지형 Collider·ProjectSettings·DOTween 플러그인 파일의 의도하지 않은 diff 없음]`
11. `[결과 기록과 계획 상태 갱신]` → verify: `[검증 결과·사람이 조정한 수치·남은 제한을 Codex 활용 기록에 남기고 완료 시 계획서를 03_completed로 이동]`

## 검증 기준

- 개방 공간에서 기존 마우스 yaw·pitch 반응성과 현재 구도가 체감상 유지된다.
- Player가 벽으로 후진하거나 벽 옆에서 카메라를 회전할 때 Main Camera가 지형 뒤로 넘어가지 않는다.
- 장애물이 가까워지는 프레임에는 easing 때문에 카메라가 잠시 벽 안에 남지 않는다.
- 장애물에서 멀어지거나 모서리를 벗어날 때 카메라가 한 프레임에 원거리로 튀지 않는다.
- 휠 입력으로 거리가 지정된 범위 안에서 변경되고, 대화·컷신 입력 상태에서는 변경되지 않는다.
- 충돌 때문에 필요한 경우 카메라가 사용자 최소 거리보다 가까워져 관통 방지를 우선한다.
- Player 자신의 CapsuleCollider와 Trigger가 카메라를 불필요하게 앞으로 밀지 않는다.
- MainCamera 태그와 AudioListener는 각각 하나이며 PlayerController의 카메라 기준 Transform 참조가 유지된다.
- 카메라 중심을 사용하는 후속 사격·그래플 Raycast의 방향이 거리 조정 전후에 바뀌지 않는다.
- 안정 상태에서 카메라 LateUpdate가 프레임마다 managed allocation이나 새 Tween을 만들지 않는다.
- 컴파일 오류, Missing Script, Missing Reference와 신규 Console 오류가 없다.
- `Original_GamePlayScene`, 팀장 지형 Collider, ProjectSettings와 DOTween 플러그인 파일을 수정하지 않는다.
- WebGL에서 마우스 회전, 벽 충돌 회피와 거리 조정이 재현된다.

## 중단 및 복구 조건

- DOTween 참조 또는 현재 플러그인 설치 상태 때문에 기존 프로젝트가 컴파일되지 않으면 플러그인 파일을 임의 수정하지 않고 오류와 필요한 설치 조치를 보고한다.
- CameraRig Prefab 적용 과정에서 기존 input·target scene override, PlayerController의 카메라 참조 또는 MainCamera·AudioListener 유일성이 깨지면 Play Mode로 진행하지 않는다.
- 충돌 회피를 위해 Original 씬이나 팀장 지형 Collider 변경이 필요해지면 이번 범위를 중단하고 원인 위치와 대안을 보고한다.
- `SphereCastNonAlloc` 결과 배열 포화가 실제 구간에서 확인되면 무조건 배열을 크게 만들지 않고 충돌 Mask 또는 필요한 크기를 측정해 조정한다.
- near clip plane 모서리 관통이 SphereCast 반경 조정만으로 해결되지 않으면 near clip 변경이나 BoxCast 확장은 별도 판단으로 남기고 임의 확장하지 않는다.
- 방향성 중력 구간에서 world-up 카메라 제한이 이번 기능 검증을 막으면 현재 계획에 섞지 않고 후속 카메라 정렬 작업으로 분리한다.
- 회귀가 발생하면 신규 카메라 코드와 Prefab 값만 되돌릴 수 있어야 하며 기존 씬·DOTween·사용자 변경을 복구 대상으로 삼지 않는다.

## 완료 후 기록

- 구현 결과와 검증 내용은 `Docs/ksh/Codex_Usage_Records.md`에 한 항목으로 남긴다.
- 실제로 확정한 거리, 충돌 반경, 여유 거리, Tween 시간과 Ease를 기록한다.
- 마스터 플랜의 범위나 핵심 기술 방향이 바뀌지 않으면 `Player_Gravity_Master_Plan.md`는 수정하지 않는다.
- 이번 계획 완료는 일반 중력 기준 TPS 카메라의 실용성 개선을 의미하며, 고급 카메라 연출이나 방향성 중력 카메라 정렬 완료로 기록하지 않는다.
- Unity Play Mode에서 사용자가 벽 접근, 모서리 회전과 휠 거리 조작을 직접 확인했고, 계획서를 `Docs/ksh/Tasks/03_completed`로 이동했다.

## 진행 중 검증 기록

- 2026-08-23: Unity 스크립트 재컴파일 성공, 컴파일 오류 0건.
- 2026-08-23: Play Mode 진입과 Camera Rig 초기화 성공. Main Camera 참조, 기존 거리 `1.605`, Console 오류·경고 0건을 확인했다.
- 2026-08-23: WebGL 빌드 사전 검사는 통과했다. 실제 빌드는 기존 Sirenix API Updater가 실행되는 `OnBuildPreProcess` 단계에서 출력 생성 전 정지해 완료 여부를 확인하지 못했다. 빌드가 만든 범위 밖 메타데이터·렌더 파이프라인 변경은 기준선으로 복구했다.
- 2026-08-23: 사용자가 벽 접근·모서리 회전·휠 거리 조작을 충분히 확인하여 이 작업의 Play Mode 완료를 결정했다. 실제 WebGL 빌드는 Sirenix API Updater 단계 문제를 분리한 후 별도 빌드 작업으로 재시도한다.

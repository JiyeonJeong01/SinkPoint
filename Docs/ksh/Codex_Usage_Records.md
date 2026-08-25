# Codex 활용 기록

이 문서는 SinkPoint 플레이어·중력 파트에서 Codex를 어떻게 활용했는지 제출과 팀 공유에 필요한 근거만 간결하게 남긴다.

## 기록 원칙

- 기능 구현, 중요한 문제 해결, 기술 방향·책임 경계 결정, 실제 실행 검증처럼 이후 작업이나 팀 공유에 의미 있는 완료 작업 단위마다 한 항목을 추가한다.
- 실행 계획은 `Docs/ksh/Tasks`에서 관리하고, 계획과 실행이 이어진 작업은 실행·검증 완료 후 하나의 활용 기록으로 남긴다. 계획 과정의 중요한 판단은 완료 항목의 `사람이 직접 결정한 부분`에 포함한다.
- 계획만 작성한 단계는 별도 항목으로 기록하지 않는다. 다만 구현 여부와 관계없이 중요한 기술 방향·책임 경계 결정 자체가 팀 공유에 필요한 완료 성과라면 기록할 수 있다.
- 무엇을 Codex에 맡겼는지와 사람이 직접 결정한 부분을 분리한다.
- 성공 여부와 확인하지 못한 항목을 사실대로 적는다.
- 원문 프롬프트, 비밀정보, 개인 정보와 사소한 탐색 과정은 기록하지 않는다.
- 단순 경로 이동·이름 변경·오탈자 수정, 반복 확인과 중간 탐색처럼 독립적으로 남길 가치가 작은 작업은 기록하지 않는다.
- 이 원칙은 이후 추가하는 기록에 적용하며, 기존 항목은 당시 작업 이력으로 보존한다.

## 기록 형식

```markdown
## YYYY-MM-DD — 작업 이름

- Codex 사용처:
- 구현하거나 정리한 기능:
- 해결한 문제:
- 사람이 직접 결정한 부분:
- 검증 결과:
```

## 2026-08-22 — 플레이어·중력 파트 문서 기반 구성

- Codex 사용처: 기획서, 저장소 구조, 기존 입력 코드, 씬 상태와 제출 기준을 조사하고 개인 지침·마스터 계획·활용 기록의 역할을 분리했다.
- 구현하거나 정리한 기능: 루트 개인용 `AGENTS.md` 규칙과 `Docs/ksh` 아래 팀 공유 문서 구조를 구성하고, 4일 MVP의 담당 범위·기술 방향·단계·완료 기준을 문서화했다.
- 해결한 문제: 아직 구현이 거의 없는 상태에서 팀원이 우리 파트의 방향을 알기 어렵고, 개인 Codex 지침과 GitHub 공유 문서가 섞일 수 있는 문제를 분리했다. 기획서의 입력 경로와 실제 코드 경로가 다른 점, Original 씬과 담당 씬의 충돌 가능성도 문서에 반영했다.
- 사람이 직접 결정한 부분: 방향성 중심의 살아 있는 마스터 계획, Rigidbody 기반 사용자 정의 중력, 팀장의 BoxCollider 사용, Collider 인계 후 `GamePlayScene_Player` 생성, `feat/player-gravity` 브랜치, 개인용 AGENTS와 추적 가능한 팀 문서의 분리를 선택했다.
- 검증 결과: `feat/player-gravity` 브랜치에서 AGENTS의 로컬 제외, 참조 경로, UTF-8 인코딩, Git 상태와 Markdown 공백 검사를 확인했다. 기존 기획서·코드·씬은 변경하지 않았다.

## 2026-08-23 — Unity Pipeline·Codex MCP 선행 도입

- Codex 사용처: Unity CLI와 Pipeline의 실제 설치 상태를 진단하고, Pipeline 패키지 설치·개인 Codex MCP 등록·Editor 읽기 도구 검증·Original 씬 Collider 구조 집계를 수행했다.
- 구현하거나 정리한 기능: 프로젝트에 `com.unity.pipeline` `0.5.0-exp.1`을 추가하고 개인 Codex 설정에 SinkPoint용 Unity MCP를 등록했다. 검증된 설치·연결·안전 경계·문제 해결 절차를 `Docs/ksh/Codex_Unity_Setup_Guide.md`에 정리했다.
- 해결한 문제: 샌드박스가 Git LFS 임시 객체와 localhost Pipeline 서버 접근을 막아 상태가 실패하는 문제를 실제 패키지나 서버 오류와 분리했다. manifest 변경 후 Editor가 종료돼 패키지가 미해석된 상태도 Editor 재실행으로 해결했다.
- 사람이 직접 결정한 부분: 첫 공유 문서는 `Docs/ksh` 내부에 유지하고, 연결과 안전한 실작업 1건을 공유 기준으로 삼았다. Original 씬은 읽기 전용으로 유지하고 Unity CLI skill, Build Settings, CI와 MCP 쓰기 시험은 이번 범위에서 제외했다.
- 검증 결과: Unity `6000.3.20f1`, CLI `1.0.0-beta.5`, Pipeline `0.5.0-exp.1`에서 Editor `ready`, Pipeline 서버 reachable, 142개 도구 노출을 확인했다. Original 씬은 dirty가 아니었고 Hierarchy 470개 오브젝트를 조회한 뒤 씬·Prefab·ProjectSettings diff가 없었다. 새 Codex 작업의 MCP 도구 노출은 재시작 후 확인이 남아 있다.

## 2026-08-23 — Unity MCP 쓰기 검증 및 플레이어 씬 복제

- Codex 사용처: 새 Codex 작업에서 Unity MCP 연결을 재검증하고, 승인된 통합용 씬 복제·활성화·Hierarchy 비교·Console 및 Git 변경 검사를 수행했다.
- 구현하거나 정리한 기능: MCP `copy_asset`으로 `Original_GamePlayScene`을 `GamePlayScene_Player`로 복제하고 새 GUID를 생성했다. 실제 검증 절차와 성공 기준을 `Docs/ksh/Codex_Unity_Setup_Guide.md`에 추가했다.
- 해결한 문제: 도구 노출만으로 연결을 판단하지 않고 실제 Unity 세션의 읽기와 쓰기까지 검증했다. 작업 중 `ProjectSettings.asset`의 `runInBackground`가 바뀐 요청 밖 변경을 발견해 작업 전 값으로 복구했다.
- 사람이 직접 결정한 부분: 팀장 소유 Original 씬은 수정하지 않고 플레이어 담당 통합용 복사본을 만드는 것을 승인했으며, 팀원이 재현할 수 있는 가이드 제공을 요청했다.
- 검증 결과: 복사본은 active·loaded·dirty false, 원본과 동일한 SHA-256, 루트 7개와 Hierarchy 470개로 확인됐다. Console 오류는 0건이고 Original 씬 diff는 없으며, 최종 변경은 새 씬과 메타 및 관련 문서로 제한했다.

## 2026-08-23 — 기본 이동·카메라 계획 최종 검토와 계약 보완

- Codex 사용처: 기본 이동·카메라 실행 계획을 마스터 계획, 실제 입력 코드와 Unity 씬 상태에 대조하고 구현 전 책임 경계와 컴포넌트 참조 계약을 검토했다.
- 구현하거나 정리한 기능: `MvpThirdPersonCamera`가 Player 루트의 `MvpPlayerInput`을 직렬화 참조로 받도록 계획을 수정했다. `/GamePlay/GravitySystem`에는 기본 아래 방향과 세기를 제공하는 최소 `MvpGravityState`를 두고 `MvpPlayerController`가 이를 참조하도록 중력 소유권을 정리했다.
- 해결한 문제: Player와 별도 Prefab인 CameraRig가 같은 루트에서 입력 컴포넌트를 찾을 수 없던 계획상 모순과, PlayerController가 중력 값을 직접 소유해 마스터 계획의 공통 중력 상태 방향과 어긋날 수 있던 문제를 구현 전에 제거했다.
- 사람이 직접 결정한 부분: 카메라의 `MvpPlayerInput` 직렬화 참조를 필수로 하고, 준비된 `GravitySystem` 오브젝트에 MVP 공통 중력 상태를 구성하기로 결정했다. 점프, 달리기, 대쉬와 웅크리기는 후속 작업으로 유지하고 이번에는 기본 이동·카메라·충돌만 완료 기준으로 삼았다.
- 검증 결과: Unity MCP로 `GamePlayScene_Player` active·dirty false, `/GamePlay/Player`와 `/GamePlay/GravitySystem`이 Transform만 가진 상태, Entry 구역 BoxCollider 50개와 Console 오류 0건을 읽기 전용으로 확인했다. 이번 작업에서는 코드·Prefab·씬을 변경하거나 Play Mode를 실행하지 않았다.

## 2026-08-23 — TPS 기본 이동·카메라 MCP 구현

- Codex 사용처: 승인된 실행 계획에 따라 Rigidbody 플레이어, TPS 카메라와 기본 공통 중력 상태를 구현하고 Unity MCP로 폴더·컴포넌트·Hierarchy·Prefab·씬 참조를 구성한 뒤 Play Mode를 검증했다.
- 구현하거나 정리한 기능: `MvpPlayerController`, `MvpThirdPersonCamera`, `MvpGravityState`를 추가했다. Player에는 카메라 기준 WASD 이동, 카메라 방향 회전, 경사 법선 이동, 지면 판정과 사용자 정의 중력을 연결했고, 별도 CameraRig Prefab에는 yaw·pitch 회전과 cursor lock을 구성했다. Player와 CameraRig를 연결된 Prefab 인스턴스로 만들고 `/GamePlay/GravitySystem`의 중력 상태 및 Player 입력 참조를 씬 override로 연결했다.
- 해결한 문제: 별도 CameraRig가 Player의 입력을 찾지 못하는 구조를 직렬화 참조로 해결했고, PlayerController가 중력을 직접 소유하지 않도록 공통 상태를 분리했다. Prefab 생성 후 루트 위치가 자산 기본값에 포함된 것을 발견해 Prefab 루트는 원점으로 정리하고 실제 스폰 위치와 초기 yaw는 씬 override로 분리했다. MCP가 자동 변경한 `runInBackground`도 기준선 값으로 복구했다.
- 사람이 직접 결정한 부분: `MvpThirdPersonCamera`의 `MvpPlayerInput` 직렬화 참조, 기존 `GravitySystem` 오브젝트의 MVP 중력 상태 사용, 점프·달리기·대쉬·웅크리기의 후속 작업 분리를 유지했다. Play Mode에서 사용자가 직접 WASD와 마우스로 Player를 움직여 기본 조작과 중력에 큰 문제가 없음을 확인했다. 작은 BoxCollider 이음새를 계단처럼 오르는 현상은 이번에 이동 보정 기능을 추가하지 않고 Collider 배치에서 단차를 줄여 해결하기로 판단했다.
- 검증 결과: Unity 재컴파일은 `failed=false`, 오류 0건이었다. Player·CameraRig Prefab은 모두 `Connected`, 씬은 active·dirty false이며 Player·Camera·Gravity 참조와 `(0, -9.81, 0)` 중력이 재조회됐다. Play Mode에서 실제 입력에 따라 Player 위치와 카메라 각도가 바뀌었고, 정지 상태에서는 Entry BoxCollider 위에 안정적으로 유지됐으며 신규 런타임 오류가 없었다. 알려진 제한은 울퉁불퉁하게 배치된 작은 BoxCollider 단차를 Player가 타고 오를 수 있다는 점이다. Original 씬, ProjectSettings와 Build Settings에는 최종 diff가 없다.

## 2026-08-23 — Player 씬 최신 Original 동기화 계획

- Codex 사용처: 두 씬의 Git 이력과 Unity YAML 오브젝트 ID를 공통 기준과 비교하고, 최신 팀 씬 변경과 우리 Player·Camera·Gravity 변경의 충돌 범위를 분석해 실행 계획을 작성했다.
- 구현하거나 정리한 기능: 최신 Original을 새 기준으로 삼고 `GamePlayScene_Player`에서만 Player Prefab, CameraRig와 활성 GravitySystem 참조를 재구성하는 절차·검증·복구 조건을 문서화했다.
- 해결한 문제: 양쪽에서 같은 오브젝트의 위치 변경, Prefab 교체와 삭제가 겹쳐 전체 YAML 자동 병합 시 최신 지형 또는 우리 참조가 손실될 수 있는 위험을 분리했다. 기존 Player 씬에서 제거된 GameSystem과 Triggers를 다시 제거하지 않고 최신 Original 상태를 보존하도록 범위를 고정했다.
- 사람이 직접 결정한 부분: `Original_GamePlayScene`은 수정하지 않고 `GamePlayScene_Player`만 갱신하며, 실제 실행 전 별도 계획서를 먼저 만들기로 했다.
- 검증 결과: 공통 기준 `384c86d`, Player 작업 `1cbdfa3`, 최신 Original 지형 변경 `46f70ad`와 DOTween 추가 `731e9eb`의 범위를 확인했다. 공통 기준 대비 중복 변경 후보 5개와 Player 씬 고유 연결을 확인했으며, 이번 작업에서는 두 씬·Prefab·스크립트를 수정하거나 Play Mode를 실행하지 않았다.

## 2026-08-23 — Player 씬 최신 Original 동기화 실행

- Codex 사용처: 최신 Original 직렬화 구조를 읽기 전용 기준으로 삼아 Player 씬의 지형·Collider·Zone·게임 흐름을 갱신하고 Player·CameraRig·Gravity 연결을 제한적으로 재구성했다.
- 구현하거나 정리한 기능: 기존 Original Player 하위 전체를 `MvpPlayer` connected instance로 교체하고 최신 스폰 위치와 `Player` 태그를 유지했다. `MvpThirdPersonCameraRig`를 `/GamePlay` 아래 형제로 배치하고 입력·대상·Pivot과 PlayerController의 카메라·중력 참조를 복구했으며, 활성 `GravitySystem`에 `MvpGravityState`를 연결했다.
- 해결한 문제: 기존 Player 오브젝트만 교체할 경우 모델·Point Light·직접 Main Camera가 중복되는 문제를 하위 트리 단위 교체로 방지했다. Player Transform의 씬 로컬 ID를 유지해 최신 `RespawnController.playerRoot` 참조를 보존했고, Prefab 기본값이 `Untagged`인 문제는 씬 override로 `Player` 태그를 유지해 해결했다.
- 사람이 직접 결정한 부분: 팀장 소유 Original 씬은 수정하지 않고 Player 씬만 갱신하며, 열린 씬에 미저장 변경이 없음을 확인한 뒤 Unity Editor를 종료했다. 최종적으로 사용자가 직접 WASD 이동과 마우스 카메라 조작을 확인했다.
- 검증 결과: Unity에서 Player 씬은 active·loaded·dirty false, 루트 6개로 로드됐다. Player·CameraRig 스폰은 `(113.51, -120.78, 30.24)`, 중력은 `(0, -1, 0) × 9.81`이며 모든 Player·Camera·Gravity·Respawn 참조가 유효했다. YAML 문서 ID 830개는 중복이 없고 로컬 참조 825개는 모두 해석됐다. 재컴파일은 up-to-date였고 Play Mode 5초 동안 Console 오류 0건, RespawnController의 Rigidbody 자동 연결과 중력 정착을 확인했다. 배치 검증은 Unity Licensing Client 재연결 문제로 중단했지만 MCP Editor 검증으로 씬 로드와 Play Mode를 완료했다. 사용자가 실제 WASD 이동·마우스 카메라 조작에도 문제가 없음을 확인했다. Original 씬과 두 meta, Player meta, Prefab·스크립트는 변경하지 않았다.

## 2026-08-23 — TPS 카메라 충돌·거리 조정 실행 계획

- Codex 사용처: 기존 TPS Camera Rig와 DOTween 설치 상태를 실제 코드·Prefab에 대조하고, 벽 관통 방지·마우스 휠 거리 조정·거리 복귀 easing의 실행 계획을 작성했다.
- 구현하거나 정리한 기능: 카메라 접근은 즉시 축소하고 장애물 이탈과 사용자 줌만 DOTween으로 보간하는 책임 경계를 정했다. 입력은 `MvpPlayerInput`, 충돌은 기존 BoxCollider와 `SphereCastNonAlloc`, 기본값은 기존 거리 `1.605`를 기준으로 구성했다.
- 해결한 문제: 카메라 안전 이동까지 easing하면 벽을 통과할 수 있는 위험, 사용자 최소 거리 때문에 충돌 회피 거리가 제한되는 문제, LateUpdate마다 Tween을 생성할 가능성을 계획 단계에서 분리했다.
- 사람이 직접 결정한 부분: 현재 Camera Rig를 유지하고 DOTween Pro를 활용한 실용적인 충돌 방지·거리 조정 카메라를 다음 구현 대상으로 삼았다. 흔들림·시네마틱 연출·방향성 중력 카메라 정렬은 이번 범위에서 제외했다.
- 검증 결과: 계획이 마스터 플랜의 3인칭 카메라 담당, `MvpPlayerInput` 단일 입력 입구, 팀장 BoxCollider 재사용과 고급 연출 제외 원칙에 일치함을 확인했다. 실행 계획서는 `Docs/ksh/Tasks/01_planned/tps_camera_collision_zoom_dotween_plan.md`에 추가했으며 이번 작업에서는 코드·Prefab·씬을 수정하거나 Play Mode를 실행하지 않았다.

## 2026-08-23 — TPS 플레이어 시각 회전 분리 실행 계획

- Codex 사용처: 마우스 카메라 회전 중 Rigidbody 기반 Player가 물리 프레임마다 카메라 방향을 추격해 보이는 떨림의 원인을 코드·Prefab 구조에 대조하고, 카메라 계획과 분리된 실행 계획을 작성했다.
- 구현하거나 정리한 기능: Player 물리 루트는 중력 up축 정렬만 유지하고, 새 `VisualRoot`가 카메라의 중력 평면상 정면을 렌더 프레임에서 부드럽게 추격하도록 책임을 분리했다. 기존 Toon Soldiers 모델은 VisualRoot 아래의 connected nested Prefab으로 보존한다.
- 해결한 문제: 단순 회전 속도 조정으로 물리·렌더 주기 차이를 숨기는 대신 물리와 표현 회전을 분리했다. 모델 내부 뼈를 선점하지 않고 후속 Animator·상하체 Aim·IK가 모델 계층을 사용할 수 있도록 경계를 정했다.
- 사람이 직접 결정한 부분: 실제 조준점, 상하체 애니메이션과 총구 보정은 후속 작업으로 남기고 이번에는 카메라 방향에 따른 플레이어 수평 시각 회전의 떨림 제거만 목표로 삼았다.
- 검증 결과: 계획이 Rigidbody 사용자 정의 중력, 카메라 중심 후속 조준과 고급 애니메이션 제외 원칙에 맞음을 확인했다. 실행 계획서는 `Docs/ksh/Tasks/01_planned/tps_player_visual_rotation_smoothing_plan.md`에 추가했으며 이번 작업에서는 코드·Prefab·씬을 수정하거나 Play Mode를 실행하지 않았다.

## 2026-08-23 — Codex 활용 기록 운영 원칙 정리

- Codex 사용처: 기존 활용 기록의 원칙과 실제 계획·실행 항목 구성을 비교하고, 개인 작업 지침과 팀 공유 문서의 책임을 분리했다.
- 구현하거나 정리한 기능: `AGENTS.md`에는 기록 시점과 정본 문서만 남기고, 계획서 관리·완료 작업 단위·계획 단독 기록의 예외와 기존 기록 보존 기준은 이 문서의 기록 원칙으로 정리했다.
- 해결한 문제: 같은 작업의 계획과 실행을 각각 기록해 내용이 중복되고, 세부 운영 원칙을 두 문서에서 함께 갱신해야 하는 문제를 줄였다.
- 사람이 직접 결정한 부분: 계획은 `Docs/ksh/Tasks`에서 관리하고 활용 기록은 실행·검증 완료 후 한 항목으로 남기며, 중요한 기술 방향·책임 경계 결정 자체가 완료 성과인 경우만 계획 단독 기록을 허용하기로 했다.
- 검증 결과: 기존 활용 기록은 변경하지 않고 보존했으며, `AGENTS.md`와 이 문서의 기록 시점·예외·정본 관계가 서로 일치하는지 확인했다.

## 2026-08-23 — TPS 플레이어 시각 회전 분리 구현

- Codex 사용처: Player Rigidbody와 보이는 캐릭터 모델의 회전 책임을 분리하고, 카메라 회전 중 물리 고정 주기 단위로 보이던 모델 떨림을 제거하는 구현과 정적 검증에 사용했다.
- 구현하거나 정리한 기능: `MvpPlayerController`의 Rigidbody 회전을 중력 up축 정렬 경로로 축소하고, `VisualRoot`가 카메라의 중력 평면상 정면을 실행 순서 `100`의 `LateUpdate`에서 즉시 적용하도록 구성했다. 기존 모델은 identity `VisualRoot` 아래의 connected nested Prefab으로 유지하고 Point Light는 물리 루트 자식으로 보존했다.
- 해결한 문제: 물리 주기의 카메라 yaw 추격과 모델 표현 회전을 분리했다. 초기 지수 보간은 카메라보다 모델이 늦게 반응하는 조작 지연을 만들었으므로 제거하고, 카메라가 갱신된 현재 렌더 프레임에 모델을 직접 동기화했다. `rotationSpeed`는 `FormerlySerializedAs`를 사용해 `gravityAlignmentSpeed`로 이름을 바꾸면서 기존 값 `720`을 보존했으며 무효 투영 벡터 fallback은 유지했다.
- 사람이 직접 결정한 부분: Play Mode에서 sharpness `18`은 조작 지연이 느껴지고 `60`은 보간 효과가 의미 없다고 판단해 시각 회전 튜닝 값을 제거했다. 현재 Rigidbody의 X/Z 회전 제약은 변경하지 않고, 이번 완료 범위를 일반 중력의 시각 회전 안정화로 한정했다. 방향성 중력 정렬과 Rigidbody 제약 재설계, 카메라 충돌·줌, Animator·조준은 후속 작업으로 남겼다.
- 검증 결과: `Assembly-CSharp.csproj` 빌드는 기존 애셋 경고 17건과 함께 오류 0건으로 성공했다. Player Prefab은 YAML 문서 ID 중복 0건, `VisualRoot` 정의·컨트롤러 참조·모델 부모 연결이 각각 유효했고 모델 위치·스케일과 Rigidbody 제약 `80`이 유지됐다. 사용자가 Unity Play Mode에서 느린·빠른 yaw, 180도 회전과 이동·리스폰을 충분히 확인하여 회전 떨림 제거와 카메라 방향 추격 감각이 완료 기준을 충족한다고 결정했다.

## 2026-08-23 — TPS 카메라 충돌·거리 조정·DOTween 보간 구현

- Codex 사용처: 기존 TPS 카메라의 충돌 거리 계산과 입력 경로를 확장하고, CameraRig Prefab 설정·컴파일·Play Mode 초기화를 검증하는 데 사용했다.
- 구현하거나 정리한 기능: `SphereCastNonAlloc`으로 Player와 카메라 사이의 안전 거리를 계산해 장애물 접근 시 즉시 축소하고, 휠 거리 변경과 장애물 이탈만 DOTween으로 보간했다. CameraRig에는 거리 `0.6`~`3`, 단계 `0.25`, 충돌 반경 `0.2`, 여유 `0.05`, 복귀 속도 `8`, 줌 `0.15초`, 충돌 이탈 `0.2초`, `OutCubic` easing을 적용했다.
- 해결한 문제: 충돌 회피를 사용자 최소 거리보다 우선하고, 안전 거리 축소에 Tween을 적용하지 않아 벽 안에 잠시 남는 상황을 막았다. Player 자신의 Collider와 Trigger는 충돌 거리 계산에서 제외하고 안정 상태에 Tween을 반복 생성하지 않도록 했다.
- 사람이 직접 결정한 부분: 사용자가 Play Mode에서 벽 접근, 모서리 회전과 휠 거리 조작을 충분히 확인해 일반 중력 TPS 카메라 작업을 완료로 결정했다. 실제 WebGL 빌드 재시도는 Sirenix API Updater 선행 문제를 해결하는 별도 작업으로 남겼다.
- 검증 결과: Unity 스크립트 재컴파일과 Camera Rig 초기화는 성공했고 컴파일 오류·Console 오류·경고는 0건이었다. Main Camera 참조와 기본 거리 `1.605`를 확인했다. WebGL 실제 출력은 Sirenix API Updater의 `OnBuildPreProcess`에서 중단되어 이번 완료에 포함하지 않았다.

## 2026-08-23 — TPS 플레이어 상태 FSM·기본 애니메이션 구현

- Codex 사용처: 기존 `MvpPlayerController.FixedUpdate`의 이동·접지·중력 책임을 상태별 물리 계약으로 분리하고, Rigidbody 결과를 Toon Soldiers Animator로 표현하는 게임플레이 FSM·Animator FSM을 구성하고 검증했다.
- 구현하거나 정리한 기능: 일반 C# 상태 인스턴스를 재사용하는 `MvpPlayerStateMachine`과 `Grounded`·`Airborne`·`ZeroGravity`를 추가했다. 기존 지상·공중 이동을 Controller 물리 helper로 보존하고 현재 중력의 반대 방향 점프를 `jumpSpeed = 5` 초기값으로 연결했다. `MvpPlayerAnimationController`는 실제 속도와 게임 상태를 `MoveX`·`MoveY`·`MoveSpeed`·`IsGrounded`·`IsZeroGravity`·`VerticalSpeed`로 공급하며, 새 2D 방향 Blend Tree와 Jump Start/Air/Land 전이를 Player Prefab의 nested Animator에 연결했다.
- 해결한 문제: 일반·측면·역중력용 상태를 복제하지 않고 `MvpGravityState`의 방향·세기를 매 물리 프레임 Context로 전달하도록 했다. 점프 직후 Ground Probe가 남아 즉시 재접지할 수 있는 경우는 위쪽 속도가 양수인 동안 Airborne을 유지해 막았다. 중력 세기 `0`에서는 이동 속도 덮어쓰기와 중력 힘을 모두 중단하며, Animator Root Motion은 Player Prefab override에서 비활성화해 게임 이동의 정본을 Rigidbody로 유지했다.
- 사람이 직접 결정한 부분: 승인된 deep 계획의 게임플레이 FSM·Animator 분리, Infantry 인플레이스 클립과 Root Motion 금지, Grappling·Dash·Crouch·Combat 제외 범위를 유지했다. `jumpSpeed = 5`와 Animator damping `0.1초`는 이번 구현의 초기값이며 장애물 높이·발 미끄러짐을 기준으로 한 사용자 체감 튜닝은 후속 조정 대상으로 남겼다.
- 검증 결과: Unity 재컴파일과 `Assembly-CSharp.csproj` 빌드는 오류 0건으로 성공했고, 빌드에는 기존 애셋 경고 17건만 남았다. Play Mode에서 Grounded/Locomotion 정지, 평면 속도 `3`과 전진 파라미터, 1회 점프 후 Airborne/JumpAir와 착지 복귀, ZeroGravity 진입 시 충돌을 끈 격리 조건에서 선형 속도 보존, 중력 복구 시 Airborne 재선택을 확인했다. Player Prefab의 모든 직렬화 참조, Controller, Apply Root Motion false, nested Prefab Connected, 모델 로컬 위치 `(0, -0.235, 0)`·identity 회전·스케일 `0.3`, Missing Script 0건과 Console 오류 0건을 확인했다. Toon Soldiers 원본과 Original·Player 씬에는 diff가 없다. 전역 Editor 계측에는 Pipeline을 포함한 프레임 할당이 섞여 컴포넌트 단독 GC Alloc 0 B를 수치로 분리하지 못했지만, 두 새 프레임 경로에는 런타임 객체·배열·delegate·컬렉션 생성이 없음을 정적으로 확인했다.

## 2026-08-23 — 점프 입력 래치·착지 버퍼 보정

- Codex 사용처: 렌더 프레임의 Space 눌림이 다음 물리 프레임 전에 덮어써지는 입력 경로와 착지 프레임의 FSM 승인 순서를 진단하고, Input·Controller·StateMachine 사이의 점프 요청 수명을 보정하는 데 사용했다.
- 구현하거나 정리한 기능: `MvpPlayerInput`이 Space 눌림과 실제 입력 시각을 다음 `FixedUpdate` 소비 시점까지 래치하도록 변경했다. `MvpPlayerController`에는 기본 `0.10초` 착지 버퍼를 추가하고, FSM은 이전 State가 아니라 현재 Ground Probe와 상승 여부를 기준으로 점프를 승인한 뒤 실제 실행 여부를 반환하도록 구성했다.
- 해결한 문제: 고주사율 렌더링에서 `GetKeyDown` 결과가 50 Hz 물리 프레임 전에 사라지는 문제와, Airborne에서 착지한 같은 물리 프레임의 Space가 이전 State 조건 때문에 무시되는 문제를 분리해 해결했다. 점프 요청은 성공, 만료, 이동 입력 차단 또는 무중력 진입 때 제거해 오래된 입력이 뒤늦게 실행되지 않도록 했다.
- 사람이 직접 결정한 부분: 공중 입력은 더블 점프나 코요테 타임으로 해석하지 않고, 착지 전 `0.10초` 안에 입력된 경우에만 착지 버퍼로 인정하기로 했다. 점프 속도와 Ground Probe, Animator, Prefab과 Scene은 이번 변경에서 유지했다.
- 검증 결과: `Assembly-CSharp.csproj` 빌드와 Unity `6000.3.20f1` 재컴파일은 오류 0건으로 성공했으며 기존 애셋 경고 17건만 남았다. Play Mode 초기화 후 기준 커서 이후 신규 런타임 오류가 없었고 정상 종료했다. 실제 Space 반복 입력, 착지 전후 `0.10초` 경계와 공중 연타 체감은 키보드 입력을 자동 주입하지 못해 사용자 Play Mode 확인이 남아 있다.

## 2026-08-23 — 플레이어 조준·사격·달리기·웅크리기 구현

- Codex 사용처: 플레이어 기본 액션 확장 계획에 따라 입력·Rigidbody 이동 stance·CapsuleCollider·Animator·카메라 조준과 Raycast 사격을 구현하고 Prefab·Player 씬 참조 및 Play Mode를 검증했다.
- 구현하거나 정리한 기능: Left Shift 전진 Sprint와 Left Ctrl 홀드 Crouch를 분리하고 속도 `3/5/1.5`, Crouch Capsule 높이 `65%`, 기립 공간 SphereCast를 연결했다. Machinegun Sprint·Crouch 방향 이동·Standing/Crouch 연사 애니메이션, Upper Body Mask, 카메라 pitch 기반 Spine 조준, M4 Muzzle, 중앙 4x4 임시 점과 카메라 중심→총구 2단계 Raycast를 추가했다.
- 해결한 문제: standing Capsule 전체 Overlap이 바닥을 천장으로 오인하던 문제를 현재 Crouch 상단에서 standing 상단까지만 검사하도록 수정했다. 카메라가 찾은 표면점과 총구 Ray 종점이 같아 적중이 누락되는 경우는 사거리 안에서 5cm 검사 여유를 추가했다. Spine·Chest·UpperChest 분배 보정이 자식 world rotation으로 부모 보정을 상쇄한 문제는 Spine 단일 pitch 보정으로 단순화했다.
- 사람이 직접 결정한 부분: Shift는 전진할 때만 Sprint, Ctrl은 누르는 동안 Crouch, 좌클릭은 `0.1초` 간격 연사로 확정했다. 사격 책임은 애니메이션과 판정까지만 두고 적 체력·피해 API, 탄약·재장전·반동·VFX·사운드와 정식 UI는 제외했다. 마스터 플랜과 게임 기획서의 오래된 Shift 단일 해석과 입력 경로도 현재 결정에 맞췄다.
- 검증 결과: 자동 Play Mode 입력으로 Sprint 속도 `5`와 Animator 상태, Crouch 속도 `1.5`·Capsule 높이 `0.585`·천장 차단 후 복귀를 확인했다. 약 6초 연사에서 56회 판정, 테스트 Collider 적중, 총구 앞 장애물 우선, Upper Body 발사 상태와 위·아래 조준 시 Muzzle/카메라 Ray 내적 `0.9998` 이상을 확인했다. Unity 재컴파일과 `Assembly-CSharp.csproj` 빌드는 오류 0건이며 기존 애셋 경고 17건만 남았고, 최종 Play Mode에는 신규 런타임 오류가 없었다. `Original_GamePlayScene`, Toon Soldiers 원본과 ProjectSettings는 변경하지 않았으며 실제 키보드·마우스 체감 튜닝과 WebGL 빌드는 이번 범위에서 실행하지 않았다.

## 2026-08-23 — 플레이어·지네 히트 및 피해 상호작용 구현

- Codex 사용처: 현재 이름으로 정리된 플레이어 코드와 지네의 실제 이동 기준인 `Nav Target`을 다시 대조하고, Hitscan·Trigger·체력 사이의 첫 전투 상호작용을 구현하고 검증하는 데 사용했다.
- 구현하거나 정리한 기능: `PlayerHealth`에 최대/현재 체력, 사망 상태, 피해·초기화 API와 이벤트를 추가했다. `PlayerCombatController`는 일반 Collider를 장애물로 유지하면서 살아 있는 `MonsterHealth`를 가진 Trigger만 사격 후보로 받아 총구 Ray 최종 적중에 피해 `1`을 전달한다. 지네의 `Nav Target`에는 월드 반경 약 `0.3`의 Sphere Trigger와 `MonsterDamageOnContact`를 배치해 플레이어에게 피해 `1`을 1초 쿨타임으로 전달한다. 새 몬스터가 같은 계약을 적용하고 양쪽 담당자가 공동 테스트할 수 있도록 `Docs/Player_Monster_Damage_Integration_Guide.md`에 피격·접촉·사망 구성과 검증 체크리스트를 정리했다.
- 해결한 문제: 환경 Trigger가 사격을 막지 않으면서 몬스터 Trigger는 맞출 수 있도록 카메라·총구 Ray의 후보 규칙을 통일했다. 태그·씬 전역 검색 기반 접촉 판정을 접촉 Collider의 부모 `PlayerHealth` 탐색으로 바꾸고, 죽은 플레이어나 몬스터의 추가 피해를 차단했다. 분리된 Player와 CameraRig Prefab 사이의 `aimCamera` 참조는 `MonsterTest` 씬 override로 연결했다.
- 사람이 직접 결정한 부분: 기본 체력 `3`, 사격·접촉 피해 `1`, 접촉 쿨타임 `1초`, `Nav Target` SphereCollider 로컬 반경 `3`을 MVP 초기값으로 사용했다. HP UI, 피격·사망 연출, 넉백, 무적 시간, 리스폰과 몸통 부위별 Hitbox는 후속 작업으로 남겼다.
- 검증 결과: Unity `6000.3.20f1` 재컴파일과 `Assembly-CSharp.csproj` 빌드는 오류 0건으로 성공했으며 빌드에는 기존 애셋·몬스터 디버그 필드 경고 19건만 남았다. `MonsterTest` Play Mode에서 지네가 RouteMove→Chase→Attack으로 전환하고 Trigger Enter 직후 1회, Stay 중 약 1초 간격으로 두 번 더 접촉 피해를 준 뒤 플레이어 사망 상태에서 추가 피해가 멈추는 것을 확인했다. Player Prefab의 `PlayerHealth`, `Nav Target`의 Trigger·접촉 컴포넌트, 루트 접촉 컴포넌트 제거와 `aimCamera` 참조를 확인했다. 사용자가 Inspector의 실시간 체력으로 플레이어 사격 시 몬스터 체력 감소와 몬스터 접촉 시 플레이어 체력 감소를 모두 확인했고, 몬스터가 정해진 횟수만큼 피격된 뒤 이동을 멈추는 사망 동작도 확인해 기본 데미지 상호작용을 완료로 결정했다. 기본 Inspector에는 비직렬화 런타임 필드가 보이지 않았으므로 두 Health 컴포넌트의 `currentHealth`와 `dead`를 Play Mode 확인용 직렬화 필드로 보완했다. `Original_GamePlayScene`, Build Settings, `ProjectSettings`와 외부 에셋 원본은 변경하지 않았다.

## 2026-08-24 — Zone 중력 전환·테스트 Inspector 구현

- Codex 사용처: Zone 기반 90도 중력 전환의 물리·표현 책임을 분리하고, Player·Camera 동기화와 팀원이 반복 사용하기 쉬운 Inspector 테스트 UX를 구현·검증하는 데 사용했다.
- 구현하거나 정리한 기능: `GravityManager`가 실제 중력을 즉시 적용하면서 단일 `PresentationUp`·진행률·시작/완료 신호를 소유하도록 확장했다. Player는 전환 중 위치와 속도를 고정하고, Camera는 같은 진행률로 Roll한 뒤 새 Up에서 Orbit을 재구성한다. Custom Inspector에는 씬 Zone 드롭다운과 고정 실행 버튼을 둔 `Gravity Zone Select`, 현재 Zone·중력·전환 진행률을 보여 주는 `Play Mode Zone Info`를 분리했다.
- 해결한 문제: 환경 `GravityBody`의 즉시 반응과 Player·Camera의 짧은 표현 전환을 분리하고, 전환 도중 최신 Zone 재요청도 현재 표시 자세에서 연속되게 했다. 직렬화된 테스트 Zone 슬롯과 Zone별 버튼을 제거하고, 선택과 실행 버튼 사이에 런타임 정보가 끼어 다른 팀원이 조작하기 불편한 Inspector 배치도 정리했다.
- 사람이 직접 결정한 부분: 실제 게임 전환은 화면 기준 반시계 Roll, 기본 전환 시간 `0.5초`, Player 속도 제거 후 새 중력 방향으로 재개하는 정책으로 확정했다. 사용자가 Play Mode 테스트 성공을 확인했고, 선택 조작과 런타임 정보를 별도 영역으로 나누는 최종 Inspector 구성을 제안했다.
- 검증 결과: 런타임·Editor 어셈블리 빌드는 오류 0건이며 기존 경고 19건만 남았다. Unity MCP에서 Normal↔Shift 5회 반복, 중간 재요청 연속 오차 `0.000000`, Player·Camera·Presentation Up 일치, Rigidbody 제약과 상위 입력 잠금 복구, Console 오류 0건과 씬 dirty 없음이 확인됐다. 사용자가 실제 Play Mode에서도 중력 전환이 성공적이라고 확인했다. `Original_GamePlayScene`, Collider, Packages, ProjectSettings와 외부 에셋 원본은 변경하지 않았다.

## 2026-08-25 — 최신 Original 레벨 변경 Player 씬 동기화

- Codex 사용처: `origin/develop` 병합 이력과 두 게임플레이 씬의 Unity YAML을 비교해 자동 Git 병합과 실제 씬 통합을 구분하고, 최신 팀 레벨을 기준으로 Player 씬을 재구성·검증하는 데 사용했다.
- 구현하거나 정리한 기능: 최신 `Original_GamePlayScene`의 MeshCollider 지형, Zone Entry Point, 바리게이트, 몬스터, NPC와 HUD 구성을 `GamePlayScene_Player`에 동기화했다. 기존 Player 씬의 `GravityManager`, Normal·World +X·World -X 프리셋, Shift·Inversion 트리거 연결, 두 `GravityBody` 테스트 오브젝트와 Respawn Player 참조는 새 레벨 위에 보존했다.
- 해결한 문제: Git 병합 커밋이 `Original`과 `_Player`를 각각 한쪽 부모 버전으로 유지해 최신 레벨과 최신 중력이 서로 다른 씬에 나뉜 상태를 해소했다. 아직 구현 대상이 아닌 FastDown·Slow·ZeroGravity 트리거는 비활성 상태로 유지했고, 테스트 큐브가 삭제된 외부 Material GUID를 참조하던 항목은 기본 Material로 정리했다.
- 사람이 직접 결정한 부분: 팀 측에서 `_Player` 변경을 `Original`에 수동 통합하지 않는 현재 작업 방식에서는 플레이어·중력 담당자가 최신 팀 씬을 받아 통합하고, `dev` 반영 시점에 최종 Original 반영까지 책임지기로 했다. 이번 단계에서는 `Original`을 직접 수정하지 않고 우리 통합 씬 갱신까지만 수행했다.
- 검증 결과: Player 씬은 Unity YAML 문서 1,508개에서 중복 ID 0건, 미해결 로컬 참조 0건이며 Original과 동일한 MeshCollider 316개, Zone Entry Trigger 4개, 바리게이트 4개, HUD·NPC 참조를 가진다. Unity Editor가 변경 씬을 실제 임포트·로드했고 최근 로그에 씬 역직렬화 오류·Missing Script·NullReferenceException이 없었다. `Assembly-CSharp-Editor.csproj` 빌드는 기존 경고 28건과 함께 오류 0건으로 성공했다. `Original_GamePlayScene`, 두 씬 meta, ProjectSettings와 Build Settings에는 diff가 없으며 실제 Zone 진행·중력 전환·Entry Point 충돌은 Play Mode 수동 확인이 남아 있다.

## 2026-08-25 — 중력 Preset·주기 전환·무중력·리스폰 코어 시스템 완료

- Codex 사용처: Zone별 중력 효과 확정과 Trigger 연결을 미룬 상태에서 재사용 가능한 중력 코어를 먼저 완성하기 위해, Preset 데이터·단일 주기 실행·무중력 상태·리스폰 복구의 책임을 구현하고 자동 Play Mode 회귀 검증에 사용했다.
- 구현하거나 정리한 기능: `GravityPreset`을 `Fixed`, `Periodic`, `ZeroGravity` 모드로 확장하고 방향 목록·변경 간격·예고 시간을 데이터화했다. `GravityManager`는 하나의 Periodic 실행과 예고 상태를 소유하며 중복 적용, Preset 변경과 비활성화 시 수명을 정리한다. Player는 무중력 진입 때 선속도·각속도를 한 번 초기화한 뒤 관성을 허용하고, 리스폰은 현재 Preset을 즉시 복원해 Player Up을 `PresentationUp`에 맞춘다. `GamePlayScene_Player`에는 Trigger와 연결하지 않은 Periodic X축·Zero Gravity 테스트 Preset을 추가했다.
- 해결한 문제: Reverse Gravity를 특정 Zone과 하드코딩하지 않고 모든 주기형 중력에 사용할 수 있는 Preset으로 분리했다. 같은 Periodic Preset 재적용으로 타이머가 초기화되거나 다른 Preset 이후 이전 Coroutine이 재발하는 문제를 막았으며, 무중력에서 매 프레임 속도를 0으로 덮어 후속 이동을 차단하지 않게 했다. 리스폰은 미확정 `GameFlowState → GravityPreset` 매핑 대신 실제 현재 Preset을 복구해 고정형·주기형·무중력을 같은 경로로 처리한다.
- 사람이 직접 결정한 부분: 각 기획 Zone과 Trigger에 어떤 중력을 연결할지는 레벨 설계가 확정될 때까지 미루고, 코어 시스템의 Inspector 기반 검증을 이번 완료선으로 삼았다. 별도 빌드 검증 단계는 폐기했으며, 사용자가 Play Mode에서 현재 기능이 모두 정상 동작함을 확인했다. 그래플이 없는 무중력 이동 보완은 사격 반작용을 별도 계획으로 작성한 뒤 구현하기로 했다.
- 검증 결과: 자동 Play Mode에서 Periodic 방향 전환 14회, 예고 상태, 중복 적용 시 카운트다운 유지, 다른 Preset 적용 시 취소, Zero Gravity의 방향·`PresentationUp` 유지와 Player 상태 진입, `GravityBody` 관성·중력 복귀를 확인했다. Periodic·Zero Gravity 리스폰 후 현재 Preset과 Player 회전이 복구됐고 Player Up과 `PresentationUp`의 내적은 `1.0`이었다. 런타임·Editor 어셈블리 빌드는 기존 경고 28건과 함께 오류 0건, 최종 새 Play Mode Console 오류는 0건이었다. 사용자가 실제 Play Mode에서 전체 동작을 확인해 완료로 판단했다.

## 2026-08-25 — 무중력 무기 발사 반작용 구현

- Codex 사용처: 무중력 코어와 기존 사격 흐름을 대조해 발사 반작용의 책임 경계·속도 상한 규칙을 구현하고 컴파일·로그 검증과 완료 문서화를 수행했다.
- 구현하거나 정리한 기능: 실제 발사마다 최종 발사 방향 반대로 `ForceMode.VelocityChange` 반작용을 요청한다. `PlayerController`는 ZeroGravity 상태와 중력 전환 여부를 판정하고, 기본 발사당 속도 변화 `0.3`, 전체 속력 상한 `3.0`, 상한 초과 상태의 추가 가속 차단·감속 허용을 소유한다. 설정과 마지막 적용 여부·현재 속력·상한 도달 상태를 Inspector에서 확인할 수 있게 했다.
- 해결한 문제: 그래플이 없는 무중력 구간에서도 사격으로 이동·조향·제동할 수 있게 하면서 연사 무한 가속을 막았다. 명중·피해·피격 Rigidbody 밀기와 플레이어 반작용을 분리하고, 일반 중력이나 중력 전환 중에는 반작용을 거부하도록 했다.
- 사람이 직접 결정한 부분: 사격 반작용을 그래플 구현 후에도 무중력 보조 이동 수단으로 유지하고 일반 중력 반동·카메라 연출·무기 시스템 변경은 제외했다. 사용자가 Play Mode 테스트 성공을 확인하고 완료 처리를 승인했다.
- 검증 결과: 런타임 어셈블리는 오류 0건과 기존 경고 28건, Editor 어셈블리는 오류·경고 0건으로 빌드됐다. 최신 `Editor.log` 범위에 컴파일 실패·예외가 없었고, 사용자의 Play Mode 테스트에서 반작용 동작이 성공했으며 무중력 상태가 아닐 때 발동하는 버그는 발견되지 않았다. 이번 반작용 작업에서는 씬·Prefab·Collider·Packages·ProjectSettings·Build Settings를 변경하지 않았다.

## 2026-08-25 — 무중력 진입 관성 유지 및 반작용 최고 속도 조정

- Codex 사용처: 무중력 진입 시 잔류 속도를 제거하는 상태 전이와 반작용 속도 상한 경로를 대조하고, 관성을 보존하는 최소 변경을 적용했다.
- 구현하거나 정리한 기능: `PlayerController.EnterZeroGravity()`의 선속도·각속도 초기화를 제거했다. `maxZeroGravityRecoilSpeed` 기본값은 `3.0`에서 `4.0`으로 올리고, C# 컴파일 오류를 내던 `4.f` 표기를 `4f`로 정정했다. 발사당 속도 변화 `0.3`, 반작용 방향, 상한 초과 시 감속 방향만 허용하는 기존 계약은 유지했다.
- 해결한 문제: `GravityManager` 전환 완료 뒤 첫 물리 프레임의 `ZeroGravityMotionState.Enter()`가 `EnterZeroGravity()`를 한 번 호출해 선속도·각속도를 0으로 만드는 경로를 제거했다. 이후 `ZeroGravityMotionState.FixedTick()`은 속도를 덮어쓰지 않으므로 진입 전 이동·낙하 및 사격 반작용 관성이 유지된다.
- 검증 결과: `Assembly-CSharp.csproj` 빌드는 오류 0건으로 성공했고 기존 경고 28건만 남았다. `git diff --check`도 통과했다. Console을 비운 새 Play Mode 실행·종료에서 신규 Error는 0건이었다. 자동 입력 도구로는 이동·낙하 중 무중력 진입 뒤 관성을 체감할 수 없어, 해당 Inspector·조작 확인은 사용자의 Play Mode 확인이 남아 있다. 씬·Prefab·Collider·Packages·ProjectSettings·Build Settings는 변경하지 않았다.

## 2026-08-25 — MeshCollider 지형 접지 이동 안정화

- Codex 사용처: MeshCollider 언덕·굴곡·턱에서 발생한 비의도 상승, 걸림과 순간 Airborne 진동을 현재 Rigidbody 이동·SphereCast 접지 코드에 대조하고, 만족할 때 즉시 중단하는 순차 계획으로 원인별 최소 보정을 구현·검증하는 데 사용했다.
- 구현하거나 정리한 기능: Grounded 이동이 이전 충돌에서 남은 중력축 속도를 다시 합치지 않게 하면서 지면 접선 이동은 유지했다. 접지 쿼리는 캡슐 하단 기준 지면 거리를 반환하도록 확장하고, 기본 Probe `0.15`를 놓친 경우 직전 상태가 Grounded이며 위쪽 속도가 `0.5` 이하일 때만 거리 `0.3`, 최대 속도 `5`의 중력 방향 Ground Snap을 적용했다. 접지·지면 각도·거리·보정 전 수직 속도·점프·Snap 작동 여부를 Inspector 런타임 값으로 추가했다.
- 해결한 문제: 경사로와 턱에서 점프 입력 없이 튀거나 걸리는 현상을 Grounded 속도 계약 수정으로 약 90% 억제하고, 울퉁불퉁한 지형에서 접지가 짧게 끊겨 낙하와 재충돌을 반복하던 떨림은 제한적 Ground Snap으로 보완했다. 실제 점프 프레임, 무중력과 중력 전환 중에는 Snap이 개입하지 않도록 기존 상태 책임을 유지했다.
- 사람이 직접 결정한 부분: 순번 1 결과를 유지하고 마찰성 걸림이 거의 사라져 Physics Material 조정은 건너뛰었다. 순번 3 적용 후 현재 지형 이동이 만족할 만하다고 직접 확인해 노멀 안정화, Step Assist와 충돌 메시 개선 요청은 미실행 상태로 남기고 작업 완료를 승인했다.
- 검증 결과: Unity `6000.3.20f1` 재컴파일은 오류 0건으로 완료됐고, 새 Play Mode에서 Ground Snap 설정과 런타임 지면 관찰값 갱신, 신규 Console Error 0건을 확인했다. 사용자가 실제 지형 이동을 다시 테스트해 최종 조작감이 만족 기준을 충족한다고 판정했다. 이번 작업에서는 `PlayerController.cs`와 계획·기록 문서만 변경했으며 `Original_GamePlayScene`, 지형 Collider, Prefab, Packages, ProjectSettings와 Build Settings는 변경하지 않았다.

## 2026-08-25 — TPS 카메라 구도 프리셋·숄더 뷰 구현

- Codex 사용처: 기존 3인칭 카메라의 중앙 구도를 보존하면서 Inspector에서 비교 가능한 숄더 뷰 프리셋과 충돌 경로를 구현·검증하는 데 사용했다.
- 구현하거나 정리한 기능: `ThirdPersonCameraController`에 `Centered`와 `ShoulderGameplay` 구도 프리셋을 추가했다. 각 프리셋은 Pivot 높이, 카메라 로컬 오프셋, 기본 거리, FOV를 소유하고, 기본 `ShoulderGameplay`는 오른쪽 `0.35m` 오프셋을 사용한다. 카메라 충돌 검사는 Pivot에서 최종 숄더 희망 위치까지 SphereCast하며, 프리팹에는 사용자 줌 범위 `0.6 ~ 2.5`를 저장했다.
- 해결한 문제: 기존 중심축 뒤쪽만 검사하던 충돌 경로를 실제 숄더 카메라 경로와 일치시켜, 구도 프리셋과 충돌 회피가 서로 다른 위치를 기준으로 동작하지 않게 했다.
- 사람이 직접 결정한 부분: 사용자가 Play Mode에서 최대 거리를 `2.5`로 조정하고 현재 구도가 적절하다고 확인했다. 좌우 어깨 전환, 자동 전환, 피치 연동 오프셋과 플레이어 디더 페이드는 후속 항목으로 남겼다.
- 검증 결과: Unity 재컴파일은 오류 0건으로 완료됐고, 새 Play Mode 시작 뒤 Console error 0건을 확인했다. 사용자가 실제 Play Mode에서 작업 성공을 확인했다. `Original_GamePlayScene`, Collider, Packages, ProjectSettings와 Build Settings는 이번 작업에서 수정하지 않았다.

## 2026-08-25 — GravitySystem 프리팹화 및 GameFlow Preset 디버그

- Codex 사용처: 중력 운영 구성의 재사용 경계를 정리하고, 기존 GravityManager의 Preset 선택 경로를 GameFlowManager Inspector에서도 같은 런타임 API로 사용할 수 있게 구현·검증했다.
- 구현하거나 정리한 기능: `/GamePlay/GravitySystem`에서 두 `GravityTestBody`를 제거하고 `GravityState`, `GravityManager`, 운영 Preset 자식을 포함한 `GravitySystem.prefab`을 생성했다. `GravityPresetSceneSelector` Editor helper로 두 Inspector의 hierarchy-path Preset 목록과 session-only 선택을 공유하고, Odin GameFlowManager Inspector 맨 아래에 Play Mode용 Preset 선택·적용·현재 Preset 표시를 추가했다.
- 해결한 문제: 중력 구성과 테스트 바디가 같은 씬 루트에 섞인 상태를 분리했고, GameFlow 디버그 중 Preset을 시험하려면 별도 GravitySystem Inspector로 이동해야 하던 흐름을 제거했다. GameFlowManager의 선택 적용은 `GravityManager.ApplyPreset()`만 호출해 Player·Camera·Periodic 처리 경로를 우회하지 않는다.
- 사람이 직접 결정한 부분: 임의 Preset 적용은 진행 Trigger가 아니므로 `CurrentZone`과 `CurrentState`를 바꾸지 않고, 선택값도 씬/Prefab override가 아닌 Editor session 값으로 유지했다. 실제 Inspector 조작감과 Trigger 통과 뒤 연속 진행은 후속 Play Mode에서 확인한다.
- 검증 결과: Unity 재컴파일은 `failed=false`, 오류 0건이었다. 새 Play Mode에서 GameFlowManager API로 Periodic Z, Normal, Zero Gravity Preset을 순서대로 적용해 CurrentPreset, Periodic routine 시작·정리와 strength `9.81`/`0`을 확인했다. Periodic 적용 전후 `CurrentZone = Zone01_Entry`, `CurrentState = Entry`가 유지됐고, 새 Console error는 없었다. `Original_GamePlayScene`, Collider, Packages, ProjectSettings와 Build Settings는 변경하지 않았다.

## 2026-08-25 — 플레이어 발소리 Animation Event 구현·필터 재설계

- Codex 사용처: 외부 원본 애니메이션을 보호하면서 Player 소유 이동 Clip을 추출하고, 발 접지 Animation Event·오디오 구성·이동 의도 필터를 구현한 뒤 오재생과 누락 원인을 반복 진단하는 데 사용했다.
- 구현하거나 정리한 기능: 네 방향 Locomotion과 Sprint 편집용 Clip에 동기화된 `PlayFootstep(AnimationEvent)` 이벤트를 배치하고 Animator 참조를 교체했다. `PlayerFootsteps`는 `PlayerController.HasMoveIntent`, 이벤트 발신 Clip의 유효 Blend Weight, 짧은 중복 방지 간격을 통과한 호출에만 다섯 발소리 중 직전과 다른 Clip을 골라 재생한다.
- 해결한 문제: Grounded 단독 조건 때문에 Idle·경사 미끄러짐에서도 소리가 나던 문제, 착지 직후 Grounded 갱신 지연으로 정상 이벤트가 누락되던 문제, 최고 Weight 대표 Clip 하나만 허용해 방향 Blend와 걷기↔Sprint 전환 중 무음이 발생하던 문제를 제거했다. 방향별 이동 Clip의 이벤트 위상을 통일해 Blend 중 리듬 혼합도 줄였다.
- 사람이 직접 결정한 부분: Rigidbody 속도와 Grounded를 발소리 조건으로 쓰지 않고 플레이어 입력 의도와 실제 재생 중인 애니메이션 이벤트를 기준으로 삼았다. 이동 Clip 전체를 허용하되 Blend Weight `0.01`과 `minimumInterval`을 최종 중복 안전장치로 사용하는 방식을 선택하고, Play Mode에서 정상 동작을 확인해 완료를 승인했다.
- 검증 결과: Unity 재컴파일은 오류 0건이었고 새 Play Mode Console 오류가 없었으며 `git diff --check`를 통과했다. 이벤트 시점은 방향 이동 `0.1 / 0.5166667`, Sprint `0.0333 / 0.3666667 / 0.7 / 1.0333`으로 확인했다. 사용자가 Play Mode에서 경사 Idle 오재생과 걷기·달리기 중 무음이 사라지고 발소리가 정상 재생됨을 확인했다. 외부 Toon Soldiers 원본, `GamePlayScene_Player`, Collider, Packages, ProjectSettings와 Build Settings는 이번 작업에서 변경하지 않았다.

## 2026-08-25 — 플레이어 SFX Mixer·동굴 리버브·사격음 통합

- Codex 사용처: 기존 Player 발소리 AudioSource와 0.15초 사격 확정 경로, GameFlow Zone 변경 이벤트를 대조하고 Player 전용 SFX 라우팅·동굴 음향 전환·사격음 재생을 통합했다.
- 구현하거나 정리한 기능: `SinkPointSfx` AudioMixer에 `Master > SFX > Player` 경로와 `Entry`·`Cave` Snapshot, SFX Reverb·Compressor 체인을 추가했다. Player 루트의 발소리·사격 AudioSource를 `Player` 그룹으로 라우팅했고, 사격 소스는 `AutoGun_3p_01.wav`, Volume `0.7`, Play On Awake 해제로 구성했다. `PlayerCombatController`는 실제 `FireShot()` 확정 뒤 기존 직접 재생을 중지하고 새 사격음을 재생한다. `AudioEnvironmentController`는 `CurrentZoneChanged`를 구독해 Entry와 동굴 Zone 02~05 사이의 Snapshot 및 Player 리버브 레벨을 0.4초 동안 전환한다.
- 해결한 문제: 발소리와 연사음을 한 Source에서 섞어 볼륨·재생을 간섭시키지 않고, 0.92초 사격 Clip이 0.15초 연사에서 직접 중첩되어 커지는 문제를 한 사격 보이스 재시작 방식으로 제한했다. 동굴 구역 전환도 플레이어 전투 코드에 Zone 조건을 넣지 않고 GameFlow 오디오 컨트롤러로 분리했다.
- 사람이 직접 결정한 부분: Player 효과음만 공통 SFX에 먼저 연결하고, Entry는 건조하게 유지하며 Zone 02~05는 강한 동굴 울림으로 전환하도록 정했다. 초기 사격음은 `AutoGun_3p_01.wav`와 70% 볼륨을 선택했으며, 리버브·컴프레서 세부 청감 수치는 Mixer/Inspector에서 후속 조정한다.
- 검증 결과: Unity 재컴파일은 오류 0건이었다. 새 Play Mode에서 Entry 리버브 레벨 `-10000`과 Zone 02 전환 뒤 강한 Cave 레벨 `+600`, 반사음 `0`, 감쇠 시간 `2.4초`를 확인했고, 사격 소스가 `AutoGun_3p_01`, Volume `0.7`으로 재생 상태가 되는 것을 확인했다. 새 Console Error는 0건이었다. 실제 연사 청감과 동굴 울림 강도는 사용자의 Play Mode 청감 확인이 남아 있다. `Original_GamePlayScene`, Collider, Packages, ProjectSettings와 Build Settings는 변경하지 않았다.

## 2026-08-25 — AudioSystem Prefab 기반 환경음 구조 전환

- Codex 사용처: 기존 `GameFlowManager` 부착 환경음 컨트롤러를 재사용 가능한 Prefab으로 분리하고, 공통 Mixer 정책과 씬별 참조의 소유 경계를 구현·검증했다.
- 구현하거나 정리한 기능: `AudioSystem.prefab`에 Entry/Cave Snapshot, 전환 시간 `0.4`, Entry/Cave Reverb 목표값(`-10000`/`300`)을 가진 `AudioEnvironmentController`와 `AudioSystemSceneBindings`를 추가했다. 바인더는 `Awake`에서 해당 씬의 `GameFlowManager`와 Player `AudioReverbFilter`만 `Configure` API로 전달하고, 컨트롤러는 `Start` 이후 유효한 바인딩 하나에만 구독·초기 Zone 적용을 수행한다.
- 해결한 문제: Prefab Asset 안에 씬 오브젝트 참조가 저장되는 문제를 막고, 재바인딩·비활성화 때 기존 Zone 이벤트를 정확히 해제해 중복 구독을 방지했다. 누락된 GameFlowManager, Snapshot 또는 Player Filter는 어떤 참조가 비었는지 오류로 남기고 AudioSystem만 비활성화한다.
- 검증 결과: Unity 재컴파일은 오류 0건이었다. Prefab Asset의 바인더 두 참조가 비어 있고 Player 씬 인스턴스에만 대상 참조가 저장된 것을 확인했다. 새 Play Mode에서 Entry 초기값 `-10000`, `Zone02_Normal` 전환 뒤 `300`, Entry 복귀 뒤 `-10000`을 확인했고 AudioEnvironmentController는 하나뿐이며 새 Console Error는 없었다. `Original_GamePlayScene`, Collider, Packages, ProjectSettings, Build Settings는 변경하지 않았다. 실제 발소리·사격음 청감과 Zone 03~05 실제 트리거 통과는 사용자 Play Mode 확인이 남아 있다.

## 2026-08-25 — 플레이어 단일 탄창·수동 및 자동 리로드 구현

- Codex 사용처: 기존 연사 확정 경로와 Upper Body Animator 구조를 대조해 최소 장탄 상태, 수동·자동 리로드와 자세별 애니메이션을 구현하고 검증하는 데 사용했다.
- 구현하거나 정리한 기능: 기본 30발 단일 탄창에서 실제 발사마다 한 발을 소비하고, 일부 장탄에서는 `R` 수동 리로드, 0발에서는 자동 리로드를 시작한다. 리로드 중 사격을 차단하고 약 `3.67초` 뒤 30발로 충전한다. Animator에는 `Reload` Trigger와 `Standing Reload`·`Crouch Reload` State를 추가해 현재 자세에 맞는 상체 클립을 재생한다.
- 해결한 문제: 탄약 아이템과 예비탄 시스템 없이도 재장전 입력과 연출이 실제 발사 상태에 연결되도록 했다. `PlayerCombatController`가 장탄·리로드 상태를 단독 소유하고 `PlayerAnimationController`는 시작 Trigger만 전달해 전투 규칙과 표현 책임을 분리했다.
- 사람이 직접 결정한 부분: 탄약 아이템·예비 탄약 수량·HUD는 초기 리로드 범위에서 제외하고 예비탄은 무한으로 간주했다. 사용자가 Play Mode에서 장탄 감소, 수동·자동 리로드와 자세별 애니메이션이 정상 동작함을 확인해 완료로 결정했다.
- 검증 결과: Unity 재컴파일은 오류 0건이었다. 자동 런타임 검증에서 시작 장탄 `30/30`, 마지막 발사 뒤 `0`과 자동 리로드 진입, 완료 뒤 `30` 복구, 서기·웅크리기 Animator 전환과 신규 Console Error 0건을 확인했다. 사용자가 실제 Play Mode 조작에서도 정상 동작을 확인했다. `Original_GamePlayScene`, Collider, Packages, ProjectSettings와 Build Settings는 변경하지 않았다.

## 2026-08-25 — 플레이어 리로드 SFX 연결

- Codex 사용처: 리로드 상태 소유 경로와 기존 Player SFX Mixer 구성을 대조해 수동·자동 리로드가 공통으로 사용하는 효과음 재생 경로를 추가했다.
- 구현하거나 정리한 기능: `PlayerCombatController`에 리로드 전용 `AudioSource` 참조와 재생 함수를 추가하고, 실제 `StartReload()`이 성립할 때 `squarebun-m4a1-reload-sound-316890.mp3`를 한 번 재생하도록 연결했다. Player Prefab에는 사격·발소리와 간섭하지 않는 별도 Source를 추가해 `SinkPointSfx`의 Player 그룹으로 라우팅했으며 Play On Awake와 Loop는 끄고 오디오 데이터 사전 로드를 켰다. Controller 비활성화로 리로드가 취소되면 재생도 중지한다.
- 검증 결과: `Assembly-CSharp.csproj` 빌드와 Unity 재컴파일은 오류 0건으로 통과했다. Unity가 MP3를 `Decompress On Load`로 임포트하고 Prefab 및 실제 Play Mode Player의 `reloadAudioSource` 참조, AudioClip, Player Mixer 그룹을 정상 해석했으며 새 Play Mode Console Error는 0건이었다. 수동·자동 리로드의 실제 애니메이션 대비 음향 타이밍과 볼륨 청감은 사용자 확인이 남아 있다. `Original_GamePlayScene`, Collider, Packages, ProjectSettings와 Build Settings는 변경하지 않았다.

## 2026-08-25 — 플레이어 Muzzle Flash 및 게임플레이 Tracer 구성

- Codex 사용처: XR Interaction Starter Kit의 Muzzle Flash 후보 구조와 기존 hitscan·LineRenderer 흐름을 대조해 Player Prefab 단일 슬롯, 런타임 재사용과 게임플레이 Tracer 표현을 구현·검증했다.
- 구현하거나 정리한 기능: Player Prefab의 `Muzzle` 아래에 2배 스케일 `MuzzleVfxAnchor`를 추가하고 기본 `RifleFlash`를 등록했다. `PlayerCombatController`는 VFX를 한 번 생성해 ParticleSystem 5개를 재사용하고 XR 원본의 `DestroyAfterTime`은 런타임 복제본에서만 비활성화한다. 기존 Tracer는 기본 ON 상태와 `#FFB52E` 색으로 통일하고 표시 중 시작점이 현재 Anchor를 따라가게 했다.
- 해결한 문제: 연사마다 VFX를 생성·파괴하지 않고 실제 발사와 Flash·Tracer를 같은 경로로 연결했다. XR 데모 Prefab의 `0.1초` 자동 파괴와 데모 루트 Transform을 Player 런타임에서 격리했으며, 작은 RifleFlash는 원본 수정 없이 Anchor 스케일로 확대했다.
- 사람이 직접 결정한 부분: 사용자가 Play Mode에서 기본 발사 연동이 정상임을 확인하고 RifleFlash 2배 크기가 충분하다고 확정했다. 큰 상하 피치의 카메라–총구 근거리 시차는 단순 VFX 조정 범위를 넘어가므로 별도 방향성·Tracer 정책 계획으로 분리한 뒤 기존 계획을 완료 처리하도록 승인했다.
- 검증 결과: Unity 재컴파일은 오류 0건이었다. 실제 `FireShot()` 호출에서 탄약 감소, Flash와 Tracer 활성화, 주황색 적용과 시간 종료 뒤 재사용 상태를 확인했고 새 Console Error는 없었다. Pistol·Rifle·TerraFormer 후보는 모두 ParticleSystem과 `DestroyAfterTime` 5개 구조로 공통 제어가 가능했다. `Original_GamePlayScene`, Collider, XR 원본 VFX, Packages, ProjectSettings와 Build Settings는 이번 작업에서 수정하지 않았다.

## 2026-08-26 — 최신 Original 기준 Player 씬 재동기화

- Codex 사용처: 최신 Original과 Player 씬의 Unity YAML·Git 이력·Prefab 인스턴스 참조를 비교하고, Original을 최종 환경 정본으로 삼아 Player 씬을 Unity Editor Save As Copy 방식으로 재구성한 뒤 정적·컴파일·Play Mode 검증을 수행했다.
- 구현하거나 정리한 기능: `GamePlayScene_Player`에 최신 Fog와 Ambient 설정, Global Volume, Entry 포인트 라이트 3개, 암석·Waypoints, Zone05·Aeropod와 최신 중력 트리거 배치·활성 상태를 모두 반영했다. 두 씬의 유일한 의도적 차이는 Player 씬의 `RespawnController.playerRoot`가 `/GamePlay/Player`를 참조하는 것이다.
- 해결한 문제: 공유 Material·Camera Prefab 변경과 씬 로컬 렌더링·배치 변경을 구분하고, 과거 Player 씬의 중복·오래된 override를 다시 이식하지 않아 Original과의 불필요한 차이를 제거했다. Original 씬과 두 씬 meta GUID는 보존했다.
- 사람이 직접 결정한 부분: Original을 환경·배치·진행 연결의 최종 기준으로 사용하고, Player 씬 고유 차이는 실행에 필요한 리스폰 참조만 허용하도록 결정했다. Collider는 Original 값을 그대로 동기화하되 별도 제작·튜닝하지 않았고 WebGL 빌드는 실행하지 않았다.
- 검증 결과: 두 씬은 각각 YAML 문서 1,529개, 중복 ID 0개, 미해결 로컬 참조 0개이며 객체 단위 차이는 GameFlowManager Prefab instance의 `playerRoot` 한 줄뿐이다. Unity에서 Player·Camera·Gravity·Audio·Respawn 참조, Global Volume과 조명 3개, Zone05·Aeropod를 재조회했다. `Assembly-CSharp-Editor.csproj` 빌드는 오류 0개와 기존 경고 43개로 통과했고, 새 Play Mode에서 정상 화면과 초기 중력 `(0, -1, 0) × 9.81`, GameFlow·Audio·Respawn 연결, Console Error 0건을 확인했다. 실제 이동·발사·리스폰, 구역 순회와 Shift/Inversion/Periodic/Zero Gravity 체감 검증은 사용자 Play Mode 확인이 남아 있다.

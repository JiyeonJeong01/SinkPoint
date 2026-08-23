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

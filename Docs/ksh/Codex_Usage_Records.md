# Codex 활용 기록

이 문서는 SinkPoint 플레이어·중력 파트에서 Codex를 어떻게 활용했는지 제출과 팀 공유에 필요한 근거만 간결하게 남긴다.

## 기록 원칙

- 기능 구현, 중요한 문제 해결, 기술 방향·책임 경계 결정, 실제 실행 검증처럼 이후 작업이나 팀 공유에 의미 있는 완료 작업 단위마다 한 항목을 추가한다.
- 무엇을 Codex에 맡겼는지와 사람이 직접 결정한 부분을 분리한다.
- 성공 여부와 확인하지 못한 항목을 사실대로 적는다.
- 원문 프롬프트, 비밀정보, 개인 정보와 사소한 탐색 과정은 기록하지 않는다.
- 단순 경로 이동·이름 변경·오탈자 수정, 반복 확인과 중간 탐색처럼 독립적으로 남길 가치가 작은 작업은 기록하지 않는다.

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

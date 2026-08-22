# Codex 활용 기록

이 문서는 SinkPoint 플레이어·중력 파트에서 Codex를 어떻게 활용했는지 제출과 팀 공유에 필요한 근거만 간결하게 남긴다.

## 기록 원칙

- 기능 구현, 문제 해결, 중요한 판단 또는 검증이 완료된 작업 단위마다 한 항목을 추가한다.
- 무엇을 Codex에 맡겼는지와 사람이 직접 결정한 부분을 분리한다.
- 성공 여부와 확인하지 못한 항목을 사실대로 적는다.
- 원문 프롬프트, 비밀정보, 개인 정보와 사소한 탐색 과정은 기록하지 않는다.
- 관련 커밋이나 PR이 생기면 링크 또는 식별자를 추가한다.

## 기록 형식

```markdown
## YYYY-MM-DD — 작업 이름

- Codex 사용처:
- 구현하거나 정리한 기능:
- 해결한 문제:
- 사람이 직접 결정한 부분:
- 검증 결과:
- GitHub 참조:
```

## 2026-08-22 — 플레이어·중력 파트 문서 기반 구성

- Codex 사용처: 기획서, 저장소 구조, 기존 입력 코드, 씬 상태와 제출 기준을 조사하고 개인 지침·마스터 계획·활용 기록의 역할을 분리했다.
- 구현하거나 정리한 기능: 루트 개인용 `AGENTS.md` 규칙과 `Docs/ksh` 아래 팀 공유 문서 구조를 구성하고, 4일 MVP의 담당 범위·기술 방향·단계·완료 기준을 문서화했다.
- 해결한 문제: 아직 구현이 거의 없는 상태에서 팀원이 우리 파트의 방향을 알기 어렵고, 개인 Codex 지침과 GitHub 공유 문서가 섞일 수 있는 문제를 분리했다. 기획서의 입력 경로와 실제 코드 경로가 다른 점, Original 씬과 담당 씬의 충돌 가능성도 문서에 반영했다.
- 사람이 직접 결정한 부분: 방향성 중심의 살아 있는 마스터 계획, Rigidbody 기반 사용자 정의 중력, 팀장의 BoxCollider 사용, Collider 인계 후 `GamePlayScene_Player` 생성, `feat/player-gravity` 브랜치, 개인용 AGENTS와 추적 가능한 팀 문서의 분리를 선택했다.
- 검증 결과: `feat/player-gravity` 브랜치에서 AGENTS의 로컬 제외, 참조 경로, UTF-8 인코딩, Git 상태와 Markdown 공백 검사를 확인했다. 기존 기획서·코드·씬은 변경하지 않았다.
- GitHub 참조: 아직 커밋 또는 PR 없음.

## 2026-08-22 — 문서 경로 재구성

- Codex 사용처: 사용자가 이동한 기획서의 실제 위치와 기존 문서 참조를 확인하고, 담당 문서 폴더 이동에 따라 관련 경로를 일괄 갱신했다.
- 구현하거나 정리한 기능: 기획서 기준 경로를 `Docs/GameDesign_MVP.md`로 변경하고, 플레이어·중력 문서를 `Docs/ksh` 아래로 이동했다.
- 해결한 문제: AGENTS와 마스터 계획서가 이전 기획서 및 담당 문서 경로를 가리켜 후속 작업에서 잘못된 문서를 읽을 수 있는 문제를 제거했다.
- 사람이 직접 결정한 부분: 기획서는 `Docs/GameDesign_MVP.md`, 담당 문서는 `Docs/ksh`에서 관리하기로 결정했다.
- 검증 결과: 새 파일 위치와 내부 링크가 유효하고, 문서에 이전 경로 참조가 남지 않았으며, Git 상태에는 기획서 이동과 `Docs/ksh`의 두 문서만 반영된 것을 확인했다.
- GitHub 참조: 아직 커밋 또는 PR 없음.

## 2026-08-23 — Unity Pipeline·Codex MCP 선행 도입

- Codex 사용처: Unity CLI와 Pipeline의 실제 설치 상태를 진단하고, Pipeline 패키지 설치·개인 Codex MCP 등록·Editor 읽기 도구 검증·Original 씬 Collider 구조 집계를 수행했다.
- 구현하거나 정리한 기능: 프로젝트에 `com.unity.pipeline` `0.5.0-exp.1`을 추가하고 개인 Codex 설정에 SinkPoint용 Unity MCP를 등록했다. 검증된 설치·연결·안전 경계·문제 해결 절차를 `Docs/ksh/Codex_Unity_Setup_Guide.md`에 정리했다.
- 해결한 문제: 샌드박스가 Git LFS 임시 객체와 localhost Pipeline 서버 접근을 막아 상태가 실패하는 문제를 실제 패키지나 서버 오류와 분리했다. manifest 변경 후 Editor가 종료돼 패키지가 미해석된 상태도 Editor 재실행으로 해결했다.
- 사람이 직접 결정한 부분: 첫 공유 문서는 `Docs/ksh` 내부에 유지하고, 연결과 안전한 실작업 1건을 공유 기준으로 삼았다. Original 씬은 읽기 전용으로 유지하고 Unity CLI skill, Build Settings, CI와 MCP 쓰기 시험은 이번 범위에서 제외했다.
- 검증 결과: Unity `6000.3.20f1`, CLI `1.0.0-beta.5`, Pipeline `0.5.0-exp.1`에서 Editor `ready`, Pipeline 서버 reachable, 142개 도구 노출을 확인했다. Original 씬은 dirty가 아니었고 Hierarchy 470개 오브젝트를 조회한 뒤 씬·Prefab·ProjectSettings diff가 없었다. 새 Codex 작업의 MCP 도구 노출은 재시작 후 확인이 남아 있다.
- GitHub 참조: 아직 커밋 또는 PR 없음.

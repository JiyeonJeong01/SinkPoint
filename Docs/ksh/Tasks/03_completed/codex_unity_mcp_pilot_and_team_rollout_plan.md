# Codex–Unity MCP 선행 도입 및 빠른 공유 실행 계획

문서 작성일: 2026-08-23  
작업 시작일: 2026-08-23  
작업 종료일: 2026-08-23  
현재 상태: 완료

## 목표

본인 환경에서 Unity Pipeline과 Codex MCP를 안전하게 연결하고, 실제 프로젝트 점검 1건에서 효과를 확인한 뒤 `Docs/ksh/Codex_Unity_Setup_Guide.md`로 재현 가능한 절차를 빠르게 공유한다.

## 범위

- Unity Pipeline 패키지의 기능 브랜치 설치와 영향 검토
- 개인 Codex MCP 연결
- Editor 연결 및 읽기 중심 smoke test
- `Original_GamePlayScene`의 Zone Collider 통합 준비 상태 점검
- 효과와 위험 측정
- 팀 공유용 최소 설치 가이드 작성
- 공용 설정과 개인 설정의 분리

## 하지 않을 것

- `.gitignore`, Tasks 운영 README, CI, Build Settings 수정
- `Original_GamePlayScene.unity` 또는 담당 밖 씬·Prefab 수정
- 게임 코드 public API, 입력 구조 또는 저장 형식 변경
- 팀원 개인 MCP 설정 생성
- Unity CLI skill 자동 설치
- push, PR 생성 또는 develop 직접 반영

## 실행 계획

1. Unity·CLI·모듈·패키지·Git 상태를 기록하고 Git LFS 오류를 해결해 변경 경계를 확정한다.
2. 기능 브랜치에서 Unity Pipeline 패키지를 설치하고 manifest/lock diff와 Unity 컴파일 상태를 확인한다.
3. dry-run을 검토한 뒤 개인 `~/.codex/config.toml`에 SinkPoint용 Unity MCP를 등록한다.
4. 프로젝트·씬·Console·도구 목록을 읽기 중심으로 조회하고 의도하지 않은 diff가 없는지 확인한다.
5. `Original_GamePlayScene`의 Zone별 Collider 계층과 주요 컴포넌트를 읽어 플레이어·중력 통합 준비 상태를 보고한다.
6. 설정 시간, 연결 성공률, 호출 실패, 수동 보정, 의도하지 않은 diff와 절약된 탐색 단계를 바탕으로 효과를 판정한다.
7. 연결과 실작업 1건에서 효과가 확인되면 `Docs/ksh/Codex_Unity_Setup_Guide.md`를 작성한다.
8. 공용 후보 변경과 개인 설정을 분리하고 `Docs/ksh/Codex_Usage_Records.md`에 결과를 기록한다.

## 검증 기준

- Pipeline 설치 후 Unity 프로젝트가 오류 없이 컴파일된다.
- `pipeline list`, `status`, `list`가 SinkPoint 연결 상태와 도구를 정상 반환한다.
- 새 Codex 작업에서도 Unity MCP 연결이 재현된다.
- Zone Collider 점검 결과가 씬 근거와 함께 작성된다.
- 도입 전후 diff에서 Original 씬과 담당 밖 파일 변경이 없다.
- 개인 절대 경로와 인증 정보가 팀 공유 문서나 Git 변경에 포함되지 않는다.
- 다른 AI 사용자가 자신의 경로만 바꿔 10~15분 안에 설치·연결을 재현할 수 있다.

## 결정 사항

- 공유 기준은 MCP 연결 성공과 안전한 실제 프로젝트 점검 1건에서의 효과 확인이다.
- 첫 공유 정본은 `Docs/ksh/Codex_Unity_Setup_Guide.md`에 둔다.
- AI를 사용하지 않는 팀원은 Unity CLI나 MCP를 별도로 설정하지 않는다.
- Unity CLI/Pipeline의 정확한 검증 버전을 기록하며 검증 없이 자동 업그레이드하지 않는다.
- MCP 쓰기 시험은 사용자 소유 통합 씬이나 별도 승인된 대상이 생길 때 후속 작업으로 수행한다.

## 현재 작업 결과

- Unity `6000.3.20f1`, Unity CLI `1.0.0-beta.5`, WebGL 모듈을 기준 상태로 확인했다.
- Git LFS 상태 확인 오류가 샌드박스의 `.git/lfs/tmp` 쓰기 제한 때문임을 확인하고 외부 읽기 검증으로 작업 전 상태를 확정했다.
- Unity 인증이 유효한 상태에서 `com.unity.pipeline` `0.5.0-exp.1`을 설치했다.
- Editor 재실행 후 Pipeline HTTP 서버가 포트 7800에서 시작됐고 Editor `ready`, 도구 142개 노출을 확인했다.
- 개인 Codex 설정에는 SinkPoint 절대 경로를 사용하는 `[mcp_servers.unity]` 항목만 추가했다.
- `Original_GamePlayScene`은 active·loaded·dirty false 상태였고, 읽기 조회 후 Git diff가 발생하지 않았다.
- MCP와 동일한 Pipeline 도구로 Hierarchy 470개 오브젝트를 집계했다.
- Zone01에는 `Zone_01_Entry_Collider` 아래 BoxCollider 43개와 환경 MeshCollider 52개가 있다.
- Zone02 기초 배치 커밋 `91e5331`은 별도 `Colliders` 루트와 BoxCollider 7개를 추가했다. 모두 trigger가 아니며 Zone02 공간의 바닥·벽 후보 형태로 배치돼 있다.
- Zone02 Collider 7개는 `/Environment/Zone_02_Normal` 아래가 아니라 일반 이름의 `/Environment/Colliders` 아래 있어, 이후 팀 통합 전에 Zone02 소유 그룹임을 명시하거나 계층 정책을 합의할 필요가 있다.
- Zone03~06에는 현재 BoxCollider가 없으며, 이후 통합 시 Collider 인계 범위를 다시 확인해야 한다.
- `/GamePlay/Player`, `/GamePlay/GravitySystem`, `/GamePlay/ZoneController`는 현재 Transform만 가진 자리표시자 상태여서 플레이어 Rigidbody·중력·구역 제어 통합은 아직 시작 전으로 확인됐다.
- Pipeline 도입 시 `System.Runtime.CompilerServices.Unsafe.dll` 중복 버전 경고가 있었지만 컴파일 오류는 없고 서버가 정상 시작됐다.
- 설치·연결·안전 경계와 대표 실패 대응을 `Docs/ksh/Codex_Unity_Setup_Guide.md`에 정리했다.
- 새 Codex 작업에서 MCP 도구 노출과 SinkPoint Editor 연결이 재현됐다.
- 승인된 쓰기 시험으로 `Original_GamePlayScene`을 새 GUID의 `GamePlayScene_Player`로 복제하고 활성 씬으로 열었다.
- 복사본은 원본과 동일한 SHA-256, 루트 7개, Hierarchy 470개였고 dirty false와 Console 오류 0건을 확인했다.
- 쓰기 검증 과정에서 생긴 `ProjectSettings.asset`의 요청 밖 변경을 발견해 작업 전 값으로 복구했다.

## 최종 효과 판정

- 새 작업에서 연결 재현, 실제 씬 조회와 승인된 복제가 모두 성공해 팀 공유 기준을 충족했다.
- Unity MCP는 씬·Hierarchy·Console 상태 조회뿐 아니라 AssetDatabase 기반 복제와 씬 활성화까지 담당 작업에 활용할 수 있다.
- 쓰기 명령은 예상 밖 ProjectSettings 변경 가능성을 포함하므로 작업 전후 Git diff 검사가 필수다.
- 설치·연결 절차와 읽기·쓰기 smoke test를 팀 가이드에 반영했으며, 추가 검증 항목은 없다.

# Codex–Unity 연동 가이드

검증일: 2026-08-23  
검증 상태: Unity Pipeline·CLI 연결 및 읽기 실작업 검증 완료, 새 Codex 작업의 MCP 도구 노출 확인 대기

## 목적

Unity Editor를 Unity CLI와 Codex에서 읽고 조작할 수 있도록 연결한다. 프로젝트 공용 패키지와 개인 설정을 분리하고, 씬 소유권과 Git 변경 경계를 우선한다.

## 검증된 환경

- Unity Editor: `6000.3.20f1`
- Unity CLI: `1.0.0-beta.5`
- Unity Pipeline: `0.5.0-exp.1`
- 빌드 모듈: WebGL 설치 확인
- 운영체제와 셸: Windows, PowerShell 7

Unity CLI와 Pipeline은 beta/experimental 단계이므로 검증 없이 버전을 올리지 않는다.

## 팀 공용과 개인 설정

| 구분 | 항목 | 공유 방식 |
| --- | --- | --- |
| 프로젝트 공용 | `com.unity.pipeline` 패키지 | `Packages/manifest.json`, `Packages/packages-lock.json`으로 공유 |
| 프로젝트 공용 | 작업 안전 규칙 | 루트 `AGENTS.md`와 담당 문서로 공유 |
| 개인 PC | Unity CLI 설치와 Unity 로그인 | 각 AI 사용자가 설정 |
| 개인 PC | Codex Unity MCP 등록 | 각 AI 사용자의 `~/.codex/config.toml`에 저장, Git 공유 금지 |
| 선택 | Unity CLI skill | 현재 미적용. 기존 `AGENTS.md`와 비교 후 별도 판단 |

AI를 사용하지 않는 팀원은 Unity CLI와 MCP를 설치할 필요가 없다. 프로젝트의 Pipeline 패키지가 정상적으로 해석되고 컴파일되는지만 확인하면 된다.

## 최초 프로젝트 설정

이 단계는 프로젝트에서 한 번 수행하고 패키지 변경을 검토한 뒤 공유한다.

```powershell
unity auth status
unity pipeline install --project-path "<프로젝트 경로>"
```

설치 후 Unity Editor를 열거나 다시 열고 패키지 해석과 컴파일이 끝날 때까지 기다린다.

```powershell
unity pipeline list --json
unity status --json
unity list --json
```

성공 기준:

- 프로젝트 경로와 Unity 버전이 현재 작업 환경과 일치한다.
- Pipeline 버전이 표시된다.
- 서버가 reachable이고 Editor 상태가 `ready`다.
- 사용 가능한 Unity 도구 목록이 반환된다.

## Codex 개인 연결

먼저 기록될 설정을 확인한다.

```powershell
unity mcp --project-path "<프로젝트 경로>" configure codex --dry-run
```

절대 경로 외에 토큰이나 예상하지 않은 설정이 없는지 확인한 뒤 적용한다.

```powershell
unity mcp --project-path "<프로젝트 경로>" configure codex
```

Codex는 열린 작업에 MCP 설정을 자동으로 다시 불러오지 않을 수 있다. 적용 후 Codex 앱을 재시작하거나 새 작업을 열고 Unity 도구 노출을 확인한다.

## 첫 읽기 검증

씬을 수정하기 전에 다음 읽기 명령으로 올바른 프로젝트에 연결됐는지 확인한다.

```powershell
unity command --project-path "<프로젝트 경로>" editor_status
unity command --project-path "<프로젝트 경로>" list_open_scenes
unity command --project-path "<프로젝트 경로>" get_console_logs --severity error --limit 100
unity command --project-path "<프로젝트 경로>" get_scene_hierarchy
```

확인 사항:

- `projectPath`와 `unityVersion`이 예상과 일치한다.
- 열린 씬의 `isDirty`가 작업 전 상태와 일치한다.
- Console에 새 컴파일 오류가 없다.
- 조회 후 씬·Prefab·ProjectSettings diff가 생기지 않는다.

## SinkPoint 안전 경계

- `Assets/_Scenes/Original_GamePlayScene.unity`는 읽기 전용으로 취급한다.
- 팀장 소유 씬에 생성·삭제·저장·컴포넌트 변경 명령을 실행하지 않는다.
- Build Settings는 팀 합의 전 변경하지 않는다.
- MCP 쓰기 작업은 사용자 소유 씬이나 명시적으로 승인된 대상에서만 수행한다.
- 변경 전후 Git 상태와 대상 diff를 비교한다.
- `clear_console`, bake, build target 전환처럼 상태를 바꾸는 명령은 읽기 검증에 사용하지 않는다.

## 확인된 문제와 대응

### Pipeline 패키지는 보이지만 서버가 reachable이 아님

1. Unity Editor 프로세스와 프로젝트 경로를 확인한다.
2. manifest만 변경되고 lock 파일과 PackageCache가 갱신되지 않았다면 Editor를 다시 연다.
3. Editor 로그에서 `Start HTTP server`와 포트를 확인한다.
4. Codex의 샌드박스가 localhost 접근을 막으면 해당 읽기 명령만 승인된 외부 실행으로 재검증한다.

### Codex 설정 후 현재 작업에 도구가 나타나지 않음

현재 작업은 시작 시점의 MCP 목록을 유지할 수 있다. Codex를 재시작하거나 새 작업을 열어 확인한다.

### Git LFS가 `.git/lfs/tmp` 접근 거부로 상태 확인에 실패함

샌드박스가 Git LFS 임시 객체 쓰기를 막을 수 있다. `git status` 읽기 검증을 승인된 외부 실행으로 다시 수행하고, 오류를 무시한 채 패키지 설치를 진행하지 않는다.

### `System.Runtime.CompilerServices.Unsafe.dll` 중복 경고

Pipeline `0.5.0-exp.1` 도입 시 서로 다른 버전의 중복 경고가 관찰됐다. Unity가 사용할 어셈블리를 선택해 컴파일과 서버 시작은 성공했지만, 버전 변경 시 재확인한다.

## 현재 검증 결과

- Pipeline 서버가 localhost 포트 7800에서 시작됐다.
- SinkPoint Editor는 `ready` 상태였고 142개 도구가 노출됐다.
- `Original_GamePlayScene`의 Hierarchy 470개 오브젝트를 읽기 전용으로 조회했다.
- 조회 후 Original 씬, Prefab과 ProjectSettings 변경은 없었다.
- 새 Codex 작업에서의 MCP 도구 노출은 다음 작업에서 최종 확인한다.


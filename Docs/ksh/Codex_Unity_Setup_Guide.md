# Codex–Unity MCP 연결 가이드

검증일: 2026-08-23  
검증 상태: SinkPoint 프로젝트에서 Codex MCP 등록과 Unity Editor 읽기 도구 연결 확인 완료

## 목적

Codex에 SinkPoint용 Unity MCP를 등록하고, 올바른 Unity Editor에 연결됐는지 확인하기 위한 가이드다.

프로젝트의 Unity Pipeline 패키지와 관련 설정은 이미 저장소에 반영돼 있다고 가정한다. 별도 `AGENTS.md`, 빌드 모듈이나 팀원별 Codex 운영 규칙은 MCP 연결의 필수 조건이 아니다.

굳이 한 단계씩 진행하지 않고, Codex에게 작업을 맡겨도 원활하게 진행되는 작업이다.

## MCP 등록

PowerShell에서 `<프로젝트 경로>`를 자신의 SinkPoint 프로젝트 절대 경로로 바꿔 실행한다.

먼저 Codex에 기록될 설정을 확인한다.

```powershell
unity mcp --project-path "<프로젝트 경로>" configure codex --dry-run
```

출력의 프로젝트 경로와 명령이 예상과 일치하면 등록한다.

```powershell
unity mcp --project-path "<프로젝트 경로>" configure codex
```

MCP 설정은 기본적으로 개인 `~/.codex/config.toml`에 저장하므로 프로젝트 Git에 추가하지 않는다. 동일한 Codex 호스트의 데스크톱 앱, CLI와 IDE 확장은 이 설정을 공유한다. 자세한 내용은 [OpenAI 공식 MCP 문서](https://learn.chatgpt.com/docs/extend/mcp?surface=cli)를 참고한다.

등록 후 Codex를 재시작하거나 새 작업을 연다. 현재 열려 있는 작업은 시작 시점의 MCP 목록을 계속 사용할 수 있다.

## 연결 확인

Codex에서 MCP 서버 목록을 확인한 뒤, Unity를 수정하지 않는 읽기 작업을 요청한다. 예시는 다음과 같다.

> Unity MCP로 현재 Editor 상태, 열린 씬과 Console 오류를 읽기 전용으로 확인해 줘. 프로젝트나 씬은 변경하지 마.

다음을 확인하면 연결이 완료된 것이다.

- Codex에 Unity MCP와 관련 도구가 표시된다.
- 응답의 프로젝트 경로와 Unity 버전이 현재 연 프로젝트와 일치한다.
- Editor 상태와 열린 씬을 조회할 수 있다.
- 조회 전후에 씬, Prefab이나 ProjectSettings 변경이 생기지 않는다.

## 작동하지 않을 때 확인할 항목

아래 항목은 기본 설치 절차가 아니다. 프로젝트에 이미 준비된 설정이 정상적으로 작동하지 않을 때만 순서대로 확인한다.

1. Unity CLI가 실행되고 로그인 상태인지 확인한다.

   ```powershell
   unity --version
   unity auth status
   ```

2. 자신의 SinkPoint 프로젝트가 Unity Editor에서 열려 있고 패키지 로드와 컴파일이 끝났는지 확인한다.

3. 프로젝트에 공유된 Unity Pipeline이 정상적으로 인식되고 Editor가 연결 가능한 상태인지 확인한다.

   ```powershell
   unity pipeline list --json
   unity status --json
   unity list --json
   ```

4. 등록 명령에 사용한 프로젝트 절대 경로가 현재 팀원의 실제 경로와 일치하는지 확인한다. 경로가 다르면 올바른 경로로 `--dry-run`부터 다시 실행한다.

5. Codex를 재시작하거나 새 작업을 연 뒤 MCP 서버 목록을 다시 확인한다.

6. 여전히 도구가 나타나지 않으면 `~/.codex/config.toml`에 Unity MCP 항목이 기록됐는지 확인한다. 파일을 공유하거나 덮어쓰기 전에 기존 개인 설정을 보존한다.

7. Pipeline이 없거나 컴파일 오류가 발생하면 임의로 패키지를 다시 설치하거나 버전을 변경하지 말고, 저장소의 패키지 상태와 Unity Console 오류를 팀에 공유한다.

## 검증된 참고 환경

아래 버전은 현재 연결을 확인한 환경이며 팀원의 필수 버전은 아니다.

- Unity Editor: `6000.3.20f1`
- Unity CLI: `1.0.0-beta.5`
- Unity Pipeline: `0.5.0-exp.1`
- 운영체제와 셸: Windows, PowerShell 7

이 환경에서는 Codex에 Unity MCP를 등록한 뒤 새 작업에서 Editor 상태, 프로젝트 경로, 열린 씬, Hierarchy와 Console을 읽기 전용으로 조회했다.

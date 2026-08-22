# Player / Gravity 담당자 전달 TODO

이 문서는 현재 씬에 배치된 이벤트/리스폰 뼈대와 플레이어/중력 시스템을 연결하기 위한 작업 목록입니다.

## 현재 준비된 것

- `GravityEventTrigger`
  - 위치: `Assets/_Scripts/GameFlow/Triggers/GravityEventTrigger.cs`
  - 플레이어가 트리거를 올바른 방향으로 통과했을 때 이벤트를 발생시킵니다.
  - 트리거 오브젝트의 로컬 파란색 Z축이 새 구역으로 나가는 기준 방향입니다.
  - 중력을 직접 바꾸지는 않습니다.

- `GameFlowManager`
  - 위치: `Assets/_Scripts/GameFlow/GameFlowManager.cs`
  - 씬의 `GravityEventTrigger`들을 구독합니다.
  - 현재 진행 상태 `GameFlowState`를 관리합니다.
  - 플레이어 사망 시 호출할 `HandlePlayerDeath()`가 있습니다.

- `RespawnController`
  - 위치: `Assets/_Scripts/GameFlow/RespawnController.cs`
  - 플레이어 Transform/Rigidbody를 보관합니다.
  - `GameFlowManager`가 넘겨준 리스폰 위치로 플레이어를 이동시킵니다.

## 플레이어 쪽 필수 연결

1. 플레이어 HP 구현
   - 플레이어 체력이 0 이하가 되면 아래 함수를 호출해주세요.

```csharp
GameFlowManager.Instance.HandlePlayerDeath();
```

2. 리스폰 후 HP 복구 연결
   - `GameFlowManager.HandlePlayerDeath()` 안에 TODO가 남아 있습니다.
   - `PlayerHealth` 같은 클래스가 생기면, 리스폰 직후 HP를 풀피로 복구하는 함수를 여기서 연결하면 됩니다.

3. 리스폰 후 플레이어 상태 초기화
   - 필요하면 사격 중, 그래플 중, 피격 중, 애니메이션 상태 등을 사망 처리 시 초기화해주세요.
   - 최소 MVP에서는 위치 이동 + Rigidbody 속도 초기화만으로 시작해도 됩니다.

## 중력 시스템 쪽 필수 연결

1. 중력 변경 함수 제공
   - `GameFlowManager`에서 호출할 수 있는 중력 변경 함수를 만들어주세요.
   - 예시 이름은 확정이 아닙니다.

```csharp
GravityManager.SetGravityMode(GravityEventTrigger.GravityEventType eventType);
```

2. `GameFlowManager.OnGravityEventTriggered()`에 연결
   - 현재 이 함수 안에 TODO가 있습니다.
   - `GravityEventTrigger`가 성공 판정을 내리면 여기로 들어옵니다.
   - 여기서 실제 중력 모드 변경 함수를 호출하면 됩니다.

3. 현재 필요한 중력 이벤트 타입
   - `ShiftGravity`
   - `Inversion`
   - `FastDown`
   - `Slow`
   - `ZeroGravity`

4. 리스폰 시 중력 복구 정책 결정
   - 플레이어가 사망했을 때 현재 체크포인트의 중력 상태로 복구할지,
   - 아니면 현재 `GameFlowState`의 중력 상태를 유지할지 정해야 합니다.
   - MVP에서는 `currentState` 기준으로 다시 적용하는 방식이 가장 단순합니다.

## 카메라 쪽 확인 필요

- 중력 방향이 바뀔 때 카메라 up 방향/회전 보정이 필요할 수 있습니다.
- 리스폰 직후 카메라가 플레이어를 즉시 다시 바라보도록 보정해주세요.
- 벽 뚫림 방지는 MVP에서는 간단한 Raycast/SphereCast 정도면 충분합니다.

## 입력 쪽 참고

- 현재 입력 래퍼:
  - `Assets/_Scripts/Input/MvpPlayerInput.cs`
- 기존 Unity 입력 방식 사용:
  - `Input.GetAxis`
  - `Input.GetKey`
  - `Input.GetKeyDown`
- 대화/이벤트 중 입력 차단을 위해 아래 플래그들이 있습니다.
  - `AllowMovement`
  - `AllowLook`
  - `AllowCombat`
  - `AllowGrapple`
  - `AllowInteract`

## 씬 연결 체크리스트

- `GameFlowManager`
  - `Gravity Event Triggers` 배열에 씬 트리거 전부 등록
  - `Respawn Controller` 연결
  - `State Respawn Points`에 상태별 리스폰 위치 등록

- `RespawnController`
  - `Player Root`에 플레이어 최상위 Transform 연결
  - `Player Rigidbody`는 비워도 되지만, 문제 생기면 직접 연결

- 플레이어 오브젝트
  - Tag: `Player`
  - Collider 필요
  - Rigidbody 필요
  - `MvpPlayerInput` 또는 Player 태그를 통해 트리거가 플레이어를 판정할 수 있어야 함

## MVP 우선순위

1. 플레이어 이동/카메라/점프가 된다.
2. `GravityEventTrigger` 통과 시 `GameFlowManager` 로그가 찍힌다.
3. 중력 변경 함수가 `GameFlowManager`에 연결된다.
4. 플레이어 HP가 0이 되면 `HandlePlayerDeath()`가 호출된다.
5. 현재 구역 리스폰 위치로 돌아간다.
6. 리스폰 후 HP와 중력 상태가 복구된다.

여기까지 되면 이벤트/전투/연출 담당 쪽에서 몬스터 스폰, 문 개방, UI, VFX를 붙일 수 있습니다.

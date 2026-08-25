# 플레이어 장탄 및 리로드 애니메이션 연결 실행 계획

문서 작성일: 2026-08-25
현재 상태: 완료 — 구현·컴파일·자동 런타임·사용자 Play Mode 검증 완료

계획 프로필: `standard`

기준 문서:

- [SinkPoint MVP 게임 기획서](../../../GameDesign_MVP.md)
- [플레이어·중력 파트 마스터 계획](../../Player_Gravity_Master_Plan.md)
- [플레이어 조준·사격·달리기·웅크리기 계획](../03_completed/player_aim_fire_sprint_crouch_plan.md)

## 1. 목표

플레이어 무기에 단일 탄창의 현재 장탄 수를 추가하고, 발사할 때마다 한 발씩 소비한다. 현재 장탄이 0이 되면 자동으로 리로드하며, `R` 입력으로도 수동 리로드할 수 있게 한다. 리로드 시 현재 자세에 맞는 상체 애니메이션을 한 번 재생하고 그동안 사격하지 않도록 기존 플레이어 전투·애니메이션 흐름에 연결한다.

이번 단계는 MVP 게임 기획서의 `재장전 연출`에 이를 작동시키는 최소 장탄 규칙만 더한다. 예비 탄약은 무한한 것으로 간주하며 탄약 아이템이나 별도 예비탄 수량은 추가하지 않는다.

## 2. 범위

- 서 있는 상태에서 `infantry_combat_reload` 재생
- 웅크린 상태에서 `infantry_crouch_reload` 재생
- 단일 탄창 용량과 현재 장탄 수
- 발사할 때마다 현재 장탄 1발 소비
- 현재 장탄이 0이 된 직후 자동 리로드
- 탄창이 가득 차지 않았을 때 `R` 수동 리로드
- 리로드 완료 시 현재 장탄을 탄창 용량까지 충전
- Animator `Reload` Trigger를 이용한 일회성 시작
- `PlayerCombatController`가 리로드 시작·진행·종료 상태 소유
- 리로드 시작 시 진행 중인 사격 중단
- 리로드 중 신규 사격 차단
- 리로드 애니메이션 종료 후 기존 `Upper Body` 대기 상태로 복귀
- Inspector에서 탄창 용량, 현재 장탄, 리로드 지속 시간과 런타임 상태 관찰
- Unity 컴파일과 Play Mode 정상·충돌 경로 검증

## 3. 하지 않을 것

- 예비 탄약 수량과 소모 규칙
- 필드에 배치하거나 획득하는 탄약·탄창 아이템
- HUD 탄약 표시
- 무기 교체, 인벤토리 또는 무기별 리로드 규칙
- 탄창 오브젝트 탈착, 리로드 사운드 또는 세부 VFX
- 이동 FSM에 `GroundedReloading`, `AirborneReloading` 같은 복합 상태 추가
- `Base Layer` 이동·점프 State 또는 Blend Tree 재구성
- Collider 변경
- `Original_GamePlayScene` 수정
- Packages, ProjectSettings 또는 Build Settings 변경
- WebGL 빌드

## 4. 현재 상태와 선행 정리

- `PlayerInput`은 `AllowCombat`이 활성화된 동안 `R` 입력을 `ReloadPressed`로 이미 제공한다.
- `PlayerCombatController`는 발사와 연사 상태를 소유하지만 `ReloadPressed`를 아직 소비하지 않는다.
- `PlayerCombatController`에는 탄창 용량이나 현재 장탄 상태가 없어 현재는 제한 없이 발사할 수 있다.
- `PlayerAnimationController`는 이동·자세·사격·조준 파라미터를 Animator에 전달하지만 리로드 파라미터는 아직 전달하지 않는다.
- `Player.controller`에는 상체 Avatar Mask가 적용된 `Upper Body` 레이어와 `Empty`, `Standing Fire`, `Crouch Fire` State가 있다.
- 현재 `Upper Body`에 추가된 리로드 State의 Motion은 `infantry_crouch_reload`다. 구현 전에 이름을 `Crouch Reload`로 정리하고, 별도 `Standing Reload` State에 `infantry_combat_reload`를 연결한다.
- 현재 리로드 State에는 진입·복귀 Transition과 `Reload` Trigger가 없다.
- 사용자가 진행 중인 Base Layer Blend Tree 클립 교체, 사격 사운드와 기타 작업 트리 변경은 이번 작업에서 수정하거나 되돌리지 않는다.

## 5. 책임 경계와 데이터 흐름

```text
PlayerCombatController.FireShot()
  -> CurrentRounds 1발 소비
      -> CurrentRounds == 0이면 자동 리로드 요청

PlayerInput.ReloadPressed
  -> CurrentRounds < MagazineCapacity이면 수동 리로드 요청

자동 또는 수동 리로드 요청
  -> PlayerCombatController가 공통 StartReload()에서 시작 가능 여부 판단
      -> 진행 중인 사격 중단
      -> IsReloading 및 ReloadStartedThisFrame 갱신
          -> PlayerAnimationController가 Reload Trigger 전달
              -> Upper Body가 현재 IsCrouching 값으로 클립 선택
                  -> Standing Reload 또는 Crouch Reload 재생
                      -> 리로드 완료 시 CurrentRounds를 MagazineCapacity로 충전
                      -> Exit Time에 Empty로 복귀
```

### 5.1 `PlayerInput`

- 기존 `ReloadPressed`를 그대로 사용한다.
- 플레이어의 다른 스크립트에서 `Input.GetKeyDown(KeyCode.R)`을 직접 호출하지 않는다.
- 리로드 가능 여부나 리로드 상태는 소유하지 않는다.

### 5.2 `PlayerCombatController`

- `ReloadPressed`를 소비하는 유일한 전투 컴포넌트다.
- 탄창 용량 `MagazineCapacity`와 현재 장탄 `CurrentRounds`를 소유한다.
- 초기 탄창 용량은 `30`, Play Mode 시작 시 현재 장탄은 탄창 용량과 같게 둔다.
- 실제 `FireShot()`이 확정될 때만 현재 장탄을 한 발 감소시킨다. 입력만 눌렀거나 연사 간격을 기다리는 동안에는 감소시키지 않는다.
- 발사 후 현재 장탄이 0이 되면 같은 전투 컴포넌트가 자동 리로드를 요청한다.
- `ReloadPressed`는 현재 장탄이 탄창 용량보다 적을 때만 수동 리로드를 요청한다. 가득 찬 탄창에서 누른 `R`은 무시한다.
- 자동·수동 리로드는 하나의 `StartReload()` 경로를 공유한다.
- `IsReloading`과 리로드 종료 시각을 소유한다.
- 새 리로드가 시작된 프레임만 애니메이션 측에서 구분할 수 있도록 읽기 전용 상태를 제공한다.
- 리로드 시작 시 `StopFiring()`을 호출하고 리로드 중에는 `FireShot()`에 도달하지 않게 한다.
- 이미 리로드 중일 때 들어오는 추가 `R` 입력은 무시한다.
- 리로드 완료 시 현재 장탄을 탄창 용량까지 채운다. 예비 탄약은 계산하거나 감소시키지 않는다.
- 컴포넌트가 비활성화되면 진행 중인 리로드를 취소하고 임의로 탄약을 충전하지 않는다.
- 리로드가 끝난 뒤에도 계속 누르고 있던 발사 입력으로 자동 연사를 재개하지 않는다. 사용자가 발사 버튼을 놓고 다시 눌러야 새 발사 시퀀스를 시작한다.

### 5.3 `PlayerAnimationController`

- `Reload` 파라미터 해시를 캐시한다.
- `PlayerCombatController`가 새 리로드 시작을 보고한 프레임에만 `animator.SetTrigger()`를 호출한다.
- 원시 `ReloadPressed`를 직접 읽지 않는다.
- 리로드 허용 여부, 지속 시간 또는 사격 차단을 결정하지 않는다.

### 5.4 `Player.controller`

- `Upper Body` 레이어에 `Standing Reload`와 `Crouch Reload` State를 둔다.
- `Standing Reload` Motion은 `infantry_combat_reload`를 사용한다.
- `Crouch Reload` Motion은 `infantry_crouch_reload`를 사용한다.
- `Reload` Trigger와 리로드 시작 순간의 `IsCrouching` 값으로 둘 중 하나만 선택한다.
- 애니메이션 종료는 Exit Time으로 처리하고 두 State 모두 `Empty`로 복귀한다.
- 실제 리로드 상태의 정본은 되지 않는다.

## 6. Animator 전환 계약

### 6.1 파라미터

- 기존 `IsCrouching`: Bool
- 신규 `Reload`: Trigger

### 6.2 진입 Transition

```text
Any State -> Standing Reload
  - Reload
  - IsCrouching == false

Any State -> Crouch Reload
  - Reload
  - IsCrouching == true
```

- 두 Transition 모두 `Has Exit Time`을 끈다.
- 리로드 입력에 즉시 반응할 수 있도록 짧은 Transition Duration을 사용한다.
- 리로드 State 자신으로 다시 진입하지 않도록 self transition을 허용하지 않는다.

### 6.3 복귀 Transition

```text
Standing Reload -> Empty
Crouch Reload -> Empty
```

- 두 Transition 모두 별도 Condition 없이 `Has Exit Time`을 사용한다.
- Exit Time은 클립을 한 번 끝까지 재생하는 값으로 둔다.
- 두 리로드 클립의 Loop Time은 비활성 상태를 유지한다.

## 7. 장탄 및 리로드 상태 계약

- `AllowCombat == false`일 때는 기존 `PlayerInput` 규칙에 따라 리로드가 시작되지 않는다.
- 탄창 용량은 최소 1로 제한하고 현재 장탄은 항상 `0..MagazineCapacity` 범위를 유지한다.
- Play Mode 시작 시 현재 장탄은 탄창 용량과 같다.
- 한 번의 실제 발사는 정확히 한 발만 소비한다.
- 마지막 한 발의 판정·피해·트레이서·사격음·무중력 반작용은 정상 처리한 뒤 자동 리로드를 시작한다.
- 현재 장탄이 0일 때는 다음 발사를 만들지 않고 자동 리로드 상태를 유지한다.
- 탄창이 가득 찬 상태에서 `R`을 눌러도 리로드하지 않는다.
- 일부 장탄이 남은 상태에서 `R`을 누르면 남은 탄을 별도로 보존하거나 회수하지 않고 리로드 완료 시 탄창을 가득 채운다.
- 예비 탄약은 무한한 것으로 간주하므로 리로드 실패나 부분 충전은 없다.
- 지상, 공중과 무중력에서 리로드를 허용한다.
- 리로드 시작 당시 `IsCrouching` 값으로 클립을 선택한다.
- 재생 도중 웅크리기 상태가 바뀌어도 다른 리로드 State로 갈아타지 않는다.
- 리로드 중 추가 `R` 입력은 현재 리로드를 처음부터 다시 시작하지 않는다.
- 리로드 중 발사 판정, 몬스터 피해, 물리 밀기, 무중력 반작용, 트레이서와 사격음이 발생하지 않는다.
- 리로드 지속 시간은 클립 길이를 Unity Inspector에서 확인한 뒤 직렬화된 설정값으로 맞춘다.
- 자동·수동 리로드 모두 같은 지속 시간, 사격 차단과 애니메이션 경로를 사용한다.

## 8. 구현 순서

1. `[Animator 리로드 구조 완성]` -> verify: `Upper Body`에 두 리로드 State가 있고 `Base Layer`에는 리로드 State가 없으며 각 Motion이 올바른 클립인지 확인한다.
2. `[Reload Trigger와 Transition 구성]` -> verify: 서기·웅크리기 진입 조건이 상호 배타적이고 두 State가 Exit Time으로 `Empty`에 복귀하는지 Animator에서 확인한다.
3. `[PlayerCombatController 탄창 상태 추가]` -> verify: 탄창 용량 30과 현재 장탄의 초기화·범위 제한이 있고 실제 발사 한 번당 정확히 한 발만 감소하는지 코드 흐름을 확인한다.
4. `[수동·자동 리로드 상태 추가]` -> verify: `R` 수동 리로드와 0발 자동 리로드가 하나의 시작 경로를 사용하며 리로드 중 `FireShot()`에 도달할 수 없는지 확인한다.
5. `[PlayerAnimationController Trigger 전달]` -> verify: 원시 입력을 읽지 않고 전투 컨트롤러가 보고한 시작 프레임에만 Trigger를 설정하는지 확인한다.
6. `[정적 검사와 Unity 컴파일]` -> verify: 코드와 Animator 파라미터 이름이 정확히 `Reload`로 일치하고 신규 `error CS`와 파라미터 누락 경고가 없는지 확인한다.
7. `[Play Mode 장탄·수동 리로드 검증]` -> verify: 발사마다 현재 장탄이 감소하고 일부 장탄에서 `R`을 누르면 사격이 중단되며 종료 시 30발로 충전되는지 확인한다.
8. `[Play Mode 자동 리로드 검증]` -> verify: 마지막 한 발이 정상 처리된 직후 자동 리로드가 한 번 시작되고 종료 시 30발로 충전되는지 확인한다.
9. `[Play Mode 애니메이션·충돌 경로 검증]` -> verify: 서기·웅크리기 클립 선택, 가득 찬 탄창의 `R`, 리로드 중 좌클릭·반복 `R`, 공중·무중력 리로드가 계약대로 동작하며 신규 Console 예외가 없는지 확인한다.
10. `[완료 기록]` -> verify: 사용자가 Play Mode 동작을 확인한 뒤 컴파일 결과와 사용자 검증 결과를 구분해 `Codex_Usage_Records.md`에 완료 작업 한 항목을 기록한다.

## 9. 검증 체크리스트

### 9.1 정적·컴파일

- [x] `Reload` Trigger 이름이 코드와 Animator에서 일치한다.
- [x] `Standing Reload`에 `infantry_combat_reload`가 연결되어 있다.
- [x] `Crouch Reload`에 `infantry_crouch_reload`가 연결되어 있다.
- [x] 두 리로드 클립의 Loop Time이 꺼져 있다.
- [x] 탄창 용량의 초기값이 30이고 최소값이 1로 제한된다.
- [x] 현재 장탄은 Inspector에서 Play Mode 중 관찰할 수 있다.
- [x] 실제 `FireShot()` 한 번당 현재 장탄이 한 발만 감소한다.
- [x] 수동·자동 리로드가 같은 시작·완료 경로를 사용한다.
- [x] `Base Layer`의 이동·점프 State와 Blend Tree를 이번 작업에서 변경하지 않았다.
- [x] Unity 스크립트 컴파일에 신규 `error CS`가 없다.
- [x] Animator 파라미터 누락 또는 잘못된 Transition 경고가 없다.

### 9.2 Play Mode

- [x] 서 있는 상태에서 `R`을 한 번 누르면 Standing Reload가 한 번 재생된다.
- [x] 웅크린 상태에서 `R`을 한 번 누르면 Crouch Reload가 한 번 재생된다.
- [x] Play Mode 시작 시 현재 장탄이 30이다.
- [x] 발사할 때마다 현재 장탄이 `30 -> 29 -> 28` 순서로 감소한다.
- [x] 일부 장탄이 남았을 때 `R`을 누르면 수동 리로드 후 30발이 된다.
- [x] 30발이 모두 남아 있을 때 `R`을 눌러도 리로드하지 않는다.
- [x] 마지막 한 발은 정상 발사되고 현재 장탄이 0이 된 직후 자동 리로드가 시작된다. (자동 런타임 검증)
- [x] 자동 리로드 완료 후 현재 장탄이 30으로 복구된다. (자동 런타임 검증)
- [x] 이동 중 리로드해도 하체 이동 애니메이션이 유지된다.
- [x] 연사 도중 리로드하면 사격이 즉시 중단된다.
- [x] 리로드 중 사격 판정·트레이서·사격음·무중력 반작용이 발생하지 않는다.
- [x] 리로드 중 `R`을 반복해도 애니메이션이 재시작되지 않는다.
- [x] 리로드 종료 후 `Empty` 또는 정상 사격 상태로 복귀한다.
- [x] 공중과 무중력에서도 상체 리로드가 재생되고 이동 상태가 손상되지 않는다.
- [x] 자동 런타임 검증 이후 신규 `NullReferenceException` 또는 Animator 경고가 없다.

## 10. 완료 기준

- `R` 입력 한 번당 리로드 애니메이션이 정확히 한 번 재생된다.
- 현재 장탄이 실제 발사 한 번당 한 발 감소한다.
- 일부 장탄에서 `R`을 누르면 수동 리로드되고, 가득 찬 상태의 `R`은 무시된다.
- 현재 장탄이 0이 되면 자동 리로드가 정확히 한 번 시작된다.
- 자동·수동 리로드 완료 시 현재 장탄이 탄창 용량까지 충전된다.
- 탄약 아이템이나 예비 탄약 상태 없이 단일 탄창 규칙만 추가된다.
- 서기와 웅크리기에서 올바른 리로드 클립이 선택된다.
- 리로드 중에는 모든 실제 사격 결과가 발생하지 않는다.
- 이동·점프·웅크리기·무중력의 기존 동작을 보존한다.
- 리로드 종료 후 상체 레이어가 정상 상태로 복귀한다.
- Unity 컴파일에 신규 오류가 없다.
- 최종 완료는 사용자의 Play Mode 확인으로 판정한다.

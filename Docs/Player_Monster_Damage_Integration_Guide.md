# 플레이어·몬스터 데미지 연동 가이드

문서 기준일: 2026-08-23

이 문서는 플레이어와 몬스터가 현재 프로젝트에서 어떻게 피해를 주고받는지 설명하고, 새 몬스터 Prefab에 같은 상호작용을 연결할 때 필요한 최소 구성을 정리한다.

## 1. 현재 데미지 흐름

### 플레이어가 몬스터를 공격

```text
PlayerInput의 Fire 입력
  → PlayerCombatController의 카메라 조준 Ray
  → Muzzle의 실제 사격 Ray
  → 적중 Collider의 부모 MonsterHealth 탐색
  → MonsterHealth.ApplyDamage(shotDamage)
```

- 실제 피해는 총구 Ray의 최종 적중에서만 한 번 적용된다.
- 일반 Collider는 기존처럼 사격을 막는다.
- Trigger Collider는 같은 부모 계층에서 살아 있는 `MonsterHealth`를 찾을 수 있을 때만 사격 대상으로 인정한다.
- 중력 구역이나 진행 Trigger처럼 `MonsterHealth`가 없는 Trigger는 사격을 막지 않는다.

관련 코드:

- [PlayerCombatController.cs](../Assets/_Scripts/Player/PlayerCombatController.cs)
- [MonsterHealth.cs](../Assets/_Scripts/Monster/MonsterHealth.cs)

### 몬스터가 플레이어를 공격

```text
몬스터의 공격 Collider 또는 Trigger
  → 같은 오브젝트의 MonsterDamageOnContact
  → 접촉 Collider의 부모 PlayerHealth 탐색
  → PlayerHealth.ApplyDamage(damage)
```

- 최초 접촉 시 즉시 한 번 피해를 준다.
- 계속 접촉하면 `cooldown`이 지난 뒤 다시 피해를 준다.
- 죽은 몬스터는 접촉 피해를 주지 않는다.
- 죽은 플레이어는 추가 피해를 받지 않는다.

관련 코드:

- [MonsterDamageOnContact.cs](../Assets/_Scripts/Monster/MonsterDamageOnContact.cs)
- [PlayerHealth.cs](../Assets/_Scripts/Player/PlayerHealth.cs)

## 2. 현재 Player와 지네 구성

### Player Prefab

[Player.prefab](../Assets/_Custom/Prefabs/Player/Player.prefab)의 루트에는 다음 컴포넌트가 있다.

- `PlayerHealth`: 플레이어 체력과 사망 상태 소유
- `PlayerCombatController`: 조준, 사격 Raycast와 몬스터 피해량 소유
- `Rigidbody`와 `CapsuleCollider`: 이동 충돌과 Trigger 이벤트 조건 제공

`PlayerHealth`의 `Runtime State`는 Play Mode에서 확인한다. Edit Mode의 `Current Health`는 아직 `Awake()`가 실행되지 않아 `0`일 수 있으며, Play Mode 진입 시 `Max Health`로 초기화된다.

### Monster_02_Centipede Prefab

[Monster_02_Centipede.prefab](../Assets/_Custom/Prefabs/Monster/Monster_02_Centipede.prefab)은 다음처럼 구성되어 있다.

- 몬스터 루트: `MonsterHealth`, `MonsterStateMachine`과 이동·감지 컴포넌트
- `Nav Target`: `SphereCollider (Is Trigger)`, `MonsterDamageOnContact`

지네는 몸체 루트가 아니라 `Nav Target`이 실제 추적·돌진 기준으로 움직이므로 판정체도 여기에 있다. `Nav Target`이라는 이름이나 구조는 모든 몬스터의 공통 요구사항이 아니다.

## 3. 새 몬스터에 적용하는 방법

### A. 플레이어의 총에 맞게 만들기

1. 몬스터 Prefab의 대표 루트에 `MonsterHealth`를 추가한다.
2. 플레이어의 총에 맞을 Collider를 몬스터 루트 또는 자식에 추가한다.
3. Collider에서 부모 방향으로 올라갔을 때 같은 몬스터의 `MonsterHealth`를 찾을 수 있게 계층을 구성한다.
4. Collider의 Layer가 Player Prefab의 `PlayerCombatController > Hit Mask`에 포함되는지 확인한다.
5. Trigger를 사용한다면 `Is Trigger`를 켠다. Trigger가 아니어도 부모에 `MonsterHealth`가 있으면 피해를 받을 수 있다.

피격만 필요하고 접촉 공격은 필요하지 않다면 Collider만 추가하고 `MonsterDamageOnContact`는 추가하지 않는다.

### B. 플레이어에게 접촉 피해를 주게 만들기

1. 공격 판정이 실제로 따라가야 할 Transform을 선택한다.
2. 그 오브젝트에 Collider를 추가한다. 물리적으로 밀지 않는 공격 판정은 `Is Trigger`를 권장한다.
3. 같은 오브젝트에 `MonsterDamageOnContact`를 추가한다.
4. 해당 오브젝트의 부모 계층에 `MonsterHealth`가 있는지 확인한다.
5. Inspector에서 `Damage`, `Cooldown`을 설정한다. 현재 지네 기본값은 각각 `1`, `1초`다.

Collider와 `MonsterDamageOnContact`를 같은 오브젝트에 두는 것이 프로젝트의 기본 구성이다. 자식 Collider의 이벤트가 임의의 상위 MonoBehaviour까지 전달된다고 가정하지 않는다.

Player에는 이미 Rigidbody가 있으므로 현재 접촉 Trigger만을 위해 몬스터에 Rigidbody를 추가할 필요는 없다. 새 몬스터 자체의 이동 방식에 Rigidbody가 필요한지는 별도로 판단한다.

### C. 사망 시 행동을 멈추게 만들기

`MonsterHealth`는 체력과 사망 이벤트만 소유한다.

- `MonsterStateMachine`이 있는 몬스터는 `Died` 이벤트를 받아 `Dead` 상태로 전환한다.
- 각 Mover나 공격 스크립트는 `Dead` 상태에서 동작하지 않아야 한다.
- `MonsterStateMachine`을 사용하지 않는 새 몬스터는 자체 스크립트가 `MonsterHealth.Died`를 구독해 이동·공격 중단을 처리해야 한다.
- 즉시 제거가 필요한 단순 몬스터만 `MonsterHealth > Destroy On Death`를 사용한다.

사망 연출, 드랍, 풀 반환은 `MonsterHealth` 안에 추가하지 않고 `Died` 이벤트 구독자가 담당한다.

## 4. 판정체 구성 선택

| 목적 | 필요한 구성 | 비고 |
|---|---|---|
| 총에 맞기만 함 | Collider + 상위 `MonsterHealth` | 접촉 피해 없음 |
| 접촉 공격만 함 | Collider/Trigger + 같은 오브젝트의 `MonsterDamageOnContact` + 상위 `MonsterHealth` | PlayerHealth를 자동 탐색 |
| 같은 판정체로 피격과 접촉 공격 | Trigger + `MonsterDamageOnContact` + 상위 `MonsterHealth` | 현재 지네 방식 |
| 피격 부위와 공격 범위 분리 | Body Collider와 Attack Trigger를 별도 자식으로 구성 | 공격 Trigger에만 접촉 피해 컴포넌트 추가 |

새 몬스터의 판정체는 모델 루트가 아니라 애니메이션·절차 이동 중 실제 공격 부위를 따라가는 Transform에 둔다. Scene View와 Play Mode에서 보이는 몸체와 Collider의 위치를 함께 확인한다.

## 5. 새 몬스터 테스트 체크리스트

테스트 씬에는 Player, ThirdPersonCameraRig와 새 몬스터를 함께 배치한다.

### 플레이어 공격 확인

1. Player의 `PlayerCombatController > Aim Camera`가 씬의 `ThirdPersonCameraController`를 참조하는지 확인한다.
2. Play Mode에서 몬스터 루트의 `MonsterHealth > Runtime State`를 펼친다.
3. 사격할 때마다 `Current Health`가 설정된 피해량만큼 감소하는지 확인한다.
4. `0`에서 `Dead`가 켜지고 이동·공격이 멈추는지 확인한다.
5. 몬스터 앞의 일반 장애물이 사격을 막는지 확인한다.
6. `MonsterHealth`가 없는 구역 Trigger는 사격을 막지 않는지 확인한다.

### 몬스터 공격 확인

1. 공격 Collider/Trigger가 실제 공격 부위를 따라 움직이는지 확인한다.
2. Player 루트의 `PlayerHealth > Runtime State`를 펼친다.
3. 최초 접촉에서 즉시 한 번 감소하는지 확인한다.
4. 접촉을 유지했을 때 `Cooldown`보다 빠르게 중복 감소하지 않는지 확인한다.
5. 몬스터가 죽은 뒤에는 접촉해도 플레이어 체력이 감소하지 않는지 확인한다.

### 완료 조건

- 사격 한 번당 몬스터 피해가 한 번만 발생한다.
- 접촉 쿨다운 한 번당 플레이어 피해가 한 번만 발생한다.
- 양쪽 체력이 Inspector에서 실시간으로 감소한다.
- 죽은 대상에게 추가 피해나 공격 동작이 발생하지 않는다.
- 신규 Console 오류와 Missing Script가 없다.

## 6. 자주 생기는 문제

### 총에 맞아도 체력이 줄지 않는다

- 피격 Collider의 부모 계층에 `MonsterHealth`가 있는지 확인한다.
- Collider Layer가 `PlayerCombatController > Hit Mask`에 포함되는지 확인한다.
- Trigger라면 다른 감지용 Trigger 아래에 잘못 배치되지 않았는지 확인한다.
- Player의 `Aim Camera`와 `Muzzle` 참조가 비어 있지 않은지 확인한다.

### 접촉해도 플레이어 체력이 줄지 않는다

- Collider와 `MonsterDamageOnContact`가 같은 오브젝트에 있는지 확인한다.
- 그 오브젝트의 부모 계층에 `MonsterHealth`가 있는지 확인한다.
- Player 루트에 `PlayerHealth`, Rigidbody와 Collider가 있는지 확인한다.
- Physics Layer Collision Matrix에서 두 Layer의 상호작용이 차단되지 않았는지 확인한다.

### 체력은 0인데 계속 움직인다

- `MonsterStateMachine`이 `MonsterHealth`와 같은 계층에서 검색 가능한지 확인한다.
- Mover가 `Dead` 상태를 무시하고 직접 이동하고 있지 않은지 확인한다.
- 별도 상태 머신을 쓰는 몬스터라면 `MonsterHealth.Died` 구독과 해제가 모두 구현됐는지 확인한다.

## 7. 이번 가이드에서 다루지 않는 것

- HP UI와 HP Bar
- 피격·사망 애니메이션, VFX, 사운드
- 넉백, 경직, 무적 시간과 부위별 피해 배율
- 플레이어 리스폰과 몬스터 풀링의 실제 연결
- 전용 Player/Monster Layer와 Physics Layer Matrix 재설계

구현 배경과 최초 검증 시나리오는 [플레이어·지네 몬스터 히트 및 피격 판정 실행 계획](ksh/Tasks/03_completed/player_monster_hit_damage_interaction_plan.md)을 참고한다.

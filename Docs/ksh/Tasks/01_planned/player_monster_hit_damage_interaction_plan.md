# 플레이어·지네 몬스터 히트 및 피격 판정 실행 계획

문서 작성일: 2026-08-23

현재 상태: 구현 대기

계획 프로필: `standard`

## 목표

기존 플레이어의 카메라 중심→총구 2단계 Raycast와 `Monster_02_Centipede`의 체력·추적·돌진 구조를 유지하면서 다음 첫 전투 상호작용을 완성한다.

- 플레이어가 발사한 Ray가 지네의 유효 피격 Trigger에 닿으면 몬스터 체력이 감소한다.
- 지네의 접촉 Trigger가 플레이어 Collider에 닿으면 쿨다운을 적용해 플레이어 체력이 감소한다.
- 판정체는 최상위 루트가 아니라 실제 이동·돌진 기준인 `Nav Target`을 따라간다.
- 체력 감소는 Inspector와 공개 읽기 API로 확인할 수 있고, 0이 되면 사망 상태를 한 번만 발생시킨다.

## 범위

- 플레이어와 몬스터의 최소 체력 컴포넌트 연결
- 플레이어 Hitscan의 몬스터 Trigger 선별과 피해 전달
- 지네 `Nav Target`의 Trigger Collider와 접촉 피해 전달
- Player·Monster Prefab의 재사용 가능한 기본 구성
- `MonsterTest`의 전투 참조 연결과 Play Mode 검증
- 구현 완료 후 마스터 계획과 Codex 활용 기록의 현재 상태 갱신

## 필요한 가정

- 이번 MVP는 로컬 플레이어 한 명과 `Monster_02_Centipede` 한 개체의 상호작용을 우선한다.
- 플레이어 루트의 기존 Rigidbody와 CapsuleCollider 구조는 유지된다.
- `Nav Target`은 지네의 전면 이동·돌진 기준이며 첫 접촉 공격 판정 위치로 사용할 수 있다.
- 기본 피해량과 최대 체력은 모두 `1`과 `3`에서 시작하고, 실제 체감은 기능 검증 뒤 별도 튜닝할 수 있다.
- `MonsterTest`는 전투 통합 검증용으로 사용할 수 있지만 팀장 소유 `Original_GamePlayScene`은 계속 수정하지 않는다.

## 현재 상태와 근거

### 플레이어

- `PlayerCombatController`는 카메라 중앙에서 조준점을 구한 뒤 Muzzle에서 조준점까지 다시 Raycast한다.
- 현재 Raycast는 `QueryTriggerInteraction.Ignore`를 사용하므로 Trigger인 몬스터 피격 판정을 감지하지 못한다.
- 사격 결과는 `lastShotCollider`와 `lastShotEnd`에 보관하지만 피해 API는 호출하지 않는다.
- `Player`에는 Rigidbody와 CapsuleCollider가 있으므로 몬스터 Trigger와의 물리 이벤트 조건을 이미 충족한다.
- `PlayerHealth`는 아직 존재하지 않는다.

### `Monster_02_Centipede`

- 최상위에는 `MonsterHealth`, `MonsterTargetSensor`, `MonsterStateMachine`, `MonsterDamageOnContact`, `CentipedeFloorMover`, `CentipedeLungeAttack`이 있다.
- `MonsterHealth.ApplyDamage(int)`와 사망 이벤트는 이미 구현되어 있다.
- `MonsterDamageOnContact`는 Collision/Trigger Enter·Stay와 쿨다운을 처리하지만 실제 플레이어 체력 호출은 비어 있다.
- 프리팹 전체에는 Collider와 Rigidbody가 없다.
- `CentipedeFloorMover`와 `CentipedeLungeAttack`이 `Nav Target`의 위치를 직접 변경한다.
- 보이는 몸체의 선행점인 `Spine Target`은 `Follow.target`으로 `Nav Target`을 참조한다. 따라서 판정체도 `Nav Target`에 속해야 이동·돌진과 함께 움직인다.

### `MonsterTest`

- `Monster_02_Centipede`, `Player`, `ThirdPersonCameraRig`가 함께 배치되어 있다.
- 현재 `PlayerCombatController.aimCamera`가 비어 있어 Play Mode에서 전투 컴포넌트가 비활성화된다.
- 구현 검증을 위해 `MonsterTest`의 플레이어와 CameraRig 참조를 명시적으로 연결해야 한다.

## 책임 경계와 데이터 흐름

```text
플레이어 사격
PlayerCombatController
  -> 카메라 조준 Ray
  -> 총구 실제 사격 Ray
  -> Nav Target Trigger Collider
  -> 부모 MonsterHealth.ApplyDamage(shotDamage)

몬스터 접촉
Nav Target Trigger Collider
  -> 같은 오브젝트의 MonsterDamageOnContact
  -> 접촉 Collider의 부모 PlayerHealth 탐색
  -> PlayerHealth.ApplyDamage(contactDamage)
```

- 플레이어 사격 판정과 피해량은 `PlayerCombatController`가 소유한다.
- 몬스터 체력과 사망 상태는 기존 `MonsterHealth`가 계속 소유한다.
- 몬스터 접촉 여부와 접촉 피해 쿨다운은 `Nav Target`의 `MonsterDamageOnContact`가 소유한다.
- 플레이어 체력과 사망 이벤트는 새 `PlayerHealth`가 소유한다.
- 이동·상태 머신은 체력을 직접 변경하지 않는다.
- UI, 이펙트, 리스폰과 입력 차단은 체력 이벤트를 구독하는 후속 시스템의 책임으로 남긴다.

## 핵심 설계 결정

### 1. `Nav Target`에 단일 Trigger 판정체를 둔다

- `Nav Target`에 방향 회전에 영향받지 않는 `SphereCollider`를 추가하고 `isTrigger = true`로 설정한다.
- 초기 반경은 Prefab/Scene View에서 지네의 전면 몸체를 감싸도록 잡고, Play Mode에서 추적·돌진 중 보이는 머리와의 간격을 기준으로 최종 조정한다.
- 첫 상호작용에서는 이 Trigger를 플레이어 사격 피격과 몬스터 접촉 공격에 함께 사용한다.
- Trigger가 지네의 전체 긴 몸통을 대표하지는 않는다. 이번 범위는 전면 접근·돌진 판정을 우선하며, 몸통 부위별 Hitbox는 후속 작업으로 둔다.

### 2. 접촉 이벤트 수신 컴포넌트도 `Nav Target`에 둔다

- 최상위의 `MonsterDamageOnContact`를 제거하고 같은 설정값을 유지한 채 `Nav Target`에 배치한다.
- 자식 Collider 이벤트가 최상위 MonoBehaviour까지 전달된다고 가정하지 않는다.
- 몬스터 Rigidbody는 추가하지 않는다. 플레이어의 기존 Rigidbody가 Trigger 이벤트 조건을 충족하며, 새 Rigidbody가 procedural Follow와 직접 Transform 이동을 방해할 위험을 피한다.
- 실제 Play Mode에서 Trigger가 발생하지 않을 경우에만 Physics Layer Matrix와 Rigidbody 연결 상태를 다시 진단한다.

### 3. 사격 Ray는 몬스터 Trigger만 의미 있게 허용한다

- Raycast 자체는 `QueryTriggerInteraction.Collide`로 변경해 몬스터 Trigger를 후보에 포함한다.
- 가장 가까운 적중점 선택 전에 다음 규칙으로 후보를 거른다.
  - 플레이어 자신의 Collider는 기존처럼 제외한다.
  - 일반 Collider는 기존 장애물 판정을 유지한다.
  - Trigger Collider는 부모 계층에서 살아 있는 `MonsterHealth`를 찾을 수 있을 때만 유효한 사격 후보로 인정한다.
  - 중력 구역, 씬 진행, 감지용 Trigger처럼 몬스터 체력이 없는 Trigger는 무시한다.
- 카메라 조준 Ray와 총구 실제 사격 Ray에 같은 필터를 적용해 조준점과 실제 적중 규칙이 어긋나지 않게 한다.
- 별도 Monster Layer와 `ProjectSettings` 변경은 이번 범위에서 추가하지 않는다.

### 4. 실제 피해는 총구 Ray의 최종 적중에만 적용한다

- 카메라 Ray는 조준점을 정할 뿐 피해를 발생시키지 않는다.
- 총구 Ray가 최종 선택한 Collider의 부모에서 `MonsterHealth`를 찾았을 때만 `ApplyDamage(shotDamage)`를 한 번 호출한다.
- `shotDamage`는 `[SerializeField, Min(0)] int`로 두고 MVP 기본값은 `1`로 설정한다.
- 장애물이 몬스터보다 총구에 가까우면 기존 계약대로 장애물에서 멈추며 몬스터 피해는 발생하지 않는다.
- `MonsterHealth.IsDead`와 기존 `ApplyDamage` 방어 로직을 존중해 사망 후 추가 피해 이벤트가 발생하지 않게 한다.

### 5. `PlayerHealth`는 `MonsterHealth`와 같은 최소 계약을 사용한다

새 `PlayerHealth`는 다음 책임만 가진다.

- 직렬화된 `maxHealth`, 런타임 `currentHealth`, `dead`
- `CurrentHealth`, `MaxHealth`, `IsDead` 읽기 API
- `ApplyDamage(int)`와 `ResetHealth()`
- `Damaged`와 `Died` 이벤트
- 0 이하 피해와 사망 후 피해 무시

MVP 기본 최대 체력은 `3`으로 시작한다. 사망 시 오브젝트 파괴, 이동·사격 비활성화, 리스폰 호출은 이번 단계에서 수행하지 않는다.

### 6. 접촉 피해는 실제 플레이어 체력 API를 직접 찾는다

- `MonsterDamageOnContact`는 태그 문자열이나 씬 전역 플레이어 캐시 대신 접촉한 Collider에서 `GetComponentInParent<PlayerHealth>()`로 피해 대상을 찾는다.
- `OnTriggerEnter`에서 즉시 한 번, 계속 겹쳐 있으면 `OnTriggerStay`에서 쿨다운 이후 다시 피해를 적용한다.
- 기본 피해 `1`, 쿨다운 `1초`를 유지한다.
- 여러 플레이어 Collider가 같은 프레임에 닿아도 컴포넌트 단위의 공통 쿨다운으로 중복 피해를 막는다.
- 부모 `MonsterHealth`가 죽어 있으면 접촉 피해를 적용하지 않는다.

## 예상 변경 파일

### 신규

- `Assets/_Scripts/Player/PlayerHealth.cs`
- `Assets/_Scripts/Player/PlayerHealth.cs.meta`

### 수정

- `Assets/_Scripts/Player/PlayerCombatController.cs`
  - `shotDamage` 추가
  - Trigger 후보 선별
  - 최종 `MonsterHealth.ApplyDamage` 호출
- `Assets/_Scripts/Monster/MonsterDamageOnContact.cs`
  - 실제 `PlayerHealth.ApplyDamage` 연결
  - 죽은 몬스터의 접촉 피해 차단
  - 태그·전역 검색 기반 임시 경로 제거
- `Assets/_Custom/Prefabs/Player/Player.prefab`
  - `PlayerHealth` 추가
- `Assets/_Custom/Prefabs/Monster/Monster_02_Centipede.prefab`
  - 최상위 `MonsterDamageOnContact` 제거
  - `Nav Target`에 SphereCollider Trigger와 `MonsterDamageOnContact` 추가
- `Assets/_Scenes/MonsterTest.unity`
  - `PlayerCombatController.aimCamera`를 배치된 `ThirdPersonCameraRig`에 연결
- `Docs/ksh/Player_Gravity_Master_Plan.md`
  - 적 체력 API 인계 대기 상태를 실제 연결 완료 상태로 구현 후 갱신
- `Docs/ksh/Codex_Usage_Records.md`
  - 구현과 Play Mode 검증이 완료된 뒤 한 항목 기록

`Assets/_Scenes/Original_GamePlayScene.unity`, Build Settings, `ProjectSettings`와 외부 에셋 원본은 변경하지 않는다.

## 단계별 실행 계획

1. `[PlayerHealth 최소 체력 계약 구현 및 Player Prefab 연결]`

   → verify: Unity 재컴파일 성공, Prefab에 Missing Script가 없고 초기 `CurrentHealth == MaxHealth == 3`

2. `[플레이어 총구 Ray와 MonsterHealth 피해 연결]`

   → verify: 일반 Collider는 기존처럼 사격을 막고, 몬스터 Trigger만 Ray 후보가 되며, 총구 최종 적중 1회당 HP가 정확히 1 감소

3. `[MonsterDamageOnContact를 실제 플레이어 피해 경로로 변경]`

   → verify: Player Collider 접촉만 피해를 발생시키고, Enter 즉시 1회·Stay 중 1초 간격·죽은 몬스터 피해 차단이 성립

4. `[Monster_02_Centipede의 Nav Target Trigger 구성]`

   → verify: 최상위에 Collider/Rigidbody가 추가되지 않고, Trigger와 접촉 컴포넌트가 모두 `Nav Target`에 있으며 이동·돌진 중 판정체가 해당 Transform을 따라감

5. `[MonsterTest 참조 연결 및 정상·실패 경로 Play Mode 검증]`

   → verify: aimCamera 누락 오류가 사라지고 사격 피격·접촉 피격·쿨다운·장애물 우선·구역 Trigger 무시·사망 후 피해 차단을 확인

6. `[회귀·문서·최종 diff 검증]`

   → verify: 이동·카메라·사격 애니메이션·몬스터 Chase/Lunge가 유지되고, Console 신규 오류 0건, 변경 파일이 예상 범위와 일치

## Play Mode 검증 시나리오

### 플레이어가 몬스터를 공격

1. `MonsterTest`에서 지네가 감지 거리 안에서 Chase하는지 확인한다.
2. 지네 Trigger가 화면 중앙에 오도록 조준하고 한 발씩 발사한다.
3. 발사마다 `MonsterHealth.CurrentHealth`가 `3 → 2 → 1 → 0`으로 감소하는지 확인한다.
4. 한 발에서 카메라 Ray와 총구 Ray가 모두 몬스터를 감지해도 피해는 한 번만 적용되는지 확인한다.
5. 총구와 지네 사이에 일반 Collider가 있으면 Collider에서 사격이 멈추고 지네 HP가 유지되는지 확인한다.
6. 중력·구역 Trigger를 사이에 두어도 그것이 총알을 막지 않는지 확인한다.

### 몬스터가 플레이어를 공격

1. 지네가 접근하거나 Lunge하여 `Nav Target` Trigger가 Player Capsule과 겹치게 한다.
2. 최초 접촉에서 플레이어 HP가 즉시 `1` 감소하는지 확인한다.
3. 계속 겹친 상태에서 1초보다 빠르게 중복 감소하지 않는지 확인한다.
4. 1초 이상 접촉을 유지하면 다음 피해가 한 번 발생하는지 확인한다.
5. 지네 HP를 0으로 만든 뒤 다시 접촉해도 플레이어 HP가 감소하지 않는지 확인한다.

### 회귀

- 플레이어 걷기·Sprint·Crouch·점프·사격 애니메이션과 Tracer가 기존처럼 동작한다.
- 카메라 조준점과 총구 앞 장애물 우선 규칙이 유지된다.
- 지네의 RouteMove·Chase·Attack·Lunge와 procedural 몸체 Follow가 유지된다.
- Trigger가 Player를 물리적으로 밀거나 지네 이동을 막지 않는다.

## 완료 기준

- 플레이어 사격과 몬스터 접촉이 각각 상대 체력의 실제 상태 변화를 만든다.
- `Nav Target` Trigger가 보이는 지네 전면과 함께 이동하며 허공 판정이나 과도한 범위를 만들지 않는다.
- 환경 Trigger가 사격을 가로막지 않고 일반 장애물은 계속 사격을 막는다.
- 사격 한 번당 몬스터 피해 한 번, 접촉 쿨다운당 플레이어 피해 한 번만 발생한다.
- 죽은 몬스터는 추가 접촉 피해를 주지 않는다.
- Unity 스크립트 재컴파일과 가능한 범위의 C# 빌드가 오류 없이 완료된다.
- Play Mode에서 신규 Console 오류가 없고 기존 플레이어·몬스터 핵심 동작이 회귀하지 않는다.
- 최종 diff에 `Original_GamePlayScene`, Build Settings, `ProjectSettings` 또는 외부 에셋 원본 변경이 없다.

## 하지 않을 것과 후속 후보

이번 구현에 포함하지 않는다.

- 플레이어 HP UI와 몬스터 HP Bar
- 피격·사망 애니메이션, 사운드, VFX와 카메라 흔들림
- 플레이어 넉백, 무적 시간, 경직과 입력 차단
- 사망 후 RespawnController 연결
- 지네 몸통의 부위별 Collider와 부위별 피해 배율
- 접촉 Trigger와 공격 프레임 전용 Hitbox 분리
- Monster/Player 전용 Layer와 Physics Layer Matrix 설계
- 탄약, 재장전, 치명타와 범용 전투 프레임워크
- 실제 WebGL 빌드

단일 `Nav Target` Trigger가 플레이 감각상 너무 넓거나 평상시 접촉까지 공격으로 판정되면, 후속 단계에서 `Body Hitbox`와 Lunge 활성 구간의 `Attack Hitbox`를 분리한다. 이 판단은 첫 Play Mode 결과를 근거로 한다.

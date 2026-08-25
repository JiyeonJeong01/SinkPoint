# TPS 이동형 Tracer Bolt 빠른 검증 계획

- 현재 상태: 계획됨
- 작성일: 2026-08-25
- 계획 프로필: `standard` — 실제 hitscan 계약은 그대로 두고, 현재의 즉시 전체 선분 Tracer만 짧게 이동하는 시각 Bolt로 교체해 극단 피치의 가독성을 빠르게 비교한다.
- 선행 판단: 거리·각도에 따라 Tracer를 숨기는 정책은 화면에서 일관되지 않아 [폐기](../04_discarded/tps_shot_direction_parallax_tracer_plan.md)한다.

## 목표

`PlayerCombatController`의 한 발 즉발 명중은 바꾸지 않는다. 기존 `LineRenderer`가 총구부터 `shotEnd`까지 전체 선분을 `0.05초` 동안 고정해 보여 주던 방식을, 실제 발사 경로를 따라 이동하는 짧은 밝은 선분(Bolt)으로 바꿔 수평·상향·하향·근거리에서의 시각적 수용성을 확인한다.

이 단계는 완성 연출이 아니라 빠른 Play Mode 비교용이다. 모델 리그, Aim IK, 카메라 위치, 크로스헤어와 실제 사격 규칙은 건드리지 않는다.

## 유지할 계약

```text
Camera center Ray -> aimPoint
Muzzle Ray        -> shotEnd / damage / physics push / zero-G recoil
Muzzle Flash      -> MuzzleVfxAnchor의 로컬 회전
Tracer Bolt       -> 발사 시 저장한 muzzle-origin ~ shotEnd 사이를 시각적으로 이동
```

- `aimRay`, `shotDirection`, `shotEnd`, 피해, 탄약, 물리 밀기와 무중력 반작용은 현행 구현 그대로다.
- Bolt의 이동 속도와 표시 시간은 시각 전용이다. 실제 명중을 지연하거나 재판정하지 않는다.
- Bolt의 시작 위치는 발사 당시 `MuzzleVfxAnchor.position`(없으면 `muzzle.position`)이고, 끝점은 발사 당시 `shotEnd`로 고정한다.
- 기존 `M_ShotTracer` 머티리얼과 주황색 색상, 하나의 `LineRenderer`를 재사용한다. 새 프리팹·머티리얼·투사체·풀은 만들지 않는다.

## 구현 방향

`LineRenderer`를 삭제하지 않고 매 프레임 두 점만 갱신한다.

- 발사 시 origin, end, normalized direction, 전체 거리와 표시 시작 시각을 저장한다.
- head는 `origin`에서 `end`까지 `visualTracerBoltSpeed`으로 이동한다.
- tail은 head 뒤 `visualTracerBoltLength`만큼 두되, 이동 초기에는 origin보다 뒤로 가지 않게 clamp한다.
- head가 end에 도착하면 `tracerBoltImpactHoldDuration`만큼 끝점 근처의 짧은 선분을 유지한 뒤 숨긴다.
- 근거리도 거리·각도 조건으로 숨기지 않는다. Bolt의 최소 시각 이동 시간 `minTracerBoltTravelDuration`을 두어 한 프레임도 보이지 않는지 확인한다.
- 연사 중 새 발이 나오면 단일 `LineRenderer`의 이전 Bolt를 새 발로 교체한다. 이 테스트에서는 다중 Bolt를 보존하지 않는다.

`PlayerCombatController`의 기존 시차 억제 설정과 관찰값은 제거한다.

- 제거: `minTracerVisualDistance`, `maxTracerCameraAngle`, `lastTracerVisualDistance`, `lastTracerCameraAngle`, `lastTracerSuppressedByParallax`
- 추가 후보: `visualTracerBoltSpeed`, `visualTracerBoltLength`, `minTracerBoltTravelDuration`, `tracerBoltImpactHoldDuration`
- 추가 관찰값: 마지막 Bolt 거리, 실제 시각 이동 시간, 현재 Bolt 진행률(필요할 때만 Inspector 표시)

## 범위

### 포함

- `PlayerCombatController`의 단일 LineRenderer를 사용한 이동형 Bolt 상태와 LateUpdate 갱신
- 근거리에서의 최소 시각 이동 시간
- 기존 Muzzle Flash와 주황색 Tracer 색 유지
- 일반 중력·무중력에서 수평, 상향, 하향 단발/연사 비교

### 하지 않을 것

- 실제 Raycast, 명중점, 피해, 물리 밀기, 반작용, 탄약 또는 재장전 변경
- 카메라 거리/오프셋/피치, 크로스헤어 또는 Build Settings 변경
- Aim IK, 무기/손/상체 Transform 회전, 애니메이션 수정
- 화면 공간 Tracer, 곡선 경로, 카메라 기준 시작점, 새 VFX 에셋, 다중 Bolt 풀
- `Original_GamePlayScene`, Collider, Packages, ProjectSettings 수정 및 별도 승인 없는 WebGL 빌드

## 실행 단계

1. `[기존 억제 정책 제거]` 거리·각도에 따른 Tracer 숨김과 해당 Inspector 값을 제거한다.  
   → verify: 근거리·극단 피치에서도 한 발이 Bolt 시도까지 도달하며, 탄약·명중 결과는 동일하다.

2. `[Bolt 상태 도입]` 한 발의 origin/end/direction/distance/start time을 저장하고, 기존 LineRenderer를 head/tail 두 점으로 사용한다.  
   → verify: 중거리에서 긴 고정 선 대신 짧은 선분이 발사 경로를 이동한다.

3. `[근거리 가시성]` 최소 시각 이동 시간과 impact hold를 적용한다.  
   → verify: 약 0.57m 바닥 발사도 조건부 숨김 없이 최소 한 프레임 이상 Bolt 또는 끝점 선분을 보인다.

4. `[연사 경계]` 새 발이 이전 Bolt를 안전하게 덮어쓰고, 표시 종료/비활성화 시 LineRenderer가 남지 않게 한다.  
   → verify: 현재 `fireInterval 0.13초` 연사에서 예외·잔상·LineRenderer 고정선이 없다.

5. `[상태 독립성]` 일반 중력과 무중력에서 같은 시각 타이밍을 사용한다.  
   → verify: 중력 방향은 Bolt 표시 여부·방향을 바꾸지 않고 실제 zero-G recoil은 기존 `shotDirection`을 유지한다.

6. `[사용자 Play Mode 비교]` 수평 원거리, 상향 하늘/천장, 하향 근거리 바닥·벽, 중거리 몬스터를 단발·연사로 비교한다.  
   → verify: 긴 선분보다 덜 거슬리는지 사용자가 판단하며, 수용 불가면 코드/Prefab 변경을 롤백하고 이 계획을 폐기한다.

## 초기 값과 조정 기준

- `visualTracerBoltSpeed`: `35m/s`부터 시작한다.
- `visualTracerBoltLength`: `0.35m`부터 시작한다.
- `minTracerBoltTravelDuration`: `0.04초`부터 시작한다.
- `tracerBoltImpactHoldDuration`: `0.02초`부터 시작한다.

이 값은 실제 탄속이 아니라 가시성 비교용이다. 첫 Play Mode에서 지연처럼 보이면 최소 시간/hold를 낮추고, 너무 긴 선처럼 보이면 Bolt 길이를 낮춘다. 구조가 불편하면 값을 계속 다듬지 않고 이 접근 자체를 중단한다.

## 완료 기준

- 실제 hitscan 결과와 탄약·피해·물리·무중력 반작용에 회귀가 없다.
- 모든 각도에서 조건부로 사라지는 고정 전체 선분은 없다.
- 중거리에서 이동형 Bolt가 기존 고정 LineRenderer보다 자연스럽다는 사용자 Play Mode 확인이 있다.
- Unity 재컴파일과 새 Play Mode에서 이번 변경으로 인한 Error가 없다.
- 사용자 확인 전에는 완료 문서 이동이나 Usage Record 추가를 하지 않는다.

## 필요한 가정

- 이번 검증의 평가는 모델 총열과 한 직선으로 완전히 일치하는지가 아니라, 긴 정적 선분보다 TPS 화면에서 덜 부자연스러운지다.
- 단일 LineRenderer에서 연사 중 이전 Bolt가 교체되는 것은 MVP 비교 단계에 허용된다.

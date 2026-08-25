# TPS 사격 방향성·근거리 시차 및 Tracer 정책 실행 계획

- 현재 상태: 계획됨
- 작성일: 2026-08-25
- 계획 프로필: `deep` — 카메라 조준, 실제 hitscan, 모델 배럴, Muzzle Flash와 Tracer가 서로 다른 방향 기준을 사용하므로 책임 경계와 시각적 예외 정책을 먼저 고정한다.
- 선행 작업: [플레이어 Muzzle Flash VFX 테스트 및 게임플레이 Tracer 구성 완료 계획](../03_completed/player_muzzle_flash_vfx_test_plan.md)

## 목표

TPS 카메라의 중앙 조준 정확도와 총구 기반 실제 사격 판정을 보존하면서, 근거리 바닥·벽 또는 극단 피치에서 Tracer가 모델과 크게 분리되거나 떠 보이는 현상을 제거한다. 모델 Aim IK를 새로 구축하지 않고도 정상 거리에서는 Tracer를 유지하고, 하나의 시각선으로 표현하기 불가능한 큰 시차 구간에서는 Muzzle Flash만 사용한다.

## 현재 구조와 확인된 원인

```text
Camera center Ray
  -> 카메라가 바라보는 aimPoint 결정
  -> Muzzle에서 aimPoint로 shotDirection 계산
  -> Muzzle Ray로 실제 shotEnd 결정
  -> 피해·물리 밀기·반작용 적용
  -> Muzzle Flash와 Tracer 표현
```

카메라가 플레이어 가까운 지면을 조준하면 `aimPoint`가 총구와 지나치게 가까워진다. 측정된 피치 약 `80도` 사례는 다음과 같다.

- Muzzle에서 실제 shotEnd까지 거리: 약 `0.57m`
- 카메라 Ray와 실제 `shotDirection` 차이: 약 `40도`
- 모델 배럴과 실제 `shotDirection` 차이: 약 `102도`

이 상태에서는 Tracer 하나가 카메라 조준점, 실제 명중점과 모델 배럴을 동시에 통과할 수 없다. 일부 선분은 캐릭터나 지형의 Depth Test에 가려져 중간부터 떠 있는 것처럼 보일 수도 있다. 따라서 단순 위치·회전 오프셋을 추가하지 않고, 물리 방향과 시각 표현 가능 여부를 분리한다.

## 책임과 인터페이스

### 유지할 실제 사격 계약

- 카메라 중앙 Ray가 조준 후보점을 결정한다.
- Muzzle에서 후보점으로 쏘는 두 번째 Ray가 실제 명중점, 피해, 물리 밀기와 반작용 방향을 결정한다.
- 가까운 지면을 맞히기 위해 최소 사거리나 임의 원거리 보정점을 물리 판정에 강제하지 않는다.
- 카메라 피치 범위 `-85 ~ 85도`는 변경하지 않는다.

### Muzzle Flash 계약

- Flash 위치는 `MuzzleVfxAnchor`를 계속 사용한다.
- Flash는 총기 모델의 배럴 표현으로 취급하여 Anchor의 로컬 회전을 따른다.
- 현재 임시 적용된 `muzzleFlashInstance.transform.rotation = Quaternion.LookRotation(shotDirection, GravityUp)`은 제거한다. 근거리에서 실제 `shotDirection`이 배럴 반대편에 가까워져 Flash가 비정상 회전하는 것을 막는다.

### Tracer 계약

- 실제 Tracer 시작점은 현재 `MuzzleVfxAnchor.position`, 끝점은 발사 당시 `shotEnd`로 유지한다.
- 정상 시차 구간에서만 Tracer를 표시한다.
- 기본 표시 조건은 실제 선분 거리 `2m 이상`이고 카메라 Ray와 `shotDirection`의 각도가 `20도 이하`인 경우다.
- 조건을 벗어나면 Tracer만 숨기고 Muzzle Flash, 탄약 소비, 피해, 물리 밀기, 사운드와 반작용은 그대로 실행한다.

`PlayerCombatController`에는 다음 private serialized 설정과 Inspector 런타임 관찰값을 추가한다. Public API는 변경하지 않는다.

- `float minTracerVisualDistance = 2f`
- `float maxTracerCameraAngle = 20f`
- `float lastTracerVisualDistance`
- `float lastTracerCameraAngle`
- `bool lastTracerSuppressedByParallax`

## 범위

### 포함

- 카메라 Ray, 실제 `shotDirection`, 모델 배럴 방향과 shotEnd 거리의 런타임 가시화
- Muzzle Flash의 모델 배럴 정렬 복구
- 거리·각도 기반 Tracer 표시 억제
- 수평·극단 피치, 근거리·중거리·원거리와 일반/무중력 상태 비교

### 하지 않을 것

- 모델 Rig, Animation Clip, Aim Offset 또는 손·무기 IK 신규 구현
- hitscan 명중 규칙, 피해, 물리 밀기 또는 반작용 방향 변경
- 카메라 Pitch 제한 축소, Crosshair 이동 또는 근거리 조준점 강제 보정
- Tracer 머티리얼의 Depth Test 무시, 화면 공간 Tracer 또는 투사체 시스템 전환
- `Original_GamePlayScene`, Collider, Packages, ProjectSettings, Build Settings 수정
- 별도 승인 없는 WebGL 빌드

## 실행 단계

1. `[기준값 가시화]` 실제 발사마다 Tracer 거리, 카메라–shot 각도와 억제 여부를 Inspector에 기록한다.  
   → verify: 수평 원거리와 극단 피치 근거리에서 값 차이가 재현되고 물리 결과는 바뀌지 않는다.

2. `[Flash 방향 복구]` 런타임 Flash의 `shotDirection` 강제 회전을 제거하고 Anchor 로컬 회전으로 복구한다.  
   → verify: 위·아래 극단 조준에서도 Flash가 모델 배럴 반대 방향으로 회전하지 않는다.

3. `[Tracer 표시 정책]` `shotEnd - tracerOrigin` 거리와 `Vector3.Angle(aimRay.direction, shotDirection)`을 계산하여 두 기본 임계값을 모두 통과할 때만 Tracer를 표시한다.  
   → verify: 약 `0.57m / 40도` 사례에서는 Flash만 보이고, 정상 원거리 발사에서는 기존 주황색 Tracer가 표시된다.

4. `[기존 시작점 추적 보존]` 표시가 허용된 Tracer는 시작점을 매 LateUpdate마다 현재 Anchor에 맞추고 끝점은 발사 당시 위치에 고정한다.  
   → verify: 애니메이션 중 시작점은 총구를 따라가며 끝점 이동 거리는 `0`이다.

5. `[상태 독립성 검증]` 일반 중력과 무중력에서 같은 거리·각도 정책을 적용한다.  
   → verify: 중력 방향이나 `PresentationUp`이 Tracer 표시 판정을 바꾸지 않고 반작용도 기존 `shotDirection`을 유지한다.

6. `[사용자 Play Mode 확정]` 수평·상향·하향 조준과 근거리 지면·벽, 중거리 몬스터, 원거리 허공을 단발·연사로 확인한다.  
   → verify: 정상 사격의 가독성을 유지하면서 구조적으로 왜곡되는 선만 숨겨지고, 필요하면 두 Inspector 임계값만 조정한다.

7. `[완료 처리]` 최종 임계값과 사용자 체감 결과를 기록한다.  
   → verify: 사용자 확인 후 문서를 `03_completed`로 이동하고 Usage Record를 하나의 완료 작업으로 남긴다.

## 실패 및 경계 처리

- Tracer 억제는 시각 효과에만 적용하며 `FireShot()` 성립 여부나 탄약을 되돌리지 않는다.
- `showShotTracer == false`이면 시차 계산 결과와 무관하게 Tracer를 표시하지 않는다.
- Muzzle Flash 슬롯이 비어 있어도 Tracer 정책과 실제 사격은 계속 작동한다.
- 거리와 각도 중 하나라도 임계값을 벗어나면 Tracer를 숨겨 경계 근처의 비정상 선분을 허용하지 않는다.
- 임계값 조정만으로 정상 구간이 확보되지 않으면 Aim IK를 별도 후속 계획으로 분리하고 이번 작업에서 확장하지 않는다.

## 완료 기준

- 정상 중·원거리 조준에서는 주황색 Tracer가 총구에서 실제 shotEnd까지 표시된다.
- 근거리 지면·벽 또는 큰 시차 구간에서는 Muzzle Flash만 표시되고 떠 있는 Tracer가 나타나지 않는다.
- Muzzle Flash는 모든 피치에서 모델 배럴 방향을 유지한다.
- Crosshair 기반 명중, 피해, 물리 밀기, 탄약, 사운드와 무중력 반작용이 기존과 같다.
- Unity 재컴파일과 새 Play Mode에서 이번 변경으로 인한 Error가 없다.
- 최종 체감은 사용자의 Play Mode 확인으로 확정한다.

## 필요한 가정

- 극단 피치 근거리에서 Tracer를 숨기는 것이 Crosshair 명중 규칙이나 카메라 Pitch를 바꾸는 것보다 MVP에 적합하다.
- `2m`와 `20도`는 측정된 실패 사례(`0.57m`, `40도`)를 확실히 제외하는 초기 기본값이며, 최종값은 Inspector에서 사용자 체감으로 조정한다.

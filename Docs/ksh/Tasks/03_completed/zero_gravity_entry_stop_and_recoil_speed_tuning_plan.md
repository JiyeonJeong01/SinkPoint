# 무중력 진입 관성 유지 및 사격 반작용 속도 조정 실행 계획

문서 작성일: 2026-08-25
현재 상태: 완료 — 코드·컴파일·기본 Play Mode 오류 검증 완료, 실제 조작 체감 확인은 사용자 확인 대기

계획 프로필: `standard`

기준 문서:

- [SinkPoint MVP 게임 기획서](../../../GameDesign_MVP.md)
- [플레이어·중력 파트 마스터 계획](../../Player_Gravity_Master_Plan.md)
- [무중력 무기 발사 반작용 구현 완료 계획](../03_completed/zero_gravity_weapon_recoil_implementation_plan.md)

## 1. 목표

무중력 상태에 진입할 때 기존 이동·낙하 관성이 갑자기 사라져 어색해지는 문제를 없앤다. 진입 전 Rigidbody의 선속도·각속도를 유지하고, 무중력 사격 반작용은 발사당 가속량을 유지한 채 최고 속도만 소폭 올려 더 긴 이동 누적을 허용한다.

## 2. 범위

- `PlayerController`의 무중력 진입 시 선속도·각속도 유지 경로 확인 및 필요한 최소 보정
- `maxZeroGravityRecoilSpeed`의 소폭 상향
- Inspector 런타임 값으로 무중력 진입 전후 관성 유지와 반작용 속도 상한 관찰
- 컴파일과 Play Mode 회귀 검증

## 3. 하지 않을 것

- 그래플링 훅, 로프 물리 또는 무중력 이동 입력 추가
- 발사당 반작용 크기 `zeroGravityRecoilVelocityChange`와 연사 간격 변경
- 일반 중력 상태의 무기 반동, 카메라 흔들림, 애니메이션, VFX 또는 사운드 변경
- Zone·Trigger·GravityPreset 구성 변경
- `Original_GamePlayScene`, Collider, Packages, ProjectSettings 또는 Build Settings 변경

## 4. 현재 구현과 가정

- 현재 `PlayerController.EnterZeroGravity()`는 상태 진입 시 Rigidbody 선속도와 각속도를 한 번 `0`으로 초기화하므로, 무중력 진입 전의 관성이 사라진다.
- `ZeroGravityMotionState`는 진입 이후 매 물리 프레임 속도를 덮어쓰지 않으므로, 진입 초기화만 제거하면 진입 전 관성과 사격 반작용 등 외부 힘으로 생긴 이후 관성을 모두 보존할 수 있다.
- 현재 사격 반작용은 발사당 속도 변화 `0.3`, 전체 속력 상한 `4.0`를 사용한다.
- 이번 요청의 “가속을 좀 더”는 발사당 변화량을 키우지 않고, 최고 속도를 소폭 높여 더 오래 누적할 수 있게 한다는 의미로 해석한다.

코드상 진입 속도 초기화가 있으므로, 실제 Play Mode에서 관성이 유지되지 않는다면 상태 진입 호출 시점, 중력 전환 중 속도 고정, 또는 중력 전환 완료 뒤의 물리 처리 중 어느 경로가 속도를 제거하는지 먼저 확인한다.

## 5. 책임 경계

```text
GravityPreset / GravityManager
    └─ 무중력 상태 선택 및 전환 완료
        └─ PlayerMotionStateMachine
            └─ ZeroGravityMotionState.Enter()
                └─ PlayerController.EnterZeroGravity()
                    └─ Rigidbody 선속도·각속도 유지

PlayerCombatController.FireShot()
    └─ PlayerController.TryApplyZeroGravityRecoil(반대 발사 방향)
        └─ 현재 속도와 maxZeroGravityRecoilSpeed를 비교
            └─ 허용된 VelocityChange만 Rigidbody에 적용
```

- `GravityManager`와 `GravityPreset`은 무중력 선택·전환만 소유하며, 플레이어 관성 유지 정책을 직접 중복하지 않는다.
- `PlayerController`는 무중력 진입 관성 유지와 반작용 상한 계산을 단독 소유한다.
- `PlayerCombatController`는 실제 발사 확정과 방향 전달만 담당한다.

## 6. 조정 계약

- 무중력에 새로 진입해도 진입 직전의 선속도·각속도를 초기화하거나 감쇠하지 않는다.
- 무중력 진입 후에도 속도를 매 프레임 제거하지 않는다. 진입 전 관성과 발사 반작용으로 생긴 속도는 모두 유지한다.
- `maxZeroGravityRecoilSpeed`는 현재값 `4.0`를 유지한다. 이 계획의 관성 수정과 별개로 추가 속도 튜닝은 하지 않는다.
- 반작용 상한 초과 상태에서는 현재 속력을 더 키우는 발사를 차단하고, 속력을 줄이는 방향의 반작용은 기존처럼 허용한다.

## 7. 실행 순서

1. `[무중력 진입 관성 소실 재현 및 상태 경로 확인]` → verify: `[무중력 Preset 진입 전후 Motion State, Rigidbody 선속도·각속도, 중력 전환 상태를 Inspector에서 비교]`
2. `[진입 시 1회 속도 초기화 경로만 제거 또는 우회]` → verify: `[낙하·이동·반작용 중 진입해도 선속도·각속도가 유지되고, 진입 뒤에도 속도를 반복 제거하지 않음]`
3. `[기존 반작용 속도 상한 4.0 유지 확인]` → verify: `[발사당 변화량은 유지하면서 연사 시 속도는 4.0까지 누적하고, 상한 이상 추가 가속은 차단]`
4. `[컴파일 및 Play Mode 회귀 확인]` → verify: `[일반 중력·중력 전환 중 반작용 미발동, 무중력 진입 관성 유지, 단발·연사 이동감, Console 신규 오류 없음]`

## 8. 완료 기준

- 낙하·이동·기존 반작용 속도가 있는 상태에서 무중력으로 진입하면 플레이어의 선속도·각속도가 유지된다.
- 관성을 유지한 상태에서도 발사한 첫 탄부터 반대 방향 반작용이 정상 적용되고, 이후 관성이 유지된다.
- 연사 시 최고 속도는 `4.0`를 넘지 않아 무한 가속하지 않는다.
- 비무중력 또는 중력 전환 중에는 플레이어 반작용이 적용되지 않는다.
- 변경된 런타임 어셈블리가 컴파일되고, 새 Console 오류 없이 사용자가 Play Mode 체감을 확인한다.

## 9. 실행 결과 (2026-08-25)

- `PlayerController.EnterZeroGravity()`에서 Rigidbody 선속도·각속도를 `Vector3.zero`로 초기화하던 두 줄을 제거했다. 상태 진입 후 `ZeroGravityMotionState.FixedTick()`은 기존대로 속도를 덮어쓰지 않으므로, 진입 전 이동·낙하 관성과 이후 반작용 속도가 유지된다.
- `maxZeroGravityRecoilSpeed` 기본값은 `4f`로 유지했고, 발사당 반작용 변화량 `0.3f`는 변경하지 않았다. 기존 작업 트리의 `4.f` 표기 오류는 C# 컴파일 오류를 일으켜 `4f`로 정정했다.
- `dotnet build .\\Assembly-CSharp.csproj --no-restore`는 오류 0건(기존 경고 28건)으로 성공했고, `git diff --check`도 통과했다.
- Console을 비운 새 Unity Play Mode 실행·종료에서 신규 Error는 0건이었다. 자동 입력만으로는 이동·낙하 중 Zone 05 진입 뒤의 체감을 확인할 수 없으므로, 해당 수동 확인은 남아 있다.

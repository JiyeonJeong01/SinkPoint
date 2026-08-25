# 플레이어 Muzzle Flash VFX 테스트 및 게임플레이 Tracer 구성 실행 계획

- 현재 상태: 완료 — 사용자 Play Mode 기능·크기 확인 완료, 극단 피치 방향성은 후속 계획으로 분리
- 작성일: 2026-08-25
- 계획 프로필: `standard` — 기존 hitscan 사격을 보존하면서 Player Prefab의 Muzzle Flash 등록 책임과 게임플레이 Tracer 표현을 함께 정리한다.

## 목표

`PlayerCombatController`의 실제 발사 시점에 XR Interaction Starter Kit의 Muzzle Flash를 재생한다. Player Prefab에는 VFX 프리팹 하나를 교체 등록할 수 있는 단일 슬롯을 두고, 다음 세 후보를 Play Mode에서 비교한다.

- `Assets/XRI Starter Kit/Assets/Interactables/Guns/HandGun/PistolFlash.prefab`
- `Assets/XRI Starter Kit/Assets/Interactables/Guns/Rifle/Prefabs/RifleFlash.prefab`
- `Assets/XRI Starter Kit/Assets/Interactables/Guns/Terraformer_Weapon_A/TerraFormer Flash.prefab`

카메라 Ray와 Muzzle Ray는 실제 조준·명중 판정이므로 유지한다. 기존 LineRenderer Tracer는 별도 Tracer 에셋으로 교체하지 않고 게임플레이 발사 표현으로 그대로 사용한다. 기본 표시 색은 명중 여부와 무관하게 밝은 노란빛 주황색 `#FFB52E`로 통일한다.

## 범위

### 포함

- `PlayerCombatController`의 단일 Muzzle Flash 프리팹 슬롯과 재생 처리
- Player Prefab의 `MuzzleVfxAnchor`와 기본 VFX 참조
- 실제 발사 1회와 Muzzle Flash 재생 1회의 연결
- LineRenderer Tracer의 기본 활성화와 밝은 노란빛 주황색 구성
- 세 VFX 후보의 수동 교체 비교 절차

### 하지 않을 것

- XR Interaction Starter Kit 원본 VFX 프리팹 수정
- 카메라 Ray, Muzzle Ray, 명중·피해·물리 밀기·무중력 반작용 로직 변경
- Tracer 코드나 기존 LineRenderer 컴포넌트 제거
- Tracer 전용 신규 에셋 제작·구매·검색 또는 머티리얼 교체
- VFX 배열, enum, 런타임 선택 UI 또는 범용 VFX 시스템 추가
- `Original_GamePlayScene`, Collider, Packages, ProjectSettings, Build Settings 수정
- 별도 승인 없는 WebGL 빌드

## 책임과 인터페이스

### Player Prefab

- 기존 `Muzzle`은 Ray와 Tracer의 시작점 책임을 유지한다.
- `Muzzle` 아래에 `MuzzleVfxAnchor`를 추가한다.
- Anchor의 Transform으로 VFX 위치·회전·전체 크기를 조정한다.
- `PlayerCombatController`의 단일 슬롯에 현재 시험할 VFX 프리팹을 등록한다.
- 기본 시험 대상은 현재 자동소총형 사격에 맞춰 `RifleFlash`로 한다.

### PlayerCombatController

다음 private serialized 설정을 추가한다. Public API는 추가하지 않는다.

- `GameObject muzzleFlashPrefab`
- `Transform muzzleFlashAnchor`
- `float muzzleFlashVisibleDuration = 0.12f`

런타임에는 선택된 VFX를 한 번만 생성하고 자식 `ParticleSystem`을 캐시한다. XR 원본 프리팹에 포함된 `DestroyAfterTime`은 런타임 복제본에서만 비활성화하여 재사용 인스턴스가 스스로 파괴되지 않게 한다. 실제 발사마다 캐시된 시스템을 처음부터 다시 재생하고, 표시 시간이 끝나면 VFX 루트를 비활성화하여 포함된 Point Light도 함께 끈다.

### Ray와 Tracer 정책

- 카메라 Ray: 화면 중앙 조준점 계산에 사용하므로 유지한다.
- Muzzle Ray: 실제 명중, 피해, 물리 밀기와 반작용 방향 계산에 사용하므로 유지한다.
- LineRenderer Tracer: 실제 발사 방향과 도달 지점을 보여주는 게임플레이 시각 효과로 사용한다.
- 코드 기본값과 Player Prefab 직렬화 값의 `showShotTracer`를 모두 `true`로 유지한다.
- 기존 `hitTracerColor`와 `missTracerColor`는 모두 `#FFB52E`(`RGB 1.0, 0.71, 0.18`, Alpha `1.0`)로 맞춰 명중 여부에 따라 색이 달라지지 않게 한다.
- 기존 `M_ShotTracer`의 흰색 Sprite/Default 머티리얼과 LineRenderer vertex color 조합을 사용하며 신규 Tracer 머티리얼은 만들지 않는다.

## 실행 단계

1. `[게임플레이 Tracer 구성]` `showShotTracer`의 코드 기본값과 Player Prefab 값을 `true`로 유지하고, `hitTracerColor`와 `missTracerColor`를 모두 밝은 노란빛 주황색 `#FFB52E`로 맞춘다. 기존 LineRenderer와 `M_ShotTracer` 머티리얼 참조는 보존한다.  
   → verify: 기본 Play Mode에서 명중·빗나감 모두 같은 밝은 노란빛 주황색 Tracer가 표시되며, Inspector에서 토글을 끄면 즉시 숨겨진다.

2. `[VFX Anchor 구성]` Player Prefab의 `Muzzle` 아래에 `MuzzleVfxAnchor`를 로컬 위치 0, 로컬 회전 identity 기준으로 추가한다.  
   → verify: Anchor 조정 전후로 기존 Muzzle Transform, 사격 Ray와 Tracer 시작점이 변하지 않는다.

3. `[단일 교체 슬롯 추가]` `PlayerCombatController`에 VFX 프리팹, Anchor, 표시 시간 슬롯을 추가하고 Player Prefab에 연결한다.  
   → verify: Prefab 직렬화 참조에 Missing이 없고 기본 슬롯이 `RifleFlash`를 가리킨다.

4. `[VFX 인스턴스 준비]` 등록된 프리팹을 Anchor 아래에 한 번 생성한다. 데모 프리팹에 저장된 루트 위치·회전은 사용하지 않고 Anchor에 정렬하며, 프리팹별 원본 스케일은 보존한다. 런타임 복제본의 `DestroyAfterTime`을 비활성화하고 대기 중에는 루트를 비활성화한다.  
   → verify: XR 원본 프리팹은 수정되지 않고, 런타임 인스턴스가 `0.1초` 뒤 파괴되지 않으며 연사마다 `Instantiate` 또는 `Destroy`가 반복되지 않는다.

5. `[실제 발사 연결]` 탄약을 소비하는 `FireShot()` 1회마다 VFX 루트를 활성화하고 모든 자식 ParticleSystem을 `Stop/Clear/Play`하여 처음부터 재생한다.  
   → verify: 명중 여부와 관계없이 실제 발사마다 한 번 재생되고, 재장전 또는 빈 탄창 상태에서는 재생되지 않는다.

6. `[종료 처리]` `muzzleFlashVisibleDuration`이 지나면 VFX 루트를 비활성화하고, 컴포넌트가 비활성화될 때도 VFX와 Tracer를 즉시 숨긴다.  
   → verify: Particle과 Point Light가 대기 중 계속 남지 않으며 재활성화 후 첫 발도 정상 재생된다.

7. `[누락 참조 처리]` VFX 프리팹 또는 Anchor가 비어 있으면 시작 시 경고를 한 번만 남기고 VFX 재생만 건너뛴다.  
   → verify: VFX 미등록 상태에서도 사격, 탄약, 피해, 사운드, Tracer와 무중력 반작용이 정상 작동한다.

8. `[후보 비교]` 단일 슬롯을 `PistolFlash`, `RifleFlash`, `TerraFormer Flash` 순서로 교체하고 각 후보를 새 Play Mode 실행에서 확인한다.  
   → verify: 총구 정렬, 크기, 색상, 밝기, 연사 깜박임과 카메라 시야 방해 여부를 후보별로 기록한다.

9. `[최종 검증과 문서 상태 정리]` 컴파일, Console, Prefab diff와 사용자 Game View 확인 결과를 정리한다.  
   → verify: 사용자 확인 전에는 최종 후보와 Anchor Transform을 확정하지 않으며, 완료 후에만 계획서를 `03_completed`로 이동하고 의미 있는 완료 작업으로 Usage Record를 남긴다.

## 실패 및 경계 처리

- VFX 슬롯이 비어 있어도 전투 컴포넌트를 비활성화하지 않는다.
- VFX 재생 실패가 Raycast, 탄약 소비, 피해, 물리 밀기 또는 반작용을 막지 않게 한다.
- Inspector에서 `showShotTracer`를 끈 경우 LineRenderer가 이전 프레임 상태로 남지 않게 즉시 숨긴다.
- Tracer 색상은 명중 여부를 디버그 색으로 표현하지 않고, 게임플레이 연출 색 `#FFB52E`로 일관되게 유지한다.
- 현재 Player Prefab의 실제 `fireInterval`인 `0.13초`를 변경하지 않는다.
- 후보 프리팹의 데모용 루트 위치·회전은 총구 배치에 사용하지 않는다.
- 후보 프리팹의 `DestroyAfterTime`은 원본 자산에서 제거하지 않고 런타임 복제본에서만 비활성화한다.
- VFX 크기나 방향이 맞지 않으면 원본 에셋 대신 `MuzzleVfxAnchor`만 조정한다.

## 검증 기준

- `Assembly-CSharp.csproj`와 `Assembly-CSharp-Editor.csproj`가 새 컴파일 오류 없이 빌드된다.
- 새 Play Mode 실행에서 Console에 이번 변경으로 인한 Error가 없다.
- Tracer 기본 ON 상태에서 명중과 빗나감이 모두 같은 밝은 노란빛 주황색으로 표시된다.
- Inspector에서 Tracer를 끄더라도 조준, 명중, 피해, 탄약, 물리 밀기와 무중력 반작용은 기존과 같다.
- Muzzle Flash와 Tracer가 동시에 표시될 때 색감과 발사 방향이 자연스럽고 카메라 시야를 과도하게 가리지 않는다.
- 세 VFX 후보가 실제 발사마다 총구에서 재생되고 대기 중 Particle과 Point Light가 남지 않는다.
- 현재 `0.13초` 연사에서 매 실제 발사에 맞춰 VFX가 다시 시작된다.
- 일반 중력과 무중력 모두 기존 사격 동작을 보존한다.
- 최종 후보와 Anchor Transform은 사용자의 Game View 비교 결과로 확정한다.

## 필요한 가정

- 세 후보는 같은 RifleFlash 계열의 비반복 Particle 프리팹이므로 동일한 재생 제어를 적용할 수 있다.
- Muzzle Flash와 기존 Tracer를 함께 게임플레이 발사 피드백으로 사용하며, Tracer의 명중·빗나감 디버그 색상 구분은 사용하지 않는다.
- 구현 승인에 따라 이 문서를 `Docs/ksh/Tasks/02_in-progress`로 이동하고 작업을 시작했다.

## 구현 및 자동 검증 결과

- Player Prefab에 `MuzzleVfxAnchor`와 단일 `RifleFlash` 슬롯을 연결했다.
- Tracer는 기본 ON을 유지하고 명중·빗나감 색을 모두 `#FFB52E`로 통일했다.
- Unity 재컴파일은 `failed: false`, 오류 0건으로 완료됐다.
- Unity Prefab 역직렬화에서 Anchor의 Muzzle 부모 관계, RifleFlash 슬롯, `0.12초` 표시 시간과 두 Tracer 색을 확인했다.
- 새 Play Mode에서 런타임 RifleFlash가 Anchor 아래 로컬 원점·원본 스케일 `0.4`로 한 번 생성되고, ParticleSystem 5개가 캐시되는 것을 확인했다.
- XR 프리팹의 `DestroyAfterTime` 5개는 런타임 복제본에서만 비활성화됐으며, 원본 프리팹은 수정하지 않았다.
- 실제 `FireShot()` 한 발 호출에서 탄약이 `30→29`, Muzzle Flash와 Tracer가 즉시 활성화되고 Tracer 양 끝에 `#FFB52E`가 적용됐다.
- 표시 시간 이후 VFX와 Tracer는 비활성화되고 Particle 재생은 0개가 됐지만 RifleFlash 인스턴스는 유지됐다.
- 두 번째 Play Mode의 기준 Cursor 이후 새 Console Error는 0건이었다. 이전 실행에서 확인된 `Effect Reverb could not be found`는 기존 오디오 플러그인 오류이며 이번 변경과 무관하다.
- `dotnet build --no-restore`는 현재 NuGet 복원 그래프가 XR Starter Kit 관련 생성 `.csproj` 3개의 프로젝트 정보를 해석하지 못해 `NU1105`가 발생했으므로 독립 검증 수단으로 사용할 수 없었다. Unity 실제 재컴파일 결과로 새 스크립트 오류가 없음을 확인했다.
- 세 후보 모두 ParticleSystem 5개와 `DestroyAfterTime` 5개를 가지며 공통 재생 제어가 적용 가능한 것을 확인했다. 원본 루트 스케일은 Pistol `0.19`, Rifle `0.4`, TerraFormer `0.4`다.
- 첫 사용자 Play Mode 확인에서 기능은 정상 작동했지만 TPS 화면 기준 RifleFlash가 작게 보였다. XR 원본 프리팹은 유지하고 `MuzzleVfxAnchor`의 균일 스케일을 `1.0→2.0`으로 조정했다. 상위 Player 모델 스케일까지 반영한 RifleFlash의 실제 lossy scale은 약 `0.12→0.24`로 정확히 2배가 됐다.
- 두 번째 사용자 확인에서 2배 크기는 충분하다고 확정했다. 다만 Flash는 총구 계층을 계속 따라가고 Tracer는 발사 순간의 월드 시작점을 `0.05초` 동안 고정해, 총기 애니메이션 중 두 효과가 분리되어 보이는 현상을 확인했다.
- Tracer의 실제 명중 끝점은 발사 당시 월드 위치에 고정하고, 표시 중 시작점만 매 LateUpdate마다 현재 `MuzzleVfxAnchor` 위치를 따라가게 변경했다. Anchor가 없으면 기존 `Muzzle` 위치를 사용한다.
- 런타임 검증에서 Anchor를 `0.1` 이동했을 때 Tracer 시작점도 `0.1`만큼 일치하여 이동했고, 명중 끝점 이동 거리는 `0`이었다. Unity 재컴파일과 해당 Play Mode 실행에서 새 오류는 없었다.
- 세 번째 사용자 확인에서 좌우 정렬은 개선됐지만 큰 상하 피치에서 RifleFlash와 Tracer 방향이 벌어지는 것을 확인했다. 원인은 카메라 조준점으로 향하는 `shotDirection`과 총기 모델 배럴 방향의 차이에 더해, RifleFlash 자식 Particle이 루트 로컬 `+Z`로 `0.256` 앞에 배치된 구조가 결합된 것이다.
- Muzzle Flash 루트 위치는 Anchor에 유지하면서, 실제 발사 직전에 런타임 VFX의 `+Z` 방향을 `shotDirection`에 맞춘다. 회전 Up 기준은 현재 중력 전환을 반영하는 `PlayerController.GravityUp`을 사용한다.
- 런타임 검증에서 일반 조준과 카메라 피치 약 `81도` 조준 모두 VFX `forward`와 Tracer 방향의 내적이 `1.0`이었고, 두 효과의 시작 위치도 일치했다. 해당 Play Mode 기준 새 Console Error는 없었다.

## 완료 판정과 후속 분리

- 사용자가 Play Mode에서 RifleFlash와 Tracer의 기본 발사 연동이 정상 작동함을 확인했다.
- `MuzzleVfxAnchor` 2배 크기가 충분하다고 사용자 확인으로 확정했다.
- RifleFlash 단일 교체 슬롯, 런타임 재사용, 자동 파괴 억제, 주황색 Tracer와 시작점 추적은 이번 계획의 완료 범위를 충족했다.
- 큰 상하 피치에서 카메라 근거리 조준점, 총구와 모델 배럴 방향이 크게 벌어지는 문제는 단순 VFX 설정 문제가 아니므로 [TPS 사격 방향성·근거리 시차 및 Tracer 정책 실행 계획](../01_planned/tps_shot_direction_parallax_tracer_plan.md)으로 분리한다.
- PistolFlash와 TerraFormer Flash 비교는 단일 슬롯을 통해 가능하지만, 최종 VFX 교체가 필요하다는 사용자 결정 전에는 추가 변경하지 않는다.

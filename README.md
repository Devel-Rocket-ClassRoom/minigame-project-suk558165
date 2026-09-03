# 미니게임 프로젝트 — 2D 횡스크롤 로그라이크

Unity 6 기반 2D 횡스크롤 로그라이크 액션 게임. 1인 개발.
모티브: **던그리드**, **스컬 더 히어로 슬레이어**

| | |
|---|---|
| **엔진** | Unity 6000.3.15f1 |
| **언어** | C# |
| **규모** | 스크립트 83개 / 약 12,200줄 |
| **콘텐츠** | 무기 10종 · 액세서리 21종 · 적 12종 · 방 프리팹 14개 |
| **개발 기간** | 2026-05-18 ~ 2026-06-05 (3주) |

---

## 게임 플레이

```
타이틀 → 튜토리얼 → 마을 ⇄ 던전(10방) → 보스 → 클리어
                      ↑                    │
                      └──── 사망 시 귀환 ────┘
```

- **런 단위 진행**: 던전 10개 방을 돌파. 4번 방은 상점, 7번 방은 미니보스, 10번 방은 보스.
- **영구 죽음**: 사망 시 액세서리·무기를 잃고 마을로 귀환. 골드는 유지.
- **메타 진행**: 마을에서 골드로 영구 강화 8종(최대 HP, 공격력, 치명타, 이동속도, 골드 획득, 포션 드랍, 보스 데미지, 1회 부활) 구매.
- **무기 3계열**: 근접 / 원거리 / 마법. 2슬롯 장착 후 전환.

---

## 프로젝트 구조

```
Assets/
├── Scene/MainTest.unity   ← 유일한 빌드 씬 (부트스트랩)
├── Scripts/
│   ├── Player/            PlayerController, Movement, Combat, Health, CameraFollow
│   ├── Enemy/             EnemyController, BossController(+Patterns), MiniBoss, SpawnManager
│   ├── Items/             Inventory, TreasureChest, WorldGold/Potion
│   ├── Weapons/           WeaponData(SO), WeaponInventory
│   ├── Shop/              Shop, ShopUI, UpgradeShopUI
│   ├── Save/              SaveManager(AES), SaveData, MetaUpgrades, ItemDatabase
│   ├── NPC/               DialogueUI, NpcController
│   └── UI/                GameFlowController, RoomManager, TimeScaleLock, 로컬라이징
├── Prefabs/               Player, Enemy, map(방), Object, UI, NPC
├── Data/                  ScriptableObject (무기 / 액세서리 / 강화 설정 / 로컬라이징)
└── Animations/
```

### 씬이 하나뿐인 이유

`SceneManager.LoadScene` 을 쓰지 않습니다. `MainTest.unity` 는 매니저만 담긴 부트스트랩이고,
타이틀·마을·던전 방·튜토리얼은 전부 **프리팹을 런타임에 Instantiate/Destroy** 해서 전환합니다.

- 씬 로딩 끊김이 없어 페이드 전환이 매끄럽습니다.
- 플레이어·인벤토리·세이브가 씬 경계를 넘나들 필요가 없습니다.
- 흐름 제어는 `GameFlowController`(화면 전환)와 `RoomManager`(던전 방 진행)로 나뉩니다.

---

## 직접 구현한 핵심 시스템

### 세이브 / 로드
- `JsonUtility` 직렬화 → **AES-256(CBC)** 암호화 → `Application.persistentDataPath/save.dat`
- **원자적 쓰기**: `.tmp` 에 먼저 쓰고 `File.Replace` 로 교체, 직전 버전은 `.bak` 로 보관.
  쓰기 도중 강제 종료돼도 한 판 전 상태로 복구됩니다.
- **더티 플래그**: 골드 획득처럼 잦은 변경은 예약만 하고, 실제 디스크 쓰기는 방 전환·종료 시 1회.
- 구버전 IV 세이브 자동 마이그레이션.

### 방 생성 / 진행
- 사전 제작한 일반 방 10종을 셔플해 순서를 세이브에 기록 → 컨티뉴 시 같은 배치 복원.
- `SpawnManager` 가 웨이브 단위로 스폰. 웨이브별 트리거 존을 지정하면 전멸 후 해당 구역 진입 시 다음 웨이브.
- 방 클리어 → 보상 상자 + 포털 활성화.

### 전투
- **히트박스 / 허트박스 분리** — `MeleeHitbox` 는 활성화 1회당 1히트만 판정.
- 데미지 단일 출처: `EnemyController.damage` 가 근접·원거리 모두를 결정.
- 액세서리 21종이 `StatBonus` 구조체로 합산되어 데미지·이속·대쉬·흡혈·반사·관통 등에 반영.
- 투사체는 `ObjectPool<T>` 로 재사용.

### 성능 / 안정성 규칙
- **`FindObjectsByType` / `FindObjectOfType` 금지.** 정적 리스트(`static List<T> Instances`)에
  `OnEnable`/`OnDisable` 로 자기 등록·해제하거나, `PlayerRef` 같은 정적 캐시를 사용합니다.
- 도메인 리로드를 꺼도 안전하도록 모든 정적 상태에 `[RuntimeInitializeOnLoadMethod]` 리셋을 둡니다.
- 비동기는 UniTask. 모든 루프에 `CancellationToken` 을 전달해 파괴된 오브젝트 접근을 막습니다.
- `Time.timeScale` 은 `TimeScaleLock` 한 곳에서만 씁니다(참조 카운트). 모달이 겹쳐도 어긋나지 않습니다.

---

## UI 디자인 시스템

에셋은 **TravelBook Lite** 픽셀 UI 키트 하나로 통일했습니다.
새 패널을 만들 때는 아래 대응표를 따르면 나머지 화면과 자동으로 맞습니다.

| 역할 | 스프라이트 | 배경색 | 그 위 텍스트 |
|---|---|---|---|
| 패널 배경 | `BookCover01a` | 와인 `#B75B5F` | 크림 `#FFEBBF` |
| 슬롯 | `Slot01a` | 다크 `#45292A` | 크림 `#FFEBBF` |
| 버튼 | `Frame01a` | 토프 `#C6B09B` | 다크 `#402E1F` |
| 선택 표시 | `FrameSelect01a` / `Select01a` | — | — |
| 게이지 배경 / 채움 | `Bar01a` / `Fill01a` | — | — |
| 구분선 | `Line01a` | — | — |
| 딤 배경 | 없음 | 검정 `a=0.66` | — |

**텍스트 3색**: 본문 크림 `#FFEBBF` · 제목/강조 골드 `#FFD966` · 버튼 위 다크 `#402E1F`

모든 Image 는 `Type: Sliced`, `Pixels Per Unit Multiplier: 0.25`
(픽셀아트 4배 확대 — 이 값을 1로 두면 테두리가 화면에서 1~2px로 뭉개집니다).

## 트러블슈팅

### 1. 상점을 열어둔 채 게임이 진행되던 버그

일시정지·인벤토리·상점·강화·게임오버·클리어 **6개 시스템이 각자 `Time.timeScale` 에 0과 1을 대입**하고 있었습니다.
상점(`timeScale=0`)을 연 상태에서 강화 패널을 열었다 닫으면, 강화 패널이 자기 기준으로 `1f` 를 복원해
**상점 UI가 떠 있는데 뒤에서 적이 움직이는** 상태가 됐습니다.

→ `TimeScaleLock` 으로 소유권을 중앙화했습니다. 잠금을 쥔 주체가 하나라도 있으면 0, 전부 놓으면 1입니다.
파괴된 소유자는 `Apply()` 에서 걸러내 잠금이 영구히 남는 것을 막습니다.

### 2. 사망 모션이 한 프레임만 보이던 문제

`GameOverUI` 가 `IsDead` 를 감지한 **그 프레임에** `timeScale = 0` 을 걸었는데,
플레이어 Animator 의 `UpdateMode` 가 `Normal`(스케일 시간)이라 죽는 애니메이션이 시작하자마자 얼어붙었습니다.

→ 사망 감지와 정지 사이에 `deathAnimDuration` 만큼 간격을 두고, 그 뒤에 잠금을 겁니다.

---

## 빌드 / 실행

1. Unity **6000.3.15f1** 로 프로젝트를 엽니다.
2. `Assets/Scene/MainTest.unity` 를 열고 재생합니다. (빌드 대상 씬도 이것 하나입니다)

### ⚠️ 아트 에셋은 이 저장소에 포함되어 있지 않습니다

`Assets/Imported/` 는 `.gitignore` 로 제외되어 있습니다(에셋스토어 라이선스 재배포 방지).
`Assets/Prefabs/Enemy/` 와 `Assets/Animations/` 가 이 폴더의 스프라이트를 참조하므로,
**새로 클론한 상태에서는 스프라이트가 깨진 채로 실행됩니다.**

복원하려면 아래 폴더를 `Assets/Imported/` 아래에 **같은 폴더명 그대로** 다시 임포트해야 합니다.
(괄호 안은 추적 중인 프리팹·애니메이션이 실제로 참조하는 파일 수)

| 폴더 | 용도 |
|---|---|
| `sanctum_pixel/14_big_monster_bundle` | 적 12종 + 보스 스프라이트 / 애니메이션 |
| `Falete` (60) | 맵 타일셋 · 배경 · 성 오브젝트 |
| `Free - Raven Fantasy Icons 1` (33) | 무기 · 액세서리 아이콘 |
| `Audio` (21) | BGM · 효과음 |
| `Object` (14) | 상자 · 골드 · 포션 · 투사체 |
| `Inventory` (7) | 인벤토리 UI 스프라이트 |
| `Player` (6) | 플레이어 스프라이트 |
| `Ground2` (5) / `Ground` (1) | 지형 타일 |
| `NPC` (5) | 마을 NPC |
| `Font` (2) | 폰트 |
| `Title` (2) | 타이틀 화면 |
| `Portal` (1) | 포털 |

`2D Platformer Enemy Pack`, `2D SD Monster Pack`, `Apk` 는 임포트만 되어 있고
현재 참조하는 자산이 하나도 없습니다(초기 프로토타입 잔재).

#### 재임포트 후 반드시 맞춰야 하는 텍스처 설정

`Assets/Imported/` 가 저장소에서 제외되므로 **`.meta` 의 임포트 설정도 함께 사라집니다.**
기본값으로 다시 임포트하면 픽셀아트가 흐릿해지고 UI 테두리가 뭉개집니다.
다음을 다시 적용해야 합니다.

| 설정 | 값 | 이유 |
|---|---|---|
| Filter Mode | **Point (no filter)** | Bilinear 이면 픽셀아트가 흐려짐 |
| Compression | **None** | 작은 픽셀 스프라이트에 압축 아티팩트가 낌 |
| Generate Mip Maps | 끔 | UI에 불필요 |

그리고 TravelBook UI 스프라이트의 **9-slice 테두리(Sprite Editor → Border)** 를
아래처럼 설정해야 패널을 늘려도 테두리가 유지됩니다.

| 스프라이트 | L, B, R, T |
|---|---|
| `BookCover01a` | 8, 7, 8, 7 |
| `Popup01a` | 2, 4, 4, 1 |
| `Frame01a` | 2, 3, 2, 2 |
| `FrameSelect01a` / `01b` | 3, 3, 3, 1 / 3, 3, 3, 2 |
| `Slot01a` / `01b` / `01c` | 2, 2, 2, 3 |
| `Bar01a` | 3, 1, 2, 1 |
| `Select01a` | 6, 6, 6, 6 |
| `Line01a` | 9, 0, 9, 0 |

---

## 조작

| 동작 | 기본 키 |
|---|---|
| 이동 | ← → |
| 아래 점프 (일방향 발판 통과) | ↓ + 점프 |
| 점프 (2단) | Space |
| 대쉬 | Z |
| 공격 | X |
| 무기 전환 | C |
| 상호작용 (NPC/상자/상점) | A |
| 인벤토리 | Tab |
| 포털 진입 | ↑ |
| 일시정지 | Esc |

모든 키는 옵션 → 조작 키에서 변경할 수 있습니다.

### 디버그 (에디터 전용)

| 키 | 동작 |
|---|---|
| F1 | 현재 방의 적 전멸 → 즉시 클리어 |

---

## 개발 규칙

작업 규칙(`CLAUDE.md`, 저장소에는 미포함) 요약:

- 가정하지 말고 질문할 것. 트레이드오프를 드러낼 것.
- 문제를 푸는 최소한의 코드만. 추측성 기능·추상화·설정 가능성 금지.
- 필요한 부분만 수정. 고장나지 않은 것을 리팩토링하지 말 것.
- 작업을 검증 가능한 목표로 바꾸고, 검증될 때까지 반복할 것.

Git 흐름: 이슈 → 브랜치 → 작업 → 커밋 → PR → 리뷰 → 머지. `main` 직접 푸시 금지.

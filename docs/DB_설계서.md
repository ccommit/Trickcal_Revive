# 트릭컬 포트폴리오 DB 설계서

> 작성 기준일: 2026-07-19 (최초 2026-07-16, 계정 로그인/12사도/AUTO/액티브스킬 쿨타임 반영해 갱신)
> 문서 상태: 확정안 (그릴링 세션 결과 반영)
> 원본 기획서: `E:\Projects\트릭컬리소스\데이터 관련\트릭컬_포트폴리오_통합_기획서.md`
> 관련 문서: `docs/전체_기획서.md`(기능 전체 요약), `docs/레벨디자인_설계서.md`(레벨·보상 수식), `docs/아키텍처_설계서.md`(클래스/시퀀스 다이어그램), `docs/서버전환_검토서.md`(서버 전환 시 위험 요소 검토)
> 이 문서는 원본 기획서의 4장(데이터 설계 원칙), 6장(재화와 아이템), 24~26장(모집·상점·성장), 28.5장(데이터와 저장 미완성 항목), 31~33장(DB 스키마·RuntimeData·트랜잭션 초안)을 바탕으로 DB 설계만 별도로 확정한 문서다.

## 1. 데이터 분류 원칙

| 종류 | 설명 | 핵심 DB 파일 카운트 포함 여부 |
|---|---|---|
| MasterData | 개발자가 제공하며 플레이 중 변경되지 않는 데이터 | 포함 |
| SaveData | 계정별 플레이 결과로 변경되는 데이터 | 포함 |
| RuntimeData | 한 번의 전투에서만 유지되는 임시 데이터 | 미포함 |
| LogData | 복구·검증·분석을 위한 기록 | 미포함 |

모든 SaveData는 `schema_version`을 포함하며, 계정 단위 파일은 `account_id`를 포함한다.

## 2. 핵심 10개 논리 데이터 파일

| 번호 | 파일 | 성격 |
|---:|---|---|
| 1 | `account_save.json` | SaveData |
| 2 | `player_character_save.json` | SaveData |
| 3 | `player_inventory_save.json` | SaveData |
| 4 | `player_party_save.json` | SaveData |
| 5 | `player_progress_save.json` | SaveData |
| 6 | `character_master.json` | MasterData |
| 7 | `combat_master.json` | MasterData |
| 8 | `battle_card_master.json` | MasterData |
| 9 | `stage_master.json` | MasterData |
| 10 | `economy_master.json` | MasterData |

`battle_runtime.json`(RuntimeData)과 `battle_result` / `gacha_log`(LogData)는 핵심 10개 카운트에 포함하지 않는다.

이 프로젝트는 단일 플레이어 클라이언트를 전제로 하며, 계정당 위 파일들이 1세트씩 존재한다. 멀티 프로필/서버 동기화는 이번 설계 범위 밖이다.

---

## 3. SaveData 스키마

### 3.1 `account_save.json`

계정 프로필, 영구 재화, 행동력, 일일 초기화, 트랜잭션 복구 정보를 저장한다.

| 필드 | 형식 | 설명 |
|---|---|---|
| `schema_version` | int | 저장 스키마 버전 |
| `account_id` | string | 계정 고유 ID (로그인 아이디와 동일) |
| `login_id` | string | 로그인 아이디, 전체 계정 중 유일해야 함(중복 생성 불가) |
| `password` | string | 로그인 비밀번호. 프로토타입은 백엔드 없는 단일 클라이언트라 평문 저장, 실서비스 전환 시 해시로 교체 |
| `nickname` | string | 닉네임, 최초 로그인(신규 아이디) 시 1회 입력받아 확정 |
| `player_level` | int | 계정 레벨 |
| `player_exp` | int | 계정 경험치 |
| `profile_character_id` | string | 대표 사도 ID |
| `gold` | long | 보유 골드 |
| `elleaf` | long | 보유 엘리프 |
| `macaron` | long | 보유 마카롱 (재화, 사도 레벨 성장 비용) |
| `stamina` | int | 현재 행동력 |
| `stamina_max` | int | 최대 행동력 (수치 미정) |
| `last_stamina_updated_at` | datetime | 행동력 계산 기준 시각 |
| `last_daily_reset_at` | datetime | 04:00 일일 초기화 기준 |
| `free_gacha_claimed` | bool | 오늘 무료 모집 사용 여부 |
| `shop_state_json` | json | 상점 진열, 구매 횟수, 수동 갱신 상태 |
| `pending_transaction_id` | string/null | 복구할 트랜잭션 ID |
| `pending_transaction_type` | string/null | `battle_reward`, `gacha`, `shop_purchase` |
| `created_at` | datetime | 계정 생성 시각 |
| `updated_at` | datetime | 마지막 저장 시각 |

마카롱은 골드·엘리프와 동일한 성격의 상한 없는 누적 재화이므로 인벤토리가 아닌 계정 재화로 관리한다. 단일 플레이어 클라이언트 특성상 한 번에 하나의 액션만 진행되므로 `pending_transaction_id`는 단일 필드로 충분하며, `pending_transaction_type`으로 복구 로직을 분기한다.

로그인은 `login_id`+`password` 일치 여부로 판정하고, `login_id`가 아직 없는 값이면 신규 가입(닉네임 입력 후 `account_save` 생성)으로 처리한다. 로그인 성공 시 별도 세션 저장소에 `account_id`만 기록해두면, 이후 클라이언트 재시작 시 세션이 남아 있는 한 로그인 절차 없이 이어서 진행할 수 있다. 세션 삭제(로그아웃)와 계정 삭제(로그인 정보 포함 전체 삭제)는 서로 다른 동작으로 구분한다.

### 3.2 `player_character_save.json`

플레이어가 보유한 사도의 영구 성장 상태만 저장한다. 몬스터 데이터는 생성하지 않는다.

| 필드 | 형식 | 설명 |
|---|---|---|
| `player_character_id` | string | 보유 사도 인스턴스 ID |
| `account_id` | string | 계정 ID |
| `character_id` | string | `character_master` 참조 |
| `level` | int | 1~100 |
| `star` | int | 1~5 |
| `exp` | long | 현재 레벨 경험치 |
| `sp_skill_level` | int | SP 스킬 레벨 1~10 |
| `active_skill_level` | int | 액티브 스킬 레벨 1~10 |
| `stat_bonus_json` | json | 예외적 영구 보너스, 사용 최소화 |
| `obtained_at` | datetime | 최초 획득 시각 |
| `updated_at` | datetime | 수정 시각 |

전투력은 가능한 한 저장하지 않고 현재 마스터와 성장값으로 계산한다.

### 3.3 `player_inventory_save.json`

수량형 아이템을 저장한다. 마카롱은 3.1의 계정 재화로 이동했으므로 포함하지 않는다.

| 필드 | 형식 | 설명 |
|---|---|---|
| `inventory_id` | string | 인벤토리 레코드 ID |
| `account_id` | string | 계정 ID |
| `item_id` | string | `economy_master.items` 참조 |
| `item_type` | string | `ticket`, `character_shard`, `common_shard` |
| `character_id` | string/null | 전용 조각일 때 대상 사도 |
| `count` | long | 보유 수량 |
| `updated_at` | datetime | 수정 시각 |

골드·엘리프·마카롱·행동력은 인벤토리가 아니라 `account_save`에서 관리한다.

### 3.4 `player_party_save.json`

파티 프리셋과 3×3 좌표를 저장한다.

| 필드 | 형식 | 설명 |
|---|---|---|
| `party_id` | string | 파티 프리셋 ID |
| `account_id` | string | 계정 ID |
| `party_name` | string | 표시 이름 |
| `player_character_id` | string | 보유 사도 참조 |
| `pos_x` | int | 1~3 |
| `pos_y` | int | 1~3 |
| `updated_at` | datetime | 수정 시각 |

검증 조건:
- 파티당 1~6명
- 동일 `player_character_id` 중복 금지
- 동일 `(pos_x, pos_y)` 중복 금지
- 보유하지 않은 사도 참조 금지

### 3.5 `player_progress_save.json`

스테이지 해금, 클리어, 별과 보상 수령 상태를 저장한다.

| 필드 | 형식 | 설명 |
|---|---|---|
| `progress_id` | string | 진행도 ID |
| `account_id` | string | 계정 ID |
| `stage_id` | string | 스테이지 ID |
| `is_unlocked` | bool | 해금 여부 |
| `is_cleared` | bool | 클리어 여부 |
| `star_1` / `star_2` / `star_3` | bool | 별 조건 달성 여부 |
| `best_combat_time_sec` | float | 전투 1~5 합산 최단 시간 |
| `best_death_count` | int | 최소 사망 횟수 |
| `clear_count` | int | 클리어 횟수 |
| `first_reward_claimed` | bool | 첫 클리어 보상 수령 여부 |
| `star_reward_claimed_json` | json | 별·챕터 누적 보상 수령 상태 |
| `last_clear_at` | datetime | 마지막 클리어 시각 |

---

## 4. MasterData 스키마

### 4.1 `character_master.json`

플레이어블 사도와 몬스터의 공통 전투 원형을 저장한다.

| 필드 | 형식 | 설명 |
|---|---|---|
| `character_id` | string | 공용 캐릭터 ID |
| `name` | string | 표시 이름 |
| `entity_type` | string | `playable`, `monster`, `boss`, `summon` |
| `is_summonable` | bool | 모집 등장 여부 |
| `is_collection_visible` | bool | 사도 목록 표시 여부 |
| `is_starter` | bool | 계정 생성 시 기본 보유 여부. 사도 12명 중 6명만 true, 나머지 6명은 false(모집으로만 획득) |
| `initial_star` | int | 최초 성급, 사도만 사용. 모집 카드 공개 화면·결과 그리드에 표시되는 성급도 이 값 그대로이며 보유 후 성장한 성급과 무관 |
| `personality_type` | string | 광기·순수·냉정·우울·활발 |
| `combat_role` | string | `dealer`, `tank`, `supporter`, `other` |
| `attack_range` | float | 공격 사거리 (근/원거리 구분은 이 값으로 표현) |
| `base_stats_json` | json | 9개 기본 스탯 |
| `normal_attack_skill_id` | string | 일반 공격 참조 |
| `sp_skill_id` | string | SP 스킬 참조 |
| `active_skill_id` | string/null | 액티브 스킬 참조 |
| `resource_key` | string | 모델·스파인·아이콘·이펙트 연결 키 |
| `enabled` | bool | 사용 여부 |

`ai_profile_id`와 `growth_json` 필드는 두지 않는다. 자동전투 규칙(§7 참고)은 사도·몬스터 구분 없이 공통 적용되고, 성장 곡선은 `economy_master.growth_table`을 전체 캐릭터가 공유하므로 캐릭터별 참조가 불필요하다.

### 4.2 `combat_master.json`

스킬, 효과, 상태이상, 공통 전투 상수를 섹션으로 구분해 하나의 파일에 담는다.

```json
{
  "skills": [],
  "effects": [],
  "status_ailments": [],
  "common_config": {}
}
```

#### 4.2.1 `skills`

| 필드 | 형식 | 설명 |
|---|---|---|
| `skill_id` | string | 스킬 ID |
| `skill_type` | string | `normal`, `sp`, `active` |
| `target_rule` | string | 가장 가까운 적, 가장 먼 적, HP 최저 아군 등 |
| `damage_coefficient` | float | 공격력 계수 |
| `hit_count` | int | 타격 횟수 |
| `cast_time_sec` | float | 시전 시간 |
| `range` | float | 스킬 사거리 |
| `area_shape` | string | 단일, 원형, 직선 등 |
| `area_size` | float | 범위 크기 |
| `sp_cost` | int | SP 스킬 비용 |
| `cooldown_sec` | float | 액티브 쿨타임 |
| `effect_ids_json` | json | `effects` 참조 배열 |
| `interruptible` | bool | 시전 취소 가능 여부 |
| `animation_key` | string | 애니메이션 키 |
| `effect_key` | string | 시각 효과 키 |

#### 4.2.2 `effects`

| 필드 | 형식 | 설명 |
|---|---|---|
| `effect_id` | string | 효과 ID |
| `effect_category` | string | `stat_percent`, `instant`, `dot_hot`, `crowd_control` |
| `stat_target` | string/null | `attack`, `defense`, `max_hp` 등, `stat_percent`일 때만 |
| `value` | float | 수치 (퍼센트 또는 절댓값) |
| `duration_type` | string | `wave_end`, `battle_end`, `fixed_sec` |
| `duration_sec` | float/null | `fixed_sec`일 때만 사용 |
| `stack_rule` | string | `sum`, `refresh`, `ignore_new` |
| `tick_interval_sec` | float/null | `dot_hot`일 때만 |

스킬과 전투 카드(§4.3)가 동일한 `effects` 정의를 공유해 효과를 이중 관리하지 않는다.

#### 4.2.3 `status_ailments`

| 필드 | 형식 | 설명 |
|---|---|---|
| `ailment_id` | string | 상태이상 ID (`silence`, `stun` 등) |
| `blocks_move` | bool | 이동 불가 여부 |
| `blocks_normal_attack` | bool | 일반 공격 불가 여부 |
| `blocks_sp_skill` | bool | SP 스킬 불가 여부 |
| `blocks_active_skill` | bool | 액티브 스킬 불가 여부 |
| `blocks_sp_regen` | bool | SP 자연 회복 정지 여부 |
| `blocks_active_cooldown` | bool | 액티브 쿨타임 정지 여부 |
| `ui_label` | string | UI 표시용 설명 |

#### 4.2.4 `common_config`

| 키 | 값 | 비고 |
|---|---|---|
| `preparation_time_sec` | 40 | 배속 버튼(x1/x2/x3)과 무관하게 항상 실제 1배 속도로 흐름 |
| `battle_time_sec` | 80 | 배속 버튼 적용 대상 |
| `wave_count` | 5 | |
| `max_battle_grade` | 6 | |
| `revive_hp_ratio` | 0.2 | |
| `revive_sp` | 0 | |
| `stamina_recover_interval_sec` | 20 | |
| `max_party_size` | 6 | |
| `party_grid_size` | 3 | |
| `elite_stat_multiplier` | 1.2 | 예시값, 밸런스 확정 전 |
| `boss_stat_multiplier` | 1.5 | 예시값, 밸런스 확정 전 |
| `min_damage` | 1 | |
| `active_skill_cooldown_sec` | 10 | 전 캐릭터 공용 고정값. 클릭 시 쿨타임 연출만 담당하며 실제 발동 로직은 미구현(후속 세션에서 `skills.cooldown_sec`로 캐릭터별 값으로 대체 예정) |
| `move_speed_pct_per_sec` | 18 | 화면 폭 대비 % 단위 이동 속도, 전 유닛 공용 고정값 |
| `auto_prep_buy_interval_sec` | 2 | AUTO 모드 준비단계 자동 카드구매 간격 |
| `auto_equip_reveal_delay_sec` | 1 | AUTO 모드에서 장비 카드 구매 후 장착 화면이 뜨기까지 지연 |
| `auto_equip_confirm_delay_sec` | 1 | AUTO 모드에서 장착 화면이 뜬 뒤 무작위 대상에게 장착하기까지 지연 |
| `auto_resume_delay_sec` | 2 | AUTO 모드에서 장착 팝업이 닫힌 뒤 다음 구매 사이클까지 지연 |

### 4.3 `battle_card_master.json`

| 필드 | 형식 | 설명 |
|---|---|---|
| `card_id` | string | 카드 ID |
| `card_category` | string | `apostle`, `battle_support`, `character_support` |
| `effect_id` | string | `combat_master.effects` 참조 |
| `cost` | int | 보조·장착 카드는 5 |
| `target_rule` | string/null | 최저 HP·SP 등, 즉시효과 카드만 |
| `allow_duplicate_offer` | bool | 중복 등장 허용 |
| `allow_duplicate_effect` | bool | 중복 적용 허용 |
| `enabled` | bool | 사용 여부 |

사도 카드 비용은 카드 행마다 저장하지 않고 학년 비용 설정 `[3, 5, 7, 10, 15, 20]`을 참조한다.

### 4.4 `stage_master.json`

| 필드 | 형식 | 설명 |
|---|---|---|
| `stage_id` | string | 스테이지 ID |
| `chapter_id` | string | 챕터 ID |
| `stage_no` | int | 챕터 내 번호 |
| `name` | string | 표시 이름 |
| `recommended_level` | int | 권장 레벨 |
| `recommended_power` | long | 권장 전투력, 필요 시 사용 |
| `stamina_cost` | int | 승리 시 행동력 소비량 |
| `star_conditions_json` | json | 별 조건 3개 |
| `unlock_condition_json` | json | 해금 조건 |
| `waves_json` | json | 5웨이브 적 편성 |
| `rewards_json` | json | 첫·반복·별 보상 |
| `next_stage_id` | string/null | 다음 스테이지 |
| `enabled` | bool | 사용 여부 |

각 스폰에는 몬스터 ID, 레벨, 등급, 좌표와 선택적 스탯 배율을 저장한다.

### 4.5 `economy_master.json`

아이템, 성장 비용, 모집 배너, 상점 상품, 재화 교환을 섹션으로 구분한다.

```json
{
  "items": [],
  "growth_table": [],
  "gacha_banners": [],
  "shop_products": [],
  "currency_exchange": []
}
```

#### 4.5.1 `items`

| 필드 | 형식 | 설명 |
|---|---|---|
| `item_id` | string | 아이템 ID |
| `item_type` | string | `ticket`, `character_shard`, `common_shard` |
| `name` | string | 표시 이름 |
| `character_id` | string/null | 전용 조각일 때만 대상 사도 참조 |
| `is_stackable` | bool | 스택 여부 |
| `sell_price_gold` | int/null | 상점 판매가, 판매 불가면 null |
| `enabled` | bool | 사용 여부 |

마카롱은 재화이므로 이 섹션에 포함하지 않는다(§3.1 참고).

#### 4.5.2 `growth_table`

레벨·성급·스킬레벨 성장을 하나의 공용 곡선으로 통합한다. 모든 캐릭터가 동일 곡선을 공유하며, 캐릭터 개성은 `character_master.base_stats_json` 차이로만 표현한다.

| 필드 | 형식 | 설명 |
|---|---|---|
| `growth_type` | string | `character_level`, `character_star`, `skill_level` |
| `step` | int | 도달하는 단계 (레벨/성급/스킬레벨) |
| `cost_gold` | long/null | 필요 골드 |
| `cost_macaron` | long/null | 필요 마카롱, 레벨 성장만 |
| `cost_character_shard` | int/null | 필요 전용 조각, 성급 성장만 |
| `cost_exp` | long/null | 필요 경험치, 레벨 성장만 |
| `stat_multiplier_json` | json/null | 해당 단계 도달 시 스탯 배율 또는 증가값 |

#### 4.5.3 `gacha_banners`

| 필드 | 형식 | 설명 |
|---|---|---|
| `banner_id` | string | 배너 ID |
| `title` | string | 표시 이름 |
| `start_at` / `end_at` | datetime/null | 노출 기간 |
| `single_cost` | int | 150엘리프 |
| `multi_cost` | int | 1,500엘리프 |
| `rarity_rates_json` | json | 1성 60%, 2성 30%, 3성 10% |
| `pool_json` | json | 등장 사도 목록 |
| `multi_guarantee` | string | 2성 이상 1개 |
| `enabled` | bool | 사용 여부 |

#### 4.5.4 `shop_products`

| 필드 | 형식 | 설명 |
|---|---|---|
| `product_id` | string | 상품 ID |
| `product_type` | string | `currency`, `item`, `shard_exchange` |
| `reward_type` | string | `gold`, `elleaf`, `macaron`, `stamina`, `character_shard`, `common_shard` |
| `reward_character_id` | string/null | 전용 조각 상품일 때 대상 사도 |
| `reward_amount` | long | 지급 수량 |
| `cost_currency_type` | string | `gold`, `elleaf`, `common_shard` 등 |
| `cost_amount` | long | 필요 수량 |
| `stock_limit` | int/null | 일일 구매 제한, 무제한이면 null |
| `refresh_type` | string | `daily_fixed`(04:00 고정), `manual`(재화 소모 갱신) |
| `slot_index` | int | 진열 순서/위치 |

#### 4.5.5 `currency_exchange`

| 필드 | 형식 | 설명 |
|---|---|---|
| `exchange_id` | string | 교환 ID |
| `from_currency` | string | `gold`, `elleaf`, `character_shard`, `common_shard` |
| `to_currency` | string | 교환 대상 재화 |
| `from_amount` | long | 지불 수량 |
| `to_amount` | long | 획득 수량 |
| `direction_limit` | string/null | 무제한이면 `unlimited`, 아니면 한도 수치 |
| `character_id` | string/null | 조각 교환일 때 대상 사도 |

---

## 5. RuntimeData

### 5.1 `battle_runtime.json`

진행 중 전투는 영구 성장과 분리한다. 웨이브 전환마다 덮어쓰기 저장한다.

```json
{
  "battle_id": "battle_260716_0001",
  "account_id": "guest_260701_0001",
  "stage_id": "stage_01_01",
  "party_id": "party_main",
  "started_at": "2026-07-16T10:30:00+09:00",
  "current_phase": "preparation",
  "current_wave": 3,
  "stamina_cost": 10,
  "stamina_state": "reserved",
  "battle_speed": 1,
  "is_auto_battle": true,
  "battle_coin": 42,
  "refresh_count": 4,
  "card_slots": [],
  "battle_support_cards": [],
  "characters": [
    {
      "player_character_id": "pc_alice_001",
      "is_summoned": true,
      "battle_grade": 4,
      "current_hp": 1020,
      "current_sp": 75,
      "active_cooldown_remaining": 8.5,
      "equipment_cards": [],
      "pos_x": 1,
      "pos_y": 2
    }
  ]
}
```

### 5.2 보관·삭제 기준

- 전투 진행 중에는 웨이브 전환마다 파일을 덮어쓴다 (복구용이 아니라 크래시 감지용).
- 전투가 정상 종료(승리/패배/포기)되면 그 즉시 파일을 삭제한다.
- 앱 시작 시 파일이 존재하면 비정상 종료(크래시, 강제 종료)로 간주하고, 예약된 행동력을 반환한 뒤 파일을 삭제한다.
- 전투 이어하기 기능은 만들지 않는다. 필요해지면 이 구조를 실제 복구 데이터로 확장한다.

---

## 6. LogData

### 6.1 `battle_result`

| 필드 | 형식 | 설명 |
|---|---|---|
| `battle_log_id` | string | 로그 ID |
| `account_id` | string | 계정 ID |
| `stage_id` | string | 스테이지 ID |
| `result` | string | `win`, `lose`, `give_up` |
| `combat_time_sec` | float | 전투 1~5 합산 시간 |
| `death_count` | int | 사망 횟수 |
| `stars_earned` | int | 획득 별 개수 |
| `created_at` | datetime | 기록 시각 |

보관 기준: 최근 **7일** 이내 기록을 보관하고, 매 일일 초기화(04:00)에 오래된 기록을 정리한다. 안전 상한으로 계정당 최대 **100건**을 두어, 7일 기준과 100건 기준 중 먼저 도달하는 쪽으로 정리한다.

`battle_log_id`는 `account_save.pending_transaction_id`가 `Committed`로 넘어갈 때 함께 생성되는 값으로, 보상 중복 지급 방지의 실제 판정 키다. `RewardService.CheckDuplicateGrant(battleId)`는 같은 `battle_id`로 이미 `battle_result` 로그가 존재하는지 조회해 존재하면 재지급을 건너뛴다(재시도·뒤로가기 후 재진입 대응).

### 6.2 `gacha_log`

| 필드 | 형식 | 설명 |
|---|---|---|
| `gacha_log_id` | string | 로그 ID |
| `account_id` | string | 계정 ID |
| `banner_id` | string | 배너 ID |
| `pull_count` | int | 1 또는 10 |
| `transaction_id` | string | 연결된 트랜잭션 |
| `result_json` | json | 획득 사도/성급/조각 목록 |
| `created_at` | datetime | 기록 시각 |

보관 기준: 최근 **30일** 이내 기록을 보관하고, 안전 상한으로 계정당 최대 **200건**을 둔다.

---

## 7. 트랜잭션 상태

보상 지급과 모집은 다음 상태를 공통으로 사용한다.

```text
Prepared → Pending → Committed → Displayed
```

| 상태 | 의미 |
|---|---|
| `Prepared` | 결과를 메모리에서 계산한 상태 |
| `Pending` | 변경 전·후와 결과를 복구 파일(`account_save.pending_transaction_id`)에 기록한 상태 |
| `Committed` | 재화와 아이템·진행도 데이터 반영 완료 |
| `Displayed` | 사용자에게 결과 화면까지 표시 완료 |

복구 원칙:
- `Pending`에서 실제 저장 미반영: 취소 또는 다시 커밋
- `Committed`에서 앱 종료: 동일 결과 화면 복구
- `Displayed`: 중복 지급 없이 로그만 유지
- 동일 `transaction_id`는 한 번만 커밋

단일 플레이어 클라이언트 특성상 한 번에 하나의 액션만 진행되므로 계정당 `pending_transaction_id` 단일 필드로 모든 트랜잭션(보상 지급, 모집, 상점 구매)을 추적한다. `pending_transaction_type`으로 복구 시 처리 로직을 분기한다.

---

## 8. 저장 마이그레이션 규칙

정식 자동 마이그레이션 시스템이 아닌 경량 버전 체인 방식을 사용한다.

- 파일별로 `schema_version`은 정수로 1씩 증가한다.
- 각 SaveData 파일 타입마다 "버전 N → N+1" 단계별 변환 함수를 순서대로 등록한다.
- 로드 시 저장된 `schema_version`이 현재 코드의 목표 버전보다 낮으면, 해당 단계 함수들을 순차 적용해 최신 버전까지 끌어올린다.
- 저장된 `schema_version`이 현재 코드보다 높으면(구버전 실행 파일에 신버전 저장 파일) 마이그레이션을 시도하지 않고 오류 처리 후 계정 초기화 여부를 사용자에게 확인한다.
- 각 변환 함수는 필드 추가(기본값 채우기), 필드 이름 변경, 필드 제거 정도의 단순 작업만 지원한다. 구조 자체가 근본적으로 바뀌는 경우는 지원 범위 밖으로 두고 계정 초기화로 처리한다.

---

## 9. 원본 기획서 대비 변경 이력

이 설계서는 원본 기획서(31장 권장 DB 상세 스키마 초안)를 아래와 같이 확정·수정했다.

| 항목 | 원본 기획서 | 이 설계서 |
|---|---|---|
| 마카롱 | `player_inventory_save`의 아이템(`item_type: macaron`) | `account_save`의 재화 필드(`macaron`) |
| `character_master.ai_profile_id` | 존재 | 제거 (23장 공통 자동전투 규칙으로 대체) |
| `character_master.growth_json` | 존재 | 제거 (`economy_master.growth_table` 공용 곡선으로 대체) |
| 레벨/성급/스킬 성장 | `economy_master`에 개별 배열 미정 | `growth_table` 단일 배열로 통합, 전체 캐릭터 공용 |
| `combat_master` 구조 | `skills` 필드 표만 존재, effects/상태이상/공통상수 위치 미정 | `skills`/`effects`/`status_ailments`/`common_config` 4개 섹션으로 확정 |
| `battle_card_master` 효과 | `effect_type`/`effect_value` 카드에 직접 저장 | `effect_id`로 `combat_master.effects` 참조 |
| `account_save.pending_transaction_id` | 필드만 존재 | `pending_transaction_type` 추가 |
| 로그 보관 기준 | 미정 | 일수 기준(7일/30일) + 안전 상한(100건/200건) 병행 |
| 저장 마이그레이션 | 미정 | 경량 버전 체인 방식 확정 |
| `battle_runtime` 보관 기준 | "삭제하는 방향" 잠정 서술 | 크래시 감지 로직 포함해 확정 |

이후 그릴링 세션에서 다음 항목을 추가로 확정했다(2026-07-19 갱신):

| 항목 | 이전 | 현재 |
|---|---|---|
| 로그인 방식 | `guest_uuid` 기반 게스트 | `login_id`+`password` 계정, 신규 아이디는 닉네임 입력 후 자동 가입 |
| `character_master.is_starter` | 없음 | 추가. 사도 12명 중 6명만 계정 생성 시 기본 보유, 나머지 6명은 모집 전용 |
| 액티브 스킬 쿨타임 | `skills.cooldown_sec`로 캐릭터별 관리 예정 | 실제 발동 로직 붙기 전까지는 `common_config.active_skill_cooldown_sec`(전 캐릭터 공용 10초)로 단순화 |
| 준비 단계 배속 | 별도 규정 없음 | 배속 버튼과 무관하게 항상 1배로 고정(`common_config.preparation_time_sec` 비고 참고) |
| AUTO 범위 | SP 스킬 자동 발동만 암묵적으로 가정 | 준비단계 자동 카드구매/장착 타이밍(`common_config.auto_*` 항목)과 전투단계 액티브스킬 자동 사용까지 명시적으로 확정 |
| 보상 중복지급 방지 키 | `pending_transaction_id`만 서술, 판정 대상 불명확 | `battle_result.battle_log_id`가 실제 판정 키임을 명시(§6.1). 클래스다이어그램 `RewardService.CheckDuplicateGrant(battleId)`와 연결 (2026-07-19, `docs/아키텍처_설계서.md` 피드백 검토 결과) |

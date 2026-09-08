using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using TrickcalRevive.Data.Character;
using TrickcalRevive.Data.Stage;

namespace TrickcalRevive.Infra.Fixtures
{
    /// <summary>
    /// RECOVERY FIXTURE — 정식 원본 데이터가 아니다.
    ///
    /// 침략 스테이지선택 → 파티편성 화면을 실제로 눌러 검증하기 위한 결정론적 임시
    /// 데이터. 원작 마스터 테이블은 파싱 가능한 형태로 추출돼 있지 않아(GameData 패킹),
    /// 아래 값은 화면 흐름 검증용으로 만든 것이다. Level/Star/전투력/보상/적구성은
    /// 레벨/별/전투력은 화면 검증용 고정 fixture다. 사도 배치열은 이전 프로젝트의
    /// 30명 선정안과 사도 목록을 대조한 참고 복구값이며 원본 master table 확정값은 아니다.
    /// (docs/UI복구_침략파티_합의.md §7)
    /// </summary>
    public static class RecoveryFixture
    {
        public const string ChapterId = "1";

        // 프로젝트에 리소스가 완비된 사도 30명. key = Content/Characters/Apostles/&lt;key&gt;.
        // baseName은 issue2 character-roster 카탈로그 기준.
        private static readonly (string Key, string Name, string Column)[] Apostles =
        {
            ("maison", "Maison", "Back"), ("yumimi", "Yumimi", "Back"), ("maestromk2", "MaestroMK2", "Front"),
            ("diana", "Diana", "Middle"), ("chloe", "Chloe", "Front"), ("arnet", "Arnet", "Middle"),
            ("lazy", "Lazy", "Back"), ("patula", "Patula", "Front"), ("ricota", "Ricota", "Front"),
            ("lethe", "Lethe", "Front"), ("amelia", "Amelia", "Back"), ("kommyswim", "KommySwim", "Middle"),
            ("allet", "Allet", "Front"), ("sari", "Sari", "Middle"), ("bigwood", "BigWood", "Front"),
            ("gabia", "Gabia", "Middle"), ("kyarot", "Kyarot", "Back"), ("ran", "Ran", "Back"),
            ("chopi", "Chopi", "Middle"), ("veroo", "Veroo", "Middle"), ("barie", "Barie", "Back"),
            ("festa", "Festa", "Front"), ("kommy", "Kommy", "Front"), ("lion", "Lion", "Front"),
            ("carren", "Carren", "Back"), ("mynx", "Mynx", "Front"), ("bana", "Bana", "Back"),
            ("jubee", "Jubee", "Middle"), ("rude", "Rude", "Front"), ("selline", "Selline", "Front"),
        };

        private static readonly (string Key, string Name)[] Monsters =
        {
            ("gluttonbear", "GluttonBear"), ("lupalu", "Lupalu"),
            ("magicfork", "MagicFork"), ("pumpkin", "Pumpkin"),
        };

        // 1챕터 10스테이지. 이름/권장전투력/보상은 fixture. 진행상태(§7): 1-1 클리어★3,
        // 1-2 해금, 1-3~1-10 잠금. 7~10의 명칭과 전투력은 UI 노드 검증용 임시 값이다.
        private static readonly (string Id, int No, string Name, long Power)[] Stages =
        {
            ("1-1", 1, "열정의 밭 갈기", 1_200_000),
            ("1-2", 2, "새참 나르기", 1_600_000),
            ("1-3", 3, "말썽꾸러기 소탕", 2_100_000),
            ("1-4", 4, "저녁놀 언덕", 2_700_000),
            ("1-5", 5, "달빛 곳간", 3_400_000),
            ("1-6", 6, "첫 수확제", 4_200_000),
            ("1-7", 7, "복구 검증 스테이지 7", 5_100_000),
            ("1-8", 8, "복구 검증 스테이지 8", 6_100_000),
            ("1-9", 9, "복구 검증 스테이지 9", 7_200_000),
            ("1-10", 10, "복구 검증 스테이지 10", 8_400_000),
        };

        // --- master data ------------------------------------------------------

        public static List<StageMasterData> StagesByChapter(string chapterId)
        {
            if (chapterId != ChapterId)
                return new List<StageMasterData>();
            return Stages.Select(BuildStage).ToList();
        }

        public static StageMasterData Stage(string stageId) =>
            Stages.Where(s => s.Id == stageId).Select(BuildStage).FirstOrDefault();

        public static UnitMasterData Character(string characterId)
        {
            var apostle = Apostles.FirstOrDefault(a => a.Key == characterId);
            if (apostle.Key != null)
                return BuildUnit(apostle.Key, apostle.Name, "Apostle", apostle.Column);
            var monster = Monsters.FirstOrDefault(m => m.Key == characterId);
            return monster.Key != null ? BuildUnit(monster.Key, monster.Name, "Monster", "All") : null;
        }

        public static List<UnitMasterData> PlayableCharacters() =>
            Apostles.Select(a => BuildUnit(a.Key, a.Name, "Apostle", a.Column)).ToList();

        // --- player data ------------------------------------------------------

        public static List<PlayerCharacterData> OwnedCharacters(string accountId) =>
            Apostles.Select(a => new PlayerCharacterData
            {
                PlayerCharacterId = $"pc_{a.Key}",
                AccountId = accountId,
                CharacterId = a.Key,
                Level = 30,
                Star = 3,
                SpSkillLevel = 1,
                ActiveSkillLevel = 1,
                StatBonusJson = "",
            }).ToList();

        /// <summary>저장된 진행이 없을 때의 초기 진행상태(§7). 1-1 클리어★3 / 1-2 해금 / 나머지 잠금.</summary>
        public static PlayerStageProgressData DefaultStageProgress(string accountId, string stageId)
        {
            var known = Stages.Any(s => s.Id == stageId);
            if (!known)
                return null;
            var cleared = stageId == "1-1";
            var unlocked = stageId == "1-1" || stageId == "1-2";
            return new PlayerStageProgressData
            {
                ProgressId = $"sp_{stageId}",
                AccountId = accountId,
                StageId = stageId,
                IsUnlocked = unlocked,
                IsCleared = cleared,
                Star1 = cleared,
                Star2 = cleared,
                BestTimeSec = 0f,
                ClearCount = cleared ? 1 : 0,
                FirstRewardClaimed = false,
                ThreeStarRewardClaimed = false,
            };
        }

        // --- builders ---------------------------------------------------------

        private static UnitMasterData BuildUnit(string key, string name, string entityType, string formationColumn) => new UnitMasterData
        {
            CharacterId = key,
            Name = name,
            EntityType = entityType,
            Role = "",                 // 미확인 (원작 테이블 파싱 불가)
            PersonalityType = "",      // 미확인
            FormationColumn = formationColumn,
            IsStarter = false,
            InitialStar = 3,
            BaseStatsJson = "",
            GrowthJson = "",
            SkillsJson = "",
            ResourceKey = key,
            Enabled = true,
        };

        private static StageMasterData BuildStage((string Id, int No, string Name, long Power) s)
        {
            var nextIndex = System.Array.FindIndex(Stages, x => x.Id == s.Id) + 1;
            var next = nextIndex < Stages.Length ? Stages[nextIndex].Id : "";
            return new StageMasterData
            {
                StageId = s.Id,
                ChapterId = ChapterId,
                StageNo = s.No,
                Name = s.Name,
                RecommendedPower = s.Power,
                StaminaCost = 10,
                StarConditions1 = "clear",
                StarConditions2 = "no_death",
                NextStageId = next,
                WavesJson = BuildWavesJson(s.No),
                RewardsJson = BuildRewardsJson(s.No),
                UnlockConditionJson = "",
                RecommendedPersonality = Personalities[(s.No - 1) % Personalities.Length],
            };
        }

        // 추천 성격(fixture, 추론) — 스테이지별 순환. 광기/순수/냉정/우울/활발.
        private static readonly string[] Personalities = { "광기", "순수", "냉정", "우울", "활발" };

        private static string BuildRewardsJson(int stageNo)
        {
            var list = new StageRewardListData();
            list.Rewards.Add(new StageRewardData { RewardType = "Gold", Amount = 1500L + stageNo * 400L });
            list.Rewards.Add(new StageRewardData { RewardType = "Macaron", Amount = 200L + stageNo * 30L });
            list.Rewards.Add(new StageRewardData { RewardType = "Elleaf", Amount = 20L });
            return JsonUtility.ToJson(list);
        }

        // 몬스터 4종을 기획배치(종족테마 무시). 마지막 스테이지는 보스 포함.
        private static string BuildWavesJson(int stageNo)
        {
            var list = new EnemyWaveListData();
            var wave = new EnemyWaveData { WaveIndex = 0 };
            var positions = new[] { (3, 2), (3, 1), (3, 3) };
            for (var i = 0; i < 3; i++)
            {
                var monster = Monsters[(stageNo + i) % Monsters.Length];
                wave.Spawns.Add(new EnemySpawnData
                {
                    CharacterId = monster.Key,
                    Level = 20 + stageNo,
                    Grade = stageNo >= 6 && i == 0 ? "boss" : "normal",
                    PosX = positions[i].Item1,
                    PosY = positions[i].Item2,
                    StatMultiplier = stageNo >= 6 && i == 0 ? 2.5f : 0f,
                });
            }
            list.Waves.Add(wave);
            return JsonUtility.ToJson(list);
        }
    }
}

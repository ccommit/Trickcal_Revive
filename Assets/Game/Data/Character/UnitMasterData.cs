using System;

namespace TrickcalRevive.Data.Character
{
    [Serializable]
    public class UnitMasterData
    {
        public string CharacterId;
        public string Name;
        public string EntityType;
        public string Role;
        // 설계서 4.1절 personality_type. 광기·순수·냉정·우울·활발.
        public string PersonalityType;
        public string FormationColumn;
        public bool IsStarter;
        // 태생 성급. 설계서 4.1절 initial_star. 성장으로 올라간 현재 성급과는 다르다.
        public int InitialStar;
        public string BaseStatsJson;
        public string GrowthJson;
        public string SkillsJson;
        public string ResourceKey;
        public bool Enabled;
    }
}

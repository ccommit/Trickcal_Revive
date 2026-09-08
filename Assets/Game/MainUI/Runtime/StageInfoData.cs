using System.Collections.Generic;

namespace TrickcalRevive.MainUI
{
    public sealed class StageInfoRewardEntry
    {
        public string RewardType;   // Gold / Macaron / Elleaf / Stamina
        public long Amount;
    }

    /// <summary>StageInfoPopupController(Presentation)가 조립해 StageInfoPopupView에 넘기는 팝업 표시 데이터.</summary>
    public sealed class StageInfoData
    {
        public string StageName;
        public int Stars;                 // 0..3
        public long RecommendedPower;
        public string PersonalityText;    // 추천 성격(광기/순수/냉정/우울/활발)
        public List<string> MonsterIds = new List<string>();   // 출현 몬스터 CharacterId
        public List<StageInfoRewardEntry> Rewards = new List<StageInfoRewardEntry>();
    }
}

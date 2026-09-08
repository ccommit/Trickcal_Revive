using System;

namespace TrickcalRevive.Data.Character
{
    // 플레이어가 보유한 사도의 영구 성장 상태. DB_설계서 3.2절 대응.
    [Serializable]
    public class PlayerCharacterData
    {
        public string PlayerCharacterId;
        public string AccountId;
        public string CharacterId;
        public int Level;
        public int Star;

        // 일반 공격은 성장 대상이 아니라 대응 필드를 두지 않는다.
        public int SpSkillLevel;
        public int ActiveSkillLevel;

        public string StatBonusJson;
    }
}

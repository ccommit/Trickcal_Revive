using System;

namespace TrickcalRevive.Data.Growth
{
    // 성장 한 단계의 비용과 효과. DB_설계서 4.5.2절 economy_master.growth_table 대응.
    // 레벨·성급·스킬레벨이 하나의 표를 공유하고 GrowthType으로 구분한다.
    [Serializable]
    public class GrowthTableData
    {
        // character_level / character_star / skill_level
        public string GrowthType;

        // 도달하는 단계. 레벨 12로 올린다면 Step은 12다.
        public int Step;

        // 해당 성장에 안 쓰이면 0. 레벨 성장은 CostMacaron만 쓴다.
        public long CostGold;
        public long CostMacaron;
        public int CostCharacterShard;

        public string StatMultiplierJson;
    }
}

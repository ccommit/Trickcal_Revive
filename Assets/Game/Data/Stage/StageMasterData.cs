using System;

namespace TrickcalRevive.Data.Stage
{
    [Serializable]
    public class StageMasterData
    {
        public string StageId;
        public string ChapterId;
        public int StageNo;
        public string Name;
        public long RecommendedPower;
        public int StaminaCost;
        public string StarConditions1;
        public string StarConditions2;
        public string NextStageId;
        public string WavesJson;
        public string RewardsJson;
        public string UnlockConditionJson;
    }
}

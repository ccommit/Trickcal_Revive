using System;

namespace TrickcalRevive.Data.Battle
{
    [Serializable]
    public class BattleResultData
    {
        public string BattleId;
        public string AccountId;
        public string StageId;
        public string ResultType;
        public string ClearReason;
        public float ClearTimeSec;
        public int StarCount;
        public int WaveClearCount;
        public string RewardJson;
        public string SnapshotJson;
        public DateTime CreatedAt;
    }
}

using System;

namespace TrickcalRevive.Data.Stage
{
    [Serializable]
    public class PlayerStageProgressData
    {
        public string ProgressId;
        public string AccountId;
        public string StageId;
        public bool IsUnlocked;
        public bool IsCleared;
        public bool Star1;
        public bool Star2;
        public float BestTimeSec;
        public int ClearCount;
        public bool FirstRewardClaimed;
        public bool ThreeStarRewardClaimed;
    }
}

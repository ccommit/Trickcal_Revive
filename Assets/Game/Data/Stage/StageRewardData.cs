using System;
using System.Collections.Generic;

namespace TrickcalRevive.Data.Stage
{
    // stage_master.rewards_json 한 항목. 스테이지 정보 팝업의 보상 목록에 쓴다.
    [Serializable]
    public class StageRewardData
    {
        // "Gold" / "Elleaf" / "Macaron" / "Stamina" — PlayerCurrencyData.CurrencyType와 같은 값.
        public string RewardType;
        public long Amount;
    }

    // JsonUtility는 최상위 배열을 못 읽어서 감싸는 그릇이 필요하다(EnemyWaveListData와 같은 이유).
    [Serializable]
    public class StageRewardListData
    {
        public List<StageRewardData> Rewards = new List<StageRewardData>();
    }
}

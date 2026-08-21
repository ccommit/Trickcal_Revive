using System;
using System.Collections.Generic;
using TrickcalRevive.Data.Party;
using TrickcalRevive.Data.Stage;

namespace TrickcalRevive.Data.Battle
{
    // 전투에 필요한 것을 전부 담은 도시락. 전투 화면은 이것 하나만 받으면 시작할 수 있다.
    [Serializable]
    public class BattleContext
    {
        public string ContextId;
        public BattleStartRequest Request;
        public StageMasterData Stage;

        // 원문 json이 아니라 이미 읽어둔 웨이브 목록을 담는다.
        public List<EnemyWaveData> Waves = new List<EnemyWaveData>();

        public string RewardsJson;
        public List<PlayerPartySlotData> Party = new List<PlayerPartySlotData>();

        // DateTime은 JsonUtility가 직렬화하지 못한다. Ticks로 저장한다.
        public long CreatedAt;
    }
}

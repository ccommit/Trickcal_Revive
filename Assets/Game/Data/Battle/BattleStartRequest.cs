using System;
using System.Collections.Generic;
using TrickcalRevive.Data.Party;

namespace TrickcalRevive.Data.Battle
{
    [Serializable]
    public class BattleStartRequest
    {
        public string RequestId;
        public string StageId;
        public string PartyId;
        public List<PlayerPartySlotData> PartyMembers;
        public string BattleOptions;
        public DateTime CreatedAt;
    }
}

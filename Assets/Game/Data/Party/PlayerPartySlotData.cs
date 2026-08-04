using System;

namespace TrickcalRevive.Data.Party
{
    [Serializable]
    public class PlayerPartySlotData
    {
        public string PartySlotId;
        public string AccountId;
        public string PartyId;
        public int SlotIndex;
        public string PlayerCharacterId;
        public string Side;
        public int PosX;
        public int PosY;
    }
}

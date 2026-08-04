using System.Collections.Generic;
using TrickcalRevive.Data.Party;

namespace TrickcalRevive.Domain.Character
{
    public interface IPartyRepository
    {
        List<PlayerPartySlotData> GetPartySlots(string partyId);
    }
}

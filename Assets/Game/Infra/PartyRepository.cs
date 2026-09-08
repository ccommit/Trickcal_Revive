using System.Collections.Generic;
using System.Linq;
using TrickcalRevive.Data.Party;
using TrickcalRevive.Domain.Character;
using TrickcalRevive.Infra.SaveHandlers;

namespace TrickcalRevive.Infra
{
    public sealed class PartyRepository : PlayerScopedRepository, IPartyRepository
    {
        private readonly PartySaveHandler partyHandler;

        public PartyRepository(IFileStore fileStore, ISessionService sessionService)
            : base(sessionService)
        {
            partyHandler = new PartySaveHandler(fileStore);
        }

        public List<PlayerPartySlotData> GetPartySlots(string partyId)
        {
            var accountId = CurrentAccountId;
            if (accountId == null)
                return new List<PlayerPartySlotData>();

            return partyHandler.Load(accountId).Where(slot => slot.PartyId == partyId).ToList();
        }
    }
}

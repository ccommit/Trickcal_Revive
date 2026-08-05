using System.Collections.Generic;
using System.Linq;
using TrickcalRevive.Data.Party;
using TrickcalRevive.Domain.Command;

namespace TrickcalRevive.Domain.Character
{
    public class PartyFormationValidator
    {
        private const int MinGridPosition = 1;
        private const int MaxGridPosition = 3;

        public CommandResult Validate(List<PlayerPartySlotData> partySlots)
        {
            if (CheckEmpty(partySlots))
                return CommandResult.Denied("파티가 비어 있다");
            if (CheckDuplicateCharacter(partySlots))
                return CommandResult.Denied("같은 캐릭터가 파티에 중복 배치돼 있다");
            if (CheckInvalidPosition(partySlots))
                return CommandResult.Denied("파티 슬롯 좌표가 잘못됐다");
            return CommandResult.Allowed;
        }

        private static bool CheckEmpty(List<PlayerPartySlotData> partySlots)
        {
            return partySlots == null || partySlots.Count == 0;
        }

        private static bool CheckDuplicateCharacter(List<PlayerPartySlotData> partySlots)
        {
            return partySlots.Select(slot => slot.PlayerCharacterId).Distinct().Count() != partySlots.Count;
        }

        private static bool CheckInvalidPosition(List<PlayerPartySlotData> partySlots)
        {
            bool OutOfGrid(int value) => value < MinGridPosition || value > MaxGridPosition;

            if (partySlots.Any(slot => OutOfGrid(slot.PosX) || OutOfGrid(slot.PosY)))
                return true;

            return partySlots.Select(slot => (slot.PosX, slot.PosY)).Distinct().Count() != partySlots.Count;
        }
    }
}

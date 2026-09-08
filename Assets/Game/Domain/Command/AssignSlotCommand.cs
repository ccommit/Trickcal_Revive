using System.Collections.Generic;
using System.Linq;
using TrickcalRevive.Data.Party;

namespace TrickcalRevive.Domain.Command
{
    /// <summary>
    /// 파티 슬롯에 캐릭터를 배치한다. 같은 캐릭터가 다른 슬롯에 있었거나
    /// 같은 좌표에 다른 캐릭터가 있었으면 밀어낸다.
    /// </summary>
    public sealed class AssignSlotCommand : IUndoableCommand
    {
        private readonly List<PlayerPartySlotData> formation;
        private readonly string partyId;
        private readonly string playerCharacterId;
        private readonly int posX;
        private readonly int posY;

        private List<PlayerPartySlotData> removedSlots;
        private PlayerPartySlotData addedSlot;

        public AssignSlotCommand(
            List<PlayerPartySlotData> formation,
            string partyId,
            string playerCharacterId,
            int posX,
            int posY)
        {
            this.formation = formation;
            this.partyId = partyId;
            this.playerCharacterId = playerCharacterId;
            this.posX = posX;
            this.posY = posY;
        }

        public CommandResult CanExecute()
            => string.IsNullOrEmpty(playerCharacterId)
                ? CommandResult.Denied("배치할 캐릭터가 선택되지 않았다")
                : CommandResult.Allowed;

        public CommandResult Execute()
        {
            var check = CanExecute();
            if (!check.IsAllowed)
                return check;

            removedSlots = formation
                .Where(slot => slot.PlayerCharacterId == playerCharacterId || (slot.PosX == posX && slot.PosY == posY))
                .ToList();
            formation.RemoveAll(slot => removedSlots.Contains(slot));

            addedSlot = new PlayerPartySlotData
            {
                PartyId = partyId,
                SlotIndex = formation.Count + 1,
                PlayerCharacterId = playerCharacterId,
                Side = "player",
                PosX = posX,
                PosY = posY
            };
            formation.Add(addedSlot);
            return CommandResult.Allowed;
        }

        public void Undo()
        {
            formation.Remove(addedSlot);
            if (removedSlots != null)
                formation.AddRange(removedSlots);
        }
    }
}

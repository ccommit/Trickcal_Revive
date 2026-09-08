using System.Collections.Generic;
using System.Linq;
using TrickcalRevive.Data.Party;

namespace TrickcalRevive.Domain.Command
{
    /// <summary>파티에서 캐릭터 하나를 뺀다.</summary>
    public sealed class RemoveSlotCommand : IUndoableCommand
    {
        private readonly List<PlayerPartySlotData> formation;
        private readonly string playerCharacterId;
        private PlayerPartySlotData removedSlot;

        public RemoveSlotCommand(List<PlayerPartySlotData> formation, string playerCharacterId)
        {
            this.formation = formation;
            this.playerCharacterId = playerCharacterId;
        }

        public CommandResult CanExecute()
            => formation.Any(slot => slot.PlayerCharacterId == playerCharacterId)
                ? CommandResult.Allowed
                : CommandResult.Denied("해당 캐릭터가 파티에 없다");

        public CommandResult Execute()
        {
            var check = CanExecute();
            if (!check.IsAllowed)
                return check;

            removedSlot = formation.First(slot => slot.PlayerCharacterId == playerCharacterId);
            formation.Remove(removedSlot);
            return CommandResult.Allowed;
        }

        public void Undo()
        {
            if (removedSlot != null)
                formation.Add(removedSlot);
        }
    }
}

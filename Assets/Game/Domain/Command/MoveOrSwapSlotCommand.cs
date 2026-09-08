using System.Collections.Generic;
using System.Linq;

using TrickcalRevive.Data.Party;

namespace TrickcalRevive.Domain.Command
{
    /// <summary>진형의 한 사도를 빈 칸으로 옮기거나, 점유된 두 칸의 사도를 원자적으로 교환한다.</summary>
    public sealed class MoveOrSwapSlotCommand : IUndoableCommand
    {
        private readonly List<PlayerPartySlotData> formation;
        private readonly int fromX;
        private readonly int fromY;
        private readonly int toX;
        private readonly int toY;

        private PlayerPartySlotData source;
        private PlayerPartySlotData target;

        public MoveOrSwapSlotCommand(
            List<PlayerPartySlotData> formation,
            int fromX,
            int fromY,
            int toX,
            int toY)
        {
            this.formation = formation;
            this.fromX = fromX;
            this.fromY = fromY;
            this.toX = toX;
            this.toY = toY;
        }

        public CommandResult CanExecute()
        {
            if (formation == null)
                return CommandResult.Denied("편성 데이터가 없다");
            if (!IsFormationCoordinate(fromX, fromY) || !IsFormationCoordinate(toX, toY))
                return CommandResult.Denied("유효하지 않은 진형 좌표다");
            if (fromX == toX && fromY == toY)
                return CommandResult.Denied("같은 자리로는 이동하지 않는다");
            return formation.Any(slot => slot.PosX == fromX && slot.PosY == fromY)
                ? CommandResult.Allowed
                : CommandResult.Denied("이동할 사도가 원래 자리에 없다");
        }

        public CommandResult Execute()
        {
            var check = CanExecute();
            if (!check.IsAllowed)
                return check;

            source = formation.First(slot => slot.PosX == fromX && slot.PosY == fromY);
            target = formation.FirstOrDefault(slot => slot.PosX == toX && slot.PosY == toY);

            source.PosX = toX;
            source.PosY = toY;
            if (target != null)
            {
                target.PosX = fromX;
                target.PosY = fromY;
            }
            return CommandResult.Allowed;
        }

        public void Undo()
        {
            if (source == null)
                return;
            source.PosX = fromX;
            source.PosY = fromY;
            if (target != null)
            {
                target.PosX = toX;
                target.PosY = toY;
            }
        }

        private static bool IsFormationCoordinate(int x, int y) => x >= 1 && x <= 3 && y >= 1 && y <= 3;
    }
}

using System.Collections.Generic;

namespace TrickcalRevive.Domain.Command
{
    /// <summary>실행된 되돌리기 가능 커맨드를 쌓아두고 되돌린다.</summary>
    public sealed class CommandHistory
    {
        private readonly List<IUndoableCommand> executed = new List<IUndoableCommand>();

        public bool CanUndo => executed.Count > 0;

        /// <summary>실행하고, 성공했을 때만 기록한다.</summary>
        public CommandResult ExecuteAndRecord(IUndoableCommand command)
        {
            var result = command.Execute();
            if (result.IsAllowed)
                executed.Add(command);
            return result;
        }

        /// <summary>가장 최근에 실행한 커맨드를 되돌린다. 기록이 없으면 아무 일도 안 한다.</summary>
        public void UndoLast()
        {
            if (executed.Count == 0)
                return;

            var last = executed[executed.Count - 1];
            executed.RemoveAt(executed.Count - 1);
            last.Undo();
        }

        public void Clear() => executed.Clear();
    }
}

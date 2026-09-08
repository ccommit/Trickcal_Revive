namespace TrickcalRevive.Domain.Command
{
    /// <summary>실행을 되돌릴 수 있는 커맨드.</summary>
    public interface IUndoableCommand : ICommand
    {
        void Undo();
    }
}

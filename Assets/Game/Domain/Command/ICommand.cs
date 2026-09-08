namespace TrickcalRevive.Domain.Command
{
    /// <summary>행동 하나를 물건으로 만든 것.</summary>
    public interface ICommand
    {
        /// <summary>지금 실행해도 되는지 미리 확인한다(버튼 비활성화 등에 쓴다).</summary>
        CommandResult CanExecute();

        /// <summary>실제로 실행한다. CanExecute 없이 바로 불러도 내부에서 재확인해 안전하다.</summary>
        CommandResult Execute();
    }
}

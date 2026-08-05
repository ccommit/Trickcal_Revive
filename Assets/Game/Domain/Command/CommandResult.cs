namespace TrickcalRevive.Domain.Command
{
    /// <summary>커맨드를 실행해도 되는지, 안 되면 왜 안 되는지.</summary>
    public readonly struct CommandResult
    {
        public bool IsAllowed { get; }
        public string Reason { get; }

        private CommandResult(bool isAllowed, string reason)
        {
            IsAllowed = isAllowed;
            Reason = reason;
        }

        public static CommandResult Allowed { get; } = new CommandResult(true, null);
        public static CommandResult Denied(string reason) => new CommandResult(false, reason);
    }
}

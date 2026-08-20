using TrickcalRevive.Data.Account;
using TrickcalRevive.Domain.Account;
using TrickcalRevive.Infra.SaveHandlers;

namespace TrickcalRevive.Infra
{
    public sealed class AccountRepository : PlayerScopedRepository, IAccountRepository
    {
        private readonly AccountSaveHandler accountHandler;

        public AccountRepository(IFileStore fileStore, ISessionService sessionService)
            : base(sessionService)
        {
            accountHandler = new AccountSaveHandler(fileStore);
        }

        public AccountData GetAccount()
        {
            var accountId = CurrentAccountId;
            return accountId == null ? null : accountHandler.Load(accountId);
        }
    }
}

using TrickcalRevive.Data.Account;

namespace TrickcalRevive.Domain.Account
{
    public interface IAccountRepository
    {
        AccountData GetAccount();
    }
}

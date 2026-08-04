using System;
using TrickcalRevive.Data.Account;
using TrickcalRevive.Domain.Account;

namespace TrickcalRevive.Infra
{
    public class AccountAuthRepository : IAccountAuthRepository
    {
        public AccountAuthRepository(JsonFileRepository jsonFileRepository)
        {
        }

        public bool IsLoginIdTaken(string loginId) => throw new NotImplementedException();
        public AccountData CreateAccount(string loginId, string password, string nickname) => throw new NotImplementedException();
        public bool VerifyPassword(string loginId, string password) => throw new NotImplementedException();
        public void DeleteAccount(string accountId) => throw new NotImplementedException();
    }
}

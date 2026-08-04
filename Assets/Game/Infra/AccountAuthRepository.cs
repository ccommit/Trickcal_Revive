using UnityEngine;
using TrickcalRevive.Data.Account;
using TrickcalRevive.Domain.Account;

namespace TrickcalRevive.Infra
{
    public class AccountAuthRepository : IAccountAuthRepository
    {
        private readonly JsonFileRepository jsonFileRepository;

        public AccountAuthRepository(JsonFileRepository jsonFileRepository)
        {
            this.jsonFileRepository = jsonFileRepository;
        }

        public bool IsLoginIdTaken(string loginId)
        {
            return jsonFileRepository.Exists(FilePathFor(loginId));
        }

        public AccountData CreateAccount(string loginId, string password, string nickname)
        {
            const int startingLevel = 1;
            var maxStamina = AccountDefaults.MaxStamina(startingLevel);
            var account = new AccountData
            {
                AccountId = loginId,
                LoginId = loginId,
                Password = password,
                Nickname = nickname,
                PlayerLevel = startingLevel,
                Exp = 0,
                Gold = 0,
                Elleaf = 0,
                Macaron = 0,
                Stamina = maxStamina,
                StaminaMax = maxStamina,
                SaveVersion = 1
            };
            jsonFileRepository.SaveJson(FilePathFor(loginId), JsonUtility.ToJson(account));
            return account;
        }

        public bool VerifyPassword(string loginId, string password)
        {
            var account = LoadAccount(loginId);
            return account != null && account.Password == password;
        }

        public void DeleteAccount(string accountId)
        {
            jsonFileRepository.Delete(FilePathFor(accountId));
        }

        private AccountData LoadAccount(string loginId)
        {
            var json = jsonFileRepository.LoadJson(FilePathFor(loginId));
            return string.IsNullOrEmpty(json) ? null : JsonUtility.FromJson<AccountData>(json);
        }

        private static string FilePathFor(string loginId) => AccountFilePaths.For(loginId);
    }
}

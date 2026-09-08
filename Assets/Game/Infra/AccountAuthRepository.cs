using UnityEngine;
using TrickcalRevive.Data.Account;
using TrickcalRevive.Domain.Account;

namespace TrickcalRevive.Infra
{
    /// <summary>
    /// 개발 단계의 로컬 계정 인증 — 계정 파일 하나에 자격 증명과 계정 값을 같이 둔다.
    /// </summary>
    /// <remarks>
    /// 서버가 없어 계정 파일이 곧 인증 저장소다. 비밀번호를 어떤 형태로 보관하고
    /// 대조할지는 이 클래스가 정하지 않고 <see cref="IPasswordHasher"/>에 맡긴다.
    /// 현재 주입되는 <see cref="PlaintextPasswordHasher"/>는 개발용이라 비밀번호가
    /// 파일에 그대로 남는다. 서버 인증을 붙일 때 구현만 갈아끼운다.
    /// </remarks>
    public class AccountAuthRepository : IAccountAuthRepository
    {
        private readonly JsonFileRepository jsonFileRepository;
        private readonly IPasswordHasher passwordHasher;

        public AccountAuthRepository(JsonFileRepository jsonFileRepository, IPasswordHasher passwordHasher)
        {
            this.jsonFileRepository = jsonFileRepository;
            this.passwordHasher = passwordHasher;
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
                Password = passwordHasher.Hash(password),
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
            return account != null && passwordHasher.Verify(password, account.Password);
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

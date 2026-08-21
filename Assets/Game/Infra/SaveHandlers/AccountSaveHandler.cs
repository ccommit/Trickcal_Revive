using UnityEngine;
using TrickcalRevive.Data.Account;

namespace TrickcalRevive.Infra.SaveHandlers
{
    /// <summary>계정 저장 파일 하나를 전담한다.</summary>
    public sealed class AccountSaveHandler
    {
        private readonly IFileStore fileStore;

        public AccountSaveHandler(IFileStore fileStore)
        {
            this.fileStore = fileStore;
        }

        public AccountData Load(string accountId)
        {
            var json = fileStore.LoadJson(AccountFilePaths.For(accountId));
            return string.IsNullOrEmpty(json) ? null : JsonUtility.FromJson<AccountData>(json);
        }

        public void Save(string accountId, AccountData data)
        {
            fileStore.SaveJson(AccountFilePaths.For(accountId), JsonUtility.ToJson(data));
        }
    }
}

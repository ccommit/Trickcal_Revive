using System;
using System.Collections.Generic;
using TrickcalRevive.Data.Account;
using TrickcalRevive.Data.Character;
using TrickcalRevive.Data.Party;
using TrickcalRevive.Data.Inventory;
using TrickcalRevive.Data.Stage;
using TrickcalRevive.Data.Battle;
using TrickcalRevive.Infra.SaveHandlers;
using UnityEngine;

namespace TrickcalRevive.Infra
{
    public class SaveManager : ISaveManager
    {
        private readonly ISessionService sessionService;
        private readonly AccountSaveHandler accountHandler;

        public SaveManager(IFileStore fileStore, ISessionService sessionService)
        {
            this.sessionService = sessionService;
            accountHandler = new AccountSaveHandler(fileStore);
        }

        public AccountData LoadAccount(string accountId) => accountHandler.Load(accountId);

        public AccountData CreateDefaultSave(string accountId, string nickname)
        {
            const int startingLevel = 1;
            var maxStamina = AccountDefaults.MaxStamina(startingLevel);
            var account = new AccountData
            {
                AccountId = accountId,
                LoginId = accountId,
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
            accountHandler.Save(accountId, account);
            return account;
        }

        public void SaveAccount(AccountData data) => SaveIfSessionActive(id => accountHandler.Save(id, data));

        // 사도/파티/인벤토리/진행도/전투결과 저장은 각 컨텐츠 이슈(06/03/07/08)에서 채운다.
        public void SaveCharacters(List<PlayerCharacterData> data) => throw new NotImplementedException();
        public void SaveParty(List<PlayerPartySlotData> data) => throw new NotImplementedException();
        public void SaveInventory(List<PlayerInventoryData> data) => throw new NotImplementedException();
        public void SaveStageProgress(List<PlayerStageProgressData> data) => throw new NotImplementedException();
        public void SaveBattleResult(List<BattleResultData> data) => throw new NotImplementedException();

        private void SaveIfSessionActive(Action<string> save)
        {
            var accountId = sessionService.GetSession();
            if (accountId == null)
            {
                Debug.LogWarning("[SaveManager] 로그인 세션이 없어 저장을 건너뜁니다.");
                return;
            }
            save(accountId);
        }
    }
}

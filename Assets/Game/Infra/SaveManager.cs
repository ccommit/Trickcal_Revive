using System;
using System.Collections.Generic;
using TrickcalRevive.Data.Account;
using TrickcalRevive.Data.Character;
using TrickcalRevive.Data.Party;
using TrickcalRevive.Data.Inventory;
using TrickcalRevive.Data.Stage;
using TrickcalRevive.Data.Battle;

namespace TrickcalRevive.Infra
{
    public class SaveManager : ISaveManager
    {
        public SaveManager(IFileStore fileStore, ISessionService sessionService)
        {
        }

        public AccountData LoadAccount(string accountId) => throw new NotImplementedException();
        public AccountData CreateDefaultSave(string accountId, string nickname) => throw new NotImplementedException();
        public void SaveAccount(AccountData data) => throw new NotImplementedException();
        public void SaveCharacters(List<PlayerCharacterData> data) => throw new NotImplementedException();
        public void SaveParty(List<PlayerPartySlotData> data) => throw new NotImplementedException();
        public void SaveInventory(List<PlayerInventoryData> data) => throw new NotImplementedException();
        public void SaveStageProgress(List<PlayerStageProgressData> data) => throw new NotImplementedException();
        public void SaveBattleResult(List<BattleResultData> data) => throw new NotImplementedException();
    }
}

using System.Collections.Generic;
using TrickcalRevive.Data.Account;
using TrickcalRevive.Data.Character;
using TrickcalRevive.Data.Party;
using TrickcalRevive.Data.Inventory;
using TrickcalRevive.Data.Stage;
using TrickcalRevive.Data.Battle;

namespace TrickcalRevive.Infra
{
    // "무엇을" 저장할지 정한다. "어떻게" 파일에 쓰는지는 IFileStore가 맡는다.
    public interface ISaveManager
    {
        AccountData LoadAccount(string accountId);
        AccountData CreateDefaultSave(string accountId, string nickname);
        void SaveAccount(AccountData data);
        void SaveCharacters(List<PlayerCharacterData> data);
        void SaveParty(List<PlayerPartySlotData> data);
        void SaveInventory(List<PlayerInventoryData> data);
        void SaveStageProgress(List<PlayerStageProgressData> data);
        void SaveBattleResult(List<BattleResultData> data);
    }
}

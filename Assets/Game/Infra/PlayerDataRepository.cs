using System;
using System.Collections.Generic;
using System.Linq;
using TrickcalRevive.Data.Account;
using TrickcalRevive.Data.Currency;
using TrickcalRevive.Data.Character;
using TrickcalRevive.Data.Party;
using TrickcalRevive.Data.Inventory;
using TrickcalRevive.Data.Stage;
using TrickcalRevive.Domain.Account;
using TrickcalRevive.Domain.Character;
using TrickcalRevive.Domain.Inventory;
using TrickcalRevive.Domain.Stage;
using TrickcalRevive.Infra.SaveHandlers;

namespace TrickcalRevive.Infra
{
    // 파티/인벤토리/진행도 연동은 각 컨텐츠 이슈(03/06/07)에서 채운다. 계정/재화만 실제 동작.
    public class PlayerDataRepository :
        IAccountRepository,
        IPlayerCharacterRepository,
        IPartyRepository,
        ICurrencyRepository,
        ICurrencyWallet,
        IInventoryRepository,
        IStageProgressRepository
    {
        private readonly ISessionService sessionService;
        private readonly AccountSaveHandler accountHandler;
        private readonly PartySaveHandler partyHandler;
        private readonly StageProgressSaveHandler stageProgressHandler;

        public PlayerDataRepository(IFileStore fileStore, ISessionService sessionService, SaveManager saveManager)
        {
            this.sessionService = sessionService;
            accountHandler = new AccountSaveHandler(fileStore);
            partyHandler = new PartySaveHandler(fileStore);
            stageProgressHandler = new StageProgressSaveHandler(fileStore);
        }

        public AccountData GetAccount()
        {
            var accountId = sessionService.GetSession();
            return accountId == null ? null : accountHandler.Load(accountId);
        }

        public List<PlayerCurrencyData> GetCurrencies()
        {
            var account = GetAccount();
            if (account == null)
                return new List<PlayerCurrencyData>();

            return new List<PlayerCurrencyData>
            {
                new PlayerCurrencyData { AccountId = account.AccountId, CurrencyType = "Gold", Amount = account.Gold },
                new PlayerCurrencyData { AccountId = account.AccountId, CurrencyType = "Elleaf", Amount = account.Elleaf },
                new PlayerCurrencyData { AccountId = account.AccountId, CurrencyType = "Macaron", Amount = account.Macaron },
                new PlayerCurrencyData { AccountId = account.AccountId, CurrencyType = "Stamina", Amount = account.Stamina }
            };
        }

        // 사도 지급(가챠/스타터) 자체가 아직 없다 — "0개 보유"가 지금 시점의 실제 상태다.
        public List<PlayerCharacterData> GetOwnedCharacters() => new List<PlayerCharacterData>();
        public void GrantCharacter(string characterId, int star) => throw new NotImplementedException();
        public void AddCharacterShard(string characterId, int count) => throw new NotImplementedException();

        public List<PlayerPartySlotData> GetPartySlots(string partyId)
        {
            var accountId = sessionService.GetSession();
            if (accountId == null)
                return new List<PlayerPartySlotData>();

            return partyHandler.Load(accountId).Where(slot => slot.PartyId == partyId).ToList();
        }

        public long GetAmount(CurrencyType currencyType) => throw new NotImplementedException();
        public bool TryConsume(CurrencyType currencyType, long amount) => throw new NotImplementedException();
        public void Add(CurrencyType currencyType, long amount) => throw new NotImplementedException();

        public List<PlayerInventoryData> GetOwnedItems() => throw new NotImplementedException();
        public long GetItemCount(string itemId) => throw new NotImplementedException();
        public bool TryConsume(string itemId, long count) => throw new NotImplementedException();
        public long GetCharacterShardCount(string characterId) => throw new NotImplementedException();
        public bool TryConsumeCharacterShard(string characterId, int count) => throw new NotImplementedException();

        public PlayerStageProgressData GetStageProgress(string stageId)
        {
            var accountId = sessionService.GetSession();
            if (accountId == null)
                return null;

            return stageProgressHandler.Load(accountId).FirstOrDefault(progress => progress.StageId == stageId);
        }

        public void SaveStageProgress(PlayerStageProgressData progress)
        {
            var accountId = sessionService.GetSession();
            if (accountId == null)
                return;

            var all = stageProgressHandler.Load(accountId);
            var index = all.FindIndex(p => p.StageId == progress.StageId);
            if (index >= 0)
                all[index] = progress;
            else
                all.Add(progress);

            stageProgressHandler.Save(accountId, all);
        }
    }
}

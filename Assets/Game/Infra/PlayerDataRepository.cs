using System;
using System.Collections.Generic;
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

        public PlayerDataRepository(IFileStore fileStore, ISessionService sessionService, SaveManager saveManager)
        {
            this.sessionService = sessionService;
            accountHandler = new AccountSaveHandler(fileStore);
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

        public List<PlayerCharacterData> GetOwnedCharacters() => throw new NotImplementedException();
        public void GrantCharacter(string characterId, int star) => throw new NotImplementedException();
        public void AddCharacterShard(string characterId, int count) => throw new NotImplementedException();

        public List<PlayerPartySlotData> GetPartySlots(string partyId) => throw new NotImplementedException();

        public long GetAmount(CurrencyType currencyType) => throw new NotImplementedException();
        public bool TryConsume(CurrencyType currencyType, long amount) => throw new NotImplementedException();
        public void Add(CurrencyType currencyType, long amount) => throw new NotImplementedException();

        public List<PlayerInventoryData> GetOwnedItems() => throw new NotImplementedException();
        public long GetItemCount(string itemId) => throw new NotImplementedException();
        public bool TryConsume(string itemId, long count) => throw new NotImplementedException();
        public long GetCharacterShardCount(string characterId) => throw new NotImplementedException();
        public bool TryConsumeCharacterShard(string characterId, int count) => throw new NotImplementedException();

        public PlayerStageProgressData GetStageProgress(string stageId) => throw new NotImplementedException();
    }
}

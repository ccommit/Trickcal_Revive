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

namespace TrickcalRevive.Infra
{
    // 세이브 파일 연동은 각 컨텐츠 이슈(01/03/06/07/08)에서 채운다. 지금은 뼈대만.
    public class PlayerDataRepository :
        IAccountRepository,
        IPlayerCharacterRepository,
        IPartyRepository,
        ICurrencyRepository,
        ICurrencyWallet,
        IInventoryRepository,
        IStageProgressRepository
    {
        public PlayerDataRepository(IFileStore fileStore, ISessionService sessionService, SaveManager saveManager)
        {
        }

        public AccountData GetAccount() => throw new NotImplementedException();

        public List<PlayerCharacterData> GetOwnedCharacters() => throw new NotImplementedException();
        public void GrantCharacter(string characterId, int star) => throw new NotImplementedException();
        public void AddCharacterShard(string characterId, int count) => throw new NotImplementedException();

        public List<PlayerPartySlotData> GetPartySlots(string partyId) => throw new NotImplementedException();

        public List<PlayerCurrencyData> GetCurrencies() => throw new NotImplementedException();

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

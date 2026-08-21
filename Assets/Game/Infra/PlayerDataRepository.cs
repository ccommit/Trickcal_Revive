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

        // 가챠/스타터 지급은 아직 없다. UI 복구 검증을 위해 RecoveryFixture(정식 원본
        // 아님)의 사도 30명을 보유분으로 제공한다.
        public List<PlayerCharacterData> GetOwnedCharacters()
        {
            var accountId = sessionService.GetSession();
            return accountId == null
                ? new List<PlayerCharacterData>()
                : Fixtures.RecoveryFixture.OwnedCharacters(accountId);
        }
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

            var saved = stageProgressHandler.Load(accountId).FirstOrDefault(progress => progress.StageId == stageId);
            // 저장분이 없으면 RecoveryFixture의 초기 진행상태로 대체(1-1 클리어/1-2 해금/나머지 잠금).
            return saved ?? Fixtures.RecoveryFixture.DefaultStageProgress(accountId, stageId);
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

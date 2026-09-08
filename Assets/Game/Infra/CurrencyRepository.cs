using System;
using System.Collections.Generic;
using TrickcalRevive.Data.Currency;
using TrickcalRevive.Domain.Account;
using TrickcalRevive.Domain.Inventory;

namespace TrickcalRevive.Infra
{
    /// <summary>
    /// 재화 값은 별도 파일이 아니라 계정 데이터 안에 있다. 그래서 저장 파일을 직접 열지
    /// 않고 <see cref="IAccountRepository"/>에서 받아 쓴다.
    /// </summary>
    public sealed class CurrencyRepository : ICurrencyRepository, ICurrencyWallet
    {
        private readonly IAccountRepository accountRepository;

        public CurrencyRepository(IAccountRepository accountRepository)
        {
            this.accountRepository = accountRepository;
        }

        public List<PlayerCurrencyData> GetCurrencies()
        {
            var account = accountRepository.GetAccount();
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

        // 차감·증가는 07_인벤토리재화 이슈에서 채운다.
        public long GetAmount(CurrencyType currencyType) => throw new NotImplementedException();
        public bool TryConsume(CurrencyType currencyType, long amount) => throw new NotImplementedException();
        public void Add(CurrencyType currencyType, long amount) => throw new NotImplementedException();
    }
}

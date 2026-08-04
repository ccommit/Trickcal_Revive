using System;

namespace TrickcalRevive.Data.Currency
{
    [Serializable]
    public class PlayerCurrencyData
    {
        public string CurrencyId;
        public string AccountId;
        public string CurrencyType;
        public long Amount;
    }
}

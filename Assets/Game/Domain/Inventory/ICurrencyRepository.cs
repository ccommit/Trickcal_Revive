using System.Collections.Generic;
using TrickcalRevive.Data.Currency;

namespace TrickcalRevive.Domain.Inventory
{
    public interface ICurrencyRepository
    {
        List<PlayerCurrencyData> GetCurrencies();
    }
}

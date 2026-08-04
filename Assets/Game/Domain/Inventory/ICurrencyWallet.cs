namespace TrickcalRevive.Domain.Inventory
{
    // 재화 차감. 조회만 하는 ICurrencyRepository와 나눈 이유는, 로비처럼 보여주기만
    // 하는 화면이 차감 기능까지 들고 있을 이유가 없기 때문이다.
    public interface ICurrencyWallet
    {
        long GetAmount(CurrencyType currencyType);
        bool TryConsume(CurrencyType currencyType, long amount);
        void Add(CurrencyType currencyType, long amount);
    }
}

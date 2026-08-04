namespace TrickcalRevive.Domain.Inventory
{
    // 계정 재화. DB_설계서 3.1절 기준. 마카롱은 인벤토리 아이템이 아니라 재화다.
    public enum CurrencyType
    {
        Unknown,

        Gold,
        Elleaf,
        Macaron,
        Stamina
    }
}

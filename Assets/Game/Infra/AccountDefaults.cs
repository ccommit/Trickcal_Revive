namespace TrickcalRevive.Infra
{
    // DB_설계서.md §3.1: stamina_max = 20 + player_level, 계정 레벨업 시마다 재계산.
    internal static class AccountDefaults
    {
        public static int MaxStamina(int playerLevel) => 20 + playerLevel;
    }
}

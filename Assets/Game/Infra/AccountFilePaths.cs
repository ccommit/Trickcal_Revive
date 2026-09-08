using System.Text;

namespace TrickcalRevive.Infra
{
    internal static class AccountFilePaths
    {
        public static string For(string loginId) => $"account_{Sanitize(loginId)}.json";
        public static string CharacterSave(string accountId) => $"player_character_save_{Sanitize(accountId)}.json";
        public static string PartySave(string accountId) => $"player_party_save_{Sanitize(accountId)}.json";
        public static string ProgressSave(string accountId) => $"player_progress_save_{Sanitize(accountId)}.json";
        public static string InventorySave(string accountId) => $"player_inventory_save_{Sanitize(accountId)}.json";
        public static string BattleResultSave(string accountId) => $"player_battle_result_save_{Sanitize(accountId)}.json";

        private static string Sanitize(string loginId)
        {
            var builder = new StringBuilder(loginId.Length);
            foreach (var c in loginId)
                builder.Append(char.IsLetterOrDigit(c) ? c : '_');
            return builder.ToString();
        }
    }
}

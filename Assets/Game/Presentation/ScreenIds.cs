namespace TrickcalRevive.Presentation
{
    // Main씬 내부 화면 id 카탈로그(02_로비화면전환_설계서 §2.4). 씬 이름 상수인
    // SceneIds와는 성격이 다르다 — 이쪽은 전부 같은 Main씬 안에서 전환된다.
    public static class ScreenIds
    {
        public const string Lobby = "Lobby";
        public const string StageSelect = "StageSelect";
        public const string PartySetup = "PartySetup";

        // 배선은 각 문서 착수 시점에.
        public const string Roster = "Roster";
        public const string CharacterDetail = "CharacterDetail";
        public const string Gacha = "Gacha";
        public const string Backpack = "Backpack";
    }
}

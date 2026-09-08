namespace TrickcalRevive.MainUI
{
    /// <summary>PartySetupController가 넘기는 진형 한 칸의 배치 정보.</summary>
    public sealed class PartyFormationEntry
    {
        public int X;              // 1=후열, 2=중열, 3=전열
        public int Y;              // 1..3
        public string CharacterId; // 사도 key (RosterIcon 조회용)
    }
}

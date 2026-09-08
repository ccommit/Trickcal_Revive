namespace TrickcalRevive.MainUI
{
    public enum StageNodeState
    {
        Current,    // 해금됐고 미클리어 — 현재 진행 대상
        Cleared,    // 클리어
        Uncleared,  // 잠금은 아니나 아직 진입 전 — 현재 fixture에는 미사용
        Locked,     // 잠금(자물쇠)
    }

    /// <summary>StageSelectController(Presentation)가 만들어 StageSelectView에 넘기는 노드 표시 데이터.</summary>
    public sealed class StageNodeData
    {
        public string StageId;
        public string Label;     // "1-1"
        public StageNodeState State;
        public int Stars;        // 0..3
    }
}

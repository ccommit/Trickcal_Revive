namespace TrickcalRevive.Domain.Stage
{
    /// <summary>스테이지 승리 결과 반영이 끝났을 때 발행된다.</summary>
    public readonly struct StageClearedEvent
    {
        public string StageId { get; }
        public int StarCount { get; }

        public StageClearedEvent(string stageId, int starCount)
        {
            StageId = stageId;
            StarCount = starCount;
        }
    }
}

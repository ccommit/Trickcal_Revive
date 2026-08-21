using System.Collections.Generic;
using TrickcalRevive.Data.Stage;

namespace TrickcalRevive.Domain.Stage
{
    // 스테이지 원본 정보. 읽기 전용.
    public interface IStageRepository
    {
        StageMasterData GetStage(string stageId);
        List<StageMasterData> GetStagesByChapter(string chapterId);
    }
}

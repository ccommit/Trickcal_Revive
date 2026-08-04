using TrickcalRevive.Data.Stage;

namespace TrickcalRevive.Domain.Stage
{
    public interface IStageProgressRepository
    {
        PlayerStageProgressData GetStageProgress(string stageId);
    }
}

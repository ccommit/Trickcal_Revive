using TrickcalRevive.Data.Stage;

namespace TrickcalRevive.Domain.Stage
{
    public interface IStageProgressRepository
    {
        PlayerStageProgressData GetStageProgress(string stageId);

        // 09_공통기반_설계서.md §5.2엔 조회만 있었다 — StageProgressService.UpdateAfterVictory가
        // 갱신한 진행도를 저장할 방법이 없어서(03_파티편성_설계서.md §3), 개발 중 추가했다.
        void SaveStageProgress(PlayerStageProgressData progress);
    }
}

using System.Linq;
using TrickcalRevive.Data.Stage;
using TrickcalRevive.Domain.Stage;
using TrickcalRevive.Infra.SaveHandlers;

namespace TrickcalRevive.Infra
{
    public sealed class StageProgressRepository : PlayerScopedRepository, IStageProgressRepository
    {
        private readonly StageProgressSaveHandler stageProgressHandler;

        public StageProgressRepository(IFileStore fileStore, ISessionService sessionService)
            : base(sessionService)
        {
            stageProgressHandler = new StageProgressSaveHandler(fileStore);
        }

        public PlayerStageProgressData GetStageProgress(string stageId)
        {
            var accountId = CurrentAccountId;
            if (accountId == null)
                return null;

            var saved = stageProgressHandler.Load(accountId).FirstOrDefault(progress => progress.StageId == stageId);
            // 저장분이 없으면 RecoveryFixture의 초기 진행상태로 대체(1-1 클리어/1-2 해금/나머지 잠금).
            return saved ?? Fixtures.RecoveryFixture.DefaultStageProgress(accountId, stageId);
        }

        public void SaveStageProgress(PlayerStageProgressData progress)
        {
            var accountId = CurrentAccountId;
            if (accountId == null)
                return;

            var all = stageProgressHandler.Load(accountId);
            var index = all.FindIndex(p => p.StageId == progress.StageId);
            if (index >= 0)
                all[index] = progress;
            else
                all.Add(progress);

            stageProgressHandler.Save(accountId, all);
        }
    }
}

using TrickcalRevive.Core;
using TrickcalRevive.Data.Battle;
using TrickcalRevive.Data.Stage;

namespace TrickcalRevive.Domain.Stage
{
    public class StageProgressService
    {
        private readonly IStageRepository stageRepository;
        private readonly IStageProgressRepository stageProgressRepository;
        private readonly EventBus eventBus;

        public StageProgressService(
            IStageRepository stageRepository,
            IStageProgressRepository stageProgressRepository,
            EventBus eventBus)
        {
            this.stageRepository = stageRepository;
            this.stageProgressRepository = stageProgressRepository;
            this.eventBus = eventBus;
        }

        /// <summary>
        /// 최고 기록 갱신(모노토닉) — 이미 딴 별은 이번 결과가 미달해도 잃지 않는다.
        /// StarCount는 04_전투_설계서.md §6의 별 조건 규칙으로 이미 계산된 값이다.
        /// </summary>
        public void UpdateAfterVictory(BattleResultData result)
        {
            var progress = stageProgressRepository.GetStageProgress(result.StageId) ?? new PlayerStageProgressData
            {
                ProgressId = result.StageId,
                AccountId = result.AccountId,
                StageId = result.StageId,
                IsUnlocked = true,
            };

            progress.IsCleared = true;
            progress.ClearCount++;
            if (result.StarCount >= 2)
                progress.Star1 = true;
            if (result.StarCount >= 3)
                progress.Star2 = true;

            stageProgressRepository.SaveStageProgress(progress);

            if (progress.Star1 && progress.Star2)
                UnlockNextStage(progress.StageId);

            eventBus.Publish(new StageClearedEvent(result.StageId, result.StarCount));
        }

        public void UnlockNextStage(string stageId)
        {
            var stage = stageRepository.GetStage(stageId);
            if (stage == null || string.IsNullOrEmpty(stage.NextStageId))
                return;

            var nextProgress = stageProgressRepository.GetStageProgress(stage.NextStageId) ?? new PlayerStageProgressData
            {
                ProgressId = stage.NextStageId,
                StageId = stage.NextStageId,
            };

            if (nextProgress.IsUnlocked)
                return;

            nextProgress.IsUnlocked = true;
            stageProgressRepository.SaveStageProgress(nextProgress);
        }

        public void MarkFirstRewardClaimed(string stageId)
        {
            throw new System.NotImplementedException();
        }

        public void MarkThreeStarRewardClaimed(string stageId)
        {
            throw new System.NotImplementedException();
        }
    }
}

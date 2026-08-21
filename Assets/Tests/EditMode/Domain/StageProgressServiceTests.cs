using System.Collections.Generic;
using NUnit.Framework;
using TrickcalRevive.Core;
using TrickcalRevive.Data.Battle;
using TrickcalRevive.Data.Stage;
using TrickcalRevive.Domain.Stage;

namespace TrickcalRevive.Domain.Tests
{
    public sealed class StageProgressServiceTests
    {
        private sealed class FakeStageRepository : IStageRepository
        {
            public Dictionary<string, StageMasterData> Stages = new Dictionary<string, StageMasterData>();
            public StageMasterData GetStage(string stageId) => Stages.TryGetValue(stageId, out var s) ? s : null;
            public List<StageMasterData> GetStagesByChapter(string chapterId) => new List<StageMasterData>();
        }

        private sealed class FakeStageProgressRepository : IStageProgressRepository
        {
            public Dictionary<string, PlayerStageProgressData> Saved = new Dictionary<string, PlayerStageProgressData>();
            public PlayerStageProgressData GetStageProgress(string stageId) => Saved.TryGetValue(stageId, out var p) ? p : null;
            public void SaveStageProgress(PlayerStageProgressData progress) => Saved[progress.StageId] = progress;
        }

        [Test]
        public void 첫_클리어는_1별_기록을_남기고_StageClearedEvent를_발행한다()
        {
            var stages = new FakeStageRepository();
            var progressRepo = new FakeStageProgressRepository();
            var bus = new EventBus();
            StageClearedEvent? received = null;
            bus.Subscribe<StageClearedEvent>(evt => received = evt);
            var service = new StageProgressService(stages, progressRepo, bus);

            service.UpdateAfterVictory(new BattleResultData { StageId = "stage_1", StarCount = 1 });

            var progress = progressRepo.GetStageProgress("stage_1");
            Assert.That(progress.IsCleared, Is.True);
            Assert.That(progress.Star1, Is.False);
            Assert.That(progress.Star2, Is.False);
            Assert.That(received, Is.Not.Null);
            Assert.That(received.Value.StarCount, Is.EqualTo(1));
        }

        [Test]
        public void 이미_딴_별은_다음_판_결과가_미달해도_잃지_않는다()
        {
            var stages = new FakeStageRepository();
            var progressRepo = new FakeStageProgressRepository();
            var service = new StageProgressService(stages, progressRepo, new EventBus());
            service.UpdateAfterVictory(new BattleResultData { StageId = "stage_1", StarCount = 3 });

            service.UpdateAfterVictory(new BattleResultData { StageId = "stage_1", StarCount = 1 });

            var progress = progressRepo.GetStageProgress("stage_1");
            Assert.That(progress.Star1, Is.True, "이미 딴 2별 조건은 유지돼야 한다");
            Assert.That(progress.Star2, Is.True, "이미 딴 3별 조건은 유지돼야 한다");
        }

        [Test]
        public void 별_3개_달성하면_다음_스테이지가_해금된다()
        {
            var stages = new FakeStageRepository
            {
                Stages =
                {
                    ["stage_1"] = new StageMasterData { StageId = "stage_1", NextStageId = "stage_2" }
                }
            };
            var progressRepo = new FakeStageProgressRepository();
            var service = new StageProgressService(stages, progressRepo, new EventBus());

            service.UpdateAfterVictory(new BattleResultData { StageId = "stage_1", StarCount = 3 });

            var next = progressRepo.GetStageProgress("stage_2");
            Assert.That(next, Is.Not.Null);
            Assert.That(next.IsUnlocked, Is.True);
        }

        [Test]
        public void 별_2개_이하면_다음_스테이지를_해금하지_않는다()
        {
            var stages = new FakeStageRepository
            {
                Stages =
                {
                    ["stage_1"] = new StageMasterData { StageId = "stage_1", NextStageId = "stage_2" }
                }
            };
            var progressRepo = new FakeStageProgressRepository();
            var service = new StageProgressService(stages, progressRepo, new EventBus());

            service.UpdateAfterVictory(new BattleResultData { StageId = "stage_1", StarCount = 2 });

            Assert.That(progressRepo.GetStageProgress("stage_2"), Is.Null);
        }
    }
}

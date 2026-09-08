using System.Collections.Generic;
using NUnit.Framework;
using TrickcalRevive.Data.Battle;
using TrickcalRevive.Data.Party;
using TrickcalRevive.Data.Stage;
using TrickcalRevive.Domain.Battle;
using TrickcalRevive.Domain.Character;
using TrickcalRevive.Domain.Stage;

namespace TrickcalRevive.Domain.Tests
{
    public sealed class BattleAssemblyTests
    {
        private sealed class FakeStageRepository : IStageRepository
        {
            public StageMasterData Stage;

            public StageMasterData GetStage(string stageId) => Stage != null && Stage.StageId == stageId ? Stage : null;
            public List<StageMasterData> GetStagesByChapter(string chapterId) => new List<StageMasterData>();
        }

        private sealed class FakePartyRepository : IPartyRepository
        {
            public List<PlayerPartySlotData> Slots = new List<PlayerPartySlotData>();
            public List<PlayerPartySlotData> GetPartySlots(string partyId) => Slots;
        }

        [Test]
        public void RequestBuilder는_파티_슬롯을_그대로_담는다()
        {
            var parties = new FakePartyRepository
            {
                Slots = new List<PlayerPartySlotData> { new PlayerPartySlotData { PlayerCharacterId = "a" } }
            };
            var builder = new BattleStartRequestBuilder(parties);

            var request = builder.Build("stage_1", "party_main");

            Assert.That(request.StageId, Is.EqualTo("stage_1"));
            Assert.That(request.PartyMembers, Has.Count.EqualTo(1));
            Assert.That(request.RequestId, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public void ContextBuilder는_스테이지를_못_찾으면_null을_돌려준다()
        {
            var stages = new FakeStageRepository();
            var parties = new FakePartyRepository();
            var builder = new BattleContextBuilder(stages, parties);
            var request = new BattleStartRequest { StageId = "missing" };

            var context = builder.Build(request);

            Assert.That(context, Is.Null);
        }

        [Test]
        public void ContextBuilder는_웨이브json을_파싱해서_컨텍스트에_담는다()
        {
            var stages = new FakeStageRepository
            {
                Stage = new StageMasterData
                {
                    StageId = "stage_1",
                    WavesJson = "{\"Waves\":[{\"WaveIndex\":1,\"Spawns\":[]}]}",
                    RewardsJson = "{}",
                }
            };
            var parties = new FakePartyRepository
            {
                Slots = new List<PlayerPartySlotData> { new PlayerPartySlotData { PlayerCharacterId = "a" } }
            };
            var requestBuilder = new BattleStartRequestBuilder(parties);
            var contextBuilder = new BattleContextBuilder(stages, parties);
            var request = requestBuilder.Build("stage_1", "party_main");

            var context = contextBuilder.Build(request);

            Assert.That(context, Is.Not.Null);
            Assert.That(context.Stage.StageId, Is.EqualTo("stage_1"));
            Assert.That(context.Waves, Has.Count.EqualTo(1));
            Assert.That(context.Party, Has.Count.EqualTo(1));
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;

using TrickcalRevive.Data.Party;
using TrickcalRevive.Data.Stage;
using TrickcalRevive.Domain.Battle;
using TrickcalRevive.Domain.Character;
using TrickcalRevive.Domain.Stage;
using TrickcalRevive.Infra;
using TrickcalRevive.Infra.SaveHandlers;
using TrickcalRevive.Presentation;

namespace TrickcalRevive.Infra.Tests
{
    // 파티 편성 파이프라인이 실제 파일 IO 위에서 동작하는지, 그리고 검증->요청 조립->
    // 컨텍스트 조립까지가 실제로 동작하는지 확인한다(03_파티편성_설계서 §0 구현 범위).
    public sealed class PartyFlowTests
    {
        private sealed class FakeStageRepository : IStageRepository
        {
            public StageMasterData Stage;
            public StageMasterData GetStage(string stageId) => Stage != null && Stage.StageId == stageId ? Stage : null;
            public List<StageMasterData> GetStagesByChapter(string chapterId) => new List<StageMasterData>();
        }

        private string tempRoot;
        private JsonFileRepository files;
        private SessionService session;
        private AccountAuthRepository accountAuth;
        private SaveManager saveManager;
        private PlayerDataRepository playerData;

        [SetUp]
        public void SetUp()
        {
            tempRoot = Path.Combine(Path.GetTempPath(), "TrickcalReviveTest_" + Guid.NewGuid());
            files = new JsonFileRepository(tempRoot);
            session = new SessionService(files);
            accountAuth = new AccountAuthRepository(files);
            saveManager = new SaveManager(files, session);
            playerData = new PlayerDataRepository(files, session, saveManager);

            accountAuth.CreateAccount("player1", "pw", "닉네임");
            session.SaveSession("player1");
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
        }

        [Test]
        public void 저장된_파티_파일이_있으면_GetPartySlots가_실제로_읽어온다()
        {
            var handler = new PartySaveHandler(files);
            handler.Save("player1", new List<PlayerPartySlotData>
            {
                new PlayerPartySlotData { PartyId = "party_main", PlayerCharacterId = "a", PosX = 1, PosY = 1 }
            });

            var slots = playerData.GetPartySlots("party_main");

            Assert.That(slots, Has.Count.EqualTo(1));
            Assert.That(slots[0].PlayerCharacterId, Is.EqualTo("a"));
        }

        [Test]
        public void 슬롯_배치와_되돌리기가_실제_파이프라인에서_동작한다()
        {
            var controller = new GameObject("PartySetupController").AddComponent<PartySetupController>();
            var stages = new FakeStageRepository();
            controller.Configure(
                playerData,
                playerData,
                new PartyFormationValidator(),
                new BattleStartRequestBuilder(playerData),
                new BattleContextBuilder(stages, playerData));

            controller.SelectCharacter("hero_1");
            var assignResult = controller.AssignToSlot(1, 1);

            Assert.That(assignResult.IsAllowed, Is.True);
            Assert.That(controller.RenderFormation(), Has.Count.EqualTo(1));

            controller.Undo();
            Assert.That(controller.RenderFormation(), Is.Empty);

            UnityEngine.Object.DestroyImmediate(controller.gameObject);
        }

        [Test]
        public void StartBattle은_검증부터_컨텍스트_조립까지_실제로_동작한다()
        {
            var controller = new GameObject("PartySetupController").AddComponent<PartySetupController>();
            var stages = new FakeStageRepository
            {
                Stage = new StageMasterData { StageId = "stage_1", WavesJson = "", RewardsJson = "{}" }
            };
            controller.Configure(
                playerData,
                playerData,
                new PartyFormationValidator(),
                new BattleStartRequestBuilder(playerData),
                new BattleContextBuilder(stages, playerData));
            controller.EnterForStage("stage_1");
            controller.SelectCharacter("hero_1");
            controller.AssignToSlot(1, 1);

            var result = controller.StartBattle();

            Assert.That(result.IsAllowed, Is.True);
            Assert.That(controller.LastBattleContext, Is.Not.Null);
            Assert.That(controller.LastBattleContext.Stage.StageId, Is.EqualTo("stage_1"));

            UnityEngine.Object.DestroyImmediate(controller.gameObject);
        }

        [Test]
        public void 빈_파티로_StartBattle하면_거부된다()
        {
            var controller = new GameObject("PartySetupController").AddComponent<PartySetupController>();
            var stages = new FakeStageRepository { Stage = new StageMasterData { StageId = "stage_1" } };
            controller.Configure(
                playerData,
                playerData,
                new PartyFormationValidator(),
                new BattleStartRequestBuilder(playerData),
                new BattleContextBuilder(stages, playerData));
            controller.EnterForStage("stage_1");

            var result = controller.StartBattle();

            Assert.That(result.IsAllowed, Is.False);
            Assert.That(controller.LastBattleContext, Is.Null);

            UnityEngine.Object.DestroyImmediate(controller.gameObject);
        }
    }
}

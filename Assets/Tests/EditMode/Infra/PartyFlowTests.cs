using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

using TrickcalRevive.Data.Character;
using TrickcalRevive.Data.Party;
using TrickcalRevive.Data.Stage;
using TrickcalRevive.Domain.Battle;
using TrickcalRevive.Domain.Character;
using TrickcalRevive.Domain.Stage;
using TrickcalRevive.Infra;
using TrickcalRevive.Infra.Fixtures;
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

        private sealed class AllMaisonCharacterRepository : ICharacterRepository
        {
            public UnitMasterData GetCharacter(string characterId)
            {
                var data = RecoveryFixture.Character(characterId);
                if (data != null && characterId == "maison")
                    data.FormationColumn = "All";
                return data;
            }

            public List<UnitMasterData> GetPlayableCharacters() => RecoveryFixture.PlayableCharacters();

            public List<UnitMasterData> GetStarterCharacters() => new List<UnitMasterData>();
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
            accountAuth = new AccountAuthRepository(files, new PlaintextPasswordHasher());
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

        [Test]
        public void 전열_사도는_전열을_가운데_위_아래_순서로만_채운다()
        {
            var controller = CreatePlacementController();

            Assert.That(controller.ToggleCharacter("pc_maestromk2").IsAllowed, Is.True);
            Assert.That(controller.ToggleCharacter("pc_chloe").IsAllowed, Is.True);
            Assert.That(controller.ToggleCharacter("pc_patula").IsAllowed, Is.True);
            var overflow = controller.ToggleCharacter("pc_ricota");

            Assert.That(overflow.IsAllowed, Is.False, "전열 세 자리가 차면 다른 열로 넘어가면 안 된다.");
            Assert.That(
                controller.RenderFormation().Select(slot => (slot.PosX, slot.PosY)),
                Is.EqualTo(new[] { (3, 2), (3, 1), (3, 3) }));

            UnityEngine.Object.DestroyImmediate(controller.gameObject);
        }

        [Test]
        public void 후열_사도는_첫_클릭에_후열_가운데에_배치된다()
        {
            var controller = CreatePlacementController();

            var result = controller.ToggleCharacter("pc_maison");

            Assert.That(result.IsAllowed, Is.True);
            Assert.That(controller.RenderFormation(), Has.Count.EqualTo(1));
            Assert.That(controller.RenderFormation()[0].PosX, Is.EqualTo(1));
            Assert.That(controller.RenderFormation()[0].PosY, Is.EqualTo(2));

            UnityEngine.Object.DestroyImmediate(controller.gameObject);
        }

        [Test]
        public void 같은_전열_사도는_드래그로_빈칸_이동과_점유칸_교환이_가능하다()
        {
            var controller = CreatePlacementController();
            controller.ToggleCharacter("pc_maestromk2");
            controller.ToggleCharacter("pc_chloe");

            var swap = controller.MoveOrSwapSlot(3, 2, 3, 1);

            Assert.That(swap.IsAllowed, Is.True);
            Assert.That(controller.RenderFormation().Single(s => s.PlayerCharacterId == "pc_maestromk2").PosY, Is.EqualTo(1));
            Assert.That(controller.RenderFormation().Single(s => s.PlayerCharacterId == "pc_chloe").PosY, Is.EqualTo(2));

            var move = controller.MoveOrSwapSlot(3, 1, 3, 3);
            Assert.That(move.IsAllowed, Is.True);
            Assert.That(controller.RenderFormation().Single(s => s.PlayerCharacterId == "pc_maestromk2").PosY, Is.EqualTo(3));

            controller.Undo();
            Assert.That(controller.RenderFormation().Single(s => s.PlayerCharacterId == "pc_maestromk2").PosY, Is.EqualTo(1));
            UnityEngine.Object.DestroyImmediate(controller.gameObject);
        }

        [Test]
        public void 전열과_중열_사도의_드래그_교환은_양쪽_모두_거부된다()
        {
            var controller = CreatePlacementController();
            controller.ToggleCharacter("pc_maestromk2");
            controller.ToggleCharacter("pc_diana");

            var result = controller.MoveOrSwapSlot(3, 2, 2, 2);

            Assert.That(result.IsAllowed, Is.False);
            Assert.That(controller.RenderFormation().Single(s => s.PlayerCharacterId == "pc_maestromk2").PosX, Is.EqualTo(3));
            Assert.That(controller.RenderFormation().Single(s => s.PlayerCharacterId == "pc_diana").PosX, Is.EqualTo(2));
            UnityEngine.Object.DestroyImmediate(controller.gameObject);
        }

        [Test]
        public void 전열과_All_사도는_전열_안에서만_교환할_수_있다()
        {
            var controller = CreatePlacementController(new AllMaisonCharacterRepository());
            controller.ToggleCharacter("pc_maestromk2");
            controller.ToggleCharacter("pc_maison");

            Assert.That(controller.MoveOrSwapSlot(3, 2, 3, 1).IsAllowed, Is.True,
                "전열 내부에서는 Front와 All이 서로 교환 가능해야 한다.");
            Assert.That(controller.MoveOrSwapSlot(3, 2, 2, 2).IsAllowed, Is.True,
                "All 사도는 빈 중열로 이동할 수 있어야 한다.");

            var denied = controller.MoveOrSwapSlot(3, 1, 2, 2);
            Assert.That(denied.IsAllowed, Is.False,
                "Front 사도가 중열로 넘어가게 되는 Front↔All 교환은 거부해야 한다.");
            Assert.That(controller.RenderFormation().Single(s => s.PlayerCharacterId == "pc_maestromk2").PosX, Is.EqualTo(3));
            Assert.That(controller.RenderFormation().Single(s => s.PlayerCharacterId == "pc_maison").PosX, Is.EqualTo(2));
            UnityEngine.Object.DestroyImmediate(controller.gameObject);
        }

        [Test]
        public void 복구용_1챕터는_스테이지_노드_10개의_데이터를_제공한다()
        {
            var stages = RecoveryFixture.StagesByChapter(RecoveryFixture.ChapterId);

            Assert.That(stages, Has.Count.EqualTo(10));
            Assert.That(stages.Select(stage => stage.StageId),
                Is.EqualTo(Enumerable.Range(1, 10).Select(number => $"1-{number}")));
            Assert.That(RecoveryFixture.DefaultStageProgress("test", "1-10"), Is.Not.Null);
            Assert.That(RecoveryFixture.DefaultStageProgress("test", "1-10").IsUnlocked, Is.False);
        }

        private PartySetupController CreatePlacementController(ICharacterRepository characters = null)
        {
            var controller = new GameObject("PartySetupController").AddComponent<PartySetupController>();
            controller.Configure(
                playerData,
                playerData,
                new PartyFormationValidator(),
                new BattleStartRequestBuilder(playerData),
                new BattleContextBuilder(new FakeStageRepository(), playerData));

            var repositoryField = typeof(PartySetupController).GetField(
                "characterRepository",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.That(repositoryField, Is.Not.Null, "배치열 판정을 위한 ICharacterRepository가 연결되어야 한다.");
            repositoryField.SetValue(controller, characters ?? new MasterDataRepository());
            return controller;
        }
    }
}

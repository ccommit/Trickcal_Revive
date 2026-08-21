using System.Collections.Generic;
using NUnit.Framework;
using TrickcalRevive.Data.Party;
using TrickcalRevive.Domain.Character;
using TrickcalRevive.Domain.Command;

namespace TrickcalRevive.Domain.Tests
{
    public sealed class CommandTests
    {
        [Test]
        public void AssignSlot는_같은_좌표의_기존_슬롯을_밀어낸다()
        {
            var formation = new List<PlayerPartySlotData>
            {
                new PlayerPartySlotData { PlayerCharacterId = "old", PosX = 1, PosY = 1 }
            };
            var history = new CommandHistory();

            var result = history.ExecuteAndRecord(new AssignSlotCommand(formation, "party_main", "new", 1, 1));

            Assert.That(result.IsAllowed, Is.True);
            Assert.That(formation, Has.Count.EqualTo(1));
            Assert.That(formation[0].PlayerCharacterId, Is.EqualTo("new"));
        }

        [Test]
        public void AssignSlot_되돌리면_밀려난_슬롯이_복원된다()
        {
            var formation = new List<PlayerPartySlotData>
            {
                new PlayerPartySlotData { PlayerCharacterId = "old", PosX = 1, PosY = 1 }
            };
            var history = new CommandHistory();
            history.ExecuteAndRecord(new AssignSlotCommand(formation, "party_main", "new", 1, 1));

            history.UndoLast();

            Assert.That(formation, Has.Count.EqualTo(1));
            Assert.That(formation[0].PlayerCharacterId, Is.EqualTo("old"));
        }

        [Test]
        public void AssignSlot_캐릭터_없으면_거부되고_기록도_안_쌓인다()
        {
            var formation = new List<PlayerPartySlotData>();
            var history = new CommandHistory();

            var result = history.ExecuteAndRecord(new AssignSlotCommand(formation, "party_main", null, 1, 1));

            Assert.That(result.IsAllowed, Is.False);
            Assert.That(history.CanUndo, Is.False);
        }

        [Test]
        public void RemoveSlot은_해당_캐릭터를_제거하고_되돌릴_수_있다()
        {
            var formation = new List<PlayerPartySlotData>
            {
                new PlayerPartySlotData { PlayerCharacterId = "a", PosX = 1, PosY = 1 }
            };
            var history = new CommandHistory();

            history.ExecuteAndRecord(new RemoveSlotCommand(formation, "a"));
            Assert.That(formation, Is.Empty);

            history.UndoLast();
            Assert.That(formation, Has.Count.EqualTo(1));
        }

        [Test]
        public void PartyFormationValidator_빈_파티는_거부한다()
        {
            var validator = new PartyFormationValidator();

            var result = validator.Validate(new List<PlayerPartySlotData>());

            Assert.That(result.IsAllowed, Is.False);
        }

        [Test]
        public void PartyFormationValidator_중복_캐릭터는_거부한다()
        {
            var validator = new PartyFormationValidator();
            var formation = new List<PlayerPartySlotData>
            {
                new PlayerPartySlotData { PlayerCharacterId = "a", PosX = 1, PosY = 1 },
                new PlayerPartySlotData { PlayerCharacterId = "a", PosX = 2, PosY = 2 },
            };

            var result = validator.Validate(formation);

            Assert.That(result.IsAllowed, Is.False);
        }

        [Test]
        public void PartyFormationValidator_격자_밖_좌표는_거부한다()
        {
            var validator = new PartyFormationValidator();
            var formation = new List<PlayerPartySlotData>
            {
                new PlayerPartySlotData { PlayerCharacterId = "a", PosX = 4, PosY = 1 },
            };

            var result = validator.Validate(formation);

            Assert.That(result.IsAllowed, Is.False);
        }

        [Test]
        public void PartyFormationValidator_유효한_배치는_허용한다()
        {
            var validator = new PartyFormationValidator();
            var formation = new List<PlayerPartySlotData>
            {
                new PlayerPartySlotData { PlayerCharacterId = "a", PosX = 1, PosY = 1 },
                new PlayerPartySlotData { PlayerCharacterId = "b", PosX = 2, PosY = 2 },
            };

            var result = validator.Validate(formation);

            Assert.That(result.IsAllowed, Is.True);
        }
    }
}

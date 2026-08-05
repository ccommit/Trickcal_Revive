using System.Collections.Generic;
using UnityEngine;

using TrickcalRevive.Data.Battle;
using TrickcalRevive.Data.Character;
using TrickcalRevive.Data.Party;
using TrickcalRevive.Domain.Battle;
using TrickcalRevive.Domain.Character;
using TrickcalRevive.Domain.Command;

namespace TrickcalRevive.Presentation
{
    public class PartySetupController : MonoBehaviour
    {
        private const string DefaultPartyId = "party_main";
        private static readonly (int X, int Y)[] AutoBuildPositions =
        {
            (3, 2), (3, 1), (3, 3), (2, 2), (1, 2), (2, 1), (2, 3), (1, 1), (1, 3)
        };

        private IPartyRepository partyRepository;
        private IPlayerCharacterRepository ownedCharacterRepository;
        private PartyFormationValidator partyFormationValidator;
        private BattleStartRequestBuilder requestBuilder;
        private BattleContextBuilder contextBuilder;
        private readonly CommandHistory history = new CommandHistory();
        private List<PlayerPartySlotData> formation;
        private string selectedPlayerCharacterId;
        private string currentStageId;

        /// <summary>StartBattle()이 성공했을 때 조립된 결과. 씬 전환은 04_전투_설계서.md 몫.</summary>
        public BattleContext LastBattleContext { get; private set; }

        public void Configure(
            IPartyRepository parties,
            IPlayerCharacterRepository ownedCharacters,
            PartyFormationValidator formationValidator,
            BattleStartRequestBuilder startRequestBuilder,
            BattleContextBuilder battleContextBuilder)
        {
            partyRepository = parties;
            ownedCharacterRepository = ownedCharacters;
            partyFormationValidator = formationValidator;
            requestBuilder = startRequestBuilder;
            contextBuilder = battleContextBuilder;
            formation = partyRepository.GetPartySlots(DefaultPartyId);
        }

        /// <summary>스테이지 정보 팝업이 어느 스테이지로 출발하는지 직접 넘긴다.</summary>
        public void EnterForStage(string stageId)
        {
            currentStageId = stageId;
        }

        public List<PlayerPartySlotData> RenderFormation() => formation;

        public List<PlayerCharacterData> RenderRoster() => ownedCharacterRepository.GetOwnedCharacters();

        public void SelectCharacter(string playerCharacterId)
        {
            selectedPlayerCharacterId = playerCharacterId;
        }

        public CommandResult AssignToSlot(int x, int y)
        {
            if (selectedPlayerCharacterId == null)
                return CommandResult.Denied("배치할 캐릭터가 선택되지 않았다");

            return history.ExecuteAndRecord(
                new AssignSlotCommand(formation, DefaultPartyId, selectedPlayerCharacterId, x, y));
        }

        public CommandResult RemoveSlot(string playerCharacterId)
        {
            return history.ExecuteAndRecord(new RemoveSlotCommand(formation, playerCharacterId));
        }

        public void Undo() => history.UndoLast();

        public void AutoBuildParty()
        {
            formation.Clear();
            history.Clear();
            var owned = ownedCharacterRepository.GetOwnedCharacters();
            for (var i = 0; i < owned.Count && i < AutoBuildPositions.Length; i++)
            {
                var position = AutoBuildPositions[i];
                formation.Add(new PlayerPartySlotData
                {
                    PartyId = DefaultPartyId,
                    SlotIndex = i + 1,
                    PlayerCharacterId = owned[i].PlayerCharacterId,
                    Side = "player",
                    PosX = position.X,
                    PosY = position.Y
                });
            }
        }

        public void ResetParty()
        {
            formation.Clear();
            history.Clear();
        }

        /// <summary>
        /// 검증 → 전투 시작 요청 조립 → 전투 컨텍스트 조립까지. 그 다음(ShowLoading으로
        /// 실제 BattleScene 전환)은 04_전투_설계서.md 구현 시점으로 미룬다
        /// (03_파티편성_설계서 §0, §4-2).
        /// </summary>
        public CommandResult StartBattle()
        {
            var validation = partyFormationValidator.Validate(formation);
            if (!validation.IsAllowed)
                return validation;

            var request = requestBuilder.Build(currentStageId, DefaultPartyId);
            var context = contextBuilder.Build(request);
            if (context == null)
            {
                Debug.LogError($"전투 컨텍스트를 만들지 못했다. stageId={currentStageId}", this);
                return CommandResult.Denied("전투 컨텍스트를 만들지 못했다");
            }

            LastBattleContext = context;
            return CommandResult.Allowed;
        }
    }
}

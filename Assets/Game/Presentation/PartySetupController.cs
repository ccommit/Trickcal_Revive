using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using TrickcalRevive.Data.Battle;
using TrickcalRevive.Data.Character;
using TrickcalRevive.Data.Party;
using TrickcalRevive.Domain.Battle;
using TrickcalRevive.Domain.Character;
using TrickcalRevive.Domain.Command;
using TrickcalRevive.MainUI;

namespace TrickcalRevive.Presentation
{
    public class PartySetupController : MonoBehaviour
    {
        private const string DefaultPartyId = "party_main";
        private const int MaxPartyMembers = 6;

        // All 타입은 전열→중열→후열, 각 열은 가운데→위→아래 순서로 검사한다.
        private static readonly int[] ColumnPriority = { 3, 2, 1 };
        private static readonly int[] RowPriority = { 2, 1, 3 };

        private IPartyRepository partyRepository;
        private IPlayerCharacterRepository ownedCharacterRepository;
        private ICharacterRepository characterRepository;
        private PartyFormationValidator partyFormationValidator;
        private BattleStartRequestBuilder requestBuilder;
        private BattleContextBuilder contextBuilder;
        private readonly CommandHistory history = new CommandHistory();
        private List<PlayerPartySlotData> formation;
        private string selectedPlayerCharacterId;
        public string CurrentStageId { get; private set; }
        private PartySetupView view;

        /// <summary>StartBattle()이 성공했을 때 조립된 결과. 씬 전환은 04_전투_설계서.md 몫.</summary>
        public BattleContext LastBattleContext { get; private set; }

        public void Configure(
            IPartyRepository parties,
            IPlayerCharacterRepository ownedCharacters,
            PartyFormationValidator formationValidator,
            BattleStartRequestBuilder startRequestBuilder,
            BattleContextBuilder battleContextBuilder,
            ICharacterRepository characters = null)
        {
            partyRepository = parties;
            ownedCharacterRepository = ownedCharacters;
            partyFormationValidator = formationValidator;
            requestBuilder = startRequestBuilder;
            contextBuilder = battleContextBuilder;
            characterRepository = characters;
            formation = partyRepository.GetPartySlots(DefaultPartyId);
        }

        public void AttachView(PartySetupView partyView)
        {
            DetachView();
            view = partyView;
            if (view == null)
                return;
            view.RosterCardClicked += HandleRosterCardClicked;
            view.AutoBuildClicked += HandleAutoBuild;
            view.ResetClicked += HandleReset;
            view.UndoClicked += HandleUndo;
            view.StartClicked += HandleStart;
            view.FormationDragRequested += HandleFormationDragRequested;
            RefreshView();
        }

        /// <summary>스테이지 정보 팝업이 어느 스테이지로 출발하는지 직접 넘긴다.</summary>
        public void EnterForStage(string stageId)
        {
            CurrentStageId = stageId;
        }

        public List<PlayerPartySlotData> RenderFormation() => formation;

        public Vector2Int GetSlotPosition(string playerCharacterId)
        {
            var slot = formation?.FirstOrDefault(item => item.PlayerCharacterId == playerCharacterId);
            return slot == null ? Vector2Int.zero : new Vector2Int(slot.PosX, slot.PosY);
        }

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

        /// <summary>
        /// 드래그한 슬롯을 빈 칸으로 옮기거나 점유 칸과 교환한다. 교환은 양쪽 사도가
        /// 서로의 목적지 열에 배치 가능한 경우에만 허용한다.
        /// </summary>
        public CommandResult MoveOrSwapSlot(int fromX, int fromY, int toX, int toY)
        {
            if (!IsFormationCoordinate(fromX, fromY) || !IsFormationCoordinate(toX, toY))
                return CommandResult.Denied("유효하지 않은 진형 좌표다");
            if (fromX == toX && fromY == toY)
                return CommandResult.Denied("같은 자리로는 이동하지 않는다");

            var source = formation.FirstOrDefault(slot => slot.PosX == fromX && slot.PosY == fromY);
            if (source == null)
                return CommandResult.Denied("이동할 사도가 원래 자리에 없다");
            if (!CanOccupyColumn(source.PlayerCharacterId, toX))
                return CommandResult.Denied("해당 사도는 목적지 열에 배치할 수 없다");

            var target = formation.FirstOrDefault(slot => slot.PosX == toX && slot.PosY == toY);
            if (target != null && !CanOccupyColumn(target.PlayerCharacterId, fromX))
                return CommandResult.Denied("두 사도가 서로의 열에 모두 배치 가능해야 한다");

            return history.ExecuteAndRecord(new MoveOrSwapSlotCommand(formation, fromX, fromY, toX, toY));
        }

        /// <summary>로스터 카드 탭 동작. 이미 배치됐으면 제외(토글), 아니면 열 규칙으로 자동 배치.</summary>
        public CommandResult ToggleCharacter(string playerCharacterId)
        {
            if (formation.Any(s => s.PlayerCharacterId == playerCharacterId))
                return RemoveSlot(playerCharacterId);

            if (formation.Count >= MaxPartyMembers)
                return CommandResult.Denied("파티가 가득 찼다(최대 6명)");

            selectedPlayerCharacterId = playerCharacterId;
            foreach (var x in ColumnsFor(playerCharacterId))
            {
                foreach (var y in RowPriority)
                {
                    if (formation.All(s => s.PosX != x || s.PosY != y))
                        return AssignToSlot(x, y);
                }
            }
            return CommandResult.Denied("빈 자리가 없다");
        }

        public void Undo() => history.UndoLast();

        public void AutoBuildParty()
        {
            formation.Clear();
            history.Clear();
            var owned = ownedCharacterRepository.GetOwnedCharacters();
            foreach (var ownedCharacter in owned)
            {
                if (formation.Count >= MaxPartyMembers)
                    break;
                ToggleCharacter(ownedCharacter.PlayerCharacterId);
            }
        }

        private IEnumerable<int> ColumnsFor(string playerCharacterId)
        {
            if (characterRepository == null)
                return ColumnPriority;

            var owned = ownedCharacterRepository.GetOwnedCharacters()
                .FirstOrDefault(character => character.PlayerCharacterId == playerCharacterId);
            var master = owned == null ? null : characterRepository.GetCharacter(owned.CharacterId);
            switch (master?.FormationColumn?.Trim().ToLowerInvariant())
            {
                case "front":
                case "전열":
                    return new[] { 3 };
                case "middle":
                case "mid":
                case "중열":
                    return new[] { 2 };
                case "back":
                case "후열":
                    return new[] { 1 };
                default:
                    return ColumnPriority;
            }
        }

        private bool CanOccupyColumn(string playerCharacterId, int column) =>
            ColumnsFor(playerCharacterId).Contains(column);

        private static bool IsFormationCoordinate(int x, int y) => x >= 1 && x <= 3 && y >= 1 && y <= 3;

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

            var request = requestBuilder.Build(CurrentStageId, DefaultPartyId);
            var context = contextBuilder.Build(request);
            if (context == null)
            {
                Debug.LogError($"전투 컨텍스트를 만들지 못했다. stageId={CurrentStageId}", this);
                return CommandResult.Denied("전투 컨텍스트를 만들지 못했다");
            }

            LastBattleContext = context;
            return CommandResult.Allowed;
        }

        // --- view wiring ------------------------------------------------------

        public void RefreshView()
        {
            if (view == null)
                return;

            var ownedById = RenderRoster().ToDictionary(c => c.PlayerCharacterId, c => c.CharacterId);
            var entries = formation.Select(s => new PartyFormationEntry
            {
                X = s.PosX,
                Y = s.PosY,
                CharacterId = ownedById.TryGetValue(s.PlayerCharacterId, out var cid) ? cid : s.PlayerCharacterId,
            }).ToList();
            var placed = formation.Select(s => s.PlayerCharacterId).ToList();
            view.RenderFormation(entries, placed);
        }

        private void OnDestroy() => DetachView();

        private void DetachView()
        {
            if (view == null)
                return;
            view.RosterCardClicked -= HandleRosterCardClicked;
            view.AutoBuildClicked -= HandleAutoBuild;
            view.ResetClicked -= HandleReset;
            view.UndoClicked -= HandleUndo;
            view.StartClicked -= HandleStart;
            view.FormationDragRequested -= HandleFormationDragRequested;
            view = null;
        }

        private void HandleRosterCardClicked(string playerCharacterId)
        {
            ToggleCharacter(playerCharacterId);
            RefreshView();
        }

        private void HandleAutoBuild()
        {
            AutoBuildParty();
            RefreshView();
        }

        private void HandleReset()
        {
            ResetParty();
            RefreshView();
        }

        private void HandleUndo()
        {
            Undo();
            RefreshView();
        }

        private void HandleStart()
        {
            var result = StartBattle();
            view?.ShowStatus(result.IsAllowed
                ? "전투 컨텍스트 조립 완료(전투 씬은 다음 이슈)"
                : result.Reason);
        }

        private void HandleFormationDragRequested(int fromX, int fromY, int toX, int toY)
        {
            var result = MoveOrSwapSlot(fromX, fromY, toX, toY);
            if (!result.IsAllowed)
                view?.ShowStatus(result.Reason);
            RefreshView();
        }

    }
}

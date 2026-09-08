using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

using Spine.Unity;

namespace TrickcalRevive.MainUI
{
    /// <summary>3×3 진형, 3열 사도 목록, 원본 InGame Spine 미리보기를 담당하는 파티 편성 View.</summary>
    public sealed class PartySetupView : MonoBehaviour
    {
        [SerializeField] private SkeletonGraphic[] formationSlots;
        [SerializeField] private string[] apostleKeys;
        [SerializeField] private SkeletonDataAsset[] apostleSkeletons;
        [SerializeField] private Sprite[] apostleIcons;
        [SerializeField] private Button[] cardButtons;
        [SerializeField] private string[] cardPlayerIds;
        [SerializeField] private Image[] cardDims;
        [SerializeField] private GameObject[] cardRoots;
        [SerializeField] private string[] cardColumns;
        [SerializeField] private long[] cardPowers;
        [SerializeField] private Button autoBuildButton;
        [SerializeField] private Button resetButton;
        [SerializeField] private Button undoButton;
        [SerializeField] private Button startButton;
        [SerializeField] private Button rentButton;
        [SerializeField] private Button filterButton;
        [SerializeField] private Button sortButton;
        [SerializeField] private Button sortDirectionButton;
        [SerializeField] private Button quickBattleButton;
        [SerializeField] private Button autoBattleButton;
        [SerializeField] private TMP_Text filterLabel;
        [SerializeField] private TMP_Text sortDirectionLabel;
        [SerializeField] private TMP_Text statusLabel;
        [SerializeField] private Button[] detailButtons;
        [SerializeField] private string[] apostleDisplayNames;
        [SerializeField] private Sprite[] admissionSkillIcons;
        [SerializeField] private Sprite[] graduateSkillIcons;
        [SerializeField] private Sprite personalityIconSprite;
        [SerializeField] private GameObject characterDetailModal;
        [SerializeField] private Button characterDetailBackdrop;
        [SerializeField] private Button characterDetailCloseButton;
        [SerializeField] private Image detailPortrait;
        [SerializeField] private Image detailPersonalityIcon;
        [SerializeField] private Image detailAdmissionSkillIcon;
        [SerializeField] private Image detailGraduateSkillIcon;
        [SerializeField] private TMP_Text detailNameLabel;
        [SerializeField] private TMP_Text detailPowerLabel;
        [SerializeField] private TMP_Text detailDescriptionLabel;

        private readonly string[] filters = { "All", "Front", "Middle", "Back" };
        private UnityAction[] cardHandlers;
        private int filterIndex;
        private bool descending = true;

        public event Action<string> RosterCardClicked;
        public event Action AutoBuildClicked;
        public event Action ResetClicked;
        public event Action UndoClicked;
        public event Action StartClicked;
        public event Action<int, int, int, int> FormationDragRequested;

        public bool IsReady =>
            formationSlots != null && formationSlots.Length == 9 && formationSlots.All(slot => slot != null)
            && apostleKeys != null && apostleSkeletons != null && apostleIcons != null
            && apostleKeys.Length == apostleSkeletons.Length && apostleKeys.Length == apostleIcons.Length && apostleKeys.Length > 0
            && cardButtons != null && cardPlayerIds != null && cardDims != null && cardRoots != null
            && cardColumns != null && cardPowers != null
            && cardButtons.Length == cardPlayerIds.Length && cardButtons.Length == cardDims.Length
            && cardButtons.Length == cardRoots.Length && cardButtons.Length == cardColumns.Length
            && cardButtons.Length == cardPowers.Length && cardButtons.Length > 0
            && autoBuildButton != null && resetButton != null
            && startButton != null
            && filterButton != null && sortButton != null && sortDirectionButton != null
            && filterLabel != null && sortDirectionLabel != null && statusLabel != null
            && detailButtons != null && detailButtons.Length == cardButtons.Length
            && apostleDisplayNames != null && apostleDisplayNames.Length == cardButtons.Length
            && admissionSkillIcons != null && admissionSkillIcons.Length == cardButtons.Length
            && graduateSkillIcons != null && graduateSkillIcons.Length == cardButtons.Length
            && personalityIconSprite != null && characterDetailModal != null
            && characterDetailBackdrop != null && characterDetailCloseButton != null
            && detailPortrait != null && detailPersonalityIcon != null
            && detailAdmissionSkillIcon != null && detailGraduateSkillIcon != null
            && detailNameLabel != null && detailPowerLabel != null && detailDescriptionLabel != null;

        public void Configure(
            SkeletonGraphic[] slots,
            string[] keys,
            SkeletonDataAsset[] skeletons,
            Sprite[] icons,
            Button[] roster,
            string[] rosterPlayerIds,
            Image[] dims,
            GameObject[] rosterRoots,
            string[] rosterColumns,
            long[] rosterPowers,
            Button autoBuild,
            Button reset,
            Button undo,
            Button start,
            Button rent,
            Button filter,
            Button sort,
            Button sortDirection,
            Button quickBattle,
            Button autoBattle,
            TMP_Text filterText,
            TMP_Text sortDirectionText,
            TMP_Text status)
        {
            formationSlots = slots;
            apostleKeys = keys;
            apostleSkeletons = skeletons;
            apostleIcons = icons;
            cardButtons = roster;
            cardPlayerIds = rosterPlayerIds;
            cardDims = dims;
            cardRoots = rosterRoots;
            cardColumns = rosterColumns;
            cardPowers = rosterPowers;
            autoBuildButton = autoBuild;
            resetButton = reset;
            undoButton = undo;
            startButton = start;
            rentButton = rent;
            filterButton = filter;
            sortButton = sort;
            sortDirectionButton = sortDirection;
            quickBattleButton = quickBattle;
            autoBattleButton = autoBattle;
            filterLabel = filterText;
            sortDirectionLabel = sortDirectionText;
            statusLabel = status;
            ApplyRosterPresentation();
        }

        public void ConfigureCharacterDetail(
            Button[] searchButtons,
            string[] displayNames,
            Sprite[] admissionSkills,
            Sprite[] graduateSkills,
            Sprite personalityIcon,
            GameObject modal,
            Button backdrop,
            Button closeButton,
            Image portrait,
            Image personality,
            Image admissionSkill,
            Image graduateSkill,
            TMP_Text nameLabel,
            TMP_Text powerLabel,
            TMP_Text descriptionLabel)
        {
            detailButtons = searchButtons;
            apostleDisplayNames = displayNames;
            admissionSkillIcons = admissionSkills;
            graduateSkillIcons = graduateSkills;
            personalityIconSprite = personalityIcon;
            characterDetailModal = modal;
            characterDetailBackdrop = backdrop;
            characterDetailCloseButton = closeButton;
            detailPortrait = portrait;
            detailPersonalityIcon = personality;
            detailAdmissionSkillIcon = admissionSkill;
            detailGraduateSkillIcon = graduateSkill;
            detailNameLabel = nameLabel;
            detailPowerLabel = powerLabel;
            detailDescriptionLabel = descriptionLabel;
            if (characterDetailModal != null)
                characterDetailModal.SetActive(false);
        }

        private void OnEnable()
        {
            BindRosterCards();
            autoBuildButton?.onClick.AddListener(RaiseAutoBuild);
            resetButton?.onClick.AddListener(RaiseReset);
            undoButton?.onClick.AddListener(RaiseUndo);
            startButton?.onClick.AddListener(RaiseStart);
            rentButton?.onClick.AddListener(ShowRentScope);
            filterButton?.onClick.AddListener(CycleFilter);
            sortButton?.onClick.AddListener(ShowSortScope);
            sortDirectionButton?.onClick.AddListener(ToggleSortDirection);
            quickBattleButton?.onClick.AddListener(ShowQuickBattleScope);
            autoBattleButton?.onClick.AddListener(ShowAutoBattleScope);
            characterDetailBackdrop?.onClick.AddListener(HideCharacterDetail);
            characterDetailCloseButton?.onClick.AddListener(HideCharacterDetail);
            ApplyRosterPresentation();
        }

        private void OnDisable()
        {
            UnbindRosterCards();
            autoBuildButton?.onClick.RemoveListener(RaiseAutoBuild);
            resetButton?.onClick.RemoveListener(RaiseReset);
            undoButton?.onClick.RemoveListener(RaiseUndo);
            startButton?.onClick.RemoveListener(RaiseStart);
            rentButton?.onClick.RemoveListener(ShowRentScope);
            filterButton?.onClick.RemoveListener(CycleFilter);
            sortButton?.onClick.RemoveListener(ShowSortScope);
            sortDirectionButton?.onClick.RemoveListener(ToggleSortDirection);
            quickBattleButton?.onClick.RemoveListener(ShowQuickBattleScope);
            autoBattleButton?.onClick.RemoveListener(ShowAutoBattleScope);
            characterDetailBackdrop?.onClick.RemoveListener(HideCharacterDetail);
            characterDetailCloseButton?.onClick.RemoveListener(HideCharacterDetail);
        }

        public void RenderFormation(IReadOnlyList<PartyFormationEntry> entries, IReadOnlyList<string> placedPlayerIds)
        {
            foreach (var slot in formationSlots)
            {
                if (slot != null)
                    slot.gameObject.SetActive(false);
            }

            if (entries != null)
            {
                foreach (var entry in entries)
                {
                    var index = (entry.X - 1) * 3 + (entry.Y - 1);
                    if (index < 0 || index >= formationSlots.Length)
                        continue;
                    ShowSpine(formationSlots[index], entry.CharacterId);
                }
            }

            if (cardDims == null || cardPlayerIds == null)
                return;
            for (var i = 0; i < cardDims.Length; i++)
            {
                if (cardDims[i] == null)
                    continue;
                var placed = placedPlayerIds != null
                    && i < cardPlayerIds.Length
                    && placedPlayerIds.Contains(cardPlayerIds[i]);
                cardDims[i].gameObject.SetActive(placed);
            }
        }

        public void ShowStatus(string message)
        {
            if (statusLabel != null)
                statusLabel.text = message ?? string.Empty;
        }

        private void ShowSpine(SkeletonGraphic graphic, string characterId)
        {
            if (graphic == null)
                return;
            var index = Array.IndexOf(apostleKeys, characterId);
            if (index < 0 || index >= apostleSkeletons.Length || apostleSkeletons[index] == null)
                return;

            var skeletonDataAsset = apostleSkeletons[index];
            graphic.gameObject.SetActive(true);
            if (graphic.skeletonDataAsset != skeletonDataAsset)
            {
                var skeletonData = skeletonDataAsset.GetSkeletonData(true);
                graphic.initialSkinName = skeletonData?.FindSkin("Normal") != null ? "Normal" : string.Empty;
                graphic.skeletonDataAsset = skeletonDataAsset;
                graphic.Initialize(true);
                PlayRecoveredIdle(graphic, skeletonDataAsset);
            }
            graphic.Update(0f);
            graphic.LateUpdate();
            FitSpineToViewport(graphic);
        }

        private static void FitSpineToViewport(SkeletonGraphic graphic)
        {
            graphic.rectTransform.pivot = new Vector2(0.5f, 0f);
            graphic.rectTransform.sizeDelta = new Vector2(750f, 1000f);
            graphic.rectTransform.localScale = Vector3.one * 0.4f;
            graphic.rectTransform.anchoredPosition = Vector2.zero;
        }

        private static void PlayRecoveredIdle(SkeletonGraphic graphic, SkeletonDataAsset dataAsset)
        {
            var skeletonData = dataAsset.GetSkeletonData(true);
            if (skeletonData == null || graphic.AnimationState == null)
                return;

            // 정확한 원작 state machine은 아직 미확인이다. binary에 실제 존재하는 이름 중
            // idle/wait/stand 계열만 선택하며, 없으면 setup pose를 유지한다.
            var animation = skeletonData.Animations.FirstOrDefault(candidate =>
                candidate.Name.IndexOf("idle", StringComparison.OrdinalIgnoreCase) >= 0)
                ?? skeletonData.Animations.FirstOrDefault(candidate =>
                    candidate.Name.IndexOf("wait", StringComparison.OrdinalIgnoreCase) >= 0)
                ?? skeletonData.Animations.FirstOrDefault(candidate =>
                    candidate.Name.IndexOf("stand", StringComparison.OrdinalIgnoreCase) >= 0);
            if (animation != null)
                graphic.AnimationState.SetAnimation(0, animation, true);
        }

        private void BindRosterCards()
        {
            if (cardButtons == null || cardPlayerIds == null)
                return;
            cardHandlers = new UnityAction[cardButtons.Length];
            for (var i = 0; i < cardButtons.Length; i++)
            {
                if (cardButtons[i] == null)
                    continue;
                var playerId = i < cardPlayerIds.Length ? cardPlayerIds[i] : null;
                cardHandlers[i] = () => RosterCardClicked?.Invoke(playerId);
                cardButtons[i].onClick.AddListener(cardHandlers[i]);
            }
        }

        private void UnbindRosterCards()
        {
            if (cardButtons == null || cardHandlers == null)
                return;
            for (var i = 0; i < cardButtons.Length && i < cardHandlers.Length; i++)
            {
                if (cardButtons[i] != null && cardHandlers[i] != null)
                    cardButtons[i].onClick.RemoveListener(cardHandlers[i]);
            }
            cardHandlers = null;
        }

        public void ShowCharacterDetail(int index)
        {
            if (characterDetailModal == null || index < 0 || index >= apostleIcons.Length)
                return;

            detailPortrait.sprite = apostleIcons[index];
            detailPersonalityIcon.sprite = personalityIconSprite;
            detailAdmissionSkillIcon.sprite = admissionSkillIcons[index];
            detailGraduateSkillIcon.sprite = graduateSkillIcons[index];
            detailNameLabel.text = index < apostleDisplayNames.Length ? apostleDisplayNames[index] : apostleKeys[index];
            detailPowerLabel.text = $"전투력   {cardPowers[index]:N0}";
            detailDescriptionLabel.text =
                "일반 공격과 저학년 스킬, 고학년 스킬을 확인할 수 있습니다.\n\n"
                + "스킬 수치와 상세 설명은 현재 recovery fixture이며, 원본 master data 연결 전까지 검증용으로 표시됩니다.";
            characterDetailModal.SetActive(true);
        }

        private void HideCharacterDetail()
        {
            if (characterDetailModal != null)
                characterDetailModal.SetActive(false);
        }

        private void ApplyRosterPresentation()
        {
            if (cardRoots == null || cardColumns == null || cardPowers == null)
                return;
            var filter = filters[Mathf.Clamp(filterIndex, 0, filters.Length - 1)];
            var visible = Enumerable.Range(0, cardRoots.Length)
                .Where(index => filter == "All" || string.Equals(cardColumns[index], filter, StringComparison.OrdinalIgnoreCase))
                .OrderBy(index => descending ? -cardPowers[index] : cardPowers[index])
                .ToArray();

            for (var i = 0; i < cardRoots.Length; i++)
                cardRoots[i]?.SetActive(false);
            for (var sibling = 0; sibling < visible.Length; sibling++)
            {
                var card = cardRoots[visible[sibling]];
                if (card == null)
                    continue;
                card.SetActive(true);
                card.transform.SetSiblingIndex(sibling);
            }

            if (filterLabel != null)
                filterLabel.text = filter == "All" ? "필터 ALL" : $"필터 {ColumnKorean(filter)}";
            if (sortDirectionLabel != null)
                sortDirectionLabel.text = descending ? "↓" : "↑";
        }

        private static string ColumnKorean(string column)
        {
            switch (column)
            {
                case "Front": return "전열";
                case "Middle": return "중열";
                case "Back": return "후열";
                default: return "ALL";
            }
        }

        private void CycleFilter()
        {
            filterIndex = (filterIndex + 1) % filters.Length;
            ApplyRosterPresentation();
        }

        public bool CanBeginFormationDrag(int x, int y)
        {
            var index = FormationIndex(x, y);
            return index >= 0
                   && index < formationSlots.Length
                   && formationSlots[index] != null
                   && formationSlots[index].gameObject.activeInHierarchy;
        }

        public void CompleteFormationDrag(int fromX, int fromY, Vector2 screenPosition, Camera eventCamera)
        {
            for (var index = 0; index < formationSlots.Length; index++)
            {
                var slot = formationSlots[index];
                var cell = slot != null ? slot.transform.parent?.parent as RectTransform : null;
                if (cell == null || !RectTransformUtility.RectangleContainsScreenPoint(cell, screenPosition, eventCamera))
                    continue;

                var toX = index / 3 + 1;
                var toY = index % 3 + 1;
                if (fromX == toX && fromY == toY)
                    return;
                FormationDragRequested?.Invoke(fromX, fromY, toX, toY);
                return;
            }

            ShowStatus("배치할 슬롯 위에 사도를 놓아야 합니다.");
        }

        private static int FormationIndex(int x, int y) =>
            x >= 1 && x <= 3 && y >= 1 && y <= 3 ? (x - 1) * 3 + (y - 1) : -1;

        private void ToggleSortDirection()
        {
            descending = !descending;
            ApplyRosterPresentation();
        }

        private void ShowRentScope() => ShowStatus("대여 기능은 다음 복구 범위입니다.");
        private void ShowSortScope() => ShowStatus("정렬 기준: 전투력");
        private void ShowQuickBattleScope() => ShowStatus("빠른 전투는 다음 복구 범위입니다.");
        private void ShowAutoBattleScope() => ShowStatus("자동 전투는 다음 복구 범위입니다.");
        private void RaiseAutoBuild() => AutoBuildClicked?.Invoke();
        private void RaiseReset() => ResetClicked?.Invoke();
        private void RaiseUndo() => UndoClicked?.Invoke();
        private void RaiseStart() => StartClicked?.Invoke();
    }
}

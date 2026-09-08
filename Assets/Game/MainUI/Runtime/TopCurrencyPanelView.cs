using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TrickcalRevive.MainUI
{
    [Flags]
    public enum TopCurrencyVisibility
    {
        None = 0,
        Stamina = 1 << 0,
        Gold = 1 << 1,
        Elleaf = 1 << 2,
        Macaroon = 1 << 3,
        All = Stamina | Gold | Elleaf | Macaroon,
    }

    /// <summary>
    /// Main씬의 모든 화면이 공유하는 상단 패널이다. Lobby는 좌측 Profile과 우측 Menu를,
    /// 나머지 화면은 좌측 Back+PageTitle과 우측 Home을 표시한다. 화면이 필요한 통화 조합도
    /// 함께 지정하며, 숨은 슬롯을 제외한 나머지는 우측부터 원작 순서로 다시 정렬한다.
    /// 데이터의 CurrencyType은 Macaron이지만 UI 오브젝트명은 복구 합의안의
    /// MacaroonCurrency를 사용한다.
    /// </summary>
    public sealed class TopCurrencyPanelView : MonoBehaviour
    {
        private const float RightMostCurrencyX = -110f;
        private const float CurrencyStep = 330f;
        private const float CurrencyY = -15f;

        [SerializeField] private GameObject staminaCurrency;
        [SerializeField] private GameObject goldCurrency;
        [SerializeField] private GameObject elleafCurrency;
        [SerializeField] private GameObject macaroonCurrency;
        [SerializeField] private TMP_Text staminaLabel;
        [SerializeField] private TMP_Text goldLabel;
        [SerializeField] private TMP_Text elleafLabel;
        [SerializeField] private TMP_Text macaroonLabel;
        [SerializeField] private Button menuButton;
        [SerializeField] private Button homeButton;
        [SerializeField] private GameObject profilePanel;
        [SerializeField] private TMP_Text profileNameLabel;
        [SerializeField] private TMP_Text profileLevelLabel;
        [SerializeField] private Image profileExperienceFill;
        [SerializeField] private Button backButton;
        [SerializeField] private TMP_Text pageTitleLabel;

        public event Action MenuRequested;
        public event Action HomeRequested;
        public event Action BackRequested;

        public bool IsReady =>
            HasSlot(staminaCurrency, staminaLabel)
            && HasSlot(goldCurrency, goldLabel)
            && HasSlot(elleafCurrency, elleafLabel)
            && HasSlot(macaroonCurrency, macaroonLabel)
            && HasNavigationButton(menuButton)
            && HasNavigationButton(homeButton)
            && profilePanel != null
            && profilePanel.GetComponent<Image>()?.sprite != null
            && profileNameLabel != null
            && profileLevelLabel != null
            && profileExperienceFill != null
            && backButton != null
            && backButton.image?.sprite != null
            && pageTitleLabel != null;

        public TopCurrencyVisibility VisibleCurrencies { get; private set; } = TopCurrencyVisibility.All;
        public bool IsLobbyMode { get; private set; } = true;
        public string Stamina => staminaLabel != null ? staminaLabel.text : string.Empty;
        public string Gold => goldLabel != null ? goldLabel.text : string.Empty;
        public string Elleaf => elleafLabel != null ? elleafLabel.text : string.Empty;
        public string Macaroon => macaroonLabel != null ? macaroonLabel.text : string.Empty;
        public string ProfileName => profileNameLabel != null ? profileNameLabel.text : string.Empty;
        public string ProfileLevel => profileLevelLabel != null ? profileLevelLabel.text : string.Empty;
        public string PageTitle => pageTitleLabel != null ? pageTitleLabel.text : string.Empty;
        public bool IsProfileVisible => profilePanel != null && profilePanel.activeSelf;
        public Button MenuButton => menuButton;
        public Button HomeButton => homeButton;
        public Button BackButton => backButton;

        public void Configure(
            GameObject stamina,
            TMP_Text staminaValue,
            GameObject gold,
            TMP_Text goldValue,
            GameObject elleaf,
            TMP_Text elleafValue,
            GameObject macaroon,
            TMP_Text macaroonValue,
            Button menu,
            Button home,
            GameObject profile,
            TMP_Text profileName,
            TMP_Text profileLevel,
            Image profileExperience,
            Button back,
            TMP_Text pageTitle)
        {
            UnbindNavigationButtons();
            staminaCurrency = stamina;
            staminaLabel = staminaValue;
            goldCurrency = gold;
            goldLabel = goldValue;
            elleafCurrency = elleaf;
            elleafLabel = elleafValue;
            macaroonCurrency = macaroon;
            macaroonLabel = macaroonValue;
            menuButton = menu;
            homeButton = home;
            profilePanel = profile;
            profileNameLabel = profileName;
            profileLevelLabel = profileLevel;
            profileExperienceFill = profileExperience;
            backButton = back;
            pageTitleLabel = pageTitle;
            if (isActiveAndEnabled)
                BindNavigationButtons();
        }

        public void RenderProfile(string nickname, int level, int experience, int experienceToNextLevel)
        {
            if (profileNameLabel != null)
                profileNameLabel.text = string.IsNullOrWhiteSpace(nickname) ? "New Leader" : nickname;
            if (profileLevelLabel != null)
                profileLevelLabel.text = $"Lv.{Mathf.Max(1, level)}";
            if (profileExperienceFill != null)
            {
                profileExperienceFill.fillAmount = experienceToNextLevel <= 0
                    ? 0f
                    : Mathf.Clamp01((float)experience / experienceToNextLevel);
            }
        }

        public void Render(long stamina, int staminaMax, long gold, long elleaf, long macaroon)
        {
            if (staminaLabel != null)
                staminaLabel.text = $"{stamina:N0}/{Mathf.Max(0, staminaMax):N0}";
            if (goldLabel != null)
                goldLabel.text = gold.ToString("N0");
            if (elleafLabel != null)
                elleafLabel.text = elleaf.ToString("N0");
            if (macaroonLabel != null)
                macaroonLabel.text = macaroon.ToString("N0");
        }

        public void Show(TopCurrencyVisibility visibleCurrencies, bool isLobby, string pageTitle)
        {
            VisibleCurrencies = visibleCurrencies & TopCurrencyVisibility.All;
            IsLobbyMode = isLobby;
            SetVisible(staminaCurrency, TopCurrencyVisibility.Stamina);
            SetVisible(goldCurrency, TopCurrencyVisibility.Gold);
            SetVisible(elleafCurrency, TopCurrencyVisibility.Elleaf);
            SetVisible(macaroonCurrency, TopCurrencyVisibility.Macaroon);
            gameObject.SetActive(true);
            menuButton?.gameObject.SetActive(isLobby);
            homeButton?.gameObject.SetActive(!isLobby);
            profilePanel?.SetActive(isLobby);
            backButton?.gameObject.SetActive(!isLobby);
            if (pageTitleLabel != null)
            {
                pageTitleLabel.text = isLobby ? string.Empty : pageTitle ?? string.Empty;
                pageTitleLabel.gameObject.SetActive(!isLobby);
            }
            PackVisibleSlotsFromRight();
        }

        public bool IsVisible(TopCurrencyVisibility currency)
        {
            return (VisibleCurrencies & currency) != 0;
        }

        private void SetVisible(GameObject slot, TopCurrencyVisibility currency)
        {
            if (slot != null)
                slot.SetActive(IsVisible(currency));
        }

        private void PackVisibleSlotsFromRight()
        {
            // 원작의 좌→우 순서는 마카롱, 캔디, 골드, 엘리프다. 역순으로 배치하면
            // 어떤 조합을 숨겨도 남은 항목이 Menu/Home 공용 자리 왼쪽에 빈칸 없이 붙는다.
            var rightToLeft = new[] { elleafCurrency, goldCurrency, staminaCurrency, macaroonCurrency };
            var positionX = RightMostCurrencyX;
            foreach (var slot in rightToLeft)
            {
                if (slot == null || !slot.activeSelf)
                    continue;

                var rect = slot.GetComponent<RectTransform>();
                if (rect == null)
                    continue;
                rect.anchorMin = Vector2.one;
                rect.anchorMax = Vector2.one;
                rect.pivot = Vector2.one;
                rect.anchoredPosition = new Vector2(positionX, CurrencyY);
                positionX -= CurrencyStep;
            }
        }

        private static bool HasSlot(GameObject slot, TMP_Text label)
        {
            return slot != null && label != null && slot.GetComponent<UnityEngine.UI.Image>()?.sprite != null;
        }

        private static bool HasNavigationButton(Button button)
        {
            return button != null
                   && button.image != null
                   && button.image.sprite != null
                   && button.transform.Find("Icon")?.GetComponent<Image>()?.sprite != null;
        }

        private void OnEnable()
        {
            BindNavigationButtons();
        }

        private void OnDisable()
        {
            UnbindNavigationButtons();
        }

        private void BindNavigationButtons()
        {
            menuButton?.onClick.RemoveListener(RaiseMenuRequested);
            menuButton?.onClick.AddListener(RaiseMenuRequested);
            homeButton?.onClick.RemoveListener(RaiseHomeRequested);
            homeButton?.onClick.AddListener(RaiseHomeRequested);
            backButton?.onClick.RemoveListener(RaiseBackRequested);
            backButton?.onClick.AddListener(RaiseBackRequested);
        }

        private void UnbindNavigationButtons()
        {
            menuButton?.onClick.RemoveListener(RaiseMenuRequested);
            homeButton?.onClick.RemoveListener(RaiseHomeRequested);
            backButton?.onClick.RemoveListener(RaiseBackRequested);
        }

        private void RaiseMenuRequested()
        {
            MenuRequested?.Invoke();
        }

        private void RaiseHomeRequested()
        {
            HomeRequested?.Invoke();
        }

        private void RaiseBackRequested()
        {
            BackRequested?.Invoke();
        }
    }
}

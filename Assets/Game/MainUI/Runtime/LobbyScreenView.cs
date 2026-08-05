using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TrickcalRevive.MainUI
{
    public sealed class LobbyScreenView : MonoBehaviour
    {
        [Header("Visual root")]
        [SerializeField] private Image background;

        [Header("Profile")]
        [SerializeField] private TMP_Text profileNameLabel;
        [SerializeField] private TMP_Text profileLevelLabel;
        [SerializeField] private Image profileExperienceFill;

        [Header("Currencies")]
        [SerializeField] private TMP_Text goldLabel;
        [SerializeField] private TMP_Text elleafLabel;
        [SerializeField] private TMP_Text macaronLabel;
        [SerializeField] private TMP_Text staminaLabel;

        [Header("Settings")]
        [SerializeField] private Button settingsButton;
        [SerializeField] private GameObject settingsMenu;
        [SerializeField] private Button logoutButton;

        [Header("Deferred navigation")]
        [SerializeField] private Button recruitButton;
        [SerializeField] private Button apostleButton;
        [SerializeField] private Button adventureButton;

        [Header("Feedback")]
        [SerializeField] private TMP_Text statusLabel;

        public event Action SettingsRequested;
        public event Action LogoutRequested;
        public event Action RecruitRequested;
        public event Action ApostleRequested;
        public event Action AdventureRequested;

        public bool IsReady =>
            background != null
            && background.sprite != null
            && profileNameLabel != null
            && profileLevelLabel != null
            && profileExperienceFill != null
            && goldLabel != null
            && elleafLabel != null
            && staminaLabel != null
            && settingsButton != null
            && settingsMenu != null
            && logoutButton != null
            && recruitButton != null
            && apostleButton != null
            && adventureButton != null;

        public string ProfileName => profileNameLabel != null ? profileNameLabel.text : string.Empty;
        public string ProfileLevel => profileLevelLabel != null ? profileLevelLabel.text : string.Empty;
        public string Gold => goldLabel != null ? goldLabel.text : string.Empty;
        public string Elleaf => elleafLabel != null ? elleafLabel.text : string.Empty;
        public string Macaron => macaronLabel != null ? macaronLabel.text : string.Empty;
        public string Stamina => staminaLabel != null ? staminaLabel.text : string.Empty;
        public bool IsSettingsOpen => settingsMenu != null && settingsMenu.activeSelf;
        public Button RecruitButton => recruitButton;
        public Button ApostleButton => apostleButton;
        public Button AdventureButton => adventureButton;

        public void Configure(
            Image lobbyBackground,
            TMP_Text profileName,
            TMP_Text profileLevel,
            Image profileExperience,
            TMP_Text gold,
            TMP_Text elleaf,
            TMP_Text macaron,
            TMP_Text stamina,
            Button settings,
            GameObject menu,
            Button logout,
            Button recruit,
            Button apostle,
            Button adventure,
            TMP_Text status)
        {
            background = lobbyBackground;
            profileNameLabel = profileName;
            profileLevelLabel = profileLevel;
            profileExperienceFill = profileExperience;
            goldLabel = gold;
            elleafLabel = elleaf;
            macaronLabel = macaron;
            staminaLabel = stamina;
            settingsButton = settings;
            settingsMenu = menu;
            logoutButton = logout;
            recruitButton = recruit;
            apostleButton = apostle;
            adventureButton = adventure;
            statusLabel = status;
        }

        private void Awake()
        {
            SetSettingsOpen(false);
        }

        private void OnEnable()
        {
            settingsButton?.onClick.AddListener(RaiseSettingsRequested);
            logoutButton?.onClick.AddListener(RaiseLogoutRequested);
            recruitButton?.onClick.AddListener(RaiseRecruitRequested);
            apostleButton?.onClick.AddListener(RaiseApostleRequested);
            adventureButton?.onClick.AddListener(RaiseAdventureRequested);
        }

        private void OnDisable()
        {
            settingsButton?.onClick.RemoveListener(RaiseSettingsRequested);
            logoutButton?.onClick.RemoveListener(RaiseLogoutRequested);
            recruitButton?.onClick.RemoveListener(RaiseRecruitRequested);
            apostleButton?.onClick.RemoveListener(RaiseApostleRequested);
            adventureButton?.onClick.RemoveListener(RaiseAdventureRequested);
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

        public void RenderCurrencies(long gold, long elleaf, long macaron, int stamina, int staminaMax)
        {
            if (goldLabel != null)
                goldLabel.text = gold.ToString("N0");
            if (elleafLabel != null)
                elleafLabel.text = elleaf.ToString("N0");
            if (macaronLabel != null)
                macaronLabel.text = macaron.ToString("N0");
            if (staminaLabel != null)
                staminaLabel.text = $"{stamina:N0}/{staminaMax:N0}";
        }

        public void ShowUnavailable(string message)
        {
            if (statusLabel != null)
            {
                statusLabel.text = message;
                statusLabel.color = new Color32(255, 174, 174, 255);
            }
        }

        public void SetSettingsOpen(bool visible)
        {
            settingsMenu?.SetActive(visible);
        }

        private void RaiseSettingsRequested()
        {
            SettingsRequested?.Invoke();
        }

        private void RaiseLogoutRequested()
        {
            LogoutRequested?.Invoke();
        }

        private void RaiseRecruitRequested()
        {
            RecruitRequested?.Invoke();
        }

        private void RaiseApostleRequested()
        {
            ApostleRequested?.Invoke();
        }

        private void RaiseAdventureRequested()
        {
            AdventureRequested?.Invoke();
        }
    }
}

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

        [Header("Settings")]
        [SerializeField] private GameObject settingsMenu;
        [SerializeField] private Button logoutButton;

        [Header("Deferred navigation")]
        [SerializeField] private Button recruitButton;
        [SerializeField] private Button apostleButton;
        [SerializeField] private Button adventureButton;

        [Header("Feedback")]
        [SerializeField] private TMP_Text statusLabel;

        public event Action LogoutRequested;
        public event Action RecruitRequested;
        public event Action ApostleRequested;
        public event Action AdventureRequested;

        public bool IsReady =>
            background != null
            && background.sprite != null
            && settingsMenu != null
            && logoutButton != null
            && recruitButton != null
            && apostleButton != null
            && adventureButton != null;

        public bool IsSettingsOpen => settingsMenu != null && settingsMenu.activeSelf;
        public Button RecruitButton => recruitButton;
        public Button ApostleButton => apostleButton;
        public Button AdventureButton => adventureButton;

        public void Configure(
            Image lobbyBackground,
            GameObject menu,
            Button logout,
            Button recruit,
            Button apostle,
            Button adventure,
            TMP_Text status)
        {
            background = lobbyBackground;
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
            logoutButton?.onClick.AddListener(RaiseLogoutRequested);
            recruitButton?.onClick.AddListener(RaiseRecruitRequested);
            apostleButton?.onClick.AddListener(RaiseApostleRequested);
            adventureButton?.onClick.AddListener(RaiseAdventureRequested);
        }

        private void OnDisable()
        {
            logoutButton?.onClick.RemoveListener(RaiseLogoutRequested);
            recruitButton?.onClick.RemoveListener(RaiseRecruitRequested);
            apostleButton?.onClick.RemoveListener(RaiseApostleRequested);
            adventureButton?.onClick.RemoveListener(RaiseAdventureRequested);
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

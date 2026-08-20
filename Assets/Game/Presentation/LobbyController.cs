using UnityEngine;

using TrickcalRevive.MainUI;

namespace TrickcalRevive.Presentation
{
    public class LobbyController : MonoBehaviour
    {
        private IPopupService popups;
        [SerializeField] private SettingsController settingsController;
        private INavigationService navigationService;
        private IScreenNavigator screenNavigator;
        private LobbyScreenView view;

        public void Configure(
            IPopupService popups,
            INavigationService navigation = null,
            IScreenNavigator screens = null)
        {
            this.popups = popups;
            navigationService = navigation;
            screenNavigator = screens;
        }

        public void AttachView(LobbyScreenView lobbyView)
        {
            DetachView();
            view = lobbyView;
            if (view == null)
                return;

            view.LogoutRequested += HandleLogoutRequested;
            view.AdventureRequested += HandleAdventureRequested;
        }

        public SettingsController OpenSettings()
        {
            popups.Open("Settings");
            return settingsController;
        }

        public void OpenSettingsMenu()
        {
            OpenSettings();
            view?.SetSettingsOpen(true);
        }

        /// <summary>스테이지 선택 화면으로. 별도 씬이 아니라 Main씬 내부 화면 전환이다
        /// (02_로비화면전환_설계서 §2.4, §0의 AdventureController 흡수 결정).</summary>
        public void OpenAdventure()
        {
            screenNavigator?.Show(ScreenIds.StageSelect);
        }

        private void OnDestroy()
        {
            DetachView();
        }

        private void DetachView()
        {
            if (view == null)
                return;

            view.LogoutRequested -= HandleLogoutRequested;
            view.AdventureRequested -= HandleAdventureRequested;
            view = null;
        }

        private void HandleLogoutRequested()
        {
            settingsController.Logout();
            view?.SetSettingsOpen(false);
            navigationService?.Go(SceneIds.Login);
        }

        private void HandleAdventureRequested()
        {
            OpenAdventure();
        }

    }
}

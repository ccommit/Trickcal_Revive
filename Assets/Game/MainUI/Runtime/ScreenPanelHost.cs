using UnityEngine;

namespace TrickcalRevive.MainUI
{
    /// <summary>
    /// Main씬 내부 화면 패널의 표시/숨김. IScreenNavigator.ScreenChanged 구독은
    /// Presentation↔MainUI 순환 의존을 피하려 설치자(MainSceneInstaller)가 문자열로
    /// 브리지한다 — 여기선 screenId 문자열만 안다.
    /// screenId 값은 Presentation.ScreenIds와 일치해야 한다(Lobby/StageSelect/PartySetup).
    /// </summary>
    public sealed class ScreenPanelHost : MonoBehaviour
    {
        [SerializeField] private GameObject lobbyPanel;
        [SerializeField] private GameObject stageSelectPanel;
        [SerializeField] private GameObject partySetupPanel;

        public void Configure(GameObject lobby, GameObject stageSelect, GameObject partySetup)
        {
            lobbyPanel = lobby;
            stageSelectPanel = stageSelect;
            partySetupPanel = partySetup;
        }

        public void ShowScreen(string screenId)
        {
            SetPanel(lobbyPanel, screenId == "Lobby");
            SetPanel(stageSelectPanel, screenId == "StageSelect");
            SetPanel(partySetupPanel, screenId == "PartySetup");
        }

        private static void SetPanel(GameObject panel, bool visible)
        {
            if (panel != null)
                panel.SetActive(visible);
        }
    }
}

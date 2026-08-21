using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

using TrickcalRevive.App;
using TrickcalRevive.Presentation;

namespace TrickcalRevive.MainUI.Tests
{
    public sealed class LoginLobbyFlowPlayModeTests
    {
        private const string LoginId = "recovery_ui_playmode_account";

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            DeleteTestFiles();
            SceneManager.LoadScene(SceneIds.Login);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (var app in Object.FindObjectsByType<GameApplication>(FindObjectsSortMode.None))
                Object.Destroy(app.gameObject);
            foreach (var flow in Object.FindObjectsByType<SceneFlowController>(FindObjectsSortMode.None))
                Object.Destroy(flow.gameObject);
            yield return null;
            DeleteTestFiles();
        }

        [UnityTest]
        public IEnumerator NewAccount_LoginToLobbyThenLogout_ReturnsToLogin()
        {
            var loginView = Object.FindFirstObjectByType<LoginScreenView>();
            Assert.That(loginView, Is.Not.Null);
            Assert.That(loginView.IsReady, Is.True);

            Input(loginView, "LoginIdInput").text = LoginId;
            Input(loginView, "PasswordInput").text = "test-password";
            Click(loginView, "LoginButton");
            Assert.That(loginView.IsSignUpVisible, Is.True);

            Input(loginView, "NicknameInput").text = "Recovery QA";
            Click(loginView, "CreateAccountButton");
            yield return null;

            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(SceneIds.Main));
            var lobbyView = Object.FindFirstObjectByType<LobbyScreenView>();
            Assert.That(lobbyView, Is.Not.Null);
            Assert.That(lobbyView.IsReady, Is.True);
            var topCurrency = Object.FindFirstObjectByType<TopCurrencyPanelView>();
            Assert.That(topCurrency, Is.Not.Null);
            Assert.That(topCurrency.IsReady, Is.True);
            Assert.That(topCurrency.ProfileName, Is.EqualTo("Recovery QA"));
            Assert.That(topCurrency.ProfileLevel, Is.EqualTo("Lv.1"));
            Assert.That(topCurrency.IsProfileVisible, Is.True);
            Assert.That(topCurrency.Gold, Is.EqualTo("0"));
            Assert.That(topCurrency.Stamina, Is.EqualTo("21/21"));
            Assert.That(topCurrency.VisibleCurrencies, Is.EqualTo(TopCurrencyVisibility.All));
            Assert.That(topCurrency.MenuButton.gameObject.activeSelf, Is.True);
            Assert.That(topCurrency.HomeButton.gameObject.activeSelf, Is.False);
            Assert.That(topCurrency.BackButton.gameObject.activeSelf, Is.False);

            var feedback = lobbyView.RecruitButton.GetComponent<LobbyButtonPressFeedback>();
            Assert.That(feedback, Is.Not.Null);
            var pointer = new PointerEventData(EventSystem.current);
            ExecuteEvents.Execute(
                lobbyView.RecruitButton.gameObject,
                pointer,
                ExecuteEvents.pointerDownHandler);
            yield return new WaitForSecondsRealtime(0.12f);
            Assert.That(feedback.CurrentScale.x, Is.EqualTo(0.9f).Within(0.02f));
            Assert.That(feedback.CurrentScale.y, Is.EqualTo(1.2f).Within(0.02f));

            ExecuteEvents.Execute(
                lobbyView.RecruitButton.gameObject,
                pointer,
                ExecuteEvents.pointerUpHandler);
            yield return new WaitForSecondsRealtime(0.22f);
            Assert.That(feedback.CurrentScale.x, Is.EqualTo(1f).Within(0.02f));
            Assert.That(feedback.CurrentScale.y, Is.EqualTo(1f).Within(0.02f));

            Click(lobbyView, "AdventureButton");
            yield return new WaitForSecondsRealtime(0.65f);
            Assert.That(
                topCurrency.VisibleCurrencies,
                Is.EqualTo(
                    TopCurrencyVisibility.Stamina
                    | TopCurrencyVisibility.Gold
                    | TopCurrencyVisibility.Elleaf));
            Assert.That(topCurrency.transform.Find("MacaroonCurrency").gameObject.activeSelf, Is.False);
            Assert.That(topCurrency.MenuButton.gameObject.activeSelf, Is.False);
            Assert.That(topCurrency.HomeButton.gameObject.activeSelf, Is.True);
            Assert.That(topCurrency.IsProfileVisible, Is.False);
            Assert.That(topCurrency.BackButton.gameObject.activeSelf, Is.True);
            Assert.That(topCurrency.PageTitle, Is.EqualTo("스테이지 리스트"));

            var stageSelect = Object.FindFirstObjectByType<StageSelectView>();
            Assert.That(stageSelect, Is.Not.Null);
            var mapBackground = stageSelect.transform.Find("MapBackground").GetComponent<Image>();
            var worldOneBackground = mapBackground.sprite;
            Click(stageSelect, "NextWorldButton");
            yield return null;
            Assert.That(mapBackground.sprite, Is.Not.SameAs(worldOneBackground));
            Click(stageSelect, "PreviousWorldButton");
            yield return null;
            Assert.That(mapBackground.sprite, Is.SameAs(worldOneBackground));

            Click(stageSelect, "Node0");
            yield return null;
            var stageInfo = Object.FindFirstObjectByType<StageInfoPopupView>();
            Assert.That(stageInfo, Is.Not.Null);
            Click(stageInfo, "DeckButton");
            yield return null;
            var partySetup = Object.FindFirstObjectByType<PartySetupView>();
            Assert.That(partySetup, Is.Not.Null);
            Assert.That(partySetup.gameObject.activeInHierarchy, Is.True);
            Assert.That(topCurrency.PageTitle, Is.EqualTo("1-1. 열정의 밭 갈기 시작!"));
            Assert.That(topCurrency.IsProfileVisible, Is.False);

            Click(partySetup, "Card_maestromk2");
            Click(partySetup, "Card_chloe");
            yield return null;
            var partyController = Object.FindFirstObjectByType<PartySetupController>();
            Assert.That(partyController.GetSlotPosition("pc_maestromk2"), Is.EqualTo(new Vector2Int(3, 2)));
            Assert.That(partyController.GetSlotPosition("pc_chloe"), Is.EqualTo(new Vector2Int(3, 1)));

            var dragSource = partySetup.transform.Find("Formation/Cell_3_2/SpineViewport/Spine").gameObject;
            var dropTarget = partySetup.transform.Find("Formation/Cell_3_1").GetComponent<RectTransform>();
            var dragPointer = new PointerEventData(EventSystem.current)
            {
                position = RectTransformUtility.WorldToScreenPoint(null, dragSource.transform.position),
            };
            ExecuteEvents.Execute(dragSource, dragPointer, ExecuteEvents.beginDragHandler);
            dragPointer.position = RectTransformUtility.WorldToScreenPoint(null, dropTarget.position);
            ExecuteEvents.Execute(dragSource, dragPointer, ExecuteEvents.dragHandler);
            ExecuteEvents.Execute(dragSource, dragPointer, ExecuteEvents.endDragHandler);
            yield return null;

            Assert.That(partyController.GetSlotPosition("pc_maestromk2"), Is.EqualTo(new Vector2Int(3, 1)));
            Assert.That(partyController.GetSlotPosition("pc_chloe"), Is.EqualTo(new Vector2Int(3, 2)));

            var detailModal = partySetup.transform.Find("CharacterDetailModal");
            var detailPanel = detailModal.Find("Panel");
            var search = partySetup.transform.Find("Roster/Viewport/Content/Card_maison/SearchButton")
                .GetComponent<Button>();
            search.onClick.Invoke();
            Assert.That(detailModal.gameObject.activeSelf, Is.True);
            Assert.That(detailPanel.localScale.x, Is.LessThan(0.8f));
            yield return new WaitForSecondsRealtime(0.18f);
            Assert.That(detailPanel.localScale.x, Is.GreaterThan(1f));
            yield return new WaitForSecondsRealtime(0.16f);
            Assert.That(detailPanel.localScale.x, Is.EqualTo(1f).Within(0.02f));
            Click(partySetup, "CloseButton");
            Assert.That(detailModal.gameObject.activeSelf, Is.False);

            Click(topCurrency, "BackButton");
            yield return null;
            Assert.That(stageSelect.gameObject.activeInHierarchy, Is.True);
            Assert.That(topCurrency.PageTitle, Is.EqualTo("스테이지 리스트"));

            Click(topCurrency, "HomeButton");
            yield return null;
            Assert.That(topCurrency.VisibleCurrencies, Is.EqualTo(TopCurrencyVisibility.All));
            Assert.That(topCurrency.transform.Find("MacaroonCurrency").gameObject.activeSelf, Is.True);
            Assert.That(topCurrency.MenuButton.gameObject.activeSelf, Is.True);
            Assert.That(topCurrency.HomeButton.gameObject.activeSelf, Is.False);
            Assert.That(topCurrency.BackButton.gameObject.activeSelf, Is.False);
            Assert.That(topCurrency.IsProfileVisible, Is.True);

            Click(topCurrency, "MenuButton");
            Assert.That(lobbyView.IsSettingsOpen, Is.True);
            Click(lobbyView, "LogoutButton");
            yield return null;

            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(SceneIds.Login));
            Assert.That(File.Exists(Path.Combine(Application.persistentDataPath, "session.json")), Is.False);
        }

        private static TMP_InputField Input(Component root, string name)
        {
            return root.GetComponentsInChildren<TMP_InputField>(true).Single(component => component.name == name);
        }

        private static void Click(Component root, string name)
        {
            root.GetComponentsInChildren<Button>(true).Single(component => component.name == name).onClick.Invoke();
        }

        private static void DeleteTestFiles()
        {
            Delete("account_" + LoginId + ".json");
            Delete("session.json");
        }

        private static void Delete(string fileName)
        {
            var path = Path.Combine(Application.persistentDataPath, fileName);
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}

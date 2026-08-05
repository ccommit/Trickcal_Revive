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
            Assert.That(lobbyView.ProfileName, Is.EqualTo("Recovery QA"));
            Assert.That(lobbyView.ProfileLevel, Is.EqualTo("Lv.1"));
            Assert.That(lobbyView.Gold, Is.EqualTo("0"));
            Assert.That(lobbyView.Stamina, Is.EqualTo("21/21"));

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

            Click(lobbyView, "SettingsButton");
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

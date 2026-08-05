using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Spine.Unity;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

using TrickcalRevive.App.Editor;

namespace TrickcalRevive.MainUI.Tests
{
    public sealed class LoginLobbyRecoveryTests
    {
        [Test]
        public void LoginManifest_RecordsVerifiedSourceHashes()
        {
            const string manifestPath = "Recovery/Manifests/login-resources.json";
            Assert.That(File.Exists(manifestPath), Is.True, manifestPath);
            var json = File.ReadAllText(manifestPath);
            StringAssert.Contains("34789fc9631a0e467af02fd2b006c671b348340505398d7440465ead600b5840", json);
            StringAssert.Contains("6acb0b9dc9baf82f9e5f01a873fccc828f6661333a82df6aa42f1341bde1fe8f", json);
            StringAssert.Contains("1c0b82fbcb2666e6a9cd5b2a867718b675aee68a95837cbb19362c4c0481a74b", json);
        }

        [Test]
        public void TitleSpine_HasConfirmedSkinAndAnimations()
        {
            var asset = AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>(LoginLobbyUIBuilder.TitleSkeletonDataPath);
            Assert.That(asset, Is.Not.Null, LoginLobbyUIBuilder.TitleSkeletonDataPath);
            var data = asset.GetSkeletonData(true);
            Assert.That(data, Is.Not.Null);
            Assert.That(data.FindSkin(TitleBackgroundPresenter.ConfirmedSkin), Is.Not.Null);
            Assert.That(data.FindAnimation(TitleBackgroundPresenter.ConfirmedStartAnimation), Is.Not.Null);
            Assert.That(data.FindAnimation(TitleBackgroundPresenter.ConfirmedIdleAnimation), Is.Not.Null);
        }

        [Test]
        public void TitleSpine_UsesStraightAlphaMaterialInLinearColorSpace()
        {
            Assert.That(QualitySettings.activeColorSpace, Is.EqualTo(ColorSpace.Linear));
            var atlas = AssetDatabase.LoadAssetAtPath<SpineAtlasAsset>(LoginLobbyUIBuilder.TitleAtlasAssetPath);
            Assert.That(atlas, Is.Not.Null);
            Assert.That(atlas.materials, Is.Not.Empty);
            foreach (var material in atlas.materials)
            {
                Assert.That(material, Is.Not.Null);
                Assert.That(MaterialChecks.IsPMATextureMaterial(material), Is.False);
                var error = string.Empty;
                Assert.That(MaterialChecks.IsColorSpaceSupported(material, ref error), Is.True, error);
            }
        }

        [Test]
        public void LoginAndLobbyPrefabs_AreComplete()
        {
            var login = AssetDatabase.LoadAssetAtPath<GameObject>(LoginLobbyUIBuilder.LoginPrefabPath);
            var lobby = AssetDatabase.LoadAssetAtPath<GameObject>(LoginLobbyUIBuilder.LobbyPrefabPath);
            Assert.That(login, Is.Not.Null);
            Assert.That(lobby, Is.Not.Null);
            Assert.That(login.GetComponent<LoginScreenView>().IsReady, Is.True);
            Assert.That(lobby.GetComponent<LobbyScreenView>().IsReady, Is.True);
            Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(login), Is.Zero);
            Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(lobby), Is.Zero);
        }

        [Test]
        public void LobbyChromeManifest_RecordsNineResourcesAndTheirMetaFiles()
        {
            const string manifestPath = "Recovery/Manifests/lobby-ui-chrome.json";
            Assert.That(File.Exists(manifestPath), Is.True, manifestPath);
            var json = File.ReadAllText(manifestPath);
            Assert.That(Regex.Matches(json, "\\\"outputRelativePath\\\"").Count, Is.EqualTo(18));
            StringAssert.Contains("issue8-lobby-ui-chrome", json);
            StringAssert.Contains("MainLobby_BattleBtn_Uros.png", json);
            StringAssert.Contains("ONE Mobile POP.ttf", json);
            StringAssert.DoesNotContain("E:\\\\", json);
        }

        [Test]
        public void LobbyPrefab_UsesConfirmedChromeAndThreeInteractiveButtons()
        {
            var lobby = AssetDatabase.LoadAssetAtPath<GameObject>(LoginLobbyUIBuilder.LobbyPrefabPath);
            Assert.That(lobby, Is.Not.Null);

            var profile = lobby.transform.Find("ProfilePanel").GetComponent<Image>();
            Assert.That(
                AssetDatabase.GetAssetPath(profile.sprite),
                Is.EqualTo("Assets/Game/Content/UI/Lobby/Chrome/MainLobby_UserInfoBase.png"));

            var currencyPanels = lobby.transform.Cast<Transform>()
                .Where(child => child.name.EndsWith("Currency"))
                .ToArray();
            Assert.That(currencyPanels.Select(child => child.name), Is.EquivalentTo(new[]
            {
                "StaminaCurrency",
                "GoldCurrency",
                "ElleafCurrency"
            }));
            foreach (var currencyPanel in currencyPanels)
            {
                var image = currencyPanel.GetComponent<Image>();
                Assert.That(
                    AssetDatabase.GetAssetPath(image.sprite),
                    Is.EqualTo("Assets/Game/Content/UI/Lobby/Chrome/TopMenu_Base.png"));
                Assert.That(currencyPanel.GetComponent<RectTransform>().sizeDelta, Is.EqualTo(new Vector2(336f, 76f)));
                Assert.That(currencyPanel.Find("Plus").GetComponent<RectTransform>().sizeDelta, Is.EqualTo(new Vector2(76f, 76f)));
            }

            AssertLobbyButton(
                lobby,
                "RecruitButton",
                "모집",
                "Assets/Game/Content/UI/Lobby/Chrome/GachaButton.png");
            AssertLobbyButton(
                lobby,
                "ApostleButton",
                "사도",
                "Assets/Game/Content/UI/Lobby/Chrome/HeroButton.png");
            AssertLobbyButton(
                lobby,
                "AdventureButton",
                "모험",
                "Assets/Game/Content/UI/Lobby/Chrome/MainLobby_BattleBtn_Uros.png");
        }

        [TestCase(FlowSceneBuilder.LoginScenePath, typeof(LoginScreenView))]
        [TestCase(FlowSceneBuilder.MainScenePath, typeof(LobbyScreenView))]
        public void FlowScene_UsesLandscapeReferenceAndExpectedView(string scenePath, System.Type viewType)
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            Assert.That(scene.IsValid(), Is.True);
            var canvas = Object.FindFirstObjectByType<Canvas>();
            Assert.That(canvas, Is.Not.Null);
            var scaler = canvas.GetComponent<CanvasScaler>();
            Assert.That(scaler, Is.Not.Null);
            Assert.That(scaler.referenceResolution, Is.EqualTo(new Vector2(2560f, 1440f)));
            Assert.That(Object.FindFirstObjectByType(viewType), Is.Not.Null);
        }

        [Test]
        public void BuildSettings_ContainsOnlyLoginThenMain()
        {
            var scenes = EditorBuildSettings.scenes;
            Assert.That(scenes, Has.Length.EqualTo(2));
            Assert.That(scenes[0].enabled, Is.True);
            Assert.That(scenes[0].path, Is.EqualTo(FlowSceneBuilder.LoginScenePath));
            Assert.That(scenes[1].enabled, Is.True);
            Assert.That(scenes[1].path, Is.EqualTo(FlowSceneBuilder.MainScenePath));
        }

        private static void AssertLobbyButton(
            GameObject lobby,
            string buttonName,
            string expectedLabel,
            string expectedIconPath)
        {
            var button = lobby.GetComponentsInChildren<Button>(true)
                .Single(candidate => candidate.name == buttonName);
            Assert.That(button.interactable, Is.True);
            Assert.That(button.GetComponent<RectTransform>().sizeDelta, Is.EqualTo(new Vector2(207f, 252f)));
            Assert.That(
                AssetDatabase.GetAssetPath(button.transform.Find("Base").GetComponent<Image>().sprite),
                Is.EqualTo("Assets/Game/Content/UI/Lobby/Chrome/MainLobby_BtnBg.png"));
            Assert.That(
                AssetDatabase.GetAssetPath(button.transform.Find("RecoveredIcon").GetComponent<Image>().sprite),
                Is.EqualTo(expectedIconPath));

            var label = button.transform.Find("Label").GetComponent<TMP_Text>();
            Assert.That(label.text, Is.EqualTo(expectedLabel));
            Assert.That(AssetDatabase.GetAssetPath(label.font), Is.EqualTo(LoginLobbyUIBuilder.LobbyFontAssetPath));

            var feedback = button.GetComponent<LobbyButtonPressFeedback>();
            Assert.That(feedback, Is.Not.Null);
            Assert.That(feedback.BaseScale, Is.EqualTo(Vector2.one));
            Assert.That(feedback.PressedScale, Is.EqualTo(new Vector2(0.9f, 1.2f)));
            Assert.That(feedback.ReleaseScale, Is.EqualTo(new Vector2(1.1f, 0.9f)));
        }
    }
}

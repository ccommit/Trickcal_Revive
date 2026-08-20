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
        public void LobbyPrefab_UsesConfirmedChromeAndSharedTopCurrencyPanel()
        {
            var lobby = AssetDatabase.LoadAssetAtPath<GameObject>(LoginLobbyUIBuilder.LobbyPrefabPath);
            var topCurrency = AssetDatabase.LoadAssetAtPath<GameObject>(TopCurrencyPanelBuilder.PrefabPath);
            Assert.That(lobby, Is.Not.Null);
            Assert.That(topCurrency, Is.Not.Null);

            Assert.That(lobby.transform.Find("ProfilePanel"), Is.Null, "프로필은 로비 화면이 아니라 공통 상단 패널이 소유한다.");
            var profile = topCurrency.transform.Find("ProfilePanel").GetComponent<Image>();
            Assert.That(
                AssetDatabase.GetAssetPath(profile.sprite),
                Is.EqualTo("Assets/Game/Content/UI/Lobby/Chrome/MainLobby_UserInfoBase.png"));

            var currencyPanels = lobby.transform.Cast<Transform>()
                .Where(child => child.name.EndsWith("Currency"))
                .ToArray();
            Assert.That(currencyPanels, Is.Empty, "통화 슬롯은 화면 프리팹에 중복 배치하지 않는다.");

            foreach (var screenPath in new[]
                     {
                         StageSelectUIBuilder.StageSelectPrefabPath,
                         PartySetupUIBuilder.PartyPrefabPath,
                     })
            {
                var screen = AssetDatabase.LoadAssetAtPath<GameObject>(screenPath);
                Assert.That(screen, Is.Not.Null, screenPath);
                Assert.That(
                    screen.transform.Cast<Transform>().Any(child => child.name.EndsWith("Currency")),
                    Is.False,
                    $"화면 프리팹에 중복 통화 슬롯이 남아 있음: {screenPath}");
                Assert.That(screen.transform.Find("BackButton"), Is.Null, $"공통 패널 밖에 BackButton이 남아 있음: {screenPath}");
                Assert.That(screen.transform.Find("Title"), Is.Null, $"공통 패널 밖에 페이지 제목이 남아 있음: {screenPath}");
            }

            currencyPanels = topCurrency.transform.Cast<Transform>()
                .Where(child => child.name.EndsWith("Currency"))
                .ToArray();
            Assert.That(currencyPanels.Select(child => child.name), Is.EquivalentTo(new[]
            {
                "MacaroonCurrency",
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
                var currencyRect = currencyPanel.GetComponent<RectTransform>();
                Assert.That(currencyRect.sizeDelta, Is.EqualTo(new Vector2(300f, 70f)));
                Assert.That(currencyRect.anchoredPosition.y, Is.EqualTo(-15f));

                var icon = currencyPanel.Find("Icon").GetComponent<RectTransform>();
                Assert.That(icon.anchoredPosition, Is.EqualTo(new Vector2(20f, 10f)));
                Assert.That(icon.sizeDelta, Is.EqualTo(new Vector2(90f, 90f)));
                Assert.That(icon.pivot, Is.EqualTo(new Vector2(0.5f, 0.5f)));

                var value = currencyPanel.Find("Value").GetComponent<TMP_Text>();
                Assert.That(value.rectTransform.offsetMin, Is.EqualTo(new Vector2(70f, 0f)));
                Assert.That(value.rectTransform.offsetMax, Is.EqualTo(new Vector2(-75f, 0f)));
                Assert.That(value.rectTransform.pivot, Is.EqualTo(new Vector2(0.5f, 0.5f)));
                Assert.That(value.fontSize, Is.EqualTo(30f));

                var plus = currencyPanel.Find("Plus").GetComponent<RectTransform>();
                Assert.That(plus.anchoredPosition, Is.EqualTo(Vector2.zero));
                Assert.That(plus.sizeDelta, Is.EqualTo(new Vector2(70f, 70f)));
                Assert.That(plus.pivot, Is.EqualTo(new Vector2(1f, 0.5f)));
            }

            var topRect = topCurrency.GetComponent<RectTransform>();
            Assert.That(lobby.transform.Find("SettingsButton"), Is.Null, "로비 전용 SettingsButton은 공통 패널의 MenuButton으로 대체한다.");

            var menuButton = topCurrency.transform.Find("MenuButton").GetComponent<Button>();
            var homeButton = topCurrency.transform.Find("HomeButton").GetComponent<Button>();
            var backButton = topCurrency.transform.Find("BackButton").GetComponent<Button>();
            var pageTitle = topCurrency.transform.Find("PageTitle").GetComponent<TMP_Text>();
            Assert.That(menuButton, Is.Not.Null);
            Assert.That(homeButton, Is.Not.Null);
            Assert.That(backButton, Is.Not.Null);
            Assert.That(pageTitle, Is.Not.Null);
            Assert.That(menuButton.name, Is.EqualTo("MenuButton"));
            Assert.That(menuButton.GetComponent<RectTransform>().sizeDelta, Is.EqualTo(new Vector2(96f, 96f)));
            Assert.That(homeButton.GetComponent<RectTransform>().sizeDelta, Is.EqualTo(menuButton.GetComponent<RectTransform>().sizeDelta));
            Assert.That(topRect.sizeDelta.y, Is.EqualTo(menuButton.GetComponent<RectTransform>().sizeDelta.y));
            Assert.That(menuButton.GetComponent<RectTransform>().anchoredPosition, Is.EqualTo(homeButton.GetComponent<RectTransform>().anchoredPosition));
            Assert.That(
                AssetDatabase.GetAssetPath(menuButton.image.sprite),
                Is.EqualTo("Assets/Game/Content/UI/Lobby/Sprites/TopMenu_ButtonBase.png"));
            Assert.That(
                AssetDatabase.GetAssetPath(menuButton.transform.Find("Icon").GetComponent<Image>().sprite),
                Is.EqualTo("Assets/Game/Content/UI/Lobby/Sprites/TopMenu_IconMenu.png"));
            Assert.That(
                AssetDatabase.GetAssetPath(homeButton.transform.Find("Icon").GetComponent<Image>().sprite),
                Is.EqualTo("Assets/Game/Content/UI/Lobby/Sprites/TopMenu_IconHome.png"));
            Assert.That(menuButton.gameObject.activeSelf, Is.True);
            Assert.That(homeButton.gameObject.activeSelf, Is.False);
            Assert.That(profile.gameObject.activeSelf, Is.True);
            Assert.That(backButton.gameObject.activeSelf, Is.False);
            Assert.That(pageTitle.gameObject.activeSelf, Is.False);
            Assert.That(
                AssetDatabase.GetAssetPath(backButton.image.sprite),
                Is.EqualTo("Assets/Game/Content/UI/PartySetup/Common_MajorBtn_010.png"));

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

        [Test]
        public void TopCurrencyPanel_HidesUnusedSlotsAndPacksRemainingSlotsFromRight()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TopCurrencyPanelBuilder.PrefabPath);
            var instance = Object.Instantiate(prefab);
            try
            {
                var view = instance.GetComponent<TopCurrencyPanelView>();
                Assert.That(view.IsReady, Is.True);
                view.Show(TopCurrencyVisibility.Gold | TopCurrencyVisibility.Elleaf, false, "스테이지 리스트");

                Assert.That(instance.transform.Find("MacaroonCurrency").gameObject.activeSelf, Is.False);
                Assert.That(instance.transform.Find("StaminaCurrency").gameObject.activeSelf, Is.False);
                Assert.That(instance.transform.Find("GoldCurrency").gameObject.activeSelf, Is.True);
                Assert.That(instance.transform.Find("ElleafCurrency").gameObject.activeSelf, Is.True);
                Assert.That(instance.transform.Find("ElleafCurrency").GetComponent<RectTransform>().anchoredPosition.x, Is.EqualTo(-110f));
                Assert.That(instance.transform.Find("GoldCurrency").GetComponent<RectTransform>().anchoredPosition.x, Is.EqualTo(-440f));
                Assert.That(view.MenuButton.gameObject.activeSelf, Is.False);
                Assert.That(view.HomeButton.gameObject.activeSelf, Is.True);
                Assert.That(view.IsProfileVisible, Is.False);
                Assert.That(view.BackButton.gameObject.activeSelf, Is.True);
                Assert.That(view.PageTitle, Is.EqualTo("스테이지 리스트"));

                view.Show(TopCurrencyVisibility.All, true, string.Empty);
                Assert.That(view.MenuButton.gameObject.activeSelf, Is.True);
                Assert.That(view.HomeButton.gameObject.activeSelf, Is.False);
                Assert.That(view.IsProfileVisible, Is.True);
                Assert.That(view.BackButton.gameObject.activeSelf, Is.False);
                Assert.That(instance.transform.Find("PageTitle").gameObject.activeSelf, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
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
            if (scenePath == FlowSceneBuilder.MainScenePath)
            {
                var currencyViews = Object.FindObjectsByType<TopCurrencyPanelView>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                Assert.That(currencyViews, Has.Length.EqualTo(1), "Main씬은 공통 상단 통화 패널을 정확히 하나만 사용한다.");
            }
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

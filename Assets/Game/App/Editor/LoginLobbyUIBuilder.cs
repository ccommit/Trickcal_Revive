using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

using Spine.Unity;
using TrickcalRevive.MainUI;

using Object = UnityEngine.Object;

namespace TrickcalRevive.App.Editor
{
    public static class LoginLobbyUIBuilder
    {
        public const string LoginPrefabPath = "Assets/Game/MainUI/Prefabs/LoginScreen.prefab";
        public const string LobbyPrefabPath = "Assets/Game/MainUI/Prefabs/LobbyScreen.prefab";
        public const string TitleSkeletonDataPath =
            "Assets/Game/Content/UI/Login/Spine/TitleBackground/Title_Background_SkeletonData.asset";
        public const string TitleAtlasAssetPath =
            "Assets/Game/Content/UI/Login/Spine/TitleBackground/Title_Background_Atlas.asset";

        private const string FontPath =
            "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
        private const string LobbyRoot = "Assets/Game/Content/UI/Lobby";

        private static readonly Color CardColor = new Color32(37, 31, 42, 244);
        private static readonly Color FieldColor = new Color32(66, 57, 72, 255);
        private static readonly Color AccentColor = new Color32(243, 171, 78, 255);
        private static readonly Color TextColor = new Color32(245, 240, 229, 255);

        [MenuItem("Tools/Trickcal Revive/Flow/Build Login-Lobby UI Prefabs")]
        public static void BuildPrefabs()
        {
            EnsureFolder("Assets/Game/MainUI/Prefabs");
            ConfigureTitleMaterialForLinearColorSpace();
            var font = Require<TMP_FontAsset>(FontPath);
            BuildLoginPrefab(font);
            BuildLobbyPrefab(font);
            AssetDatabase.SaveAssets();
            Debug.Log("Login/Lobby UI prefabs built.");
        }

        private static void ConfigureTitleMaterialForLinearColorSpace()
        {
            var atlas = Require<SpineAtlasAsset>(TitleAtlasAssetPath);
            if (atlas.materials == null || atlas.materials.Length == 0)
                throw new InvalidOperationException("Title Spine atlas has no material.");

            foreach (var material in atlas.materials)
            {
                if (material == null)
                    throw new InvalidOperationException("Title Spine atlas has a missing material reference.");
                MaterialChecks.EnablePMATextureAtMaterial(material, false);
                EditorUtility.SetDirty(material);
            }
        }

        private static void BuildLoginPrefab(TMP_FontAsset font)
        {
            var root = RectObject("LoginScreen", null);
            Stretch((RectTransform)root.transform);

            var shade = Image(root.transform, "ReadabilityShade", new Color(0f, 0f, 0f, 0.27f));
            Stretch(shade.rectTransform);
            shade.raycastTarget = false;

            var card = Image(root.transform, "LoginCard", CardColor);
            Anchored(card.rectTransform, new Vector2(1f, 0.5f), new Vector2(-410f, 0f), new Vector2(650f, 940f));
            var outline = card.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color32(255, 215, 151, 110);
            outline.effectDistance = new Vector2(3f, -3f);

            var title = Text(card.transform, "Title", "TRICKCAL REVIVE", font, 48f, TextAlignmentOptions.Center, TextColor);
            Anchored(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -80f), new Vector2(570f, 80f));
            var subtitle = Text(card.transform, "Subtitle", "LOCAL RECOVERY BUILD", font, 19f, TextAlignmentOptions.Center, AccentColor);
            Anchored(subtitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -142f), new Vector2(570f, 40f));

            var loginId = Input(card.transform, "LoginIdInput", "Account ID", font, false);
            Anchored(loginId.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0f, -245f), new Vector2(520f, 78f));
            var password = Input(card.transform, "PasswordInput", "Password", font, true);
            Anchored(password.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0f, -345f), new Vector2(520f, 78f));

            var status = Text(
                card.transform,
                "Status",
                "Enter your account ID and password.",
                font,
                21f,
                TextAlignmentOptions.Center,
                TextColor);
            Anchored(status.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -438f), new Vector2(540f, 58f));

            var loginButton = Button(card.transform, "LoginButton", "LOGIN", font, AccentColor);
            Anchored(loginButton.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0f, -535f), new Vector2(520f, 84f));

            var help = Text(
                card.transform,
                "Help",
                "Unknown account IDs can be created after the first login attempt.",
                font,
                17f,
                TextAlignmentOptions.Center,
                new Color32(196, 187, 199, 255));
            Anchored(help.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -600f), new Vector2(540f, 48f));

            var signUpPanel = Image(card.transform, "SignUpPanel", new Color32(48, 41, 53, 255));
            Anchored(signUpPanel.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 118f), new Vector2(550f, 242f), new Vector2(0.5f, 0f));
            var nickname = Input(signUpPanel.transform, "NicknameInput", "Nickname", font, false);
            Anchored(nickname.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0f, -59f), new Vector2(500f, 70f));
            var create = Button(signUpPanel.transform, "CreateAccountButton", "CREATE", font, AccentColor);
            Anchored(create.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(-128f, 50f), new Vector2(235f, 68f), new Vector2(0.5f, 0f));
            var cancel = Button(signUpPanel.transform, "CancelSignUpButton", "CANCEL", font, new Color32(104, 91, 111, 255));
            Anchored(cancel.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(128f, 50f), new Vector2(235f, 68f), new Vector2(0.5f, 0f));

            var view = root.AddComponent<LoginScreenView>();
            view.Configure(loginId, password, loginButton, signUpPanel.gameObject, nickname, create, cancel, status);
            view.SetSignUpVisible(false);

            SavePrefab(root, LoginPrefabPath, view.IsReady);
        }

        private static void BuildLobbyPrefab(TMP_FontAsset font)
        {
            var backgroundSprite = Require<Sprite>(LobbyRoot + "/Background/Lobby_Default.png");
            var currencyBase = Require<Sprite>(LobbyRoot + "/Sprites/TopMenu_CurrencyBase.png");
            var plus = Require<Sprite>(LobbyRoot + "/Sprites/TopMenu_Plus.png");
            var profileBase = Require<Sprite>(LobbyRoot + "/Sprites/MainLobby_UserInfoBase.png");
            var levelBase = Require<Sprite>(LobbyRoot + "/Sprites/MainLobby_LevelBase.png");
            var buttonBase = Require<Sprite>(LobbyRoot + "/Sprites/TopMenu_ButtonBase.png");
            var menuIcon = Require<Sprite>(LobbyRoot + "/Sprites/TopMenu_IconMenu.png");
            var navigationBase = Require<Sprite>(LobbyRoot + "/Sprites/MainLobby_BtnBg.png");

            var root = RectObject("LobbyScreen", null);
            Stretch((RectTransform)root.transform);

            var background = Image(root.transform, "LobbyBackground", Color.white, backgroundSprite);
            Stretch(background.rectTransform);
            background.raycastTarget = false;

            var topShade = Image(root.transform, "TopShade", new Color32(18, 15, 22, 130));
            SetRect(topShade.rectTransform, new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -170f), Vector2.zero);
            topShade.raycastTarget = false;

            var profile = Image(root.transform, "ProfilePanel", Color.white, profileBase);
            Anchored(profile.rectTransform, new Vector2(0f, 1f), new Vector2(195f, -82f), new Vector2(360f, 128f), new Vector2(0f, 1f));
            profile.preserveAspect = true;
            var profileName = Text(profile.transform, "ProfileName", "New Leader", font, 21f, TextAlignmentOptions.MidlineLeft, Color.white);
            SetRect(profileName.rectTransform, new Vector2(0.16f, 0.43f), new Vector2(0.78f, 1f), Vector2.zero, new Vector2(0f, -8f));
            var levelImage = Image(profile.transform, "LevelBase", Color.white, levelBase);
            Anchored(levelImage.rectTransform, new Vector2(1f, 0.5f), new Vector2(-46f, 5f), new Vector2(76f, 66f));
            levelImage.preserveAspect = true;
            var profileLevel = Text(levelImage.transform, "Level", "Lv.1", font, 18f, TextAlignmentOptions.Center, Color.white);
            Stretch(profileLevel.rectTransform, 4f);
            var expTrack = Image(profile.transform, "ExperienceTrack", new Color32(31, 27, 33, 255));
            SetRect(expTrack.rectTransform, new Vector2(0.21f, 0.12f), new Vector2(0.70f, 0.25f), Vector2.zero, Vector2.zero);
            var expFill = Image(expTrack.transform, "Fill", AccentColor);
            Stretch(expFill.rectTransform);
            expFill.type = UnityEngine.UI.Image.Type.Filled;
            expFill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            expFill.fillAmount = 0f;

            var gold = Currency(root.transform, "Gold", Require<Sprite>(LobbyRoot + "/Sprites/Currency_Gold.png"), currencyBase, plus, font, -1010f);
            var macaron = Currency(root.transform, "Macaron", Require<Sprite>(LobbyRoot + "/Sprites/Currency_Macaron.png"), currencyBase, plus, font, -770f);
            var stamina = Currency(root.transform, "Stamina", Require<Sprite>(LobbyRoot + "/Sprites/Currency_Stamina.png"), currencyBase, plus, font, -530f);
            var elleaf = Currency(root.transform, "Elleaf", Require<Sprite>(LobbyRoot + "/Sprites/Currency_Elif.png"), currencyBase, plus, font, -290f);

            var settingsButton = Button(root.transform, "SettingsButton", string.Empty, font, Color.white, buttonBase);
            Anchored(settingsButton.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(-67f, -82f), new Vector2(105f, 105f), new Vector2(1f, 1f));
            var settingsIcon = Image(settingsButton.transform, "Icon", Color.white, menuIcon);
            Anchored(settingsIcon.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(58f, 58f));
            settingsIcon.preserveAspect = true;
            settingsIcon.raycastTarget = false;

            var bottomShade = Image(root.transform, "BottomShade", new Color32(18, 15, 22, 145));
            SetRect(bottomShade.rectTransform, Vector2.zero, new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, 220f));
            bottomShade.raycastTarget = false;

            var recruit = NavigationButton(
                root.transform, "RecruitButton", "RECRUIT", navigationBase,
                Require<Sprite>(LobbyRoot + "/Sprites/Lobby_GachaButton.png"), font, -340f);
            var apostle = NavigationButton(
                root.transform, "ApostleButton", "APOSTLE", navigationBase,
                Require<Sprite>(LobbyRoot + "/Sprites/Lobby_HeroButton.png"), font, 0f);
            var adventure = NavigationButton(
                root.transform, "AdventureButton", "ADVENTURE", navigationBase,
                Require<Sprite>(LobbyRoot + "/Sprites/Lobby_StoryButton.png"), font, 340f);

            var status = Text(
                root.transform,
                "ScopeStatus",
                "Lobby resource recovery preview - navigation is reserved for later branches.",
                font,
                18f,
                TextAlignmentOptions.MidlineLeft,
                new Color32(228, 220, 232, 255));
            Anchored(status.rectTransform, new Vector2(0f, 0f), new Vector2(28f, 24f), new Vector2(850f, 44f), Vector2.zero);

            var settingsMenu = Image(root.transform, "SettingsMenu", new Color32(37, 30, 41, 248));
            Anchored(settingsMenu.rectTransform, new Vector2(1f, 1f), new Vector2(-78f, -198f), new Vector2(380f, 220f), Vector2.one);
            var settingsTitle = Text(settingsMenu.transform, "Title", "SETTINGS", font, 26f, TextAlignmentOptions.Center, Color.white);
            SetRect(settingsTitle.rectTransform, new Vector2(0f, 0.62f), Vector2.one, Vector2.zero, new Vector2(0f, -6f));
            var logout = Button(settingsMenu.transform, "LogoutButton", "LOG OUT", font, new Color32(156, 70, 76, 255));
            Anchored(logout.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0f, 55f), new Vector2(300f, 72f), new Vector2(0.5f, 0f));

            var view = root.AddComponent<LobbyScreenView>();
            view.Configure(
                background,
                profileName,
                profileLevel,
                expFill,
                gold,
                elleaf,
                macaron,
                stamina,
                settingsButton,
                settingsMenu.gameObject,
                logout,
                recruit,
                apostle,
                adventure,
                status);
            view.SetSettingsOpen(false);

            SavePrefab(root, LobbyPrefabPath, view.IsReady);
        }

        private static TMP_Text Currency(
            Transform parent,
            string name,
            Sprite icon,
            Sprite baseSprite,
            Sprite plusSprite,
            TMP_FontAsset font,
            float x)
        {
            var panel = Image(parent, name + "Currency", new Color32(42, 35, 45, 235));
            Anchored(panel.rectTransform, new Vector2(1f, 1f), new Vector2(x, -82f), new Vector2(225f, 82f), new Vector2(1f, 1f));

            var iconBase = Image(panel.transform, "IconBase", Color.white, baseSprite);
            Anchored(iconBase.rectTransform, new Vector2(0f, 0.5f), new Vector2(38f, 0f), new Vector2(76f, 76f));
            iconBase.preserveAspect = true;
            var iconImage = Image(iconBase.transform, "Icon", Color.white, icon);
            Anchored(iconImage.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(60f, 60f));
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;

            var value = Text(panel.transform, "Value", "0", font, 23f, TextAlignmentOptions.MidlineRight, Color.white);
            SetRect(value.rectTransform, new Vector2(0.31f, 0f), new Vector2(0.84f, 1f), Vector2.zero, Vector2.zero);
            var plus = Image(panel.transform, "Plus", Color.white, plusSprite);
            Anchored(plus.rectTransform, new Vector2(1f, 0.5f), new Vector2(-18f, 0f), new Vector2(31f, 31f));
            plus.preserveAspect = true;
            plus.raycastTarget = false;
            return value;
        }

        private static Button NavigationButton(
            Transform parent,
            string name,
            string label,
            Sprite baseSprite,
            Sprite icon,
            TMP_FontAsset font,
            float x)
        {
            var button = Button(parent, name, string.Empty, font, Color.white, baseSprite);
            Anchored(button.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(x, 112f), new Vector2(300f, 185f), new Vector2(0.5f, 0f));
            var iconImage = Image(button.transform, "RecoveredIcon", Color.white, icon);
            Anchored(iconImage.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 12f), new Vector2(185f, 116f));
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
            var text = Text(button.transform, "Label", label, font, 22f, TextAlignmentOptions.Center, Color.white);
            SetRect(text.rectTransform, Vector2.zero, new Vector2(1f, 0.28f), new Vector2(8f, 0f), new Vector2(-8f, 0f));
            return button;
        }

        private static TMP_InputField Input(Transform parent, string name, string placeholderText, TMP_FontAsset font, bool password)
        {
            var root = RectObject(name, parent, typeof(Image), typeof(TMP_InputField));
            root.GetComponent<Image>().color = FieldColor;
            var viewport = RectObject("Text Area", root.transform, typeof(RectMask2D));
            SetRect((RectTransform)viewport.transform, Vector2.zero, Vector2.one, new Vector2(22f, 9f), new Vector2(-22f, -9f));
            var placeholder = Text(viewport.transform, "Placeholder", placeholderText, font, 25f, TextAlignmentOptions.MidlineLeft, new Color32(174, 165, 180, 255));
            Stretch(placeholder.rectTransform);
            var text = Text(viewport.transform, "Text", string.Empty, font, 25f, TextAlignmentOptions.MidlineLeft, TextColor);
            Stretch(text.rectTransform);

            var field = root.GetComponent<TMP_InputField>();
            field.textViewport = (RectTransform)viewport.transform;
            field.textComponent = (TextMeshProUGUI)text;
            field.placeholder = placeholder;
            field.lineType = TMP_InputField.LineType.SingleLine;
            if (password)
            {
                field.contentType = TMP_InputField.ContentType.Password;
                field.asteriskChar = '\u2022';
            }
            return field;
        }

        private static Button Button(
            Transform parent,
            string name,
            string label,
            TMP_FontAsset font,
            Color color,
            Sprite sprite = null)
        {
            var root = RectObject(name, parent, typeof(Image), typeof(Button));
            var image = root.GetComponent<Image>();
            image.color = color;
            image.sprite = sprite;
            image.preserveAspect = sprite != null;
            var button = root.GetComponent<Button>();
            button.targetGraphic = image;
            if (!string.IsNullOrEmpty(label))
            {
                var text = Text(root.transform, "Label", label, font, 25f, TextAlignmentOptions.Center, Color.white);
                Stretch(text.rectTransform, 5f);
                text.raycastTarget = false;
            }
            return button;
        }

        private static Image Image(Transform parent, string name, Color color, Sprite sprite = null)
        {
            var root = RectObject(name, parent, typeof(Image));
            var image = root.GetComponent<Image>();
            image.color = color;
            image.sprite = sprite;
            return image;
        }

        private static TMP_Text Text(
            Transform parent,
            string name,
            string value,
            TMP_FontAsset font,
            float size,
            TextAlignmentOptions alignment,
            Color color)
        {
            var root = RectObject(name, parent, typeof(TextMeshProUGUI));
            var text = root.GetComponent<TextMeshProUGUI>();
            text.text = value;
            text.font = font;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = color;
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
            return text;
        }

        private static GameObject RectObject(string name, Transform parent, params Type[] components)
        {
            var all = new Type[components.Length + 1];
            all[0] = typeof(RectTransform);
            Array.Copy(components, 0, all, 1, components.Length);
            var root = new GameObject(name, all);
            if (parent != null)
                root.transform.SetParent(parent, false);
            return root;
        }

        private static void SavePrefab(GameObject root, string path, bool ready)
        {
            if (!ready)
            {
                Object.DestroyImmediate(root);
                throw new InvalidOperationException($"Generated UI is incomplete: {path}");
            }

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            if (prefab == null)
                throw new InvalidOperationException($"Could not save prefab: {path}");
        }

        private static T Require<T>(string path) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
                throw new InvalidOperationException($"Required asset is missing: {path}");
            return asset;
        }

        private static void EnsureFolder(string path)
        {
            var segments = path.Split('/');
            var current = segments[0];
            for (var i = 1; i < segments.Length; i++)
            {
                var next = current + "/" + segments[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, segments[i]);
                current = next;
            }
        }

        private static void Stretch(RectTransform rect, float inset = 0f)
        {
            SetRect(rect, Vector2.zero, Vector2.one, new Vector2(inset, inset), new Vector2(-inset, -inset));
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static void Anchored(
            RectTransform rect,
            Vector2 anchor,
            Vector2 position,
            Vector2 size,
            Vector2? pivot = null)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot ?? new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }
    }
}

using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

using TrickcalRevive.MainUI;

using Object = UnityEngine.Object;

namespace TrickcalRevive.App.Editor
{
    /// <summary>좌측 헤더와 우측 TopCurrencySlot을 한 번만 생성해 Main씬의 모든 화면이 공유한다.</summary>
    public static class TopCurrencyPanelBuilder
    {
        public const string PrefabPath = "Assets/Game/MainUI/Prefabs/TopCurrencyPanel.prefab";

        private const string LobbySprites = "Assets/Game/Content/UI/Lobby/Sprites";
        private const string LobbyChrome = "Assets/Game/Content/UI/Lobby/Chrome";
        private const string PartySetupUi = "Assets/Game/Content/UI/PartySetup";
        private const string FontPath = "Assets/Game/Content/UI/Fonts/ONE Mobile POP SDF.asset";
        private const float PanelHeight = 96f;

        [MenuItem("Tools/Trickcal Revive/Flow/Build Shared Top Panel")]
        public static void BuildPrefab()
        {
            EnsureFolder("Assets/Game/MainUI/Prefabs");
            var font = Require<TMP_FontAsset>(FontPath);
            var valueBase = Require<Sprite>(LobbyChrome + "/TopMenu_Base.png");
            var plusBase = Require<Sprite>(LobbyChrome + "/TopMenu_CurrencyBase.png");
            var buttonBase = Require<Sprite>(LobbySprites + "/TopMenu_ButtonBase.png");
            var menuIcon = Require<Sprite>(LobbySprites + "/TopMenu_IconMenu.png");
            var homeIcon = Require<Sprite>(LobbySprites + "/TopMenu_IconHome.png");
            var profileBase = Require<Sprite>(LobbyChrome + "/MainLobby_UserInfoBase.png");
            var levelBase = Require<Sprite>(LobbyChrome + "/MainLobby_LevelBase.png");
            var avatarIcon = Require<Sprite>(LobbyChrome + "/HeroButton.png");
            var backButtonBase = Require<Sprite>(PartySetupUi + "/Common_MajorBtn_010.png");

            var root = RectObject("TopCurrencyPanel", null);
            var rootRect = (RectTransform)root.transform;
            rootRect.anchorMin = new Vector2(0f, 1f);
            rootRect.anchorMax = Vector2.one;
            rootRect.pivot = new Vector2(0.5f, 1f);
            rootRect.anchoredPosition = new Vector2(0f, -24f);
            rootRect.sizeDelta = new Vector2(-32f, PanelHeight);

            var menu = NavigationButton(root.transform, "MenuButton", buttonBase, menuIcon);
            var home = NavigationButton(root.transform, "HomeButton", buttonBase, homeIcon);
            var profile = Profile(root.transform, font, profileBase, levelBase, buttonBase, avatarIcon);
            var back = BackButton(root.transform, font, backButtonBase);
            var pageTitle = Text(root.transform, "PageTitle", "스테이지 리스트", font, 48f, TextAlignmentOptions.MidlineLeft, Color.white);
            Anchored(pageTitle.rectTransform, new Vector2(0f, 1f), new Vector2(158f, -42f), new Vector2(760f, 78f), new Vector2(0f, 1f));
            pageTitle.fontStyle = FontStyles.Bold;
            RecoveredTextStyles.Apply(pageTitle, RecoveredTextStyle.DarkWithLightBorder);

            var macaroon = Currency(
                root.transform,
                "MacaroonCurrency",
                Require<Sprite>(LobbySprites + "/Currency_Macaron.png"),
                valueBase,
                plusBase,
                font);
            var stamina = Currency(
                root.transform,
                "StaminaCurrency",
                Require<Sprite>(LobbySprites + "/Currency_Stamina.png"),
                valueBase,
                plusBase,
                font);
            var gold = Currency(
                root.transform,
                "GoldCurrency",
                Require<Sprite>(LobbySprites + "/Currency_Gold.png"),
                valueBase,
                plusBase,
                font);
            var elleaf = Currency(
                root.transform,
                "ElleafCurrency",
                Require<Sprite>(LobbySprites + "/Currency_Elif.png"),
                valueBase,
                plusBase,
                font);

            var view = root.AddComponent<TopCurrencyPanelView>();
            view.Configure(
                stamina.Root,
                stamina.Value,
                gold.Root,
                gold.Value,
                elleaf.Root,
                elleaf.Value,
                macaroon.Root,
                macaroon.Value,
                menu,
                home,
                profile.Root,
                profile.Name,
                profile.Level,
                profile.ExperienceFill,
                back,
                pageTitle);
            view.Show(TopCurrencyVisibility.All, true, string.Empty);

            if (!view.IsReady)
                throw new InvalidOperationException("TopCurrencyPanelView is not ready.");
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            Debug.Log("Shared top panel prefab built.");
        }

        private static CurrencySlot Currency(
            Transform parent,
            string name,
            Sprite icon,
            Sprite valueBase,
            Sprite plusBase,
            TMP_FontAsset font)
        {
            var panel = Image(parent, name, Color.white, valueBase);
            var panelRect = panel.rectTransform;
            panelRect.anchorMin = Vector2.one;
            panelRect.anchorMax = Vector2.one;
            panelRect.pivot = Vector2.one;
            panelRect.sizeDelta = new Vector2(300f, 70f);
            panel.type = UnityEngine.UI.Image.Type.Sliced;
            panel.raycastTarget = false;

            var iconImage = Image(panel.transform, "Icon", Color.white, icon);
            Anchored(iconImage.rectTransform, new Vector2(0f, 0.5f), new Vector2(20f, 10f), new Vector2(90f, 90f), new Vector2(0.5f, 0.5f));
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;

            var value = Text(panel.transform, "Value", "0", font, 30f, TextAlignmentOptions.MidlineRight, new Color32(50, 48, 42, 255));
            SetRect(value.rectTransform, Vector2.zero, Vector2.one, new Vector2(70f, 0f), new Vector2(-75f, 0f));
            value.rectTransform.pivot = new Vector2(0.5f, 0.5f);

            var plus = Image(panel.transform, "Plus", Color.white, plusBase);
            Anchored(plus.rectTransform, new Vector2(1f, 0.5f), Vector2.zero, new Vector2(70f, 70f), new Vector2(1f, 0.5f));
            plus.preserveAspect = true;
            plus.raycastTarget = false;
            return new CurrencySlot(panel.gameObject, value);
        }

        private static Button NavigationButton(Transform parent, string name, Sprite buttonBase, Sprite icon)
        {
            var background = Image(parent, name, Color.white, buttonBase);
            Anchored(background.rectTransform, Vector2.one, Vector2.zero, new Vector2(PanelHeight, PanelHeight), Vector2.one);
            background.preserveAspect = true;

            var button = background.gameObject.AddComponent<Button>();
            button.targetGraphic = background;

            var iconImage = Image(background.transform, "Icon", Color.white, icon);
            Anchored(iconImage.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(54f, 54f));
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
            return button;
        }

        private static ProfileChrome Profile(
            Transform parent,
            TMP_FontAsset font,
            Sprite profileBase,
            Sprite levelBase,
            Sprite avatarFrameBase,
            Sprite avatarIcon)
        {
            var profile = Image(parent, "ProfilePanel", Color.white, profileBase);
            Anchored(profile.rectTransform, new Vector2(0f, 1f), new Vector2(76f, -8f), new Vector2(578f, 112f), new Vector2(0f, 1f));
            profile.type = UnityEngine.UI.Image.Type.Sliced;

            var avatarFrame = Image(profile.transform, "ProfileAvatarFrame", Color.white, avatarFrameBase);
            Anchored(avatarFrame.rectTransform, new Vector2(0f, 0.5f), new Vector2(35f, 0f), new Vector2(132f, 132f));
            avatarFrame.preserveAspect = true;
            avatarFrame.raycastTarget = false;
            var avatar = Image(avatarFrame.transform, "ProfileAvatarReconstructed", Color.white, avatarIcon);
            Anchored(avatar.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 3f), new Vector2(103f, 103f));
            avatar.preserveAspect = true;
            avatar.raycastTarget = false;

            var profileName = Text(profile.transform, "ProfileName", "새로운 교주", font, 30f, TextAlignmentOptions.MidlineLeft, Color.white);
            Anchored(profileName.rectTransform, new Vector2(0f, 1f), new Vector2(150f, -31f), new Vector2(350f, 44f), new Vector2(0f, 1f));

            var levelImage = Image(profile.transform, "LevelBase", Color.white, levelBase);
            Anchored(levelImage.rectTransform, new Vector2(0f, 0f), new Vector2(150f, 25f), new Vector2(102f, 38f), new Vector2(0f, 0.5f));
            levelImage.type = UnityEngine.UI.Image.Type.Sliced;
            var profileLevel = Text(levelImage.transform, "Level", "Lv.1", font, 22f, TextAlignmentOptions.Center, new Color32(39, 88, 39, 255));
            SetRect(profileLevel.rectTransform, Vector2.zero, Vector2.one, new Vector2(2f, 2f), new Vector2(-2f, -2f));

            var expTrack = Image(profile.transform, "ExperienceTrack", new Color32(36, 70, 44, 255), null);
            Anchored(expTrack.rectTransform, new Vector2(0f, 0f), new Vector2(268f, 25f), new Vector2(200f, 17f), new Vector2(0f, 0.5f));
            var expFill = Image(expTrack.transform, "Fill", new Color32(55, 235, 204, 255), null);
            SetRect(expFill.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            expFill.type = UnityEngine.UI.Image.Type.Filled;
            expFill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            expFill.fillAmount = 0f;

            return new ProfileChrome(profile.gameObject, profileName, profileLevel, expFill);
        }

        private static Button BackButton(Transform parent, TMP_FontAsset font, Sprite baseSprite)
        {
            var background = Image(parent, "BackButton", Color.white, baseSprite);
            Anchored(background.rectTransform, new Vector2(0f, 1f), new Vector2(12f, -40f), new Vector2(118f, 118f), new Vector2(0f, 1f));
            background.preserveAspect = true;
            var button = background.gameObject.AddComponent<Button>();
            button.targetGraphic = background;

            var arrow = Text(background.transform, "Arrow", "←", font, 54f, TextAlignmentOptions.Center, new Color32(37, 84, 52, 255));
            SetRect(arrow.rectTransform, Vector2.zero, Vector2.one, new Vector2(8f, 8f), new Vector2(-8f, -8f));
            arrow.raycastTarget = false;
            return button;
        }

        private static GameObject RectObject(string name, Transform parent, params Type[] components)
        {
            var types = new Type[components.Length + 1];
            types[0] = typeof(RectTransform);
            Array.Copy(components, 0, types, 1, components.Length);
            var root = new GameObject(name, types);
            if (parent != null)
                root.transform.SetParent(parent, false);
            return root;
        }

        private static Image Image(Transform parent, string name, Color color, Sprite sprite)
        {
            var root = RectObject(name, parent, typeof(Image));
            var image = root.GetComponent<Image>();
            image.color = color;
            image.sprite = sprite;
            return image;
        }

        private static TMP_Text Text(Transform parent, string name, string value, TMP_FontAsset font, float size, TextAlignmentOptions alignment, Color color)
        {
            var root = RectObject(name, parent, typeof(TextMeshProUGUI));
            var text = root.GetComponent<TextMeshProUGUI>();
            text.text = value;
            text.font = font;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = color;
            text.enableWordWrapping = false;
            return text;
        }

        private static void Anchored(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size, Vector2? pivot = null)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot ?? new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void SetRect(RectTransform rect, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static T Require<T>(string path) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
                throw new InvalidOperationException($"Required asset missing: {path}");
            return asset;
        }

        private static void EnsureFolder(string path)
        {
            var parts = path.Split('/');
            var current = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        private readonly struct CurrencySlot
        {
            public readonly GameObject Root;
            public readonly TMP_Text Value;

            public CurrencySlot(GameObject root, TMP_Text value)
            {
                Root = root;
                Value = value;
            }
        }

        private readonly struct ProfileChrome
        {
            public readonly GameObject Root;
            public readonly TMP_Text Name;
            public readonly TMP_Text Level;
            public readonly Image ExperienceFill;

            public ProfileChrome(GameObject root, TMP_Text name, TMP_Text level, Image experienceFill)
            {
                Root = root;
                Name = name;
                Level = level;
                ExperienceFill = experienceFill;
            }
        }
    }
}

using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

using TrickcalRevive.MainUI;

using Object = UnityEngine.Object;

namespace TrickcalRevive.App.Editor
{
    /// <summary>스테이지 정보 팝업(StageInfoPopupScreen.prefab)을 코드로 생성. 기본 상태는 우측 패널만 표시한다.</summary>
    // ponytail: UI 헬퍼가 3번째 복사본이다(로비/스테이지선택 빌더와 동일). 4번째 빌더가 생기면
    // App.Editor 공용 UiKit으로 추출.
    public static class StageInfoPopupUIBuilder
    {
        public const string PopupPrefabPath = "Assets/Game/MainUI/Prefabs/StageInfoPopupScreen.prefab";
        private const string StageRoot = "Assets/Game/Content/UI/StageSelect";
        private const string LobbySprites = "Assets/Game/Content/UI/Lobby/Sprites";
        private const string MonstersRoot = "Assets/Game/Content/Characters/Monsters";
        private const string FontPath = "Assets/Game/Content/UI/Fonts/ONE Mobile POP SDF.asset";

        [MenuItem("Tools/Trickcal Revive/Flow/Build Stage-Info Popup UI Prefab")]
        public static void BuildPrefab()
        {
            EnsureFolder("Assets/Game/MainUI/Prefabs");
            var font = EnsureFont();

            var panelBg = Require<Sprite>(StageRoot + "/Stage_MonsterInfoBg.png");
            var difficultyFrame = Require<Sprite>(StageRoot + "/Stage_Level_Easy.png");
            var headerDeco = Require<Sprite>(StageRoot + "/StagePopup_HeaderDeco.png");
            var starSprite = Require<Sprite>(StageRoot + "/StageReward_Star.png");
            var deckSprite = Require<Sprite>(StageRoot + "/Btn_Battle_Deck.png");
            var rewardBg = Require<Sprite>(StageRoot + "/StageReward_ItemBg.png");
            var closeSprite = Require<Sprite>(StageRoot + "/CommonButton_Close_1.png");
            var searchSprite = Require<Sprite>(StageRoot + "/Common_Icon_Search.png");
            var enemySprite = Require<Sprite>(StageRoot + "/CommonIcon_Enemy.png");
            var personalityNaive = Require<Sprite>(StageRoot + "/Common_UnitPersonality_Naive.png");
            var personalityMad = Require<Sprite>(StageRoot + "/Common_UnitPersonality_Mad.png");
            var personalityJolly = Require<Sprite>(StageRoot + "/Common_UnitPersonality_Jolly.png");
            var personalityGloomy = Require<Sprite>(StageRoot + "/Common_UnitPersonality_Gloomy.png");
            var personalityCool = Require<Sprite>(StageRoot + "/Common_UnitPersonality_Cool.png");
            var gluttonbear = Require<Sprite>(MonstersRoot + "/gluttonbear/Presentation/Naive.png");
            var lupalu = Require<Sprite>(MonstersRoot + "/lupalu/Presentation/Naive.png");
            var magicfork = Require<Sprite>(MonstersRoot + "/magicfork/Presentation/Naive.png");
            var pumpkin = Require<Sprite>(MonstersRoot + "/pumpkin/Presentation/Naive.png");
            var gold = Require<Sprite>(LobbySprites + "/Currency_Gold.png");
            var macaron = Require<Sprite>(LobbySprites + "/Currency_Macaron.png");
            var elleaf = Require<Sprite>(LobbySprites + "/Currency_Elif.png");
            var stamina = Require<Sprite>(LobbySprites + "/Currency_Stamina.png");

            var root = RectObject("StageInfoPopupScreen", null);
            Stretch((RectTransform)root.transform);

            var content = RectObject("Content", root.transform);
            Stretch((RectTransform)content.transform);

            // 원본 PopupStageDetail 계층을 따라 기본 창은 우측 패널만 표시한다.
            // 검은 Backdrop은 추천 성격/몬스터 상세 같은 2차 팝업에서 별도로 생성한다.
            var panel = RectObject("Panel", content.transform);
            Anchored((RectTransform)panel.transform, new Vector2(1f, 0.5f), new Vector2(-500f, 0f), new Vector2(960f, 1040f));

            var frame = Image(panel.transform, "DifficultyFrame", Color.white, difficultyFrame);
            Stretch(frame.rectTransform);
            frame.type = UnityEngine.UI.Image.Type.Sliced;
            frame.raycastTarget = true;

            var body = Image(panel.transform, "Body", Color.white, panelBg);
            Anchored(body.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(900f, 980f));
            body.type = UnityEngine.UI.Image.Type.Sliced;
            body.raycastTarget = true;

            var headerGroup = RectObject("HeaderGroup", panel.transform);
            Anchored((RectTransform)headerGroup.transform, new Vector2(0.5f, 1f), new Vector2(0f, 70f), new Vector2(540f, 190f), new Vector2(0.5f, 1f));
            var headerBase = Image(headerGroup.transform, "HeaderBase", Color.white, difficultyFrame);
            Stretch(headerBase.rectTransform);
            headerBase.type = UnityEngine.UI.Image.Type.Sliced;
            headerBase.raycastTarget = false;
            var headerPattern = Image(headerGroup.transform, "HeaderDeco", Color.white, headerDeco);
            Anchored(headerPattern.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -8f), new Vector2(470f, 90f));
            headerPattern.preserveAspect = true;
            headerPattern.raycastTarget = false;

            var stars = new Image[3];
            for (var i = 0; i < 3; i++)
            {
                var star = Image(headerGroup.transform, $"Star{i}", Color.white, starSprite);
                Anchored(star.rectTransform, new Vector2(0.5f, 1f), new Vector2(-90f + i * 90f, -82f), new Vector2(78f, 74f), new Vector2(0.5f, 1f));
                star.preserveAspect = true;
                star.raycastTarget = false;
                stars[i] = star;
            }

            var close = Button(panel.transform, "CloseButton", string.Empty, font, new Color32(55, 51, 48, 255), closeSprite);
            Anchored(close.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(-52f, -52f), new Vector2(64f, 64f), new Vector2(1f, 1f));

            // 이름/권장 전투력은 원본에서 선택 노드 위 말풍선 영역이다. 현재 데이터 연결은
            // 유지하되 우측 상세 패널 안에서는 숨겨 둔다.
            var mission = RectObject("Mission", panel.transform);
            var stageName = Text(mission.transform, "StageName", "스테이지", font, 40f, TextAlignmentOptions.Center, new Color32(60, 50, 46, 255));
            Anchored(stageName.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(640f, 56f), new Vector2(0.5f, 1f));
            stageName.fontStyle = FontStyles.Bold;
            var power = Text(mission.transform, "Power", "권장 전투력 0", font, 28f, TextAlignmentOptions.Center, new Color32(196, 90, 110, 255));
            Anchored(power.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -80f), new Vector2(640f, 44f), new Vector2(0.5f, 1f));
            mission.SetActive(false);

            // 추천 성격
            var personalityGroup = RectObject("Personality", panel.transform);
            Anchored((RectTransform)personalityGroup.transform, new Vector2(0.5f, 1f), new Vector2(-220f, -185f), new Vector2(360f, 240f), new Vector2(0.5f, 1f));
            var personalityTitle = Text(personalityGroup.transform, "Title", "추천 성격", font, 30f, TextAlignmentOptions.Center, new Color32(55, 51, 48, 255));
            Anchored(personalityTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(-22f, -12f), new Vector2(250f, 44f), new Vector2(0.5f, 1f));
            personalityTitle.fontStyle = FontStyles.Bold;
            var personalityInfo = Button(personalityGroup.transform, "InfoButton", string.Empty, font, new Color32(55, 51, 48, 255), searchSprite);
            Anchored(personalityInfo.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(-28f, -14f), new Vector2(50f, 50f), new Vector2(1f, 1f));
            var personalityIcon = Image(personalityGroup.transform, "Icon", Color.white, personalityNaive);
            Anchored(personalityIcon.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -75f), new Vector2(112f, 112f), new Vector2(0.5f, 1f));
            personalityIcon.preserveAspect = true;
            personalityIcon.raycastTarget = false;
            var personality = Text(personalityGroup.transform, "Value", "-", font, 28f, TextAlignmentOptions.Center, new Color32(55, 51, 48, 255));
            Anchored(personality.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 8f), new Vector2(240f, 42f), new Vector2(0.5f, 0f));
            personality.fontStyle = FontStyles.Bold;
            personality.gameObject.SetActive(false);

            // 기본 패널은 적 종류 아이콘만 표시한다. 개별 몬스터는 2차 상세 팝업에서 사용한다.
            var enemyGroup = RectObject("Enemy", panel.transform);
            Anchored((RectTransform)enemyGroup.transform, new Vector2(0.5f, 1f), new Vector2(220f, -185f), new Vector2(360f, 240f), new Vector2(0.5f, 1f));
            var monsterTitle = Text(enemyGroup.transform, "Title", "출현 몬스터", font, 30f, TextAlignmentOptions.Center, new Color32(55, 51, 48, 255));
            Anchored(monsterTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(-22f, -12f), new Vector2(250f, 44f), new Vector2(0.5f, 1f));
            monsterTitle.fontStyle = FontStyles.Bold;
            var enemyInfo = Button(enemyGroup.transform, "InfoButton", string.Empty, font, new Color32(55, 51, 48, 255), searchSprite);
            Anchored(enemyInfo.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(-28f, -14f), new Vector2(50f, 50f), new Vector2(1f, 1f));
            var enemyIcon = Image(enemyGroup.transform, "Icon", Color.white, enemySprite);
            Anchored(enemyIcon.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -82f), new Vector2(118f, 102f), new Vector2(0.5f, 1f));
            enemyIcon.preserveAspect = true;
            enemyIcon.raycastTarget = false;

            var monsterDetails = RectObject("MonsterDetails", enemyGroup.transform);
            Stretch((RectTransform)monsterDetails.transform);
            var monsters = new Image[4];
            for (var i = 0; i < 4; i++)
            {
                var slot = Image(monsterDetails.transform, $"Monster{i}", Color.white, gluttonbear);
                Anchored(slot.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-120f + i * 80f, 0f), new Vector2(72f, 72f));
                slot.preserveAspect = true;
                slot.raycastTarget = false;
                monsters[i] = slot;
            }
            monsterDetails.SetActive(false);

            var divider = Image(panel.transform, "Divider", new Color32(226, 221, 211, 255));
            Anchored(divider.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -210f), new Vector2(3f, 190f), new Vector2(0.5f, 1f));
            divider.raycastTarget = false;

            // 보상 목록 (up to 3)
            var rewardGroup = RectObject("Reward", panel.transform);
            Anchored((RectTransform)rewardGroup.transform, new Vector2(0.5f, 1f), new Vector2(0f, -455f), new Vector2(840f, 350f), new Vector2(0.5f, 1f));
            var rewardPanel = Image(rewardGroup.transform, "Base", new Color32(248, 246, 239, 255), panelBg);
            Stretch(rewardPanel.rectTransform);
            rewardPanel.type = UnityEngine.UI.Image.Type.Sliced;
            rewardPanel.raycastTarget = false;
            var rewardTitle = Text(rewardGroup.transform, "Title", "보상 목록", font, 30f, TextAlignmentOptions.Center, new Color32(55, 51, 48, 255));
            Anchored(rewardTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -26f), new Vector2(300f, 44f), new Vector2(0.5f, 1f));
            rewardTitle.fontStyle = FontStyles.Bold;
            var rewardIcons = new Image[3];
            var rewardAmounts = new TMP_Text[3];
            for (var i = 0; i < 3; i++)
            {
                var cell = Image(rewardGroup.transform, $"RewardCell{i}", Color.white, rewardBg);
                Anchored(cell.rectTransform, new Vector2(0.5f, 1f), new Vector2(-205f + i * 205f, -95f), new Vector2(156f, 170f), new Vector2(0.5f, 1f));
                cell.raycastTarget = false;
                var icon = Image(cell.transform, "Icon", Color.white, gold);
                Anchored(icon.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(92f, 92f), new Vector2(0.5f, 1f));
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                var amount = Text(cell.transform, "Amount", "0", font, 24f, TextAlignmentOptions.Center, new Color32(55, 51, 48, 255));
                Anchored(amount.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(150f, 38f), new Vector2(0.5f, 0f));
                rewardIcons[i] = icon;
                rewardAmounts[i] = amount;
            }

            // buttons: 면제(비활성) / 덱선택
            var buttonGroup = RectObject("ButtonGroup", panel.transform);
            Anchored((RectTransform)buttonGroup.transform, new Vector2(0.5f, 0f), new Vector2(0f, 45f), new Vector2(840f, 120f), new Vector2(0.5f, 0f));
            var exempt = Button(buttonGroup.transform, "ExemptButton", "면제", font, new Color32(174, 174, 174, 255), panelBg, new Color32(55, 51, 48, 255));
            Anchored(exempt.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(-215f, 0f), new Vector2(370f, 112f));
            var exemptImage = exempt.targetGraphic as Image;
            exemptImage.type = UnityEngine.UI.Image.Type.Sliced;
            exemptImage.preserveAspect = false;
            exempt.interactable = false;
            var deck = Button(buttonGroup.transform, "DeckButton", "덱 선택", font, new Color32(255, 205, 74, 255), panelBg, new Color32(55, 51, 48, 255));
            Anchored(deck.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(215f, 0f), new Vector2(370f, 112f));
            var deckImage = deck.targetGraphic as Image;
            deckImage.type = UnityEngine.UI.Image.Type.Sliced;
            deckImage.preserveAspect = false;
            var staminaIcon = Image(deck.transform, "StaminaIcon", Color.white, stamina);
            Anchored(staminaIcon.rectTransform, new Vector2(0f, 0.5f), new Vector2(58f, 4f), new Vector2(68f, 68f), new Vector2(0f, 0.5f));
            staminaIcon.preserveAspect = true;
            staminaIcon.raycastTarget = false;
            var staminaValue = Text(deck.transform, "StaminaValue", "10", font, 22f, TextAlignmentOptions.Center, Color.white);
            Anchored(staminaValue.rectTransform, new Vector2(0f, 0.5f), new Vector2(58f, -35f), new Vector2(80f, 34f), new Vector2(0f, 0.5f));
            var deckIcon = Image(deck.transform, "DeckIcon", Color.white, deckSprite);
            Anchored(deckIcon.rectTransform, new Vector2(1f, 0.5f), new Vector2(-42f, 0f), new Vector2(86f, 74f), new Vector2(1f, 0.5f));
            deckIcon.preserveAspect = true;
            deckIcon.raycastTarget = false;

            var view = root.AddComponent<StageInfoPopupView>();
            view.Configure(
                content, stageName, stars, power, personality, personalityIcon, monsters, rewardIcons, rewardAmounts,
                close, deck, exempt,
                gluttonbear, lupalu, magicfork, pumpkin, gold, macaron, elleaf, stamina,
                personalityNaive, personalityMad, personalityJolly, personalityGloomy, personalityCool);

            // 기본은 닫힌 상태(에디트 캡처는 Awake가 안 돌아 명시적으로 꺼둔다).
            content.SetActive(false);

            SavePrefab(root, PopupPrefabPath, view.IsReady);
            AssetDatabase.SaveAssets();
            Debug.Log("StageInfo popup UI prefab built.");
        }

        private static TMP_FontAsset EnsureFont()
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (font == null)
                throw new InvalidOperationException($"Lobby font missing (build lobby first): {FontPath}");
            const string glyphs = "추천성격출현몬스터보상목록권장전투력덱선택면제광순냉정우울활발스테이지X0123456789,";
            if (!font.TryAddCharacters(glyphs, out var missing))
                throw new InvalidOperationException($"Popup font missing glyphs: {missing}");
            EditorUtility.SetDirty(font);
            return font;
        }

        // --- helpers (mirror LoginLobbyUIBuilder / StageSelectUIBuilder) --------

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
            text.overflowMode = TextOverflowModes.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private static Button Button(
            Transform parent,
            string name,
            string label,
            TMP_FontAsset font,
            Color color,
            Sprite sprite = null,
            Color? labelColor = null)
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
                var text = Text(root.transform, "Label", label, font, 30f, TextAlignmentOptions.Center, labelColor ?? Color.white);
                Stretch(text.rectTransform);
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

        private static GameObject RectObject(string name, Transform parent, params Type[] components)
        {
            var all = new Type[components.Length + 1];
            all[0] = typeof(RectTransform);
            Array.Copy(components, 0, all, 1, components.Length);
            var go = new GameObject(name, all);
            if (parent != null)
                go.transform.SetParent(parent, false);
            return go;
        }

        private static void SavePrefab(GameObject root, string path, bool ready)
        {
            if (!ready)
            {
                Object.DestroyImmediate(root);
                throw new InvalidOperationException($"Generated StageInfo popup is incomplete: {path}");
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

        private static void Anchored(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size, Vector2? pivot = null)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot ?? new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }
    }
}

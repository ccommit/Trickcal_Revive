using System;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;

using Spine.Unity;
using TrickcalRevive.MainUI;

using Object = UnityEngine.Object;

namespace TrickcalRevive.App.Editor
{
    /// <summary>
    /// 원본 WindowDeckEdit 프리팹 구조와 Deck/CommonUI 스프라이트를 근거로 파티 편성 화면을 생성한다.
    /// 좌표와 전투력 수치는 복구 검증용 fixture이며, 원본 master data 확정값으로 취급하지 않는다.
    /// </summary>
    public static class PartySetupUIBuilder
    {
        public const string PartyPrefabPath = "Assets/Game/MainUI/Prefabs/PartySetupScreen.prefab";

        private const string ApostlesRoot = "Assets/Game/Content/Characters/Apostles";
        private const string PartyUiRoot = "Assets/Game/Content/UI/PartySetup";
        private const string StageUiRoot = "Assets/Game/Content/UI/StageSelect";
        private const string FontPath = "Assets/Game/Content/UI/Fonts/ONE Mobile POP SDF.asset";
        private const string SkeletonGraphicMaterialPath =
            "Packages/com.esotericsoftware.spine.spine-unity/Runtime/spine-unity/Materials/SkeletonGraphicDefault.mat";

        private static readonly string[] ApostleKeys =
        {
            "maison", "yumimi", "maestromk2", "diana", "chloe", "arnet", "lazy", "patula", "ricota", "lethe",
            "amelia", "kommyswim", "allet", "sari", "bigwood", "gabia", "kyarot", "ran", "chopi", "veroo",
            "barie", "festa", "kommy", "lion", "carren", "mynx", "bana", "jubee", "rude", "selline",
        };

        private static readonly string[] ApostleAssetNames =
        {
            "Maison", "Yumimi", "MaestroMK2", "Diana", "Chloe", "Arnet", "Lazy", "Patula", "Ricota", "Lethe",
            "Amelia", "KommySwim", "Allet", "Sari", "BigWood", "Gabia", "Kyarot", "Ran", "Chopi", "Veroo",
            "Barie", "Festa", "Kommy", "Lion", "Carren", "Mynx", "Bana", "Jubee", "Rude", "Selline",
        };

        // 이전 프로젝트의 30명 선정안/사도 목록을 대조한 참고 복구값.
        private static readonly string[] ApostleColumns =
        {
            "Back", "Back", "Front", "Middle", "Front", "Middle", "Back", "Front", "Front", "Front",
            "Back", "Middle", "Front", "Middle", "Front", "Middle", "Back", "Back", "Middle", "Middle",
            "Back", "Front", "Front", "Front", "Back", "Front", "Back", "Middle", "Front", "Front",
        };

        [MenuItem("Tools/Trickcal Revive/Flow/Build Party-Setup UI Prefab")]
        public static void BuildPrefab()
        {
            EnsureFolder("Assets/Game/MainUI/Prefabs");
            var font = EnsureFont();
            var skeletonGraphicMaterial = Require<Material>(SkeletonGraphicMaterialPath);

            var icons = new Sprite[ApostleKeys.Length];
            var admissionSkills = new Sprite[ApostleKeys.Length];
            var graduateSkills = new Sprite[ApostleKeys.Length];
            var skeletons = new SkeletonDataAsset[ApostleKeys.Length];
            var powers = new long[ApostleKeys.Length];
            for (var i = 0; i < ApostleKeys.Length; i++)
            {
                icons[i] = Require<Sprite>($"{ApostlesRoot}/{ApostleKeys[i]}/Presentation/RosterIcon.png");
                admissionSkills[i] = Require<Sprite>($"{ApostlesRoot}/{ApostleKeys[i]}/Presentation/AdmissionSkill.png");
                graduateSkills[i] = Require<Sprite>($"{ApostlesRoot}/{ApostleKeys[i]}/Presentation/GraduateSkill.png");
                skeletons[i] = Require<SkeletonDataAsset>(
                    $"{ApostlesRoot}/{ApostleKeys[i]}/Spine/InGame/{ApostleAssetNames[i]}_SkeletonData.asset");
                powers[i] = 1_803_163L - i * 17_321L;
            }

            var deckPositionBg = PartySprite("Deck_PositionBg");
            var deckPositionTile = PartySprite("Deck_PositionTile");
            var deckListDim = PartySprite("Deck_ListDim");
            var deckClearIcon = PartySprite("Deck_IconClearDeck");
            var commonMajorStart = PartySprite("Common_MajorBtn_002");
            var commonRound = PartySprite("Common_MajorBtn_010");
            var commonPale = PartySprite("Common_MajorBtn_014");
            var commonMinor = PartySprite("Common_MinorBtn_3030_001");
            var commonMinorDark = PartySprite("Common_MinorBtn_3030_011");
            var commonTop = PartySprite("CommonHalf_Top_4040");
            var commonBox = PartySprite("CommonBox_6060");
            var deckArtifact = PartySprite("Deck_Artifact");
            var deckWhiteBox = PartySprite("Deck_WhiteBox");
            var searchIcon = Require<Sprite>(StageUiRoot + "/Common_Icon_Search.png");
            var searchBase = Require<Sprite>(StageUiRoot + "/CommonCircle_1.png");
            var closeIcon = Require<Sprite>(StageUiRoot + "/CommonButton_Close_1.png");
            var personalityIcon = Require<Sprite>(StageUiRoot + "/Common_UnitPersonality_Naive.png");
            var battleBackgrounds = new Sprite[5];
            for (var index = 0; index < battleBackgrounds.Length; index++)
                battleBackgrounds[index] = PartySprite($"BG_Stage1_1_{index + 1}");

            var root = RectObject("PartySetupScreen", null);
            Stretch((RectTransform)root.transform);

            BuildBattleBackground(root.transform, battleBackgrounds);

            var shade = Image(root.transform, "BackgroundShade", new Color(0.04f, 0.06f, 0.08f, 0.08f));
            Stretch(shade.rectTransform);
            shade.raycastTarget = false;

            BuildPartySummary(root.transform, font, commonTop);

            var formationSpines = BuildFormation(
                root.transform,
                font,
                deckPositionBg,
                deckPositionTile,
                skeletonGraphicMaterial);

            var cardButtons = new Button[ApostleKeys.Length];
            var detailButtons = new Button[ApostleKeys.Length];
            var cardPlayerIds = new string[ApostleKeys.Length];
            var cardDims = new Image[ApostleKeys.Length];
            var cardRoots = new GameObject[ApostleKeys.Length];
            var roster = BuildRoster(
                root.transform,
                font,
                icons,
                powers,
                deckListDim,
                commonTop,
                commonBox,
                commonMinor,
                commonRound,
                commonMajorStart,
                searchIcon,
                searchBase,
                cardButtons,
                detailButtons,
                cardPlayerIds,
                cardDims,
                cardRoots);

            var autoBuild = Button(root.transform, "AutoBuildButton", "자동 편성", font, commonPale, Color.white, new Color32(55, 80, 35, 255));
            Anchored(autoBuild.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(142f, 76f), new Vector2(242f, 92f), new Vector2(0f, 0f));
            var reset = Button(root.transform, "ResetButton", "일괄 해제", font, commonMinorDark, Color.white, new Color32(55, 60, 55, 255));
            Anchored(reset.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(398f, 76f), new Vector2(242f, 92f), new Vector2(0f, 0f));
            var resetIcon = Image(reset.transform, "ClearIcon", Color.white, deckClearIcon);
            Anchored(resetIcon.rectTransform, new Vector2(0f, 0.5f), new Vector2(12f, 0f), new Vector2(54f, 54f), new Vector2(0f, 0.5f));
            resetIcon.preserveAspect = true;
            resetIcon.raycastTarget = false;
            SetRect(reset.transform.Find("Label").GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(62f, 5f), new Vector2(-8f, -5f));
            var status = Text(root.transform, "Status", string.Empty, font, 28f, TextAlignmentOptions.Center, Color.white);
            Anchored(status.rectTransform, new Vector2(0.5f, 0f), new Vector2(-420f, 158f), new Vector2(1040f, 52f), new Vector2(0.5f, 0f));
            RecoveredTextStyles.Apply(status, RecoveredTextStyle.LightWithDarkBorder);

            var detail = BuildCharacterDetail(
                root.transform,
                font,
                commonTop,
                commonBox,
                deckWhiteBox,
                deckArtifact,
                closeIcon,
                personalityIcon,
                icons[0],
                admissionSkills[0],
                graduateSkills[0]);

            var view = root.AddComponent<PartySetupView>();
            view.Configure(
                formationSpines,
                (string[])ApostleKeys.Clone(),
                skeletons,
                icons,
                cardButtons,
                cardPlayerIds,
                cardDims,
                cardRoots,
                (string[])ApostleColumns.Clone(),
                powers,
                autoBuild,
                reset,
                null,
                roster.Start,
                null,
                roster.Filter,
                roster.Sort,
                roster.SortDirection,
                null,
                null,
                roster.FilterLabel,
                roster.SortDirectionLabel,
                status);
            view.ConfigureCharacterDetail(
                detailButtons,
                (string[])ApostleAssetNames.Clone(),
                admissionSkills,
                graduateSkills,
                personalityIcon,
                detail.Modal,
                detail.Backdrop,
                detail.Close,
                detail.Portrait,
                detail.Personality,
                detail.AdmissionSkill,
                detail.GraduateSkill,
                detail.Name,
                detail.Power,
                detail.Description);
            for (var index = 0; index < formationSpines.Length; index++)
            {
                var handle = formationSpines[index].GetComponent<FormationSlotDragHandle>();
                handle.Configure(view, index / 3 + 1, index % 3 + 1);
            }
            for (var index = 0; index < detailButtons.Length; index++)
                UnityEventTools.AddIntPersistentListener(detailButtons[index].onClick, view.ShowCharacterDetail, index);

            SavePrefab(root, PartyPrefabPath, view.IsReady);
            AssetDatabase.SaveAssets();
            Debug.Log("PartySetup UI prefab built from recovered Deck/CommonUI sprites.");
        }

        private static SkeletonGraphic[] BuildFormation(
            Transform parent,
            TMP_FontAsset font,
            Sprite positionBackground,
            Sprite positionTile,
            Material skeletonGraphicMaterial)
        {
            var spines = new SkeletonGraphic[9];
            var root = RectObject("Formation", parent);
            Anchored((RectTransform)root.transform, new Vector2(0f, 0.5f), new Vector2(810f, -100f), new Vector2(1480f, 1020f), new Vector2(0.5f, 0.5f));

            var columnColors = new[]
            {
                new Color32(66, 126, 204, 196),
                new Color32(98, 197, 85, 196),
                new Color32(226, 92, 86, 196),
            };
            var columnLabels = new[] { "후열", "중열", "전열" };
            for (var x = 1; x <= 3; x++)
            {
                var xPosition = FormationColumnX(x);
                var column = Image(root.transform, $"Column_{x}", columnColors[x - 1], positionBackground);
                Anchored(column.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(xPosition, 0f), new Vector2(572f, 480f));
                column.type = UnityEngine.UI.Image.Type.Sliced;
                column.raycastTarget = false;

                var label = Text(column.transform, "Label", columnLabels[x - 1], font, 28f, TextAlignmentOptions.Center, Color.white);
                Anchored(label.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -42f), new Vector2(180f, 48f), new Vector2(0.5f, 1f));
                RecoveredTextStyles.Apply(label, RecoveredTextStyle.LightWithDarkBorder);
            }

            for (var x = 1; x <= 3; x++)
            {
                for (var y = 1; y <= 3; y++)
                {
                    var cell = Image(root.transform, $"Cell_{x}_{y}", new Color32(255, 255, 255, 96), positionTile);
                    Anchored(
                        cell.rectTransform,
                        new Vector2(0.5f, 0.5f),
                        new Vector2(FormationCellX(x, y), RowPosition(y)),
                        new Vector2(270f, 90f));
                    cell.type = UnityEngine.UI.Image.Type.Sliced;
                    cell.raycastTarget = false;

                    var spineViewport = RectObject("SpineViewport", cell.transform);
                    Anchored(
                        (RectTransform)spineViewport.transform,
                        new Vector2(0.5f, 0.5f),
                        Vector2.zero,
                        new Vector2(100f, 100f),
                        new Vector2(0.5f, 0.5f));
                    var spineGo = RectObject(
                        "Spine",
                        spineViewport.transform,
                        typeof(CanvasRenderer),
                        typeof(FormationSlotDragHandle));
                    var spine = spineGo.AddComponent<SkeletonGraphic>();
                    spine.material = skeletonGraphicMaterial;
                    spine.raycastTarget = true;
                    spine.layoutScaleMode = SkeletonGraphic.LayoutMode.None;
                    spine.allowMultipleCanvasRenderers = false;
                    spine.initialFlipX = true;
                    Anchored(
                        spine.rectTransform,
                        new Vector2(0.5f, 0.5f),
                        Vector2.zero,
                        new Vector2(750f, 1000f),
                        new Vector2(0.5f, 0f));
                    spine.rectTransform.localScale = Vector3.one * 0.4f;
                    spine.gameObject.SetActive(false);
                    spines[(x - 1) * 3 + (y - 1)] = spine;
                }
            }

            return spines;
        }

        private static float FormationCellX(int x, int y)
        {
            var columnX = FormationColumnX(x);
            return y == 2 ? columnX + 110f : columnX - 60f;
        }

        private static float FormationColumnX(int x)
        {
            switch (x)
            {
                case 1: return -510f;
                case 2: return -80f;
                default: return 350f;
            }
        }

        private static float RowPosition(int y)
        {
            switch (y)
            {
                case 1: return 150f;
                case 2: return 0f;
                default: return -150f;
            }
        }

        private static void BuildPartySummary(Transform parent, TMP_FontAsset font, Sprite panelSprite)
        {
            var panel = Image(parent, "PartySummary", new Color(0.05f, 0.07f, 0.08f, 0.78f), panelSprite);
            Anchored(panel.rectTransform, new Vector2(0f, 1f), new Vector2(1170f, -270f), new Vector2(380f, 250f), new Vector2(0f, 1f));
            panel.type = UnityEngine.UI.Image.Type.Sliced;
            var lines = Text(
                panel.transform,
                "Summary",
                "편성된 사도     0 / 6\n침략군          1\n편성된 카드   70 / 114\n침략군 카드     1",
                font,
                26f,
                TextAlignmentOptions.MidlineLeft,
                Color.white);
            Stretch(lines.rectTransform, 22f);
        }

        private static void BuildBattleBackground(Transform parent, Sprite[] layers)
        {
            if (layers == null || layers.Length != 5)
                throw new InvalidOperationException("BG_Stage1_1 must contain five recovered layers.");

            var root = RectObject("BattleBackground", parent);
            Stretch((RectTransform)root.transform);
            var sizes = new[]
            {
                new Vector2(2760f, 430f),
                new Vector2(2760f, 520f),
                new Vector2(2760f, 550f),
                new Vector2(2760f, 1470f),
                new Vector2(2760f, 360f),
            };
            var positions = new[] { 520f, 380f, 220f, 0f, -560f };
            for (var index = 0; index < layers.Length; index++)
            {
                var image = Image(root.transform, $"BG_Stage1_1_{index + 1}", Color.white, layers[index]);
                Anchored(image.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, positions[index]), sizes[index]);
                image.preserveAspect = false;
                image.raycastTarget = false;
            }
        }

        private sealed class CharacterDetailControls
        {
            public GameObject Modal;
            public Button Backdrop;
            public Button Close;
            public Image Portrait;
            public Image Personality;
            public Image AdmissionSkill;
            public Image GraduateSkill;
            public TMP_Text Name;
            public TMP_Text Power;
            public TMP_Text Description;
        }

        private static CharacterDetailControls BuildCharacterDetail(
            Transform parent,
            TMP_FontAsset font,
            Sprite headerSprite,
            Sprite panelSprite,
            Sprite whiteBoxSprite,
            Sprite artifactSprite,
            Sprite closeSprite,
            Sprite personalitySprite,
            Sprite portraitSprite,
            Sprite admissionSkillSprite,
            Sprite graduateSkillSprite)
        {
            var modal = RectObject("CharacterDetailModal", parent);
            Stretch((RectTransform)modal.transform);

            var backdropRoot = RectObject("Backdrop", modal.transform, typeof(Image), typeof(Button));
            Stretch((RectTransform)backdropRoot.transform);
            var backdropImage = backdropRoot.GetComponent<Image>();
            backdropImage.color = new Color(0f, 0f, 0f, 0.58f);
            var backdrop = backdropRoot.GetComponent<Button>();
            backdrop.targetGraphic = backdropImage;

            var panelRoot = RectObject("Panel", modal.transform, typeof(Image), typeof(CanvasGroup));
            var panelRect = (RectTransform)panelRoot.transform;
            Anchored(panelRect, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000f, 1340f));
            var panelImage = panelRoot.GetComponent<Image>();
            panelImage.sprite = panelSprite;
            panelImage.color = Color.white;
            panelImage.type = UnityEngine.UI.Image.Type.Sliced;
            var bounce = panelRoot.AddComponent<PopupBounceView>();
            bounce.Configure(panelRoot.GetComponent<CanvasGroup>());

            var header = Image(panelRoot.transform, "Header", new Color32(48, 191, 48, 255), headerSprite);
            Anchored(header.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(960f, 140f), new Vector2(0.5f, 0.5f));
            header.type = UnityEngine.UI.Image.Type.Sliced;
            var name = Text(header.transform, "Name", ApostleAssetNames[0], font, 44f, TextAlignmentOptions.Center, Color.white);
            Stretch(name.rectTransform, 18f);
            RecoveredTextStyles.Apply(name, RecoveredTextStyle.LightWithDarkBorder);

            var closeRoot = RectObject("CloseButton", header.transform, typeof(Image), typeof(Button));
            Anchored((RectTransform)closeRoot.transform, new Vector2(1f, 0.5f), new Vector2(-34f, 0f), new Vector2(86f, 86f), new Vector2(1f, 0.5f));
            var closeImage = closeRoot.GetComponent<Image>();
            closeImage.sprite = closeSprite;
            closeImage.color = Color.white;
            closeImage.preserveAspect = true;
            var close = closeRoot.GetComponent<Button>();
            close.targetGraphic = closeImage;

            var upper = RectObject("Upper", panelRoot.transform);
            Anchored((RectTransform)upper.transform, new Vector2(0.5f, 1f), new Vector2(0f, -315f), new Vector2(920f, 330f), new Vector2(0.5f, 0.5f));

            var unitSlot = Image(upper.transform, "UnitSlot", new Color32(169, 239, 174, 255), whiteBoxSprite);
            Anchored(unitSlot.rectTransform, new Vector2(0f, 0.5f), new Vector2(12f, 10f), new Vector2(260f, 290f), new Vector2(0f, 0.5f));
            unitSlot.type = UnityEngine.UI.Image.Type.Sliced;
            var portrait = Image(unitSlot.transform, "Portrait", Color.white, portraitSprite);
            SetRect(portrait.rectTransform, Vector2.zero, Vector2.one, new Vector2(12f, 46f), new Vector2(-12f, -12f));
            portrait.preserveAspect = true;
            portrait.raycastTarget = false;
            var level = Text(unitSlot.transform, "Level", "Lv.30   ★★★", font, 25f, TextAlignmentOptions.Center, Color.white);
            Anchored(level.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(240f, 46f), new Vector2(0.5f, 0.5f));
            RecoveredTextStyles.Apply(level, RecoveredTextStyle.LightWithDarkBorder);

            var powerBase = Image(upper.transform, "PowerBase", new Color32(47, 171, 47, 255), whiteBoxSprite);
            Anchored(powerBase.rectTransform, new Vector2(1f, 1f), new Vector2(-14f, -44f), new Vector2(610f, 86f), new Vector2(1f, 1f));
            powerBase.type = UnityEngine.UI.Image.Type.Sliced;
            var power = Text(powerBase.transform, "Power", "전투력   1,803,163", font, 37f, TextAlignmentOptions.Center, Color.white);
            Stretch(power.rectTransform, 10f);
            RecoveredTextStyles.Apply(power, RecoveredTextStyle.LightWithDarkBorder);

            var information = Image(upper.transform, "Information", new Color32(226, 247, 178, 255), whiteBoxSprite);
            Anchored(information.rectTransform, new Vector2(1f, 0f), new Vector2(-14f, 18f), new Vector2(610f, 170f), new Vector2(1f, 0f));
            information.type = UnityEngine.UI.Image.Type.Sliced;
            var personality = Image(information.transform, "Personality", Color.white, personalitySprite);
            Anchored(personality.rectTransform, new Vector2(0f, 0.5f), new Vector2(62f, 0f), new Vector2(92f, 92f), new Vector2(0.5f, 0.5f));
            personality.preserveAspect = true;
            personality.raycastTarget = false;
            var infoText = Text(information.transform, "Labels", "순수     탱커     전열     물리", font, 29f, TextAlignmentOptions.Center, new Color32(55, 59, 46, 255));
            SetRect(infoText.rectTransform, Vector2.zero, Vector2.one, new Vector2(105f, 8f), new Vector2(-12f, -8f));

            var artifact = RectObject("Artifact", panelRoot.transform);
            Anchored((RectTransform)artifact.transform, new Vector2(0f, 1f), new Vector2(68f, -535f), new Vector2(250f, 92f), new Vector2(0f, 1f));
            for (var index = 0; index < 3; index++)
            {
                var slot = Image(artifact.transform, $"Slot_{index + 1}", Color.white, artifactSprite);
                Anchored(slot.rectTransform, new Vector2(0f, 0.5f), new Vector2(index * 82f, 0f), new Vector2(72f, 72f), new Vector2(0f, 0.5f));
                slot.preserveAspect = true;
            }

            var tabGroup = RectObject("TabGroup", panelRoot.transform);
            Anchored((RectTransform)tabGroup.transform, new Vector2(0.5f, 1f), new Vector2(0f, -590f), new Vector2(920f, 88f), new Vector2(0.5f, 0.5f));
            var skillTab = Image(tabGroup.transform, "SkillTab", new Color32(255, 202, 70, 255), whiteBoxSprite);
            Anchored(skillTab.rectTransform, new Vector2(0f, 0.5f), Vector2.zero, new Vector2(445f, 78f), new Vector2(0f, 0.5f));
            skillTab.type = UnityEngine.UI.Image.Type.Sliced;
            var skillTabText = Text(skillTab.transform, "Text", "스킬", font, 32f, TextAlignmentOptions.Center, new Color32(50, 50, 45, 255));
            Stretch(skillTabText.rectTransform);
            var asideTab = Image(tabGroup.transform, "AsideTab", Color.white, whiteBoxSprite);
            Anchored(asideTab.rectTransform, new Vector2(1f, 0.5f), Vector2.zero, new Vector2(445f, 78f), new Vector2(1f, 0.5f));
            asideTab.type = UnityEngine.UI.Image.Type.Sliced;
            var asideText = Text(asideTab.transform, "Text", "어사이드", font, 32f, TextAlignmentOptions.Center, new Color32(50, 50, 45, 255));
            Stretch(asideText.rectTransform);

            var skill = RectObject("Skill", panelRoot.transform);
            Anchored((RectTransform)skill.transform, new Vector2(0.5f, 0f), new Vector2(0f, 330f), new Vector2(920f, 640f), new Vector2(0.5f, 0.5f));
            var skillList = RectObject("SkillList", skill.transform);
            Anchored((RectTransform)skillList.transform, new Vector2(0f, 0.5f), new Vector2(12f, 0f), new Vector2(205f, 590f), new Vector2(0f, 0.5f));
            var admission = SkillIcon(skillList.transform, "AdmissionSkill", admissionSkillSprite, whiteBoxSprite, 186f);
            var graduate = SkillIcon(skillList.transform, "GraduateSkill", graduateSkillSprite, whiteBoxSprite, 0f);
            SkillIcon(skillList.transform, "BasicAttack", portraitSprite, whiteBoxSprite, -186f);

            var descriptionBase = Image(skill.transform, "DescriptionBase", new Color32(238, 249, 203, 255), whiteBoxSprite);
            Anchored(descriptionBase.rectTransform, new Vector2(1f, 0.5f), new Vector2(-10f, 0f), new Vector2(675f, 590f), new Vector2(1f, 0.5f));
            descriptionBase.type = UnityEngine.UI.Image.Type.Sliced;
            var title = Text(descriptionBase.transform, "Title", "사도 스킬 정보", font, 34f, TextAlignmentOptions.Center, Color.white);
            Anchored(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -52f), new Vector2(620f, 70f), new Vector2(0.5f, 0.5f));
            title.color = new Color32(55, 75, 45, 255);
            var description = Text(
                descriptionBase.transform,
                "Description",
                "일반 공격과 저학년 스킬, 고학년 스킬을 확인할 수 있습니다.",
                font,
                28f,
                TextAlignmentOptions.TopLeft,
                new Color32(54, 58, 49, 255));
            SetRect(description.rectTransform, Vector2.zero, Vector2.one, new Vector2(34f, 42f), new Vector2(-34f, -110f));
            description.enableWordWrapping = true;
            description.overflowMode = TextOverflowModes.Ellipsis;

            var detailButton = Button(panelRoot.transform, "OpenFullDetailButton", "상세정보 보기", font, whiteBoxSprite, Color.white, new Color32(55, 59, 48, 255));
            Anchored(detailButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0f, 54f), new Vector2(460f, 86f), new Vector2(0.5f, 0f));

            modal.SetActive(false);
            return new CharacterDetailControls
            {
                Modal = modal,
                Backdrop = backdrop,
                Close = close,
                Portrait = portrait,
                Personality = personality,
                AdmissionSkill = admission,
                GraduateSkill = graduate,
                Name = name,
                Power = power,
                Description = description,
            };
        }

        private static Image SkillIcon(Transform parent, string name, Sprite iconSprite, Sprite frameSprite, float y)
        {
            var frame = Image(parent, name, new Color32(75, 190, 45, 255), frameSprite);
            Anchored(frame.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, y), new Vector2(175f, 175f));
            frame.type = UnityEngine.UI.Image.Type.Sliced;
            var icon = Image(frame.transform, "Icon", Color.white, iconSprite);
            Stretch(icon.rectTransform, 10f);
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            return icon;
        }

        private sealed class RosterControls
        {
            public Button Filter;
            public Button Sort;
            public Button SortDirection;
            public Button Start;
            public TMP_Text FilterLabel;
            public TMP_Text SortDirectionLabel;
        }

        private static RosterControls BuildRoster(
            Transform parent,
            TMP_FontAsset font,
            Sprite[] icons,
            long[] powers,
            Sprite listDim,
            Sprite panelSprite,
            Sprite cardSprite,
            Sprite minorButtonSprite,
            Sprite roundButtonSprite,
            Sprite startButtonSprite,
            Sprite searchIconSprite,
            Sprite searchBaseSprite,
            Button[] cardButtons,
            Button[] detailButtons,
            string[] cardPlayerIds,
            Image[] cardDims,
            GameObject[] cardRoots)
        {
            var rosterGo = RectObject("Roster", parent, typeof(Image), typeof(ScrollRect));
            var rosterImage = rosterGo.GetComponent<Image>();
            rosterImage.sprite = panelSprite;
            rosterImage.color = Color.white;
            rosterImage.type = UnityEngine.UI.Image.Type.Sliced;
            Anchored((RectTransform)rosterGo.transform, new Vector2(1f, 0.5f), new Vector2(-18f, -58f), new Vector2(900f, 1296f), new Vector2(1f, 0.5f));
            var scroll = rosterGo.GetComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.scrollSensitivity = 48f;

            var viewportGo = RectObject("Viewport", rosterGo.transform, typeof(RectMask2D), typeof(Image));
            viewportGo.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.01f);
            var viewport = (RectTransform)viewportGo.transform;
            SetRect(viewport, Vector2.zero, Vector2.one, new Vector2(22f, 158f), new Vector2(-22f, -126f));

            var contentGo = RectObject("Content", viewportGo.transform, typeof(GridLayoutGroup), typeof(ContentSizeFitter));
            var content = (RectTransform)contentGo.transform;
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;
            var grid = contentGo.GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(268f, 334f);
            grid.spacing = new Vector2(10f, 12f);
            grid.padding = new RectOffset(6, 6, 8, 8);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            var fitter = contentGo.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport;
            scroll.content = content;

            for (var i = 0; i < ApostleKeys.Length; i++)
            {
                var card = RectObject($"Card_{ApostleKeys[i]}", contentGo.transform, typeof(Image), typeof(Outline), typeof(Button));
                var cardImage = card.GetComponent<Image>();
                cardImage.sprite = cardSprite;
                cardImage.color = Color.white;
                cardImage.type = UnityEngine.UI.Image.Type.Sliced;
                var outline = card.GetComponent<Outline>();
                outline.effectColor = new Color32(42, 190, 50, 255);
                outline.effectDistance = new Vector2(5f, -5f);
                var button = card.GetComponent<Button>();
                button.targetGraphic = cardImage;

                var icon = Image(card.transform, "Portrait", Color.white, icons[i]);
                SetRect(icon.rectTransform, new Vector2(0f, 0.29f), Vector2.one, new Vector2(8f, 0f), new Vector2(-8f, -8f));
                icon.preserveAspect = true;
                icon.raycastTarget = false;

                var dim = Image(card.transform, "Dim", new Color(0f, 0f, 0f, 0.42f), listDim);
                Stretch(dim.rectTransform);
                dim.type = UnityEngine.UI.Image.Type.Sliced;
                dim.raycastTarget = false;
                dim.gameObject.SetActive(false);

                var columnBadge = Image(card.transform, "ColumnBadge", ColumnColor(ApostleColumns[i]), roundButtonSprite);
                Anchored(columnBadge.rectTransform, new Vector2(0f, 1f), new Vector2(8f, -12f), new Vector2(52f, 52f), new Vector2(0f, 1f));
                columnBadge.raycastTarget = false;
                var columnText = Text(columnBadge.transform, "Label", ColumnShortName(ApostleColumns[i]), font, 22f, TextAlignmentOptions.Center, Color.white);
                Stretch(columnText.rectTransform);

                var searchRoot = RectObject("SearchButton", card.transform, typeof(Image), typeof(Button));
                var searchImage = searchRoot.GetComponent<Image>();
                searchImage.sprite = searchBaseSprite;
                searchImage.color = new Color32(43, 45, 44, 245);
                searchImage.preserveAspect = true;
                var searchButton = searchRoot.GetComponent<Button>();
                searchButton.targetGraphic = searchImage;
                Anchored(
                    (RectTransform)searchRoot.transform,
                    new Vector2(1f, 1f),
                    new Vector2(-8f, -10f),
                    new Vector2(56f, 56f),
                    new Vector2(1f, 1f));
                var searchIcon = Image(searchRoot.transform, "Icon", Color.white, searchIconSprite);
                Anchored(searchIcon.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(38f, 38f));
                searchIcon.preserveAspect = true;
                searchIcon.raycastTarget = false;

                var name = Text(card.transform, "Name", ApostleAssetNames[i], font, 22f, TextAlignmentOptions.Center, new Color32(55, 51, 48, 255));
                SetRect(name.rectTransform, new Vector2(0f, 0.21f), new Vector2(1f, 0.30f), new Vector2(4f, 0f), new Vector2(-4f, 0f));
                var level = Text(card.transform, "Level", "Lv.30   ★★★", font, 23f, TextAlignmentOptions.Center, new Color32(65, 58, 52, 255));
                SetRect(level.rectTransform, new Vector2(0f, 0.12f), new Vector2(1f, 0.22f), new Vector2(4f, 0f), new Vector2(-4f, 0f));
                var power = Text(card.transform, "Power", powers[i].ToString("N0"), font, 28f, TextAlignmentOptions.Center, new Color32(43, 42, 40, 255));
                SetRect(power.rectTransform, Vector2.zero, new Vector2(1f, 0.13f), new Vector2(4f, 0f), new Vector2(-4f, 0f));
                power.fontStyle = FontStyles.Bold;

                cardButtons[i] = button;
                detailButtons[i] = searchButton;
                cardPlayerIds[i] = $"pc_{ApostleKeys[i]}";
                cardDims[i] = dim;
                cardRoots[i] = card;
            }

            var controls = new RosterControls();
            controls.Filter = Button(rosterGo.transform, "FilterButton", "필터 ALL", font, minorButtonSprite, Color.white, new Color32(58, 62, 54, 255));
            Anchored(controls.Filter.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(356f, -68f), new Vector2(224f, 82f), new Vector2(0.5f, 0.5f));
            controls.FilterLabel = controls.Filter.transform.Find("Label").GetComponent<TMP_Text>();
            controls.Sort = Button(rosterGo.transform, "SortButton", "전투력", font, minorButtonSprite, Color.white, new Color32(58, 62, 54, 255));
            Anchored(controls.Sort.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(588f, -68f), new Vector2(224f, 82f), new Vector2(0.5f, 0.5f));
            controls.SortDirection = Button(rosterGo.transform, "SortDirectionButton", string.Empty, font, roundButtonSprite, Color.white, Color.white);
            Anchored(controls.SortDirection.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(812f, -68f), new Vector2(74f, 74f), new Vector2(0.5f, 0.5f));
            controls.SortDirectionLabel = Text(controls.SortDirection.transform, "Direction", "↓", font, 38f, TextAlignmentOptions.Center, new Color32(48, 73, 40, 255));
            Stretch(controls.SortDirectionLabel.rectTransform);

            controls.Start = Button(rosterGo.transform, "StartButton", "출발", font, startButtonSprite, Color.white, new Color32(63, 66, 51, 255));
            Anchored(controls.Start.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0f, 72f), new Vector2(460f, 130f), new Vector2(0.5f, 0.5f));
            controls.Start.transform.Find("Label").GetComponent<TMP_Text>().fontSize = 38f;
            return controls;
        }

        private static Color ColumnColor(string column)
        {
            switch (column)
            {
                case "Front": return new Color32(232, 99, 84, 255);
                case "Middle": return new Color32(76, 150, 224, 255);
                default: return new Color32(118, 78, 205, 255);
            }
        }

        private static string ColumnShortName(string column)
        {
            switch (column)
            {
                case "Front": return "전";
                case "Middle": return "중";
                default: return "후";
            }
        }

        private static Sprite PartySprite(string name) => Require<Sprite>($"{PartyUiRoot}/{name}.png");

        private static TMP_FontAsset EnsureFont()
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (font == null)
                throw new InvalidOperationException($"UI font missing: {FontPath}");
            const string glyphs = "동굴탐험시작전투준비후열중열전열편성된사도침략군카드자동일괄해제되돌리기대여필터빠른출발전투력정렬기준미구현범위입니다가운데위아래순수탱커물리스킬어사이드정보상세보기일반공격저학년고학년확인할수있습니다수치와설명현재복구원본데이터연결전까지검증용으로표시됩니다★←↓↑0123456789,/-ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
            if (!font.TryAddCharacters(glyphs, out var missing))
                throw new InvalidOperationException($"Party font missing glyphs: {missing}");
            EditorUtility.SetDirty(font);
            return font;
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
            text.overflowMode = TextOverflowModes.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private static Button Button(Transform parent, string name, string label, TMP_FontAsset font, Sprite sprite, Color color, Color labelColor)
        {
            var root = RectObject(name, parent, typeof(Image), typeof(Button));
            var image = root.GetComponent<Image>();
            image.color = color;
            image.sprite = sprite;
            image.type = UnityEngine.UI.Image.Type.Sliced;
            var button = root.GetComponent<Button>();
            button.targetGraphic = image;
            if (!string.IsNullOrEmpty(label))
            {
                var text = Text(root.transform, "Label", label, font, 28f, TextAlignmentOptions.Center, labelColor);
                Stretch(text.rectTransform, 5f);
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
            var gameObject = new GameObject(name, all);
            if (parent != null)
                gameObject.transform.SetParent(parent, false);
            return gameObject;
        }

        private static void SavePrefab(GameObject root, string path, bool ready)
        {
            if (!ready)
            {
                Object.DestroyImmediate(root);
                throw new InvalidOperationException($"Generated PartySetup UI is incomplete: {path}");
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

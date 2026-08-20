using System.IO;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using UnityEngine.UI;

using Spine.Unity;
using TrickcalRevive.App.Editor;

namespace TrickcalRevive.MainUI.Tests
{
    public sealed class InvasionPartyUIRecoveryTests
    {
        private static TestRunnerApi testRunnerApi;

        [Test]
        public void PartySetupPrefab_RosterUsesThreeVerticalColumns()
        {
            var prefab = LoadPartyPrefab();
            var roster = prefab.transform.Find("Roster");
            Assert.That(roster, Is.Not.Null);
            var grid = roster.Find("Viewport/Content").GetComponent<GridLayoutGroup>();
            Assert.That(grid.constraint, Is.EqualTo(GridLayoutGroup.Constraint.FixedColumnCount));
            Assert.That(grid.constraintCount, Is.EqualTo(3), "원작 우측 사도 목록은 3열이어야 한다.");
            Assert.That(grid.cellSize.y, Is.GreaterThan(grid.cellSize.x), "원작 사도 카드는 세로형이어야 한다.");
        }

        [Test]
        public void PartySetupPrefab_UsesRecoveredButtonSprites()
        {
            var prefab = LoadPartyPrefab();
            var requiredRecoveredButtons = new[]
            {
                "AutoBuildButton",
                "ResetButton",
                "FilterButton",
                "SortButton",
                "SortDirectionButton",
                "StartButton",
            };

            var requiredNames = requiredRecoveredButtons.ToHashSet();
            var buttons = prefab.GetComponentsInChildren<Button>(true)
                .Where(button => requiredNames.Contains(button.name))
                .ToDictionary(button => button.name);
            foreach (var buttonName in requiredRecoveredButtons)
            {
                Assert.That(buttons.ContainsKey(buttonName), Is.True, $"필수 버튼 누락: {buttonName}");
                var image = buttons[buttonName].targetGraphic as Image;
                Assert.That(image, Is.Not.Null, $"버튼 targetGraphic 누락: {buttonName}");
                Assert.That(image.sprite, Is.Not.Null, $"원본 Sprite 미사용: {buttonName}");
                StringAssert.StartsWith(
                    "Assets/Game/Content/UI/",
                    AssetDatabase.GetAssetPath(image.sprite),
                    $"복구 리소스 경계 밖 Sprite: {buttonName}");
            }

            var requiredRecoveredIcons = new[]
            {
                "ResetButton/ClearIcon",
            };
            foreach (var iconPath in requiredRecoveredIcons)
            {
                var icon = prefab.transform.Find(iconPath)?.GetComponent<Image>();
                Assert.That(icon, Is.Not.Null, $"필수 버튼 아이콘 누락: {iconPath}");
                Assert.That(icon.sprite, Is.Not.Null, $"원본 아이콘 Sprite 미사용: {iconPath}");
                StringAssert.StartsWith(
                    "Assets/Game/Content/UI/PartySetup/",
                    AssetDatabase.GetAssetPath(icon.sprite),
                    $"파티 UI 복구 리소스 경계 밖 아이콘: {iconPath}");
            }

            foreach (var removedButton in new[]
                     {
                         "UndoButton",
                         "Roster/RentButton",
                         "Roster/QuickBattleButton",
                         "Roster/AutoBattleButton",
                     })
            {
                Assert.That(prefab.transform.Find(removedButton), Is.Null, $"제거하기로 한 버튼이 남아 있음: {removedButton}");
            }
        }

        [Test]
        public void PartySetupPrefab_UsesRecoveredFormationTiles()
        {
            var prefab = LoadPartyPrefab();
            var formationCells = prefab.transform.Find("Formation")
                .Cast<Transform>()
                .Where(child => child.name.StartsWith("Cell_"))
                .ToArray();
            Assert.That(formationCells, Has.Length.EqualTo(9));
            Assert.That(
                formationCells.All(cell => cell.GetComponent<Image>().sprite != null),
                Is.True,
                "3×3 진형 셀은 원본 덱 위치 Sprite를 사용해야 한다.");
        }

        [Test]
        public void PartySetupPrefab_MapsRowsAsMiddleThenTopThenBottom()
        {
            var formation = LoadPartyPrefab().transform.Find("Formation");
            var top = formation.Find("Cell_3_1").GetComponent<RectTransform>().anchoredPosition.y;
            var middle = formation.Find("Cell_3_2").GetComponent<RectTransform>().anchoredPosition.y;
            var bottom = formation.Find("Cell_3_3").GetComponent<RectTransform>().anchoredPosition.y;

            Assert.That(top, Is.GreaterThan(middle));
            Assert.That(middle, Is.GreaterThan(bottom));
        }

        [Test]
        public void PartySetupPrefab_AllFormationSpinesAreConfiguredAsDragHandles()
        {
            var prefab = LoadPartyPrefab();
            var handles = prefab.GetComponentsInChildren<FormationSlotDragHandle>(true);
            Assert.That(handles, Has.Length.EqualTo(9));

            foreach (var handle in handles)
            {
                Assert.That(handle.IsReady, Is.True);
                Assert.That(handle.SlotX, Is.InRange(1, 3));
                Assert.That(handle.SlotY, Is.InRange(1, 3));
                Assert.That(handle.GetComponent<SkeletonGraphic>().raycastTarget, Is.True,
                    $"배치 사도 Spine이 드래그 포인터를 받아야 한다: {handle.SlotX},{handle.SlotY}");
            }

            Assert.That(handles.Select(handle => (handle.SlotX, handle.SlotY)).Distinct().Count(), Is.EqualTo(9));
        }

        [Test]
        public void PartySetupPrefab_FormationMatchesRecoveredChevronGrounding()
        {
            var formation = LoadPartyPrefab().transform.Find("Formation");
            var expectedColumnColors = new[]
            {
                new Color32(66, 126, 204, 196),
                new Color32(98, 197, 85, 196),
                new Color32(226, 92, 86, 196),
            };

            for (var x = 1; x <= 3; x++)
            {
                var column = formation.Find($"Column_{x}").GetComponent<Image>();
                var expectedColumnX = new[] { -510f, -80f, 350f }[x - 1];
                Assert.That((Color32)column.color, Is.EqualTo(expectedColumnColors[x - 1]),
                    $"후열·중열·전열 배경은 각각 파랑·초록·빨강이어야 한다: x={x}");
                Assert.That(column.rectTransform.sizeDelta, Is.EqualTo(new Vector2(572f, 480f)));
                Assert.That(column.rectTransform.anchoredPosition, Is.EqualTo(new Vector2(expectedColumnX, 0f)));

                var upper = formation.Find($"Cell_{x}_1").GetComponent<RectTransform>();
                var center = formation.Find($"Cell_{x}_2").GetComponent<RectTransform>();
                var lower = formation.Find($"Cell_{x}_3").GetComponent<RectTransform>();
                Assert.That(upper.anchoredPosition, Is.EqualTo(new Vector2(expectedColumnX - 60f, 150f)));
                Assert.That(center.anchoredPosition, Is.EqualTo(new Vector2(expectedColumnX + 110f, 0f)),
                    $"가운데 자리는 > 형태를 따라 오른쪽으로 돌출되어야 한다: x={x}");
                Assert.That(lower.anchoredPosition, Is.EqualTo(new Vector2(expectedColumnX - 60f, -150f)));

                for (var y = 1; y <= 3; y++)
                {
                    var cell = formation.Find($"Cell_{x}_{y}");
                    Assert.That(cell.GetComponent<RectTransform>().sizeDelta, Is.EqualTo(new Vector2(270f, 90f)));
                    Assert.That((Color32)cell.GetComponent<Image>().color,
                        Is.EqualTo(new Color32(255, 255, 255, 96)),
                        $"원형 장판은 열 색상이 아니라 동일한 흰색이어야 한다: {cell.name}");
                    var viewport = cell.Find("SpineViewport").GetComponent<RectTransform>();
                    Assert.That(viewport.anchoredPosition, Is.EqualTo(Vector2.zero));
                    Assert.That(viewport.pivot, Is.EqualTo(new Vector2(0.5f, 0.5f)));
                    Assert.That(viewport.sizeDelta, Is.EqualTo(new Vector2(100f, 100f)));
                    var spine = cell.Find("SpineViewport/Spine").GetComponent<SkeletonGraphic>();
                    Assert.That(spine.initialFlipX,
                        Is.True, $"배치된 사도는 오른쪽을 바라보도록 반전되어야 한다: {cell.name}");
                    Assert.That(spine.rectTransform.anchoredPosition, Is.EqualTo(Vector2.zero));
                    Assert.That(spine.rectTransform.pivot, Is.EqualTo(new Vector2(0.5f, 0f)));
                    Assert.That(spine.rectTransform.sizeDelta, Is.EqualTo(new Vector2(750f, 1000f)));
                    Assert.That(spine.rectTransform.localScale, Is.EqualTo(Vector3.one * 0.4f));
                }
            }
        }

        [Test]
        public void PartySetupPrefab_AllRosterEntriesHaveNormalSkinAndIdleAnimation()
        {
            var view = LoadPartyPrefab().GetComponent<PartySetupView>();
            var serialized = new SerializedObject(view);
            var skeletons = serialized.FindProperty("apostleSkeletons");

            Assert.That(skeletons, Is.Not.Null);
            Assert.That(skeletons.arraySize, Is.EqualTo(30));
            for (var index = 0; index < skeletons.arraySize; index++)
            {
                var asset = skeletons.GetArrayElementAtIndex(index).objectReferenceValue as SkeletonDataAsset;
                Assert.That(asset, Is.Not.Null, $"InGame SkeletonData 누락: index={index}");
                var data = asset.GetSkeletonData(true);
                Assert.That(data.FindSkin("Normal"), Is.Not.Null, $"Normal skin 누락: {asset.name}");
                Assert.That(data.FindAnimation("Idle"), Is.Not.Null, $"Idle animation 누락: {asset.name}");
            }
        }

        [Test]
        public void StageSelectPrefab_SwitchesAcrossTenRecoveredWorlds()
        {
            var prefab = LoadStagePrefab();
            var serialized = new SerializedObject(prefab.GetComponent<StageSelectView>());
            var worldBackgrounds = serialized.FindProperty("worldBackgrounds");

            Assert.That(worldBackgrounds, Is.Not.Null, "월드별 배경 직렬화 배열이 필요하다.");
            Assert.That(worldBackgrounds.arraySize, Is.EqualTo(10), "침략 월드 1~10 배경을 모두 연결해야 한다.");
            for (var index = 0; index < worldBackgrounds.arraySize; index++)
            {
                var sprite = worldBackgrounds.GetArrayElementAtIndex(index).objectReferenceValue as Sprite;
                Assert.That(sprite, Is.Not.Null, $"월드 {index + 1} 배경 누락");
                StringAssert.StartsWith(
                    "Assets/Game/Content/UI/StageSelect/",
                    AssetDatabase.GetAssetPath(sprite),
                    $"월드 {index + 1} 배경은 복구 경계 안에 있어야 한다.");
            }

            Assert.That(prefab.transform.Find("PreviousWorldButton"), Is.Not.Null);
            Assert.That(prefab.transform.Find("NextWorldButton"), Is.Not.Null);
            Assert.That(
                prefab.transform.Find("NextWorldButton/Icon").GetComponent<Image>().sprite.name,
                Is.EqualTo("Common_Arrow_Stretch"));

            var instance = Object.Instantiate(prefab);
            try
            {
                instance.SetActive(false);
                instance.SetActive(true);
                var view = instance.GetComponent<StageSelectView>();
                var next = instance.transform.Find("NextWorldButton").GetComponent<Button>();
                Assert.That(next.onClick.GetPersistentEventCount(), Is.EqualTo(1));
                Assert.That(next.onClick.GetPersistentMethodName(0), Is.EqualTo("ShowNextWorld"));
                var background = instance.transform.Find("MapBackground").GetComponent<Image>();
                var before = background.sprite;
                view.ShowNextWorld();

                Assert.That(background.sprite, Is.Not.SameAs(before));
                Assert.That(
                    instance.transform.Find("NodesScroll/Viewport/Content/Node0/NumberPlate/Number")
                        .GetComponent<TMP_Text>().text,
                    Is.EqualTo("2 - 1"));
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void PartySetupPrefab_UsesRecoveredStageOneOneBackgroundAndRosterSearchButtons()
        {
            var prefab = LoadPartyPrefab();
            var background = prefab.transform.Find("BattleBackground");
            Assert.That(background, Is.Not.Null, "1-1 전투 배경 레이어 루트가 필요하다.");

            var layers = background.Cast<Transform>().ToArray();
            Assert.That(layers, Has.Length.EqualTo(5), "BG_Stage1_1 원본은 5개 레이어로 구성된다.");
            foreach (var layer in layers)
            {
                var image = layer.GetComponent<Image>();
                Assert.That(image, Is.Not.Null, layer.name);
                Assert.That(image.sprite, Is.Not.Null, layer.name);
                StringAssert.StartsWith(
                    "Assets/Game/Content/UI/PartySetup/BG_Stage1_1_",
                    AssetDatabase.GetAssetPath(image.sprite).Replace('\\', '/'));
            }

            var searchButtons = prefab.transform.Find("Roster/Viewport/Content")
                .GetComponentsInChildren<Button>(true)
                .Where(button => button.name == "SearchButton")
                .ToArray();
            Assert.That(searchButtons, Has.Length.EqualTo(30), "모든 사도 카드에 상세보기 돋보기가 필요하다.");
            foreach (var searchButton in searchButtons)
            {
                Assert.That(searchButton.targetGraphic.GetComponent<Image>().sprite.name, Is.EqualTo("CommonCircle_1"));
                Assert.That(searchButton.transform.Find("Icon").GetComponent<Image>().sprite.name, Is.EqualTo("Common_Icon_Search"));
            }
        }

        [Test]
        public void PartySetupCharacterDetail_OpensAsCenteredBouncyModal()
        {
            var prefab = LoadPartyPrefab();
            var modal = prefab.transform.Find("CharacterDetailModal");
            Assert.That(modal, Is.Not.Null);
            Assert.That(modal.gameObject.activeSelf, Is.False, "상세 팝업은 기본적으로 닫혀 있어야 한다.");
            Assert.That(modal.Find("Backdrop"), Is.Not.Null);

            var panel = modal.Find("Panel").GetComponent<RectTransform>();
            Assert.That(panel, Is.Not.Null);
            Assert.That(panel.anchoredPosition, Is.EqualTo(Vector2.zero));
            Assert.That(
                panel.GetComponents<MonoBehaviour>().Any(component => component.GetType().Name == "PopupBounceView"),
                Is.True,
                "중앙 팝업에는 뽀잉 등장 애니메이션 View가 필요하다.");

            Assert.That(panel.Find("Header/Name"), Is.Not.Null);
            Assert.That(panel.Find("Upper/UnitSlot/Portrait"), Is.Not.Null);
            Assert.That(panel.Find("Upper/Information/Personality"), Is.Not.Null);
            Assert.That(panel.Find("TabGroup/SkillTab"), Is.Not.Null);
            Assert.That(panel.Find("Skill/SkillList"), Is.Not.Null);

            var instance = Object.Instantiate(prefab);
            try
            {
                instance.SetActive(false);
                instance.SetActive(true);
                var instanceModal = instance.transform.Find("CharacterDetailModal");
                var search = instance.transform.Find("Roster/Viewport/Content/Card_maison/SearchButton")
                    .GetComponent<Button>();
                Assert.That(search.onClick.GetPersistentEventCount(), Is.EqualTo(1));
                Assert.That(search.onClick.GetPersistentMethodName(0), Is.EqualTo("ShowCharacterDetail"));
                instance.GetComponent<PartySetupView>().ShowCharacterDetail(0);
                Assert.That(instanceModal.gameObject.activeSelf, Is.True);
                Assert.That(instanceModal.Find("Panel/Header/Name").GetComponent<TMP_Text>().text, Is.Not.Empty);
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void StageSelectPrefab_UsesWorldOneBackgroundAndBlueNodeComposition()
        {
            var prefab = LoadStagePrefab();
            var background = prefab.transform.Find("MapBackground").GetComponent<Image>();
            Assert.That(background.preserveAspect, Is.True);
            AssertRect(background.rectTransform, new Vector2(8192f, 4096f), new Vector2(-150f, 90f));
            StringAssert.EndsWith(
                "/Stage_fairy_c.png",
                AssetDatabase.GetAssetPath(background.sprite).Replace('\\', '/'));

            var scroll = prefab.transform.Find("NodesScroll").GetComponent<ScrollRect>();
            Assert.That(scroll.horizontal, Is.True);
            Assert.That(scroll.vertical, Is.True);
            Assert.That(scroll.content.sizeDelta, Is.EqualTo(new Vector2(8192f, 4096f)));

            for (var index = 0; index < 10; index++)
            {
                var positionedNode = scroll.content.Find($"Node{index}").GetComponent<RectTransform>();
                Assert.That(positionedNode.anchoredPosition.x, Is.EqualTo(340f + index * 375f).Within(0.01f));
                Assert.That(positionedNode.anchoredPosition.y, Is.EqualTo(index % 2 == 0 ? -150f : 120f).Within(0.01f));
            }

            Assert.That(prefab.GetComponentsInChildren<StageNodeView>(true), Has.Length.EqualTo(10));

            var nodeTransform = prefab.transform.Find("NodesScroll/Viewport/Content/Node0");
            Assert.That(nodeTransform, Is.Not.Null);
            var node = nodeTransform.GetComponent<StageNodeView>();
            Assert.That(node, Is.Not.Null);
            Assert.That(node.IsReady, Is.True);

            var serialized = new SerializedObject(node);
            AssertSpritePath(serialized, "currentSprite", "StageTile_Blue_UnClear.png");
            AssertSpritePath(serialized, "clearedSprite", "StageTile_Blue_Clear.png");
            AssertSpritePath(serialized, "unclearedSprite", "StageTile_Blue_UnClear.png");
            AssertSpritePath(serialized, "lockedSprite", "StageTile_Brown_UnClear.png");
            AssertSpritePath(serialized, "filledStarSprite", "Star_Clear.png");
            AssertSpritePath(serialized, "emptyStarSprite", "Star_Easy_Off.png");

            AssertRect(nodeTransform.Find("Tile").GetComponent<RectTransform>(), new Vector2(615f, 485f), Vector2.zero);
            AssertRect(nodeTransform.Find("Garland").GetComponent<RectTransform>(), new Vector2(588f, 300f), new Vector2(0f, -83f));

            var mapDeco = nodeTransform.Find("MapDeco").GetComponent<Image>();
            Assert.That(mapDeco, Is.Not.Null);
            StringAssert.EndsWith(
                "/StageTile_MapDeco.png",
                AssetDatabase.GetAssetPath(mapDeco.sprite).Replace('\\', '/'));
            AssertRect(mapDeco.rectTransform, new Vector2(615f, 485f), Vector2.zero);
            Assert.That(mapDeco.transform.GetSiblingIndex(), Is.LessThan(nodeTransform.Find("Tower").GetSiblingIndex()));

            var plate = nodeTransform.Find("NumberPlate").GetComponent<Image>();
            Assert.That(plate, Is.Not.Null);
            StringAssert.EndsWith(
                "/Stage_TileLevel_Easy.png",
                AssetDatabase.GetAssetPath(plate.sprite).Replace('\\', '/'));
            Assert.That(plate.type, Is.EqualTo(Image.Type.Sliced));
            Assert.That(plate.preserveAspect, Is.False);
            AssertRect(plate.rectTransform, new Vector2(230f, 155f), new Vector2(0f, -110f));
            var nodeZeroStarPositions = new Vector2[3];
            for (var index = 0; index < 3; index++)
            {
                var star = plate.transform.Find($"Star{index}").GetComponent<RectTransform>();
                AssertRect(star, new Vector2(46f, 46f), new Vector2(-40f + index * 40f, 40f));
                nodeZeroStarPositions[index] = star.anchoredPosition;
            }

            for (var nodeIndex = 1; nodeIndex < 10; nodeIndex++)
            {
                var otherPlate = scroll.content.Find($"Node{nodeIndex}/NumberPlate");
                Assert.That(otherPlate, Is.Not.Null, $"Node{nodeIndex}/NumberPlate");
                for (var starIndex = 0; starIndex < 3; starIndex++)
                {
                    var otherStar = otherPlate.Find($"Star{starIndex}").GetComponent<RectTransform>();
                    AssertRect(otherStar, new Vector2(46f, 46f), nodeZeroStarPositions[starIndex]);
                }
            }

            var number = plate.transform.Find("Number").GetComponent<TMP_Text>();
            AssertRect(number.rectTransform, new Vector2(230f, 50f), new Vector2(0f, -12f));
            Assert.That(number.text, Is.EqualTo("1 - 1"));
            Assert.That(number.fontSize, Is.EqualTo(40f).Within(0.01f));
            Assert.That(number.fontStyle, Is.EqualTo(FontStyles.Normal));

            var tower = nodeTransform.Find("Tower").GetComponent<RectTransform>();
            AssertRect(tower, new Vector2(265f, 265f), new Vector2(0f, 100f));
            AssertRect(nodeTransform.Find("Lock").GetComponent<RectTransform>(), new Vector2(120f, 148f), new Vector2(0f, 50f));
        }

        [Test]
        public void StageNodeView_LockedStateDimsContentAndKeepsLockBright()
        {
            var instance = Object.Instantiate(LoadStagePrefab());
            try
            {
                var nodeTransform = instance.transform.Find("NodesScroll/Viewport/Content/Node0");
                var node = nodeTransform.GetComponent<StageNodeView>();
                var tile = nodeTransform.Find("Tile").GetComponent<Image>();
                var tower = nodeTransform.Find("Tower").gameObject;
                var mapDeco = nodeTransform.Find("MapDeco").gameObject;
                var plate = nodeTransform.Find("NumberPlate").GetComponent<Image>();
                var number = nodeTransform.Find("NumberPlate/Number").GetComponent<TMP_Text>();
                var lockIcon = nodeTransform.Find("Lock").GetComponent<Image>();
                var button = nodeTransform.GetComponent<Button>();

                node.Render(new StageNodeData
                {
                    StageId = "1-4",
                    Label = "1-4",
                    State = StageNodeState.Locked,
                    Stars = 0,
                });

                Assert.That(tower.activeSelf, Is.False);
                Assert.That(mapDeco.activeSelf, Is.False);
                Assert.That(lockIcon.gameObject.activeSelf, Is.True);
                Assert.That(lockIcon.color, Is.EqualTo(Color.white));
                Assert.That(tile.color.r, Is.LessThan(0.7f));
                Assert.That(plate.color.r, Is.LessThan(0.7f));
                Assert.That(button.interactable, Is.False);

                node.Render(new StageNodeData
                {
                    StageId = "1-2",
                    Label = "1-2",
                    State = StageNodeState.Current,
                    Stars = 0,
                });

                Assert.That(tower.activeSelf, Is.False, "0별 미클리어 노드에는 성을 표시하지 않아야 한다.");
                Assert.That(mapDeco.activeSelf, Is.False);
                Assert.That(number.text, Is.EqualTo("1 - 2"));
                Assert.That(lockIcon.gameObject.activeSelf, Is.False);
                Assert.That(tile.color, Is.EqualTo(Color.white));
                Assert.That(plate.color, Is.EqualTo(Color.white));
                Assert.That(button.interactable, Is.True);
                var emptyStar = nodeTransform.Find("NumberPlate/Star0").GetComponent<Image>();
                Assert.That(emptyStar.sprite.name, Is.EqualTo("Star_Easy_Off"));
                Assert.That(emptyStar.color, Is.EqualTo(Color.white));

                node.Render(new StageNodeData
                {
                    StageId = "1-1",
                    Label = "1-1",
                    State = StageNodeState.Cleared,
                    Stars = 3,
                });

                Assert.That(tower.activeSelf, Is.True);
                Assert.That(mapDeco.activeSelf, Is.True);
                Assert.That(number.text, Is.EqualTo("1 - 1"));
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void StageSelectBackground_PansOnBothAxes()
        {
            var instance = Object.Instantiate(LoadStagePrefab());
            try
            {
                var background = instance.transform.Find("MapBackground").GetComponent<RectTransform>();
                var scroll = instance.transform.Find("NodesScroll").GetComponent<ScrollRect>();
                var pan = instance.GetComponent<ScrollBackgroundPan>();
                var baseline = background.anchoredPosition;
                pan.Configure(scroll, background);

                scroll.content.anchoredPosition = new Vector2(-240f, 180f);
                pan.RefreshPosition();

                Assert.That(background.anchoredPosition, Is.EqualTo(baseline + new Vector2(-240f, 180f)));
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void StageSelectViewport_FillsScreenAndContentMatchesMapBounds()
        {
            var prefab = LoadStagePrefab();
            var scroll = prefab.transform.Find("NodesScroll").GetComponent<ScrollRect>();
            var scrollRect = scroll.GetComponent<RectTransform>();
            var viewport = scroll.viewport;

            AssertStretch(scrollRect, "노드 스크롤 영역은 화면 전체여야 한다.");
            AssertStretch(viewport, "노드 마스크 영역은 화면 전체여야 한다.");
            Assert.That(scroll.content.sizeDelta, Is.EqualTo(new Vector2(8192f, 4096f)));
            Assert.That(
                scroll.transform.GetSiblingIndex(),
                Is.LessThan(prefab.transform.Find("WorldMapButton").GetSiblingIndex()),
                "전체 화면 드래그 영역은 정적 UI 버튼의 입력을 가로채면 안 된다.");
        }

        [Test]
        public void StageInfoPopup_DefaultPanelDoesNotDimStageMap()
        {
            var prefab = LoadStageInfoPrefab();
            var content = prefab.transform.Find("Content");

            Assert.That(content, Is.Not.Null);
            Assert.That(
                content.Find("Backdrop"),
                Is.Null,
                "검은 Backdrop은 추천 성격/몬스터 상세 같은 2차 팝업에서만 생성해야 한다.");
        }

        [Test]
        public void StageInfoPopup_UsesRecoveredRightPanelComposition()
        {
            var prefab = LoadStageInfoPrefab();
            var panel = prefab.transform.Find("Content/Panel").GetComponent<RectTransform>();
            AssertRect(panel, new Vector2(960f, 1040f), new Vector2(-500f, 0f));

            AssertImageSprite(panel.Find("DifficultyFrame"), "Stage_Level_Easy.png");
            AssertImageSprite(panel.Find("Body"), "Stage_MonsterInfoBg.png");
            AssertImageSprite(panel.Find("HeaderGroup/HeaderBase"), "Stage_Level_Easy.png");
            AssertImageSprite(panel.Find("HeaderGroup/HeaderDeco"), "StagePopup_HeaderDeco.png");
            AssertImageSprite(panel.Find("Personality/Icon"), "Common_UnitPersonality_Naive.png");
            AssertImageSprite(panel.Find("Enemy/Icon"), "CommonIcon_Enemy.png");

            Assert.That(panel.Find("Reward"), Is.Not.Null);
            Assert.That(panel.Find("ButtonGroup"), Is.Not.Null);
            Assert.That(panel.Find("Divider"), Is.Not.Null);
        }

        [Test]
        public void StageInfoPopup_MapsPersonalityTextToRecoveredIcon()
        {
            var instance = Object.Instantiate(LoadStageInfoPrefab());
            try
            {
                var view = instance.GetComponent<StageInfoPopupView>();
                view.Show(new StageInfoData
                {
                    StageName = "검증",
                    PersonalityText = "우울",
                });

                var icon = instance.transform.Find("Content/Panel/Personality/Icon").GetComponent<Image>();
                Assert.That(icon.sprite.name, Is.EqualTo("Common_UnitPersonality_Gloomy"));
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        private static GameObject LoadPartyPrefab()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PartySetupUIBuilder.PartyPrefabPath);
            Assert.That(prefab, Is.Not.Null, PartySetupUIBuilder.PartyPrefabPath);
            return prefab;
        }

        private static GameObject LoadStagePrefab()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(StageSelectUIBuilder.StageSelectPrefabPath);
            Assert.That(prefab, Is.Not.Null, StageSelectUIBuilder.StageSelectPrefabPath);
            return prefab;
        }

        private static GameObject LoadStageInfoPrefab()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(StageInfoPopupUIBuilder.PopupPrefabPath);
            Assert.That(prefab, Is.Not.Null, StageInfoPopupUIBuilder.PopupPrefabPath);
            return prefab;
        }

        private static void AssertSpritePath(SerializedObject serialized, string propertyName, string fileName)
        {
            var property = serialized.FindProperty(propertyName);
            Assert.That(property, Is.Not.Null, propertyName);
            var sprite = property.objectReferenceValue as Sprite;
            Assert.That(sprite, Is.Not.Null, propertyName);
            StringAssert.EndsWith(
                "/" + fileName,
                AssetDatabase.GetAssetPath(sprite).Replace('\\', '/'),
                propertyName);
        }

        private static void AssertImageSprite(Transform transform, string fileName)
        {
            Assert.That(transform, Is.Not.Null, fileName);
            var image = transform.GetComponent<Image>();
            Assert.That(image, Is.Not.Null, fileName);
            Assert.That(image.sprite, Is.Not.Null, fileName);
            StringAssert.EndsWith(
                "/" + fileName,
                AssetDatabase.GetAssetPath(image.sprite).Replace('\\', '/'));
        }

        private static void AssertRect(RectTransform rect, Vector2 size, Vector2 position)
        {
            Assert.That(rect, Is.Not.Null);
            Assert.That(rect.sizeDelta.x, Is.EqualTo(size.x).Within(0.01f));
            Assert.That(rect.sizeDelta.y, Is.EqualTo(size.y).Within(0.01f));
            Assert.That(rect.anchoredPosition.x, Is.EqualTo(position.x).Within(0.01f));
            Assert.That(rect.anchoredPosition.y, Is.EqualTo(position.y).Within(0.01f));
        }

        private static void AssertStretch(RectTransform rect, string message)
        {
            Assert.That(rect, Is.Not.Null, message);
            Assert.That(rect.anchorMin, Is.EqualTo(Vector2.zero), message);
            Assert.That(rect.anchorMax, Is.EqualTo(Vector2.one), message);
            Assert.That(rect.offsetMin, Is.EqualTo(Vector2.zero), message);
            Assert.That(rect.offsetMax, Is.EqualTo(Vector2.zero), message);
        }

        [MenuItem("Tools/Trickcal Revive/Tests/Run MainUI EditMode Tests %#F9")]
        public static void RunMainUIEditModeTests()
        {
            var resultPath = Path.GetFullPath(Path.Combine(
                Application.dataPath,
                "..",
                "Artifacts",
                "SpineRecovery",
                "StageSelect",
                "mainui-editmode-results.txt"));
            Directory.CreateDirectory(Path.GetDirectoryName(resultPath));

            testRunnerApi = ScriptableObject.CreateInstance<TestRunnerApi>();
            testRunnerApi.RegisterCallbacks(new ResultWriter(resultPath));
            testRunnerApi.Execute(new ExecutionSettings(new Filter
            {
                testMode = TestMode.EditMode,
                assemblyNames = new[] { "TrickcalRevive.MainUI.EditModeTests" },
            }));
        }

        private sealed class ResultWriter : ICallbacks
        {
            private readonly string resultPath;

            public ResultWriter(string resultPath)
            {
                this.resultPath = resultPath;
            }

            public void RunStarted(ITestAdaptor testsToRun)
            {
                Debug.Log($"Running {testsToRun.TestCaseCount} MainUI EditMode tests.");
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                File.WriteAllText(
                    resultPath,
                    $"status={result.TestStatus}\npassed={result.PassCount}\nfailed={result.FailCount}\n"
                    + $"skipped={result.SkipCount}\ninconclusive={result.InconclusiveCount}\n"
                    + $"durationSeconds={result.Duration:F3}\n");
                Debug.Log($"MainUI EditMode tests: {result.TestStatus} ({result.PassCount} passed, {result.FailCount} failed). Results: {resultPath}");
            }

            public void TestStarted(ITestAdaptor test)
            {
            }

            public void TestFinished(ITestResultAdaptor result)
            {
                if (result.TestStatus == TestStatus.Failed)
                    Debug.LogError($"MainUI EditMode test failed: {result.FullName}\n{result.Message}\n{result.StackTrace}");
            }
        }
    }
}

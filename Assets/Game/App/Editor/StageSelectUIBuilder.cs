using System;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;

using TrickcalRevive.MainUI;

using Object = UnityEngine.Object;

namespace TrickcalRevive.App.Editor
{
    /// <summary>
    /// 침략 스테이지선택 화면(StageSelectScreen.prefab)을 코드로 생성한다. 로비 빌더와
    /// 같은 방식(Canvas 하위 코드 배치 + View.Configure + IsReady 검사). 2560x1440 기준.
    /// 노드 좌표는 원본 정확좌표 미확보 = 추론 배치.
    /// </summary>
    public static class StageSelectUIBuilder
    {
        public const string StageSelectPrefabPath = "Assets/Game/MainUI/Prefabs/StageSelectScreen.prefab";
        private const string Root = "Assets/Game/Content/UI/StageSelect";
        private const string FontPath = "Assets/Game/Content/UI/Fonts/ONE Mobile POP SDF.asset";

        // 8192x4096 맵 좌표계 안에 배치한다.
        private const int NodeCount = 10;
        private const float NodePitch = 375f;
        private const float NodePadX = 340f;
        private static readonly Vector2 MapSize = new Vector2(8192f, 4096f);

        [MenuItem("Tools/Trickcal Revive/Flow/Build Stage-Select UI Prefab %#F10")]
        public static void BuildPrefab()
        {
            EnsureFolder("Assets/Game/MainUI/Prefabs");
            var font = EnsureFont();

            // 월드 1 기준 이미지와 픽셀 패턴이 일치하는 야간 요정 맵.
            // 타일 색은 진행 상태가 아니라 월드 테마를 따른다. 월드 1은 Blue이며,
            // 잠긴 노드만 Brown unavailable 변형을 사용한다.
            var worldBackgrounds = LoadWorldBackgrounds();
            var background = worldBackgrounds[0];
            var tileCurrent = Require<Sprite>(Root + "/StageTile_Blue_UnClear.png");
            var tileCleared = Require<Sprite>(Root + "/StageTile_Blue_Clear.png");
            var tileUncleared = Require<Sprite>(Root + "/StageTile_Blue_UnClear.png");
            var tileLockedSprite = Require<Sprite>(Root + "/StageTile_Brown_UnClear.png");
            var lockSprite = Require<Sprite>(Root + "/Stage_TileLock.png");
            var starFilled = Require<Sprite>(Root + "/Star_Clear.png");
            var starEmpty = Require<Sprite>(Root + "/Star_Easy_Off.png");
            var bannerSprite = Require<Sprite>(Root + "/Stage_TileLevel_Easy.png");
            var mapDecoSprite = Require<Sprite>(Root + "/StageTile_MapDeco.png");
            var towerSprite = Require<Sprite>(Root + "/Object_3.png");
            var garlandSprite = Require<Sprite>(Root + "/Stage_Garland.png");
            var compass = Require<Sprite>(Root + "/Stage_WorldMapBtn.png");
            var mild = Require<Sprite>(Root + "/WorldListTitleBase_Mild.png");
            var hot = Require<Sprite>(Root + "/WorldListTitleBase_Hot.png");
            var fire = Require<Sprite>(Root + "/WorldListTitleBase_Fire.png");
            var worldArrow = Require<Sprite>(Root + "/Common_Arrow_Stretch.png");

            var blueCurrent = Require<Sprite>(Root + "/StageTile_Blue_UnClear.png");
            var blueClear = Require<Sprite>(Root + "/StageTile_Blue_Clear.png");
            var greenCurrent = Require<Sprite>(Root + "/StageTile_Green_UnClear.png");
            var greenClear = Require<Sprite>(Root + "/StageTile_Green_Clear.png");
            var brownCurrent = Require<Sprite>(Root + "/StageTile_Brown_UnClear.png");
            var brownClear = Require<Sprite>(Root + "/StageTile_Brown_Clear.png");
            var yellowCurrent = Require<Sprite>(Root + "/StageTile_Yellow_UnClear.png");
            var yellowClear = Require<Sprite>(Root + "/StageTile_Yellow_Clear.png");
            var worldCurrentTiles = new[]
            {
                blueCurrent, greenCurrent, brownCurrent, greenCurrent, brownCurrent,
                yellowCurrent, brownCurrent, yellowCurrent, greenCurrent, blueCurrent,
            };
            var worldClearedTiles = new[]
            {
                blueClear, greenClear, brownClear, greenClear, brownClear,
                yellowClear, brownClear, yellowClear, greenClear, blueClear,
            };
            var worldUnclearedTiles = (Sprite[])worldCurrentTiles.Clone();
            var worldLockedTiles = (Sprite[])worldCurrentTiles.Clone();
            worldLockedTiles[0] = tileLockedSprite;

            var root = RectObject("StageSelectScreen", null);
            Stretch((RectTransform)root.transform);

            // 맵 배경은 화면보다 크게(오버사이즈) — ScrollBackgroundPan이 스크롤과 함께 팬한다.
            var bg = Image(root.transform, "MapBackground", Color.white, background);
            Anchored(bg.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-150f, 90f), MapSize);
            bg.raycastTarget = false;
            bg.preserveAspect = true;

            // world-map compass (bottom-right)
            var worldMap = Button(root.transform, "WorldMapButton", string.Empty, font, Color.white, compass);
            Anchored(worldMap.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(-150f, 90f), new Vector2(150f, 150f), new Vector2(1f, 0f));

            // difficulty tabs (bottom-center, static — Mild active). fixture는 순한맛만 데이터 있음.
            DifficultyTab(root.transform, "Tab_Mild", "순한맛", mild, font, -320f, true);
            DifficultyTab(root.transform, "Tab_Hot", "매운맛", hot, font, 0f, false);
            DifficultyTab(root.transform, "Tab_Fire", "핵불맛", fire, font, 320f, false);

            var previousWorld = WorldNavigationButton(root.transform, "PreviousWorldButton", "이전 월드", worldArrow, font, false);
            var nextWorld = WorldNavigationButton(root.transform, "NextWorldButton", "다음 월드", worldArrow, font, true);

            // nodes — 맵 ScrollRect 안에 지정된 2단 좌표로 배치한다.
            var scroll = BuildNodeScroll(root.transform);
            var content = (RectTransform)scroll.content;
            var nodeViews = new StageNodeView[NodeCount];
            for (var i = 0; i < NodeCount; i++)
            {
                var x = NodePadX + i * NodePitch;
                var y = (i % 2 == 0) ? -150f : 120f;
                nodeViews[i] = BuildNode(
                    content, i, new Vector2(x, y), font,
                    tileCurrent, tileCleared, tileUncleared, tileLockedSprite, lockSprite,
                    starFilled, starEmpty, bannerSprite, mapDecoSprite, towerSprite, garlandSprite);
            }
            content.sizeDelta = MapSize;

            // 전체 화면 드래그 영역은 정적 하단/우측 버튼보다 뒤에 둬 버튼 입력을 가로채지 않는다.
            scroll.transform.SetSiblingIndex(1);

            // 배경을 스크롤과 함께 팬(원본처럼 맵이 타일과 같이 움직임).
            var pan = root.AddComponent<ScrollBackgroundPan>();
            pan.Configure(scroll, bg.rectTransform);

            var view = root.AddComponent<StageSelectView>();
            view.Configure(
                bg,
                nodeViews,
                worldMap,
                previousWorld,
                nextWorld,
                scroll,
                worldBackgrounds,
                worldCurrentTiles,
                worldClearedTiles,
                worldUnclearedTiles,
                worldLockedTiles);
            UnityEventTools.AddPersistentListener(previousWorld.onClick, view.ShowPreviousWorld);
            UnityEventTools.AddPersistentListener(nextWorld.onClick, view.ShowNextWorld);

            SavePrefab(root, StageSelectPrefabPath, view.IsReady);
            AssetDatabase.SaveAssets();
            Debug.Log("StageSelect UI prefab built.");
        }

        private static StageNodeView BuildNode(
            Transform parent,
            int index,
            Vector2 position,
            TMP_FontAsset font,
            Sprite current,
            Sprite cleared,
            Sprite uncleared,
            Sprite locked,
            Sprite lockSprite,
            Sprite starFilled,
            Sprite starEmpty,
            Sprite bannerSprite,
            Sprite mapDecoSprite,
            Sprite towerSprite,
            Sprite garlandSprite)
        {
            var nodeRoot = RectObject($"Node{index}", parent, typeof(Image), typeof(Button));
            var hit = nodeRoot.GetComponent<Image>();
            hit.color = new Color(1f, 1f, 1f, 0.002f);
            hit.raycastTarget = true;
            var button = nodeRoot.GetComponent<Button>();
            button.targetGraphic = hit;
            button.transition = Selectable.Transition.None;
            // 콘텐츠 좌측 기준(0,0.5)으로 x를 잰다(가로 스크롤).
            Anchored((RectTransform)nodeRoot.transform, new Vector2(0f, 0.5f), position, new Vector2(640f, 505f));

            var tile = Image(nodeRoot.transform, "Tile", Color.white, current);
            Anchored(tile.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(615f, 485f));
            tile.preserveAspect = true;
            tile.raycastTarget = false;

            // 원본 블록 하단의 삼각 깃발 장식.
            var garland = Image(nodeRoot.transform, "Garland", Color.white, garlandSprite);
            Anchored(garland.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -83f), new Vector2(588f, 300f));
            garland.preserveAspect = true;
            garland.raycastTarget = false;

            // 원본 Sprite가 이미 낮은 알파를 포함한다. 클리어 상태에서만 Tile 위, Tower 아래에 표시한다.
            var mapDeco = Image(nodeRoot.transform, "MapDeco", Color.white, mapDecoSprite);
            Anchored(mapDeco.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(615f, 485f));
            mapDeco.preserveAspect = true;
            mapDeco.raycastTarget = false;

            // 초록 성(Object_3) — 별이 하나 이상인 해금 노드에 표시한다.
            var tower = Image(nodeRoot.transform, "Tower", Color.white, towerSprite);
            Anchored(tower.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 100f), new Vector2(265f, 265f));
            tower.preserveAspect = true;
            tower.raycastTarget = false;

            // 별과 번호를 녹색 팻말 안에 묶어 해상도별 간격을 고정한다.
            var plate = Image(nodeRoot.transform, "NumberPlate", Color.white, bannerSprite);
            Anchored(plate.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -110f), new Vector2(230f, 155f));
            plate.type = UnityEngine.UI.Image.Type.Sliced;
            plate.preserveAspect = false;
            plate.raycastTarget = false;
            var stars = new Image[3];
            for (var s = 0; s < 3; s++)
            {
                var star = Image(plate.transform, $"Star{s}", Color.white, starFilled);
                Anchored(star.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-40f + s * 40f, 40f), new Vector2(46f, 46f));
                star.preserveAspect = true;
                star.raycastTarget = false;
                stars[s] = star;
            }

            var number = Text(plate.transform, "Number", "1 - 1", font, 40f, TextAlignmentOptions.Center, Color.white);
            Anchored(number.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -12f), new Vector2(230f, 50f));
            number.fontStyle = FontStyles.Normal;
            RecoveredTextStyles.Apply(number, RecoveredTextStyle.StageNumber);

            // 잠금은 음영 처리된 노드 위에서 선명하도록 마지막 레이어에 둔다.
            var lockIcon = Image(nodeRoot.transform, "Lock", Color.white, lockSprite);
            Anchored(lockIcon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 50f), new Vector2(120f, 148f));
            lockIcon.preserveAspect = true;
            lockIcon.raycastTarget = false;

            var node = nodeRoot.AddComponent<StageNodeView>();
            node.Configure(tile, tower, garland, mapDeco, plate, lockIcon, stars, number, button, current, cleared, uncleared, locked, starFilled, starEmpty);
            return node;
        }

        // 노드와 맵을 담는 양방향 ScrollRect를 반환한다(content는 scroll.content).
        private static ScrollRect BuildNodeScroll(Transform parent)
        {
            var scrollGo = RectObject("NodesScroll", parent, typeof(Image), typeof(ScrollRect));
            scrollGo.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.002f);
            var scrollRect = (RectTransform)scrollGo.transform;
            Stretch(scrollRect);
            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.horizontal = true;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 40f;

            var viewportGo = RectObject("Viewport", scrollGo.transform, typeof(RectMask2D), typeof(Image));
            viewportGo.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.002f);
            var viewport = (RectTransform)viewportGo.transform;
            SetRect(viewport, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var contentGo = RectObject("Content", viewportGo.transform);
            var content = (RectTransform)contentGo.transform;
            content.anchorMin = new Vector2(0f, 0.5f);
            content.anchorMax = new Vector2(0f, 0.5f);
            content.pivot = new Vector2(0f, 0.5f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = MapSize;

            scroll.viewport = viewport;
            scroll.content = content;
            return scroll;
        }

        private static void DifficultyTab(Transform parent, string name, string label, Sprite baseSprite, TMP_FontAsset font, float x, bool active)
        {
            var tab = Image(parent, name, active ? Color.white : new Color(1f, 1f, 1f, 0.6f), baseSprite);
            Anchored(tab.rectTransform, new Vector2(0.5f, 0f), new Vector2(x, 70f), new Vector2(300f, 96f), new Vector2(0.5f, 0f));
            tab.preserveAspect = true;
            tab.raycastTarget = false;
            var text = Text(tab.transform, "Label", label, font, 32f, TextAlignmentOptions.Center, new Color32(47, 45, 47, 255));
            Stretch(text.rectTransform);
            text.fontStyle = FontStyles.Bold;
        }

        private static Sprite[] LoadWorldBackgrounds()
        {
            var names = new[]
            {
                "Stage_fairy_c", "Stage_furry_a", "Stage_elf_d", "Stage_soul_a", "Stage_ghost_a",
                "Stage_furry_b", "Stage_ghost_c", "Stage_ghost_b", "Stage_elf_a", "Stage_elf_c",
            };
            var sprites = new Sprite[names.Length];
            for (var index = 0; index < names.Length; index++)
                sprites[index] = Require<Sprite>($"{Root}/{names[index]}.png");
            return sprites;
        }

        private static Button WorldNavigationButton(
            Transform parent,
            string name,
            string label,
            Sprite sprite,
            TMP_FontAsset font,
            bool right)
        {
            var buttonRoot = RectObject(name, parent, typeof(Image), typeof(Button));
            var rect = (RectTransform)buttonRoot.transform;
            Anchored(
                rect,
                new Vector2(right ? 1f : 0f, 0.5f),
                new Vector2(right ? -72f : 72f, 0f),
                new Vector2(170f, 260f),
                new Vector2(right ? 1f : 0f, 0.5f));
            var image = buttonRoot.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.002f);
            var button = buttonRoot.GetComponent<Button>();
            button.targetGraphic = image;

            var icon = Image(buttonRoot.transform, "Icon", new Color(1f, 1f, 1f, 0.68f), sprite);
            Anchored(icon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(100f, 150f));
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            if (!right)
                icon.rectTransform.localScale = new Vector3(-1f, 1f, 1f);

            var text = Text(buttonRoot.transform, "Label", label, font, 30f, TextAlignmentOptions.Center, Color.white);
            Anchored(text.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, -28f), new Vector2(190f, 58f), new Vector2(0.5f, 1f));
            RecoveredTextStyles.Apply(text, RecoveredTextStyle.LightWithDarkBorder);
            return button;
        }

        private static TMP_FontAsset EnsureFont()
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (font == null)
                throw new InvalidOperationException($"Lobby font missing (build lobby first): {FontPath}");
            const string glyphs = "스테이지리스트순한맛매운핵불0123456789-◀";
            if (!font.TryAddCharacters(glyphs, out var missing))
                throw new InvalidOperationException($"Stage-select font missing glyphs: {missing}");
            EditorUtility.SetDirty(font);
            return font;
        }

        // --- helpers (mirror LoginLobbyUIBuilder) ------------------------------

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

        private static Button Button(Transform parent, string name, string label, TMP_FontAsset font, Color color, Sprite sprite = null)
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
                var text = Text(root.transform, "Label", label, font, 30f, TextAlignmentOptions.Center, Color.white);
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
                throw new InvalidOperationException($"Generated StageSelect UI is incomplete: {path}");
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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using TrickcalRevive.Recovery.Harness;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TrickcalRevive.Recovery.Editor
{
    public static class LobbyRecoveryHarnessBuilder
    {
        public const string PrefabPath = "Assets/Recovery/Harness/Prefabs/LobbyResourceHarness.prefab";
        public const string ScenePath = "Assets/Recovery/Harness/Scenes/LobbyResourceHarness.unity";
        public const string BackgroundPath = "Assets/Game/Content/UI/Lobby/Background/Lobby_Default.png";
        public const string SpriteRoot = "Assets/Game/Content/UI/Lobby/Sprites";

        private static readonly string[] SpriteNames =
        {
            "Currency_Macaron",
            "Currency_Gold",
            "Currency_Elif",
            "Currency_Stamina",
            "TopMenu_ButtonBase",
            "TopMenu_IconMenu",
            "TopMenu_CurrencyBase",
            "TopMenu_Plus",
            "MainLobby_UserInfoBase",
            "MainLobby_LevelBase",
            "MainLobby_BtnBg",
            "Lobby_GachaButton",
            "Lobby_HeroButton",
            "Lobby_StoryButton"
        };

        private readonly struct CaptureSize
        {
            public CaptureSize(int width, int height, string name)
            {
                Width = width;
                Height = height;
                Name = name;
            }

            public int Width { get; }
            public int Height { get; }
            public string Name { get; }
        }

        private static readonly CaptureSize[] CaptureSizes =
        {
            new(1920, 1080, "16x9"),
            new(1170, 540, "19_5x9"),
            new(1440, 1080, "4x3"),
            new(1080, 1920, "9x16")
        };

        [MenuItem("Tools/Trickcal Revive/Recovery/Lobby/Build Harness")]
        public static void BuildAll()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var background = RequireSprite(BackgroundPath);
            var sprites = SpriteNames.Select(name => RequireSprite($"{SpriteRoot}/{name}.png")).ToArray();
            EnsureDirectory(PrefabPath);
            EnsureDirectory(ScenePath);
            BuildPrefab(background, sprites);
            BuildScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ValidateGenerated();
            Debug.Log("Lobby Recovery Harness prefab and scene are ready for integration testing.");
        }

        [MenuItem("Tools/Trickcal Revive/Recovery/Lobby/Validate Harness")]
        public static void ValidateGenerated()
        {
            RequireSprite(BackgroundPath);
            foreach (var name in SpriteNames)
            {
                RequireSprite($"{SpriteRoot}/{name}.png");
            }

            var excludedAlternate = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(
                $"{SpriteRoot}/Currency_Elif_Alternate.png");
            if (excludedAlternate != null)
            {
                throw new InvalidOperationException("The unconfirmed Currency_Elif_Alternate must remain excluded.");
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                throw new InvalidOperationException($"Lobby Harness prefab is missing: {PrefabPath}");
            }

            var harness = prefab.GetComponent<LobbyResourceHarness>();
            if (harness == null || !harness.IsReady)
            {
                throw new InvalidOperationException("Lobby Harness resource references are incomplete.");
            }

            ValidateComponents(prefab, PrefabPath);
            var iconImages = prefab.GetComponentsInChildren<Image>(true)
                .Where(image => image.gameObject.name == "Icon")
                .ToArray();
            if (iconImages.Length != SpriteNames.Length || iconImages.Any(image => image.sprite == null))
            {
                throw new InvalidOperationException("Lobby Harness does not contain all 14 visible UI sprites.");
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                throw new InvalidOperationException($"Lobby Harness scene is missing: {ScenePath}");
            }
            if (EditorBuildSettings.scenes.Any(scene => scene.path == ScenePath && scene.enabled))
            {
                throw new InvalidOperationException("Recovery Harness scenes must not be included in Player builds.");
            }

            Debug.Log("Lobby Recovery Harness validation passed: ready-for-integration (not production-complete).");
        }

        [MenuItem("Tools/Trickcal Revive/Recovery/Lobby/Capture Four Ratios")]
        public static void CaptureAll()
        {
            BuildAll();
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var camera = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Camera>(true))
                .Single();
            var canvasRect = (RectTransform)scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Canvas>(true))
                .Single()
                .transform;
            var outputRoot = GetProjectPath("Artifacts/SpineRecovery/LobbyUI/Captures");
            Directory.CreateDirectory(outputRoot);
            var records = new List<string>();

            foreach (var size in CaptureSizes)
            {
                ConfigureViewport(canvasRect, camera, size);
                Canvas.ForceUpdateCanvases();
                var output = Path.Combine(outputRoot, $"lobby-harness-{size.Name}.png");
                Capture(camera, size, output);
                records.Add(
                    $"{size.Name}|{size.Width}x{size.Height}|{new FileInfo(output).Length}|{ComputeSha256(output)}");
            }

            File.WriteAllLines(Path.Combine(outputRoot, "capture-hashes.txt"), records);
            Debug.Log($"Created {CaptureSizes.Length} Lobby Recovery Harness captures at {outputRoot}.");
        }

        private static void BuildPrefab(Sprite background, Sprite[] sprites)
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
            {
                throw new InvalidOperationException("Unity built-in LegacyRuntime.ttf is unavailable.");
            }

            var root = new GameObject("LobbyResourceHarness", typeof(RectTransform), typeof(LobbyResourceHarness));
            try
            {
                var rootRect = (RectTransform)root.transform;
                rootRect.sizeDelta = new Vector2(1920f, 1080f);

                var backgroundObject = CreateRect("Background", rootRect);
                Stretch(backgroundObject);
                var backgroundImage = backgroundObject.gameObject.AddComponent<Image>();
                backgroundImage.sprite = background;
                backgroundImage.preserveAspect = true;
                backgroundImage.raycastTarget = false;
                var backgroundFitter = backgroundObject.gameObject.AddComponent<AspectRatioFitter>();
                backgroundFitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                backgroundFitter.aspectRatio = background.rect.width / background.rect.height;

                var shade = CreateRect("ReadabilityShade", rootRect);
                Stretch(shade);
                var shadeImage = shade.gameObject.AddComponent<Image>();
                shadeImage.color = new Color(0.03f, 0.02f, 0.06f, 0.5f);
                shadeImage.raycastTarget = false;

                var titleRect = CreateRect("Title", rootRect);
                titleRect.anchorMin = new Vector2(0.05f, 1f);
                titleRect.anchorMax = new Vector2(0.95f, 1f);
                titleRect.pivot = new Vector2(0.5f, 1f);
                titleRect.anchoredPosition = new Vector2(0f, -28f);
                titleRect.sizeDelta = new Vector2(0f, 72f);
                var title = titleRect.gameObject.AddComponent<Text>();
                title.font = font;
                title.fontSize = 34;
                title.alignment = TextAnchor.MiddleCenter;
                title.color = Color.white;
                title.text = "Issue2 Lobby Resource Recovery Harness - 15 approved resources";
                title.raycastTarget = false;
                title.resizeTextForBestFit = true;
                title.resizeTextMinSize = 18;
                title.resizeTextMaxSize = 34;

                var gridRect = CreateRect("ApprovedResourceGrid", rootRect);
                gridRect.anchorMin = new Vector2(0.5f, 0.5f);
                gridRect.anchorMax = new Vector2(0.5f, 0.5f);
                gridRect.pivot = new Vector2(0.5f, 0.5f);
                gridRect.sizeDelta = new Vector2(1000f, 720f);
                gridRect.anchoredPosition = new Vector2(0f, -35f);
                var grid = gridRect.gameObject.AddComponent<GridLayoutGroup>();
                grid.cellSize = new Vector2(232f, 164f);
                grid.spacing = new Vector2(18f, 16f);
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = 4;
                grid.childAlignment = TextAnchor.MiddleCenter;

                for (var index = 0; index < sprites.Length; index++)
                {
                    CreateResourceCell(gridRect, sprites[index], SpriteNames[index], font);
                }

                var badgeRect = CreateRect("EvidenceBadge", rootRect);
                badgeRect.anchorMin = new Vector2(1f, 0f);
                badgeRect.anchorMax = new Vector2(1f, 0f);
                badgeRect.pivot = new Vector2(1f, 0f);
                badgeRect.anchoredPosition = new Vector2(-28f, 24f);
                badgeRect.sizeDelta = new Vector2(900f, 46f);
                var badge = badgeRect.gameObject.AddComponent<Text>();
                badge.font = font;
                badge.fontSize = 22;
                badge.alignment = TextAnchor.MiddleRight;
                badge.color = new Color(1f, 0.9f, 0.45f, 1f);
                badge.text = "confirmed: 14 | inferred: Currency_Elif | unconfirmed: excluded";
                badge.raycastTarget = false;
                badge.resizeTextForBestFit = true;
                badge.resizeTextMinSize = 14;
                badge.resizeTextMaxSize = 22;

                root.GetComponent<LobbyResourceHarness>().Configure(background, sprites);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void CreateResourceCell(RectTransform parent, Sprite sprite, string label, Font font)
        {
            var cell = CreateRect(label, parent);
            var cellImage = cell.gameObject.AddComponent<Image>();
            var darkSource = label == "Lobby_HeroButton" || label == "Lobby_StoryButton";
            cellImage.color = darkSource
                ? new Color(0.62f, 0.62f, 0.64f, 0.9f)
                : new Color(0.08f, 0.07f, 0.12f, 0.88f);
            cellImage.raycastTarget = false;

            var iconRect = CreateRect("Icon", cell);
            iconRect.anchorMin = new Vector2(0.12f, 0.29f);
            iconRect.anchorMax = new Vector2(0.88f, 0.92f);
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;
            var icon = iconRect.gameObject.AddComponent<Image>();
            icon.sprite = sprite;
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            var labelRect = CreateRect("Label", cell);
            labelRect.anchorMin = new Vector2(0.03f, 0.02f);
            labelRect.anchorMax = new Vector2(0.97f, 0.28f);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            var text = labelRect.gameObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = 17;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = label == "Currency_Elif" ? new Color(1f, 0.86f, 0.35f) : Color.white;
            text.text = label == "Currency_Elif" ? $"{label} (inferred)" : label;
            text.raycastTarget = false;
            var outline = labelRect.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
            outline.effectDistance = new Vector2(1f, -1f);
        }

        private static void BuildScene()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                throw new InvalidOperationException($"Generated prefab is missing: {PrefabPath}");
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cameraObject = new GameObject("LobbyHarnessCamera", typeof(Camera));
            var camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 2000f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(23, 18, 25, 255);
            camera.transform.position = new Vector3(0f, 0f, -1000f);

            var canvasObject = new GameObject("LobbyHarnessCanvas", typeof(RectTransform), typeof(Canvas));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1000f;
            var canvasRect = (RectTransform)canvasObject.transform;
            ConfigureViewport(canvasRect, camera, CaptureSizes[0]);

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            var instanceRect = (RectTransform)instance.transform;
            instanceRect.SetParent(canvasRect, false);
            Stretch(instanceRect);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException($"Failed to save Lobby Harness scene: {ScenePath}");
            }
        }

        private static void ConfigureViewport(RectTransform canvasRect, Camera camera, CaptureSize size)
        {
            const float referenceWidth = 1920f;
            const float referenceHeight = 1080f;
            var widthScale = size.Width / referenceWidth;
            var heightScale = size.Height / referenceHeight;
            var scaleFactor = Mathf.Sqrt(widthScale * heightScale);
            var logicalWidth = size.Width / scaleFactor;
            var logicalHeight = size.Height / scaleFactor;
            canvasRect.sizeDelta = new Vector2(logicalWidth, logicalHeight);
            camera.aspect = (float)size.Width / size.Height;
            camera.orthographicSize = logicalHeight * 0.5f;
        }

        private static void Capture(Camera camera, CaptureSize size, string outputPath)
        {
            var renderTexture = new RenderTexture(size.Width, size.Height, 24, RenderTextureFormat.ARGB32);
            var texture = new Texture2D(size.Width, size.Height, TextureFormat.RGBA32, false);
            try
            {
                renderTexture.Create();
                camera.targetTexture = renderTexture;
                camera.Render();
                RenderTexture.active = renderTexture;
                texture.ReadPixels(new Rect(0f, 0f, size.Width, size.Height), 0, 0, false);
                texture.Apply(false, false);
                File.WriteAllBytes(outputPath, texture.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = null;
                renderTexture.Release();
                UnityEngine.Object.DestroyImmediate(renderTexture);
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static Sprite RequireSprite(string assetPath)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite == null || sprite.texture == null || sprite.rect.width <= 0f || sprite.rect.height <= 0f)
            {
                throw new InvalidOperationException($"Recovered sprite failed to import: {assetPath}");
            }

            return sprite;
        }

        private static RectTransform CreateRect(string name, RectTransform parent)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)gameObject.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        private static void ValidateComponents(GameObject root, string assetPath)
        {
            foreach (var component in root.GetComponentsInChildren<Component>(true))
            {
                if (component == null)
                {
                    throw new InvalidOperationException($"Missing Script detected in {assetPath}");
                }
            }
        }

        private static void EnsureDirectory(string assetPath)
        {
            var absolute = GetProjectPath(Path.GetDirectoryName(assetPath)?.Replace('\\', '/') ?? string.Empty);
            Directory.CreateDirectory(absolute);
        }

        private static string GetProjectPath(string relativePath)
        {
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName
                              ?? throw new InvalidOperationException("Unity project root is unavailable.");
            return Path.Combine(projectRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        }

        private static string ComputeSha256(string path)
        {
            using var sha = SHA256.Create();
            using var stream = File.OpenRead(path);
            return string.Concat(sha.ComputeHash(stream).Select(value => value.ToString("x2")));
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Spine;
using Spine.Unity;
using TrickcalRevive.Recovery.Harness;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TrickcalRevive.Recovery.Editor
{
    public static class CharacterRecoveryHarnessBuilder
    {
        private const string RosterPath = "Recovery/Catalogs/character-roster.json";
        private const string ContentRoot = "Assets/Game/Content/Characters";
        private const string CatalogAssetPath = "Assets/Recovery/Harness/CharacterResourceCatalog.asset";
        private const string PrefabPath = "Assets/Recovery/Harness/Prefabs/CharacterResourceHarness.prefab";
        private const string ScenePath = "Assets/Recovery/Harness/Scenes/CharacterResourceHarness.unity";
        private const string ReportJsonPath = "Recovery/Reports/character-validation.json";
        private const string ReportMarkdownPath = "Recovery/Reports/character-validation.md";
        private const string CaptureRoot = "Artifacts/SpineRecovery/Characters/Captures";

        [MenuItem("Tools/Trickcal Revive/Recovery/Characters/Build Harness")]
        public static void BuildAll()
        {
            var catalog = BuildCatalog();
            BuildPrefab(catalog);
            BuildScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            WriteReport(Validate(catalog, Array.Empty<CaptureRecord>()));
            Debug.Log("Character Recovery Harness build completed.");
        }

        [MenuItem("Tools/Trickcal Revive/Recovery/Characters/Validate and Capture")]
        public static void CaptureAll()
        {
            var catalog = BuildCatalog();
            BuildPrefab(catalog);
            BuildScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            var captures = CaptureCatalog(catalog);
            var report = Validate(catalog, captures);
            WriteReport(report);
            if (report.failures.Count > 0)
            {
                throw new InvalidOperationException(
                    "Character validation failed:\n" + string.Join("\n", report.failures));
            }
            if (report.captureCount != 80)
            {
                throw new InvalidOperationException(
                    $"Expected 80 character captures, found {report.captureCount}.");
            }
            Debug.Log("Character Recovery Harness validation and capture completed.");
        }

        public static CharacterValidationReport ValidateAllForTests()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CharacterResourceCatalog>(CatalogAssetPath);
            if (catalog == null)
            {
                throw new FileNotFoundException("Character Recovery Harness catalog is missing.", CatalogAssetPath);
            }
            return Validate(catalog, Array.Empty<CaptureRecord>());
        }

        private static CharacterResourceCatalog BuildCatalog()
        {
            var roster = LoadRoster();
            var entries = new List<CharacterResourceEntry>(50);
            foreach (var source in roster.apostles)
            {
                entries.Add(BuildApostle(source));
            }

            foreach (var source in roster.monsters)
            {
                var inGame = LoadSkeleton(
                    $"{ContentRoot}/Monsters/{source.key}/Spine/InGame/{source.baseName}_SkeletonData.asset");
                var voices = LoadSortedAudio(
                    $"{ContentRoot}/Monsters/{source.key}/Audio/Voice/Battle");
                var sfx = LoadSortedAudio($"{ContentRoot}/Monsters/{source.key}/Audio/Sfx");
                foreach (var variant in roster.monsterDisplayVariants)
                {
                    entries.Add(new CharacterResourceEntry
                    {
                        key = $"{source.key}-{variant.ToLowerInvariant()}",
                        baseKey = source.key,
                        displayName = $"{source.baseName} {variant}",
                        kind = CharacterResourceKind.Monster,
                        rosterIcon = LoadSprite(
                            $"{ContentRoot}/Monsters/{source.key}/Presentation/{variant}.png"),
                        inGameSkeleton = inGame,
                        inGameSkin = $"Skin_{variant}",
                        battleVoices = voices,
                        battleSfx = sfx
                    });
                }
            }

            if (entries.Count != 50)
            {
                throw new InvalidOperationException($"Expected 50 registry entries, found {entries.Count}.");
            }

            var catalog = AssetDatabase.LoadAssetAtPath<CharacterResourceCatalog>(CatalogAssetPath);
            if (catalog == null)
            {
                EnsureAssetFolder(Path.GetDirectoryName(CatalogAssetPath)?.Replace('\\', '/'));
                catalog = ScriptableObject.CreateInstance<CharacterResourceCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogAssetPath);
            }
            catalog.SetEntries(entries);
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        private static CharacterResourceEntry BuildApostle(CharacterSource source)
        {
            var root = $"{ContentRoot}/Apostles/{source.key}";
            return new CharacterResourceEntry
            {
                key = source.key,
                baseKey = source.key,
                displayName = source.baseName,
                kind = CharacterResourceKind.Apostle,
                portrait = LoadSprite($"{root}/Presentation/Portrait.png"),
                rosterIcon = LoadSprite($"{root}/Presentation/RosterIcon.png"),
                admissionSkillIcon = LoadSprite($"{root}/Presentation/AdmissionSkill.png"),
                graduateSkillIcon = LoadSprite($"{root}/Presentation/GraduateSkill.png"),
                inGameSkeleton = LoadSkeleton(
                    $"{root}/Spine/InGame/{source.baseName}_SkeletonData.asset"),
                standingSkeleton = LoadSkeleton(
                    $"{root}/Spine/Standing/{source.baseName}_SkeletonData.asset"),
                standingSkin = source.standingSkin,
                battleVoices = LoadSortedAudio($"{root}/Audio/Voice/Battle"),
                battleSfx = LoadSortedAudio($"{root}/Audio/Sfx")
            };
        }

        private static void BuildPrefab(CharacterResourceCatalog catalog)
        {
            EnsureAssetFolder(Path.GetDirectoryName(PrefabPath)?.Replace('\\', '/'));
            var root = new GameObject("CharacterResourceHarness");
            try
            {
                var harness = root.AddComponent<CharacterResourceHarness>();
                harness.Configure(catalog, "amelia", CharacterPresentation.InGame);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void BuildScene()
        {
            EnsureAssetFolder(Path.GetDirectoryName(ScenePath)?.Replace('\\', '/'));
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cameraObject = new GameObject("Recovery Camera", typeof(Camera));
            var camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 3f;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.055f, 0.065f, 0.09f, 1f);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                throw new FileNotFoundException("Character Recovery Harness prefab is missing.", PrefabPath);
            }
            PrefabUtility.InstantiatePrefab(prefab, scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        private static CaptureRecord[] CaptureCatalog(CharacterResourceCatalog catalog)
        {
            var absoluteRoot = Path.GetFullPath(CaptureRoot);
            Directory.CreateDirectory(absoluteRoot);
            var records = new List<CaptureRecord>(80);
            var root = new GameObject("Character Capture Harness");
            var cameraObject = new GameObject("Character Capture Camera", typeof(Camera));
            var harness = root.AddComponent<CharacterResourceHarness>();
            var camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.055f, 0.065f, 0.09f, 1f);
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 100f;
            camera.allowHDR = false;
            camera.allowMSAA = false;

            try
            {
                foreach (var entry in catalog.Entries)
                {
                    CaptureOne(
                        harness,
                        camera,
                        catalog,
                        entry,
                        CharacterPresentation.InGame,
                        absoluteRoot,
                        records);
                    if (entry.kind == CharacterResourceKind.Apostle)
                    {
                        CaptureOne(
                            harness,
                            camera,
                            catalog,
                            entry,
                            CharacterPresentation.Standing,
                            absoluteRoot,
                            records);
                    }
                }
            }
            finally
            {
                camera.targetTexture = null;
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(root);
            }
            return records.ToArray();
        }

        private static void CaptureOne(
            CharacterResourceHarness harness,
            Camera camera,
            CharacterResourceCatalog catalog,
            CharacterResourceEntry entry,
            CharacterPresentation presentation,
            string absoluteRoot,
            List<CaptureRecord> records)
        {
            harness.Configure(catalog, entry.key, presentation, true);
            if (!harness.IsReady)
            {
                throw new InvalidOperationException($"Empty Spine mesh: {entry.key}/{presentation}");
            }

            var renderer = harness.ActiveSkeleton.GetComponent<MeshRenderer>();
            var bounds = renderer.bounds;
            if (bounds.size.x <= 0f || bounds.size.y <= 0f)
            {
                throw new InvalidOperationException($"Invalid Spine bounds: {entry.key}/{presentation}");
            }

            camera.transform.position = new Vector3(bounds.center.x, bounds.center.y, -10f);
            camera.orthographicSize = Mathf.Max(bounds.extents.y, bounds.extents.x) * 1.25f;
            camera.orthographicSize = Mathf.Max(camera.orthographicSize, 0.5f);

            var renderTexture = new RenderTexture(800, 800, 24, RenderTextureFormat.ARGB32)
            {
                antiAliasing = 1
            };
            var texture = new Texture2D(800, 800, TextureFormat.RGBA32, false);
            try
            {
                camera.targetTexture = renderTexture;
                camera.Render();
                RenderTexture.active = renderTexture;
                texture.ReadPixels(new Rect(0, 0, 800, 800), 0, 0);
                texture.Apply(false, false);
                var bytes = texture.EncodeToPNG();
                var fileName = $"{entry.key}-{presentation}.png";
                var absolutePath = Path.Combine(absoluteRoot, fileName);
                File.WriteAllBytes(absolutePath, bytes);
                records.Add(new CaptureRecord
                {
                    key = entry.key,
                    presentation = presentation.ToString(),
                    artifactRelativePath = $"{CaptureRoot}/{fileName}".Replace('\\', '/'),
                    bytes = bytes.Length,
                    sha256 = HashBytes(bytes),
                    width = 800,
                    height = 800
                });
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

        private static CharacterValidationReport Validate(
            CharacterResourceCatalog catalog,
            IReadOnlyCollection<CaptureRecord> captures)
        {
            var report = new CharacterValidationReport
            {
                generatedUtc = DateTime.UtcNow.ToString("O"),
                status = "ready-for-integration",
                productionComplete = false,
                registryEntryCount = catalog.Entries.Count,
                captureCount = captures.Count,
                captures = captures.OrderBy(item => item.artifactRelativePath, StringComparer.Ordinal).ToList(),
                evidenceBoundary = new List<string>
                {
                    "confirmed: raw and Unity-exported file hashes, preserved GUIDs, Spine 4.1 import, catalog references",
                    "inferred: battle voice membership selected by approved base-directory and battle-name rule",
                    "unconfirmed: original FX mapping/timing and production gameplay transitions"
                },
                renderingColorSpace = PlayerSettings.colorSpace.ToString()
            };

            report.pmaAtlasCount = Directory.GetFiles(
                    Path.GetFullPath(ContentRoot),
                    "*.atlas.txt",
                    SearchOption.AllDirectories)
                .Count(path => File.ReadAllText(path).Contains("pma:true", StringComparison.Ordinal));
            if (report.pmaAtlasCount > 0 && PlayerSettings.colorSpace == ColorSpace.Linear)
            {
                report.renderingRisks.Add(
                    "All selected atlases declare pma:true while the project uses Linear Color Space. "
                    + "The Spine runtime warns that PMA atlas textures are not fully supported in Linear; "
                    + "captures contain populated meshes without black/white quads, but original color parity remains unconfirmed.");
            }

            var uniqueSkeletons = new Dictionary<string, SkeletonDataAsset>(StringComparer.Ordinal);
            foreach (var entry in catalog.Entries)
            {
                ValidateEntry(entry, report);
                AddSkeleton(uniqueSkeletons, entry.inGameSkeleton);
                AddSkeleton(uniqueSkeletons, entry.standingSkeleton);
            }

            report.apostleEntryCount = catalog.Entries.Count(
                entry => entry.kind == CharacterResourceKind.Apostle);
            report.monsterVariantEntryCount = catalog.Entries.Count(
                entry => entry.kind == CharacterResourceKind.Monster);
            report.uniqueSkeletonSetCount = uniqueSkeletons.Count;
            foreach (var pair in uniqueSkeletons.OrderBy(item => item.Key, StringComparer.Ordinal))
            {
                report.spineInventories.Add(BuildInventory(pair.Value, pair.Key, report.failures));
            }

            ValidatePrefabAndScene(report.failures);
            report.generatedAtlasAssetCount = AssetDatabase.FindAssets(
                    "t:SpineAtlasAsset", new[] { ContentRoot })
                .Length;
            report.generatedSkeletonDataAssetCount = AssetDatabase.FindAssets(
                    "t:SkeletonDataAsset", new[] { ContentRoot })
                .Length;
            report.generatedMaterialCount = AssetDatabase.FindAssets(
                    "t:Material", new[] { ContentRoot })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Count(path => path.EndsWith("_Material.mat", StringComparison.Ordinal));

            if (report.registryEntryCount != 50)
            {
                report.failures.Add($"Expected 50 registry entries, found {report.registryEntryCount}.");
            }
            if (report.apostleEntryCount != 30 || report.monsterVariantEntryCount != 20)
            {
                report.failures.Add(
                    $"Expected 30 apostles and 20 monster variants, found "
                    + $"{report.apostleEntryCount} and {report.monsterVariantEntryCount}.");
            }
            if (report.uniqueSkeletonSetCount != 64)
            {
                report.failures.Add(
                    $"Expected 64 unique Spine sets, found {report.uniqueSkeletonSetCount}.");
            }
            if (report.generatedAtlasAssetCount != 64
                || report.generatedSkeletonDataAssetCount != 64
                || report.generatedMaterialCount != 64)
            {
                report.failures.Add(
                    "Expected 64 generated AtlasAssets, SkeletonDataAssets, and Materials; found "
                    + $"{report.generatedAtlasAssetCount}, {report.generatedSkeletonDataAssetCount}, "
                    + $"{report.generatedMaterialCount}.");
            }

            if (report.failures.Count > 0)
            {
                report.status = "blocked";
            }
            return report;
        }

        private static void ValidateEntry(
            CharacterResourceEntry entry,
            CharacterValidationReport report)
        {
            if (entry.rosterIcon == null || entry.inGameSkeleton == null)
            {
                report.failures.Add($"Missing required registry resource: {entry.key}");
            }
            if (entry.battleVoices == null || entry.battleVoices.Length == 0)
            {
                report.failures.Add($"Missing battle voice: {entry.key}");
            }
            if (entry.battleSfx == null || entry.battleSfx.Length == 0)
            {
                report.failures.Add($"Missing battle SFX: {entry.key}");
            }
            if (entry.kind == CharacterResourceKind.Apostle)
            {
                if (entry.portrait == null
                    || entry.admissionSkillIcon == null
                    || entry.graduateSkillIcon == null
                    || entry.standingSkeleton == null)
                {
                    report.failures.Add($"Missing apostle presentation resource: {entry.key}");
                }
            }

            if (entry.battleVoices.Any(clip => clip == null)
                || entry.battleSfx.Any(clip => clip == null))
            {
                report.failures.Add($"Null audio reference: {entry.key}");
            }
            if (entry.kind == CharacterResourceKind.Monster
                && entry.inGameSkeleton != null
                && entry.inGameSkeleton.GetSkeletonData(false).FindSkin(entry.inGameSkin) == null)
            {
                report.failures.Add($"Missing monster display skin {entry.inGameSkin}: {entry.key}");
            }

            report.totalBattleVoiceCount += entry.kind == CharacterResourceKind.Apostle
                ? entry.battleVoices.Length
                : 0;
            report.totalBattleSfxCount += entry.kind == CharacterResourceKind.Apostle
                ? entry.battleSfx.Length
                : 0;
        }

        private static void AddSkeleton(
            IDictionary<string, SkeletonDataAsset> unique,
            SkeletonDataAsset asset)
        {
            if (asset == null)
            {
                return;
            }
            var path = AssetDatabase.GetAssetPath(asset);
            unique[path] = asset;
        }

        private static SpineInventory BuildInventory(
            SkeletonDataAsset asset,
            string assetPath,
            ICollection<string> failures)
        {
            var inventory = new SpineInventory
            {
                assetPath = assetPath,
                animations = new List<string>(),
                skins = new List<string>(),
                slots = new List<string>(),
                attachments = new List<string>(),
                events = new List<string>()
            };
            try
            {
                var data = asset.GetSkeletonData(false);
                if (data == null)
                {
                    throw new InvalidOperationException("SkeletonData is null.");
                }
                inventory.spineVersion = data.Version;
                inventory.animations = data.Animations
                    .Take(data.Animations.Count)
                    .Select(item => item.Name)
                    .OrderBy(name => name, StringComparer.Ordinal)
                    .ToList();
                inventory.skins = data.Skins
                    .Take(data.Skins.Count)
                    .Select(item => item.Name)
                    .OrderBy(name => name, StringComparer.Ordinal)
                    .ToList();
                inventory.slots = data.Slots
                    .Take(data.Slots.Count)
                    .Select(item => item.Name)
                    .OrderBy(name => name, StringComparer.Ordinal)
                    .ToList();
                inventory.events = data.Events
                    .Take(data.Events.Count)
                    .Select(item => item.Name)
                    .OrderBy(name => name, StringComparer.Ordinal)
                    .ToList();
                foreach (var skin in data.Skins.Take(data.Skins.Count))
                {
                    foreach (var attachment in skin.Attachments)
                    {
                        inventory.attachments.Add(
                            $"{skin.Name}/{attachment.SlotIndex}/{attachment.Name}/"
                            + attachment.Attachment.GetType().Name);
                    }
                }
                inventory.attachments.Sort(StringComparer.Ordinal);
                if (string.IsNullOrWhiteSpace(data.Version)
                    || !data.Version.StartsWith("4.1", StringComparison.Ordinal))
                {
                    failures.Add($"Unexpected Spine version {data.Version}: {assetPath}");
                }
                if (inventory.animations.Count == 0
                    || inventory.skins.Count == 0
                    || inventory.slots.Count == 0
                    || inventory.attachments.Count == 0)
                {
                    failures.Add($"Incomplete Spine inventory: {assetPath}");
                }
                if (assetPath.Contains("/kyarot/Spine/Standing/", StringComparison.Ordinal)
                    && data.FindSkin("Normal") == null)
                {
                    failures.Add("Kyarot Standing skin 'Normal' is missing.");
                }
            }
            catch (Exception exception)
            {
                failures.Add($"Spine inventory failed for {assetPath}: {exception.Message}");
            }
            return inventory;
        }

        private static void ValidatePrefabAndScene(ICollection<string> failures)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                failures.Add("Character Recovery Harness prefab is missing.");
            }
            else
            {
                ValidateComponents(prefab, PrefabPath, failures);
            }

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            foreach (var root in scene.GetRootGameObjects())
            {
                ValidateComponents(root, ScenePath, failures);
            }
        }

        private static void ValidateComponents(
            GameObject root,
            string context,
            ICollection<string> failures)
        {
            foreach (var component in root.GetComponentsInChildren<Component>(true))
            {
                if (component == null)
                {
                    failures.Add($"Missing Script in {context}/{root.name}");
                }
            }
        }

        private static CharacterRosterSource LoadRoster()
        {
            var absolutePath = Path.GetFullPath(RosterPath);
            if (!File.Exists(absolutePath))
            {
                throw new FileNotFoundException("Character recovery roster is missing.", absolutePath);
            }
            var roster = JsonUtility.FromJson<CharacterRosterSource>(File.ReadAllText(absolutePath));
            if (roster == null
                || roster.apostles == null
                || roster.monsters == null
                || roster.monsterDisplayVariants == null
                || roster.apostles.Length != 30
                || roster.monsters.Length != 4
                || roster.monsterDisplayVariants.Length != 5)
            {
                throw new InvalidOperationException("Character recovery roster boundary is invalid.");
            }
            return roster;
        }

        private static Sprite LoadSprite(string path)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                throw new FileNotFoundException("Sprite import failed.", path);
            }
            return sprite;
        }

        private static SkeletonDataAsset LoadSkeleton(string path)
        {
            var asset = AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>(path);
            if (asset == null)
            {
                throw new FileNotFoundException("SkeletonDataAsset import failed.", path);
            }
            return asset;
        }

        private static AudioClip[] LoadSortedAudio(string folder)
        {
            var clips = AssetDatabase.FindAssets("t:AudioClip", new[] { folder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(path => path, StringComparer.Ordinal)
                .Select(AssetDatabase.LoadAssetAtPath<AudioClip>)
                .Where(clip => clip != null)
                .ToArray();
            if (clips.Length == 0)
            {
                throw new FileNotFoundException("No AudioClip imported from character folder.", folder);
            }
            return clips;
        }

        private static void EnsureAssetFolder(string folder)
        {
            if (string.IsNullOrWhiteSpace(folder) || AssetDatabase.IsValidFolder(folder))
            {
                return;
            }
            var parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
            EnsureAssetFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }

        private static string HashBytes(byte[] bytes)
        {
            using var algorithm = SHA256.Create();
            return string.Concat(algorithm.ComputeHash(bytes).Select(value => value.ToString("x2")));
        }

        private static void WriteReport(CharacterValidationReport report)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(ReportJsonPath))!);
            File.WriteAllText(
                Path.GetFullPath(ReportJsonPath),
                JsonUtility.ToJson(report, true) + "\n");
            var markdown = $@"# Character resource validation

- Status: `{report.status}`
- Production complete: `{report.productionComplete}`
- Registry: {report.registryEntryCount} (apostles {report.apostleEntryCount}, monster variants {report.monsterVariantEntryCount})
- Unique Spine sets: {report.uniqueSkeletonSetCount}
- Generated assets: SkeletonData {report.generatedSkeletonDataAssetCount}, Atlas {report.generatedAtlasAssetCount}, Material {report.generatedMaterialCount}
- Captures: {report.captureCount}/80
- Failures: {report.failures.Count}
- Rendering: {report.renderingColorSpace}, PMA atlases {report.pmaAtlasCount}, known risks {report.renderingRisks.Count}

## Evidence boundary

{string.Join(Environment.NewLine, report.evidenceBoundary.Select(item => $"- {item}"))}

## Rendering risks

{string.Join(Environment.NewLine, report.renderingRisks.Select(item => $"- {item}"))}

## Deferred from issue2

- Production scenes and gameplay state transitions
- Original FX mapping and timing
- Android device validation
";
            File.WriteAllText(Path.GetFullPath(ReportMarkdownPath), markdown.Replace("\r\n", "\n"));
            AssetDatabase.Refresh();
        }

        [Serializable]
        private sealed class CharacterRosterSource
        {
            public CharacterSource[] apostles;
            public CharacterSource[] monsters;
            public string[] monsterDisplayVariants;
        }

        [Serializable]
        private sealed class CharacterSource
        {
            public string key;
            public string baseName;
            public string standingSkin;
        }
    }

    [Serializable]
    public sealed class CharacterValidationReport
    {
        public string generatedUtc;
        public string status;
        public bool productionComplete;
        public int registryEntryCount;
        public int apostleEntryCount;
        public int monsterVariantEntryCount;
        public int uniqueSkeletonSetCount;
        public int generatedSkeletonDataAssetCount;
        public int generatedAtlasAssetCount;
        public int generatedMaterialCount;
        public int totalBattleVoiceCount;
        public int totalBattleSfxCount;
        public int captureCount;
        public string renderingColorSpace;
        public int pmaAtlasCount;
        public List<string> evidenceBoundary = new();
        public List<string> renderingRisks = new();
        public List<string> failures = new();
        public List<SpineInventory> spineInventories = new();
        public List<CaptureRecord> captures = new();
    }

    [Serializable]
    public sealed class SpineInventory
    {
        public string assetPath;
        public string spineVersion;
        public List<string> animations;
        public List<string> skins;
        public List<string> slots;
        public List<string> attachments;
        public List<string> events;
    }

    [Serializable]
    public sealed class CaptureRecord
    {
        public string key;
        public string presentation;
        public string artifactRelativePath;
        public int bytes;
        public string sha256;
        public int width;
        public int height;
    }
}

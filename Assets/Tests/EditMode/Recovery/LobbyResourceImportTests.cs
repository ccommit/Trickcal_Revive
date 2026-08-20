using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using NUnit.Framework;
using TrickcalRevive.Recovery.Editor;
using TrickcalRevive.Recovery.Harness;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TrickcalRevive.Recovery.Tests
{
    public sealed class LobbyResourceImportTests
    {
        private static readonly string[] RequiredPaths =
        {
            LobbyRecoveryHarnessBuilder.BackgroundPath,
            LobbyRecoveryHarnessBuilder.SpriteRoot + "/Currency_Macaron.png",
            LobbyRecoveryHarnessBuilder.SpriteRoot + "/Currency_Gold.png",
            LobbyRecoveryHarnessBuilder.SpriteRoot + "/Currency_Elif.png",
            LobbyRecoveryHarnessBuilder.SpriteRoot + "/Currency_Stamina.png",
            LobbyRecoveryHarnessBuilder.SpriteRoot + "/TopMenu_ButtonBase.png",
            LobbyRecoveryHarnessBuilder.SpriteRoot + "/TopMenu_IconMenu.png",
            LobbyRecoveryHarnessBuilder.SpriteRoot + "/TopMenu_IconHome.png",
            LobbyRecoveryHarnessBuilder.SpriteRoot + "/TopMenu_CurrencyBase.png",
            LobbyRecoveryHarnessBuilder.SpriteRoot + "/TopMenu_Plus.png",
            LobbyRecoveryHarnessBuilder.SpriteRoot + "/MainLobby_UserInfoBase.png",
            LobbyRecoveryHarnessBuilder.SpriteRoot + "/MainLobby_LevelBase.png",
            LobbyRecoveryHarnessBuilder.SpriteRoot + "/MainLobby_BtnBg.png",
            LobbyRecoveryHarnessBuilder.SpriteRoot + "/Lobby_GachaButton.png",
            LobbyRecoveryHarnessBuilder.SpriteRoot + "/Lobby_HeroButton.png",
            LobbyRecoveryHarnessBuilder.SpriteRoot + "/Lobby_StoryButton.png"
        };

        [Serializable]
        private sealed class RecoveryManifest
        {
            public int schemaVersion;
            public RecoveryEntry[] entries = Array.Empty<RecoveryEntry>();
        }

        [Serializable]
        private sealed class RecoveryEntry
        {
            public string evidenceStatus = string.Empty;
            public string outputRelativePath = string.Empty;
            public long outputBytes;
            public string outputSha256 = string.Empty;
        }

        [Test]
        public void ApprovedLobbyResourcesImportAsVisibleSpritesWithUniqueGuids()
        {
            var guids = new HashSet<string>();
            foreach (var path in RequiredPaths)
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                Assert.That(sprite, Is.Not.Null, path);
                Assert.That(sprite.texture, Is.Not.Null, path);
                Assert.That(sprite.rect.width, Is.GreaterThan(0f), path);
                Assert.That(sprite.rect.height, Is.GreaterThan(0f), path);
                var guid = AssetDatabase.AssetPathToGUID(path);
                Assert.That(guid, Has.Length.EqualTo(32), path);
                Assert.That(guids.Add(guid), Is.True, $"Duplicate GUID: {path}");
            }
        }

        [Test]
        public void LobbyManifestCoversEveryAssetAndMetaWithoutAbsolutePaths()
        {
            var manifestPath = GetProjectPath("Recovery/Manifests/lobby-resources.json");
            Assert.That(File.Exists(manifestPath), Is.True, manifestPath);
            var json = File.ReadAllText(manifestPath);
            StringAssert.DoesNotContain("Trickcal_Reference", json);
            StringAssert.DoesNotContain(":\\", json);
            StringAssert.DoesNotContain("Currency_Elif_Alternate", json);

            var manifest = JsonUtility.FromJson<RecoveryManifest>(json);
            Assert.That(manifest, Is.Not.Null);
            Assert.That(manifest.schemaVersion, Is.EqualTo(1));
            Assert.That(manifest.entries, Has.Length.EqualTo(32));
            Assert.That(manifest.entries.Count(entry => entry.evidenceStatus == "inferred"), Is.EqualTo(2));
            Assert.That(manifest.entries.Any(entry => entry.evidenceStatus == "unconfirmed"), Is.False);

            foreach (var entry in manifest.entries)
            {
                var outputPath = GetProjectPath(entry.outputRelativePath);
                Assert.That(File.Exists(outputPath), Is.True, entry.outputRelativePath);
                Assert.That(new FileInfo(outputPath).Length, Is.EqualTo(entry.outputBytes), entry.outputRelativePath);
                Assert.That(ComputeSha256(outputPath), Is.EqualTo(entry.outputSha256), entry.outputRelativePath);
            }
        }

        [Test]
        public void GeneratedHarnessIsTestOnlyAndHasNoMissingReferences()
        {
            LobbyRecoveryHarnessBuilder.ValidateGenerated();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(LobbyRecoveryHarnessBuilder.PrefabPath);
            Assert.That(prefab, Is.Not.Null);
            var harness = prefab.GetComponent<LobbyResourceHarness>();
            Assert.That(harness, Is.Not.Null);
            Assert.That(harness.IsReady, Is.True);
            foreach (var image in prefab.GetComponentsInChildren<Image>(true))
            {
                Assert.That(image.materialForRendering, Is.Not.Null, image.name);
            }

            var previousScene = SceneManager.GetActiveScene().path;
            var scene = EditorSceneManager.OpenScene(LobbyRecoveryHarnessBuilder.ScenePath, OpenSceneMode.Single);
            try
            {
                foreach (var root in scene.GetRootGameObjects())
                {
                    foreach (var component in root.GetComponentsInChildren<Component>(true))
                    {
                        Assert.That(component, Is.Not.Null, $"Missing Script in {root.name}");
                    }
                }
            }
            finally
            {
                if (!string.IsNullOrWhiteSpace(previousScene) && File.Exists(previousScene))
                {
                    EditorSceneManager.OpenScene(previousScene, OpenSceneMode.Single);
                }
                else
                {
                    EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                }
            }
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

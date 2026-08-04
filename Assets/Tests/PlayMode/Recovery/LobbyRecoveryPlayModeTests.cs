using System.Collections;
using System.Linq;
using NUnit.Framework;
using TrickcalRevive.Recovery.Harness;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TrickcalRevive.Recovery.Tests
{
    public sealed class LobbyRecoveryPlayModeTests
    {
        private const string ScenePath = "Assets/Recovery/Harness/Scenes/LobbyResourceHarness.unity";

        [UnityTest]
        public IEnumerator HarnessSceneLoadsAndRendersAllApprovedResources()
        {
            var scene = EditorSceneManager.LoadSceneInPlayMode(
                ScenePath,
                new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;

            Assert.That(scene.IsValid(), Is.True);
            Assert.That(scene.isLoaded, Is.True);
            var harness = Object.FindFirstObjectByType<LobbyResourceHarness>();
            Assert.That(harness, Is.Not.Null);
            Assert.That(harness.IsReady, Is.True);
            Assert.That(harness.ApprovedSprites.Count, Is.EqualTo(14));

            var images = Object.FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var iconImages = images.Where(image => image.gameObject.name == "Icon").ToArray();
            Assert.That(iconImages, Has.Length.EqualTo(14));
            foreach (var image in iconImages)
            {
                Assert.That(image.sprite, Is.Not.Null, image.transform.parent.name);
                Assert.That(image.sprite.texture, Is.Not.Null, image.transform.parent.name);
                Assert.That(image.materialForRendering, Is.Not.Null, image.transform.parent.name);
            }

            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var component in root.GetComponentsInChildren<Component>(true))
                {
                    Assert.That(component, Is.Not.Null, $"Missing Script in {root.name}");
                }
            }
        }
    }
}

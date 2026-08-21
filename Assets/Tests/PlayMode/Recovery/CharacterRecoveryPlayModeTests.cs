using System.Collections;
using System.Linq;
using NUnit.Framework;
using TrickcalRevive.Recovery.Harness;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TrickcalRevive.Recovery.Tests
{
    public sealed class CharacterRecoveryPlayModeTests
    {
        private const string ScenePath =
            "Assets/Recovery/Harness/Scenes/CharacterResourceHarness.unity";

        [UnityTest]
        public IEnumerator HarnessLoadsAmeliaAndKyarotStandingWithDeterministicAudio()
        {
            var scene = EditorSceneManager.LoadSceneInPlayMode(
                ScenePath,
                new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;

            Assert.That(scene.IsValid(), Is.True);
            Assert.That(scene.isLoaded, Is.True);
            var harness = Object.FindFirstObjectByType<CharacterResourceHarness>();
            Assert.That(harness, Is.Not.Null);
            Assert.That(harness.Catalog, Is.Not.Null);
            Assert.That(harness.Catalog.Entries, Has.Count.EqualTo(50));
            Assert.That(harness.IsReady, Is.True);

            harness.Select("kyarot", CharacterPresentation.Standing);
            yield return null;
            Assert.That(harness.IsReady, Is.True);
            Assert.That(harness.ActiveSkeleton.Skeleton.Skin.Name, Is.EqualTo("Normal"));

            var firstRun = Enumerable.Range(0, 12)
                .Select(step => harness.SelectBattleVoice(20260804, step)?.name)
                .ToArray();
            var secondRun = Enumerable.Range(0, 12)
                .Select(step => harness.SelectBattleVoice(20260804, step)?.name)
                .ToArray();
            Assert.That(secondRun, Is.EqualTo(firstRun));
            Assert.That(firstRun, Has.None.Null);

            harness.Select("gluttonbear-cool", CharacterPresentation.InGame);
            yield return null;
            Assert.That(harness.IsReady, Is.True);

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

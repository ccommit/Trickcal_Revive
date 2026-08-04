using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Spine.Unity;
using TrickcalRevive.Recovery.Editor;
using TrickcalRevive.Recovery.Harness;
using UnityEditor;

namespace TrickcalRevive.Recovery.Tests
{
    public sealed class CharacterResourceImportTests
    {
        private const string CatalogPath = "Assets/Recovery/Harness/CharacterResourceCatalog.asset";

        [Test]
        public void CatalogHasApprovedThirtyApostlesAndTwentyMonsterVariants()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CharacterResourceCatalog>(CatalogPath);
            Assert.That(catalog, Is.Not.Null);
            Assert.That(catalog.Entries, Has.Count.EqualTo(50));
            Assert.That(
                catalog.Entries.Count(entry => entry.kind == CharacterResourceKind.Apostle),
                Is.EqualTo(30));
            Assert.That(
                catalog.Entries.Count(entry => entry.kind == CharacterResourceKind.Monster),
                Is.EqualTo(20));
            Assert.That(
                catalog.Entries.Select(entry => entry.key).Distinct(StringComparer.Ordinal).Count(),
                Is.EqualTo(50));
        }

        [Test]
        public void EveryApprovedRegistryEntryHasRenderableResourcesAndAudio()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CharacterResourceCatalog>(CatalogPath);
            foreach (var entry in catalog.Entries)
            {
                Assert.That(entry.rosterIcon, Is.Not.Null, entry.key);
                Assert.That(entry.inGameSkeleton, Is.Not.Null, entry.key);
                Assert.That(entry.battleVoices, Is.Not.Empty, entry.key);
                Assert.That(entry.battleSfx, Is.Not.Empty, entry.key);
                Assert.That(entry.battleVoices, Has.None.Null, entry.key);
                Assert.That(entry.battleSfx, Has.None.Null, entry.key);
                if (entry.kind == CharacterResourceKind.Apostle)
                {
                    Assert.That(entry.portrait, Is.Not.Null, entry.key);
                    Assert.That(entry.admissionSkillIcon, Is.Not.Null, entry.key);
                    Assert.That(entry.graduateSkillIcon, Is.Not.Null, entry.key);
                    Assert.That(entry.standingSkeleton, Is.Not.Null, entry.key);
                }
            }
        }

        [Test]
        public void AllSixtyFourUniqueSpineSetsLoadAsVersionFourPointOne()
        {
            var skeletons = AssetDatabase.FindAssets(
                    "t:SkeletonDataAsset",
                    new[] { "Assets/Game/Content/Characters" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>)
                .Where(asset => asset != null)
                .ToArray();
            Assert.That(skeletons, Has.Length.EqualTo(64));
            foreach (var asset in skeletons)
            {
                var data = asset.GetSkeletonData(false);
                Assert.That(data, Is.Not.Null, AssetDatabase.GetAssetPath(asset));
                Assert.That(data.Version, Does.StartWith("4.1"), AssetDatabase.GetAssetPath(asset));
                Assert.That(data.Animations.Count, Is.GreaterThan(0), AssetDatabase.GetAssetPath(asset));
                Assert.That(data.Skins.Count, Is.GreaterThan(0), AssetDatabase.GetAssetPath(asset));
                Assert.That(data.Slots.Count, Is.GreaterThan(0), AssetDatabase.GetAssetPath(asset));
            }

            var kyarot = AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>(
                "Assets/Game/Content/Characters/Apostles/kyarot/Spine/Standing/Kyarot_SkeletonData.asset");
            Assert.That(kyarot.GetSkeletonData(false).FindSkin("Normal"), Is.Not.Null);
        }

        [Test]
        public void BuilderValidationHasNoFailures()
        {
            var report = CharacterRecoveryHarnessBuilder.ValidateAllForTests();
            Assert.That(report.failures, Is.Empty);
            Assert.That(report.uniqueSkeletonSetCount, Is.EqualTo(64));
            Assert.That(report.generatedSkeletonDataAssetCount, Is.EqualTo(64));
            Assert.That(report.generatedAtlasAssetCount, Is.EqualTo(64));
            Assert.That(report.generatedMaterialCount, Is.EqualTo(64));
            Assert.That(report.pmaAtlasCount, Is.EqualTo(64));
            Assert.That(report.renderingRisks, Is.Not.Empty);
        }

        [Test]
        public void ManifestContainsNoAbsoluteReferencePath()
        {
            var manifestPath = Path.GetFullPath("Recovery/Manifests/character-resources.json");
            var text = File.ReadAllText(manifestPath);
            Assert.That(text, Does.Not.Contain("E:\\"));
            Assert.That(text, Does.Not.Contain("Trickcal_Reference\\"));
            Assert.That(text, Does.Contain("\"resourceFileCount\": 2082"));
            Assert.That(text, Does.Contain("\"manifestEntryCount\": 4164"));
        }
    }
}

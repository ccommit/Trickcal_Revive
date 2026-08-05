using System.IO;
using NUnit.Framework;
using Spine.Unity;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

using TrickcalRevive.App.Editor;

namespace TrickcalRevive.MainUI.Tests
{
    public sealed class LoginLobbyRecoveryTests
    {
        [Test]
        public void LoginManifest_RecordsVerifiedSourceHashes()
        {
            const string manifestPath = "Recovery/Manifests/login-resources.json";
            Assert.That(File.Exists(manifestPath), Is.True, manifestPath);
            var json = File.ReadAllText(manifestPath);
            StringAssert.Contains("34789fc9631a0e467af02fd2b006c671b348340505398d7440465ead600b5840", json);
            StringAssert.Contains("6acb0b9dc9baf82f9e5f01a873fccc828f6661333a82df6aa42f1341bde1fe8f", json);
            StringAssert.Contains("1c0b82fbcb2666e6a9cd5b2a867718b675aee68a95837cbb19362c4c0481a74b", json);
        }

        [Test]
        public void TitleSpine_HasConfirmedSkinAndAnimations()
        {
            var asset = AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>(LoginLobbyUIBuilder.TitleSkeletonDataPath);
            Assert.That(asset, Is.Not.Null, LoginLobbyUIBuilder.TitleSkeletonDataPath);
            var data = asset.GetSkeletonData(true);
            Assert.That(data, Is.Not.Null);
            Assert.That(data.FindSkin(TitleBackgroundPresenter.ConfirmedSkin), Is.Not.Null);
            Assert.That(data.FindAnimation(TitleBackgroundPresenter.ConfirmedStartAnimation), Is.Not.Null);
            Assert.That(data.FindAnimation(TitleBackgroundPresenter.ConfirmedIdleAnimation), Is.Not.Null);
        }

        [Test]
        public void TitleSpine_UsesStraightAlphaMaterialInLinearColorSpace()
        {
            Assert.That(QualitySettings.activeColorSpace, Is.EqualTo(ColorSpace.Linear));
            var atlas = AssetDatabase.LoadAssetAtPath<SpineAtlasAsset>(LoginLobbyUIBuilder.TitleAtlasAssetPath);
            Assert.That(atlas, Is.Not.Null);
            Assert.That(atlas.materials, Is.Not.Empty);
            foreach (var material in atlas.materials)
            {
                Assert.That(material, Is.Not.Null);
                Assert.That(MaterialChecks.IsPMATextureMaterial(material), Is.False);
                var error = string.Empty;
                Assert.That(MaterialChecks.IsColorSpaceSupported(material, ref error), Is.True, error);
            }
        }

        [Test]
        public void LoginAndLobbyPrefabs_AreComplete()
        {
            var login = AssetDatabase.LoadAssetAtPath<GameObject>(LoginLobbyUIBuilder.LoginPrefabPath);
            var lobby = AssetDatabase.LoadAssetAtPath<GameObject>(LoginLobbyUIBuilder.LobbyPrefabPath);
            Assert.That(login, Is.Not.Null);
            Assert.That(lobby, Is.Not.Null);
            Assert.That(login.GetComponent<LoginScreenView>().IsReady, Is.True);
            Assert.That(lobby.GetComponent<LobbyScreenView>().IsReady, Is.True);
            Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(login), Is.Zero);
            Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(lobby), Is.Zero);
        }

        [TestCase(FlowSceneBuilder.LoginScenePath, typeof(LoginScreenView))]
        [TestCase(FlowSceneBuilder.MainScenePath, typeof(LobbyScreenView))]
        public void FlowScene_UsesLandscapeReferenceAndExpectedView(string scenePath, System.Type viewType)
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            Assert.That(scene.IsValid(), Is.True);
            var canvas = Object.FindFirstObjectByType<Canvas>();
            Assert.That(canvas, Is.Not.Null);
            var scaler = canvas.GetComponent<CanvasScaler>();
            Assert.That(scaler, Is.Not.Null);
            Assert.That(scaler.referenceResolution, Is.EqualTo(new Vector2(2560f, 1440f)));
            Assert.That(Object.FindFirstObjectByType(viewType), Is.Not.Null);
        }

        [Test]
        public void BuildSettings_ContainsOnlyLoginThenMain()
        {
            var scenes = EditorBuildSettings.scenes;
            Assert.That(scenes, Has.Length.EqualTo(2));
            Assert.That(scenes[0].enabled, Is.True);
            Assert.That(scenes[0].path, Is.EqualTo(FlowSceneBuilder.LoginScenePath));
            Assert.That(scenes[1].enabled, Is.True);
            Assert.That(scenes[1].path, Is.EqualTo(FlowSceneBuilder.MainScenePath));
        }
    }
}

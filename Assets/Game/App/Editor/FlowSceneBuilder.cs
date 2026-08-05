using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

using Spine.Unity;
using TrickcalRevive.MainUI;
using TrickcalRevive.Presentation;

namespace TrickcalRevive.App.Editor
{
    public static class FlowSceneBuilder
    {
        public const float ReferenceWidth = 2560f;
        public const float ReferenceHeight = 1440f;

        public const string LoginScenePath = "Assets/Scenes/Flow/Login.unity";
        public const string MainScenePath = "Assets/Scenes/Flow/Main.unity";

        private const string ScenesFolder = "Assets/Scenes/Flow";

        [MenuItem("Tools/Trickcal Revive/Flow/Build Login-Main Scenes")]
        public static void BuildAll()
        {
            EnsureFolder(ScenesFolder);
            LoginLobbyUIBuilder.BuildPrefabs();
            BuildLoginScene();
            BuildMainScene();
            RegisterBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Debug.Log("Login/Main scenes rebuilt at 2560x1440 reference resolution.");
        }

        private static void BuildLoginScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var appGo = new GameObject("GameApplication");
            var app = appGo.AddComponent<GameApplication>();

            var flowGo = new GameObject("SceneFlowController");
            var flow = flowGo.AddComponent<SceneFlowController>();
            SetSerializedField(app, "sceneFlowController", flow);

            BuildBaseline(out var camera, out var canvas);
            BuildTitleBackground(camera);

            var loginPrefab = Require<GameObject>(LoginLobbyUIBuilder.LoginPrefabPath);
            var loginObject = (GameObject)PrefabUtility.InstantiatePrefab(loginPrefab, scene);
            loginObject.transform.SetParent(canvas.transform, false);
            Stretch((RectTransform)loginObject.transform);
            var loginView = loginObject.GetComponent<LoginScreenView>();
            if (loginView == null || !loginView.IsReady)
                throw new InvalidOperationException("LoginScreen prefab is not ready.");

            var installerGo = new GameObject("LoginSceneInstaller");
            var installer = installerGo.AddComponent<LoginSceneInstaller>();
            var authGo = new GameObject("AuthController");
            var auth = authGo.AddComponent<AuthController>();
            SetSerializedField(installer, "authController", auth);
            SetSerializedField(installer, "loginScreenView", loginView);

            EditorSceneManager.SaveScene(scene, LoginScenePath);
        }

        private static void BuildMainScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BuildBaseline(out _, out var canvas);

            var lobbyPrefab = Require<GameObject>(LoginLobbyUIBuilder.LobbyPrefabPath);
            var lobbyObject = (GameObject)PrefabUtility.InstantiatePrefab(lobbyPrefab, scene);
            lobbyObject.transform.SetParent(canvas.transform, false);
            Stretch((RectTransform)lobbyObject.transform);
            var lobbyView = lobbyObject.GetComponent<LobbyScreenView>();
            if (lobbyView == null || !lobbyView.IsReady)
                throw new InvalidOperationException("LobbyScreen prefab is not ready.");

            var installerGo = new GameObject("MainSceneInstaller");
            var installer = installerGo.AddComponent<MainSceneInstaller>();
            var lobbyGo = new GameObject("LobbyController");
            var lobby = lobbyGo.AddComponent<LobbyController>();
            var settingsGo = new GameObject("SettingsController");
            var settings = settingsGo.AddComponent<SettingsController>();

            SetSerializedField(installer, "lobbyController", lobby);
            SetSerializedField(installer, "settingsController", settings);
            SetSerializedField(installer, "lobbyScreenView", lobbyView);
            SetSerializedField(lobby, "settingsController", settings);

            EditorSceneManager.SaveScene(scene, MainScenePath);
        }

        private static void BuildTitleBackground(Camera camera)
        {
            camera.orthographic = false;
            camera.fieldOfView = 60f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;
            camera.transform.position = new Vector3(0f, 0f, 5.5f);

            var skeletonData = Require<SkeletonDataAsset>(LoginLobbyUIBuilder.TitleSkeletonDataPath);
            var title = SkeletonAnimation.NewSkeletonAnimationGameObject(skeletonData);
            title.name = "Title_Background";
            title.skeletonDataAsset = skeletonData;
            title.initialSkinName = TitleBackgroundPresenter.ConfirmedSkin;
            title.transform.position = new Vector3(0f, 0f, 20f);
            title.transform.localScale = new Vector3(2f, 2f, 1f);
            title.GetComponent<MeshRenderer>().sortingOrder = 0;
            var presenter = title.gameObject.AddComponent<TitleBackgroundPresenter>();
            presenter.Configure(title);
            EditorUtility.SetDirty(title);
        }

        private static void BuildBaseline(out Camera camera, out Canvas canvas)
        {
            var cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            camera = cameraGo.AddComponent<Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            cameraGo.AddComponent<UniversalAdditionalCameraData>();
            cameraGo.AddComponent<AudioListener>();

            var canvasGo = new GameObject("Canvas");
            canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            var eventSystemGo = new GameObject("EventSystem");
            eventSystemGo.AddComponent<EventSystem>();
            eventSystemGo.AddComponent<InputSystemUIInputModule>();
        }

        private static void RegisterBuildSettings()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(LoginScenePath, true),
                new EditorBuildSettingsScene(MainScenePath, true)
            };
        }

        private static T Require<T>(string path) where T : UnityEngine.Object
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

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetSerializedField(UnityEngine.Object target, string fieldName, UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(fieldName);
            if (property == null)
                throw new MissingFieldException(target.GetType().Name, fieldName);
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}

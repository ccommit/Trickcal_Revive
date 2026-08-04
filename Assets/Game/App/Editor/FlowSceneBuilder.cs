using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

using TrickcalRevive.App;
using TrickcalRevive.Presentation;

namespace TrickcalRevive.App.Editor
{
    // 로그인->로비 구조 씬을 코드로 생성한다. 화면 배치(스프라이트·레이아웃)는 여기서
    // 하지 않는다 — 컨트롤러가 붙은 빈 GameObject, DI 배선(SerializeField 참조),
    // 그리고 어떤 UI 작업이든 요구하는 최소 기반(Camera/Canvas/EventSystem)까지만
    // 만들고, 실제 화면 구성은 이후 UI 작업에서 이 GameObject들에 얹는다.
    public static class FlowSceneBuilder
    {
        private const float ReferenceWidth = 1080f;
        private const float ReferenceHeight = 1920f;

        private const string ScenesFolder = "Assets/Scenes/Flow";
        private const string LoginScenePath = ScenesFolder + "/Login.unity";
        private const string MainScenePath = ScenesFolder + "/Main.unity";

        [MenuItem("Tools/Trickcal Revive/Flow/Build Login-Main Scenes")]
        public static void BuildAll()
        {
            EnsureFolder();
            BuildLoginScene();
            BuildMainScene();
            RegisterBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Debug.Log("Login/Main 씬 생성 완료.");
        }

        private static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder(ScenesFolder))
                AssetDatabase.CreateFolder("Assets/Scenes", "Flow");
        }

        private static void BuildLoginScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // GameApplication을 계층 맨 위에 둔다 — LoginSceneInstaller.Awake()가
            // GameApplication.RootContainer를 참조하므로, 같은 프레임의 Awake 순서 중
            // GameApplication이 먼저 돌아야 한다(엔진이 공식 보장하진 않지만, 계층 순서로
            // 실질적으로 보장된다).
            BuildBaseline();

            var appGo = new GameObject("GameApplication");
            var app = appGo.AddComponent<GameApplication>();

            var flowGo = new GameObject("SceneFlowController");
            var flow = flowGo.AddComponent<SceneFlowController>();

            var installerGo = new GameObject("LoginSceneInstaller");
            var installer = installerGo.AddComponent<LoginSceneInstaller>();

            var authGo = new GameObject("AuthController");
            var auth = authGo.AddComponent<AuthController>();

            SetSerializedField(app, "sceneFlowController", flow);
            SetSerializedField(installer, "authController", auth);

            EditorSceneManager.SaveScene(scene, LoginScenePath);
        }

        private static void BuildMainScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            BuildBaseline();

            var installerGo = new GameObject("MainSceneInstaller");
            var installer = installerGo.AddComponent<MainSceneInstaller>();

            var lobbyGo = new GameObject("LobbyController");
            var lobby = lobbyGo.AddComponent<LobbyController>();

            var settingsGo = new GameObject("SettingsController");
            var settings = settingsGo.AddComponent<SettingsController>();

            SetSerializedField(installer, "lobbyController", lobby);
            SetSerializedField(installer, "settingsController", settings);
            SetSerializedField(lobby, "settingsController", settings);

            EditorSceneManager.SaveScene(scene, MainScenePath);
        }

        private static void RegisterBuildSettings()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(LoginScenePath, true),
                new EditorBuildSettingsScene(MainScenePath, true)
            };
        }

        // 어떤 화면이든 요구하는 최소 기반. UI 배치 작업이 여기 얹기만 하면 되게 한다.
        private static void BuildBaseline()
        {
            var cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            var camera = cameraGo.AddComponent<Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            cameraGo.AddComponent<UniversalAdditionalCameraData>();
            cameraGo.AddComponent<AudioListener>();

            var canvasGo = new GameObject("Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
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

        private static void SetSerializedField(UnityEngine.Object target, string fieldName, UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(fieldName);
            property.objectReferenceValue = value;
            serialized.ApplyModifiedProperties();
        }
    }
}

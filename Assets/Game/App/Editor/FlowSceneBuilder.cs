using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

using TrickcalRevive.App;
using TrickcalRevive.Presentation;

namespace TrickcalRevive.App.Editor
{
    // 인트로->로그인->로비 구조 씬을 코드로 생성한다. UI 배치는 여기서 하지 않는다 —
    // 컨트롤러가 붙은 빈 GameObject와 DI 배선(SerializeField 참조)까지만 만들고,
    // 실제 화면(Canvas 등)은 이후 UI 작업에서 이 GameObject들에 얹는다.
    public static class FlowSceneBuilder
    {
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

        private static void SetSerializedField(UnityEngine.Object target, string fieldName, UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(fieldName);
            property.objectReferenceValue = value;
            serialized.ApplyModifiedProperties();
        }
    }
}

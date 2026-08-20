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
            StageSelectUIBuilder.BuildPrefab();
            StageInfoPopupUIBuilder.BuildPrefab();
            PartySetupUIBuilder.BuildPrefab();
            TopCurrencyPanelBuilder.BuildPrefab();
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

            var stageSelectPrefab = Require<GameObject>(StageSelectUIBuilder.StageSelectPrefabPath);
            var stageSelectObject = (GameObject)PrefabUtility.InstantiatePrefab(stageSelectPrefab, scene);
            stageSelectObject.transform.SetParent(canvas.transform, false);
            Stretch((RectTransform)stageSelectObject.transform);
            var stageSelectView = stageSelectObject.GetComponent<StageSelectView>();
            if (stageSelectView == null || !stageSelectView.IsReady)
                throw new InvalidOperationException("StageSelectScreen prefab is not ready.");

            var partyPrefab = Require<GameObject>(PartySetupUIBuilder.PartyPrefabPath);
            var partyObject = (GameObject)PrefabUtility.InstantiatePrefab(partyPrefab, scene);
            partyObject.transform.SetParent(canvas.transform, false);
            Stretch((RectTransform)partyObject.transform);
            var partySetupView = partyObject.GetComponent<PartySetupView>();
            if (partySetupView == null || !partySetupView.IsReady)
                throw new InvalidOperationException("PartySetupScreen prefab is not ready.");

            // 화면 프리팹 밖에 한 번만 배치되는 공통 상단 통화 패널.
            // 팝업보다는 아래, 모든 화면 패널보다는 위에 렌더된다.
            var currencyPrefab = Require<GameObject>(TopCurrencyPanelBuilder.PrefabPath);
            var currencyObject = (GameObject)PrefabUtility.InstantiatePrefab(currencyPrefab, scene);
            currencyObject.transform.SetParent(canvas.transform, false);
            var topCurrencyPanelView = currencyObject.GetComponent<TopCurrencyPanelView>();
            if (topCurrencyPanelView == null || !topCurrencyPanelView.IsReady)
                throw new InvalidOperationException("TopCurrencyPanel prefab is not ready.");

            var hostGo = new GameObject("ScreenPanelHost");
            var host = hostGo.AddComponent<ScreenPanelHost>();
            SetSerializedField(host, "lobbyPanel", lobbyObject);
            SetSerializedField(host, "stageSelectPanel", stageSelectObject);
            SetSerializedField(host, "partySetupPanel", partyObject);

            // 팝업은 화면 위에 뜬다 — Canvas 하위 마지막(최상위 렌더)으로 배치.
            var popupPrefab = Require<GameObject>(StageInfoPopupUIBuilder.PopupPrefabPath);
            var popupObject = (GameObject)PrefabUtility.InstantiatePrefab(popupPrefab, scene);
            popupObject.transform.SetParent(canvas.transform, false);
            Stretch((RectTransform)popupObject.transform);
            var stageInfoPopupView = popupObject.GetComponent<StageInfoPopupView>();
            if (stageInfoPopupView == null || !stageInfoPopupView.IsReady)
                throw new InvalidOperationException("StageInfoPopupScreen prefab is not ready.");

            var screenTransitionView = BuildScreenTransition(canvas.transform);

            var installerGo = new GameObject("MainSceneInstaller");
            var installer = installerGo.AddComponent<MainSceneInstaller>();
            var lobbyGo = new GameObject("LobbyController");
            var lobby = lobbyGo.AddComponent<LobbyController>();
            var settingsGo = new GameObject("SettingsController");
            var settings = settingsGo.AddComponent<SettingsController>();

            // StageSelect/PartySetup 화면(View)은 아직 없다 — UI 배치 전이라 컨트롤러만
            // 빈 GameObject로 배치한다(로그인/로비 때와 같은 순서: 구조 먼저, 화면은 나중).
            var stageSelectGo = new GameObject("StageSelectController");
            var stageSelect = stageSelectGo.AddComponent<StageSelectController>();
            var stageInfoGo = new GameObject("StageInfoPopupController");
            var stageInfo = stageInfoGo.AddComponent<StageInfoPopupController>();
            var partySetupGo = new GameObject("PartySetupController");
            var partySetup = partySetupGo.AddComponent<PartySetupController>();
            var topCurrencyGo = new GameObject("TopCurrencyController");
            var topCurrency = topCurrencyGo.AddComponent<TopCurrencyController>();

            SetSerializedField(installer, "lobbyController", lobby);
            SetSerializedField(installer, "settingsController", settings);
            SetSerializedField(installer, "lobbyScreenView", lobbyView);
            SetSerializedField(installer, "stageSelectController", stageSelect);
            SetSerializedField(installer, "stageInfoPopupController", stageInfo);
            SetSerializedField(installer, "partySetupController", partySetup);
            SetSerializedField(installer, "stageSelectView", stageSelectView);
            SetSerializedField(installer, "screenPanelHost", host);
            SetSerializedField(installer, "stageInfoPopupView", stageInfoPopupView);
            SetSerializedField(installer, "partySetupView", partySetupView);
            SetSerializedField(installer, "screenTransitionView", screenTransitionView);
            SetSerializedField(installer, "topCurrencyController", topCurrency);
            SetSerializedField(installer, "topCurrencyPanelView", topCurrencyPanelView);
            SetSerializedField(lobby, "settingsController", settings);
            SetSerializedField(stageSelect, "stageInfoPopupController", stageInfo);
            SetSerializedField(stageInfo, "partySetupController", partySetup);

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

        private static ScreenTransitionView BuildScreenTransition(Transform canvas)
        {
            var root = new GameObject("ScreenTransition", typeof(RectTransform));
            root.transform.SetParent(canvas, false);
            Stretch((RectTransform)root.transform);
            var view = root.AddComponent<ScreenTransitionView>();

            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(root.transform, false);
            Stretch((RectTransform)content.transform);

            var coverGo = new GameObject("Cover", typeof(RectTransform), typeof(Image));
            coverGo.transform.SetParent(content.transform, false);
            Stretch((RectTransform)coverGo.transform);
            var cover = coverGo.GetComponent<Image>();
            cover.color = Color.white;
            cover.raycastTarget = true;
            cover.material = EnsureStarWipeMaterial();

            view.Configure(content, cover);
            // 별 구멍 중심 = 로비의 우측정렬 모험 버튼 위치(우하단, 추론 UV).
            view.SetHoleCenter(0.86f, 0.13f);
            if (!view.IsReady)
                throw new InvalidOperationException("ScreenTransition overlay is not ready.");
            return view;
        }

        private static Material EnsureStarWipeMaterial()
        {
            var shader = Shader.Find("TrickcalRevive/StarWipe");
            if (shader == null)
                throw new InvalidOperationException("StarWipe shader not found.");
            const string path = "Assets/Game/Content/UI/StageSelect/StarWipe.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader) { name = "StarWipe" };
                AssetDatabase.CreateAsset(mat, path);
            }
            else
            {
                mat.shader = shader;
            }
            mat.SetFloat("_Progress", 0f);
            EditorUtility.SetDirty(mat);
            return mat;
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

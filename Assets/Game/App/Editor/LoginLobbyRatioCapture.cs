using System;
using System.IO;
using Spine.Unity;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

using TrickcalRevive.MainUI;

namespace TrickcalRevive.App.Editor
{
    public static class LoginLobbyRatioCapture
    {
        private readonly struct CaptureSize
        {
            public CaptureSize(int width, int height, string name)
            {
                Width = width;
                Height = height;
                Name = name;
            }

            public int Width { get; }
            public int Height { get; }
            public string Name { get; }
        }

        private static readonly CaptureSize[] Sizes =
        {
            new CaptureSize(1920, 1080, "16x9"),
            new CaptureSize(1440, 1080, "4x3"),
            new CaptureSize(2200, 1000, "reference-wide")
        };

        [MenuItem("Tools/Trickcal Revive/Flow/Capture Login-Lobby Ratios")]
        public static void CaptureAll()
        {
            FlowSceneBuilder.BuildAll();
            var outputRoot = Path.GetFullPath(
                Path.Combine(
                    Directory.GetParent(Application.dataPath)?.FullName ?? ".",
                    "Artifacts",
                    "SpineRecovery",
                    "LoginLobby",
                    "Captures"));
            Directory.CreateDirectory(outputRoot);

            CaptureScene(FlowSceneBuilder.LoginScenePath, "login", outputRoot, PrepareLogin);
            CaptureScene(FlowSceneBuilder.MainScenePath, "main", outputRoot, PrepareMain);
            Debug.Log($"Login/Lobby ratio captures created at {outputRoot}.");
        }

        private static void CaptureScene(
            string scenePath,
            string prefix,
            string outputRoot,
            Action prepare)
        {
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            var camera = UnityEngine.Object.FindFirstObjectByType<Camera>()
                         ?? throw new InvalidOperationException($"{scenePath} has no camera.");
            ConfigureCanvases(camera);
            prepare();

            foreach (var size in Sizes)
                Capture(camera, size, prefix, outputRoot);
        }

        private static void PrepareLogin()
        {
            var presenter = UnityEngine.Object.FindFirstObjectByType<TitleBackgroundPresenter>()
                            ?? throw new InvalidOperationException("Login has no TitleBackgroundPresenter.");
            if (!presenter.InitializeAndPlay())
                throw new InvalidOperationException(presenter.LastError);
            var skeleton = presenter.GetComponent<SkeletonAnimation>();
            skeleton.Update(0f);
            skeleton.LateUpdateMesh();
            var mesh = skeleton.GetComponent<MeshFilter>()?.sharedMesh;
            if (mesh == null || mesh.vertexCount == 0)
                throw new InvalidOperationException("Login Title Spine generated no renderable mesh.");
        }

        private static void PrepareMain()
        {
            var view = UnityEngine.Object.FindFirstObjectByType<LobbyScreenView>()
                       ?? throw new InvalidOperationException("Main has no LobbyScreenView.");
            view.RenderProfile("Recovery Preview", 1, 0, 0);
            view.RenderCurrencies(0, 0, 0, 21, 21);
        }

        private static void ConfigureCanvases(Camera camera)
        {
            foreach (var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 0.5f;
            }
        }

        private static void Capture(
            Camera camera,
            CaptureSize size,
            string prefix,
            string outputRoot)
        {
            var renderTexture = new RenderTexture(
                size.Width,
                size.Height,
                24,
                RenderTextureFormat.ARGB32);
            var texture = new Texture2D(size.Width, size.Height, TextureFormat.RGBA32, false);

            try
            {
                camera.targetTexture = renderTexture;
                camera.aspect = (float)size.Width / size.Height;
                Canvas.ForceUpdateCanvases();
                camera.Render();
                RenderTexture.active = renderTexture;
                texture.ReadPixels(new Rect(0f, 0f, size.Width, size.Height), 0, 0, false);
                texture.Apply(false, false);

                var path = Path.Combine(outputRoot, $"{prefix}-{size.Name}.png");
                File.WriteAllBytes(path, texture.EncodeToPNG());
                if (new FileInfo(path).Length < 1024)
                    throw new InvalidOperationException($"Capture is unexpectedly small: {path}");
                Debug.Log($"Captured {path} ({size.Width}x{size.Height}).");
            }
            finally
            {
                camera.targetTexture = null;
                camera.ResetAspect();
                RenderTexture.active = null;
                renderTexture.Release();
                UnityEngine.Object.DestroyImmediate(renderTexture);
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }
    }
}

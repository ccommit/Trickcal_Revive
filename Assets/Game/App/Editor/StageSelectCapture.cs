using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

using TrickcalRevive.MainUI;

namespace TrickcalRevive.App.Editor
{
    /// <summary>StageSelect 화면을 참고 스크린샷과 대조하기 위한 비율 캡처(육안 검증용).</summary>
    public static class StageSelectCapture
    {
        [MenuItem("Tools/Trickcal Revive/Flow/Capture Stage-Select %#F7")]
        public static void Capture()
        {
            StageSelectUIBuilder.BuildPrefab();
            PartySetupUIBuilder.BuildPrefab();
            var outputRoot = Path.GetFullPath(Path.Combine(
                Directory.GetParent(Application.dataPath)?.FullName ?? ".",
                "Artifacts", "SpineRecovery", "StageSelect", "Captures"));
            Directory.CreateDirectory(outputRoot);

            EditorSceneManager.OpenScene(FlowSceneBuilder.MainScenePath, OpenSceneMode.Single);
            var camera = UnityEngine.Object.FindFirstObjectByType<Camera>()
                         ?? throw new InvalidOperationException("Main has no camera.");
            foreach (var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 0.5f;
            }

            var host = UnityEngine.Object.FindFirstObjectByType<ScreenPanelHost>(FindObjectsInactive.Include)
                       ?? throw new InvalidOperationException("Main has no ScreenPanelHost.");
            host.ShowScreen("StageSelect");

            var view = UnityEngine.Object.FindFirstObjectByType<StageSelectView>(FindObjectsInactive.Include)
                       ?? throw new InvalidOperationException("Main has no StageSelectView.");
            view.RenderNodes(SampleNodes());
            var currencies = UnityEngine.Object.FindFirstObjectByType<TopCurrencyPanelView>(FindObjectsInactive.Include)
                             ?? throw new InvalidOperationException("Main has no TopCurrencyPanelView.");
            currencies.Render(644, 2444, 3272587, 10519, 2337514);
            currencies.Show(
                TopCurrencyVisibility.Stamina
                | TopCurrencyVisibility.Gold
                | TopCurrencyVisibility.Elleaf,
                false,
                "스테이지 리스트");

            for (var world = 1; world <= 10; world++)
            {
                view.ShowWorld(world);
                Canvas.ForceUpdateCanvases();
                CaptureOne(
                    camera,
                    1920,
                    1080,
                    Path.Combine(outputRoot, $"stageselect-world{world:00}-16x9.png"));
            }
            File.Copy(
                Path.Combine(outputRoot, "stageselect-world01-16x9.png"),
                Path.Combine(outputRoot, "stageselect-16x9.png"),
                true);
            view.ShowWorld(1);

            // 스테이지 정보 팝업도 캡처(샘플 데이터로 Show).
            var popup = UnityEngine.Object.FindFirstObjectByType<StageInfoPopupView>(FindObjectsInactive.Include);
            if (popup != null)
            {
                popup.Show(SampleStageInfo());
                Canvas.ForceUpdateCanvases();
                CaptureOne(camera, 1920, 1080, Path.Combine(outputRoot, "stageinfo-16x9.png"));
                popup.Hide();
            }

            // 파티 편성 화면 캡처(샘플 배치로 RenderFormation).
            var party = UnityEngine.Object.FindFirstObjectByType<PartySetupView>(FindObjectsInactive.Include);
            if (party != null)
            {
                host.ShowScreen("PartySetup");
                currencies.Show(
                    TopCurrencyVisibility.Stamina
                    | TopCurrencyVisibility.Gold
                    | TopCurrencyVisibility.Elleaf,
                    false,
                    "1-1. 열정의 밭 갈기 시작!");
                party.RenderFormation(SampleFormation(), SamplePlaced());
                Canvas.ForceUpdateCanvases();
                CaptureOne(camera, 1920, 1080, Path.Combine(outputRoot, "partysetup-16x9.png"));

                party.ShowCharacterDetail(0);
                Canvas.ForceUpdateCanvases();
                CaptureOne(camera, 1920, 1080, Path.Combine(outputRoot, "partysetup-characterdetail-16x9.png"));
                party.transform.Find("CharacterDetailModal")?.gameObject.SetActive(false);
            }

            // 화면 전환(초록 별 와이프) 중간 프레임 캡처.
            var transition = UnityEngine.Object.FindFirstObjectByType<ScreenTransitionView>(FindObjectsInactive.Include);
            if (transition != null)
            {
                host.ShowScreen("StageSelect");
                transition.PreviewAt(0.5f);
                Canvas.ForceUpdateCanvases();
                CaptureOne(camera, 1920, 1080, Path.Combine(outputRoot, "transition-16x9.png"));
            }
            Debug.Log($"StageSelect captures created at {outputRoot}.");
        }

        // fixture 초기 상태: 1-1 클리어★3 / 1-2 현재 / 1-3~1-10 잠금.
        private static List<StageNodeData> SampleNodes()
        {
            var list = new List<StageNodeData>
            {
                new StageNodeData { StageId = "1-1", Label = "1-1", State = StageNodeState.Cleared, Stars = 3 },
                new StageNodeData { StageId = "1-2", Label = "1-2", State = StageNodeState.Current, Stars = 0 },
            };
            for (var i = 3; i <= 10; i++)
                list.Add(new StageNodeData { StageId = $"1-{i}", Label = $"1-{i}", State = StageNodeState.Locked, Stars = 0 });
            return list;
        }

        private static StageInfoData SampleStageInfo() => new StageInfoData
        {
            StageName = "새참 나르기",
            Stars = 3,
            RecommendedPower = 1_600_000,
            PersonalityText = "순수",
            MonsterIds = new List<string> { "gluttonbear", "lupalu", "magicfork" },
            Rewards = new List<StageInfoRewardEntry>
            {
                new StageInfoRewardEntry { RewardType = "Gold", Amount = 2300 },
                new StageInfoRewardEntry { RewardType = "Macaron", Amount = 260 },
                new StageInfoRewardEntry { RewardType = "Elleaf", Amount = 20 },
            },
        };

        private static List<PartyFormationEntry> SampleFormation() => new List<PartyFormationEntry>
        {
            new PartyFormationEntry { X = 3, Y = 2, CharacterId = "maestromk2" },
            new PartyFormationEntry { X = 3, Y = 1, CharacterId = "chloe" },
            new PartyFormationEntry { X = 2, Y = 2, CharacterId = "diana" },
            new PartyFormationEntry { X = 2, Y = 1, CharacterId = "arnet" },
            new PartyFormationEntry { X = 1, Y = 2, CharacterId = "maison" },
            new PartyFormationEntry { X = 1, Y = 1, CharacterId = "yumimi" },
        };

        private static List<string> SamplePlaced() => new List<string>
        {
            "pc_maestromk2", "pc_chloe", "pc_diana", "pc_arnet", "pc_maison", "pc_yumimi",
        };

        private static void CaptureOne(Camera camera, int width, int height, string path)
        {
            var renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            try
            {
                renderTexture.Create();
                if (!renderTexture.IsCreated())
                    throw new InvalidOperationException(
                        "Capture RenderTexture could not be created. Run the capture without -nographics.");
                camera.targetTexture = renderTexture;
                camera.aspect = (float)width / height;
                Canvas.ForceUpdateCanvases();
                camera.Render();
                RenderTexture.active = renderTexture;
                texture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0, false);
                texture.Apply(false, false);
                File.WriteAllBytes(path, texture.EncodeToPNG());
                if (new FileInfo(path).Length < 1024)
                    throw new InvalidOperationException($"Capture unexpectedly small: {path}");
                Debug.Log($"Captured {path} ({width}x{height}).");
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

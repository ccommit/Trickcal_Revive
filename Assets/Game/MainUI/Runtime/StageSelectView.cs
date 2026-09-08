using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TrickcalRevive.MainUI
{
    /// <summary>침략 "스테이지 리스트" 화면. 월드 1~10 배경/테마 전환과 아이소 노드를 표시한다.</summary>
    public sealed class StageSelectView : MonoBehaviour
    {
        [SerializeField] private Image background;
        [SerializeField] private StageNodeView[] nodes;
        [SerializeField] private Button worldMapButton;
        [SerializeField] private Button previousWorldButton;
        [SerializeField] private Button nextWorldButton;
        [SerializeField] private ScrollRect worldScroll;
        [SerializeField] private Sprite[] worldBackgrounds;
        [SerializeField] private Sprite[] worldCurrentTiles;
        [SerializeField] private Sprite[] worldClearedTiles;
        [SerializeField] private Sprite[] worldUnclearedTiles;
        [SerializeField] private Sprite[] worldLockedTiles;

        public event Action<string> NodeClicked;

        private IReadOnlyList<StageNodeData> worldOneData;
        private int currentWorld = 1;

        public bool IsReady
        {
            get
            {
                if (background == null || background.sprite == null || worldMapButton == null
                    || previousWorldButton == null || nextWorldButton == null || worldScroll == null
                    || !HasTen(worldBackgrounds) || !HasTen(worldCurrentTiles)
                    || !HasTen(worldClearedTiles) || !HasTen(worldUnclearedTiles) || !HasTen(worldLockedTiles)
                    || nodes == null || nodes.Length == 0)
                    return false;
                foreach (var node in nodes)
                {
                    if (node == null || !node.IsReady)
                        return false;
                }
                return true;
            }
        }

        public void Configure(
            Image mapBackground,
            StageNodeView[] nodeViews,
            Button worldMap,
            Button previousWorld,
            Button nextWorld,
            ScrollRect scroll,
            Sprite[] backgrounds,
            Sprite[] currentTiles,
            Sprite[] clearedTiles,
            Sprite[] unclearedTiles,
            Sprite[] lockedTiles)
        {
            background = mapBackground;
            nodes = nodeViews;
            worldMapButton = worldMap;
            previousWorldButton = previousWorld;
            nextWorldButton = nextWorld;
            worldScroll = scroll;
            worldBackgrounds = backgrounds;
            worldCurrentTiles = currentTiles;
            worldClearedTiles = clearedTiles;
            worldUnclearedTiles = unclearedTiles;
            worldLockedTiles = lockedTiles;
            ShowWorld(1);
        }

        private void OnEnable()
        {
            if (nodes != null)
            {
                foreach (var node in nodes)
                {
                    if (node != null)
                        node.Clicked += RaiseNodeClicked;
                }
            }
            ShowWorld(currentWorld);
        }

        private void OnDisable()
        {
            if (nodes != null)
            {
                foreach (var node in nodes)
                {
                    if (node != null)
                        node.Clicked -= RaiseNodeClicked;
                }
            }
        }

        public void RenderNodes(IReadOnlyList<StageNodeData> data)
        {
            worldOneData = data;
            RenderCurrentWorld();
        }

        public void ShowWorld(int world)
        {
            currentWorld = Mathf.Clamp(world, 1, 10);
            var index = currentWorld - 1;
            if (background != null && HasTen(worldBackgrounds))
                background.sprite = worldBackgrounds[index];

            if (nodes != null && HasTen(worldCurrentTiles) && HasTen(worldClearedTiles)
                && HasTen(worldUnclearedTiles) && HasTen(worldLockedTiles))
            {
                foreach (var node in nodes)
                {
                    node?.SetTheme(
                        worldCurrentTiles[index],
                        worldClearedTiles[index],
                        worldUnclearedTiles[index],
                        worldLockedTiles[index]);
                }
            }

            if (previousWorldButton != null)
                previousWorldButton.gameObject.SetActive(currentWorld > 1);
            if (nextWorldButton != null)
                nextWorldButton.gameObject.SetActive(currentWorld < 10);
            if (worldScroll?.content != null)
                worldScroll.content.anchoredPosition = Vector2.zero;
            RenderCurrentWorld();
        }

        private void RenderCurrentWorld()
        {
            if (nodes == null)
                return;
            for (var index = 0; index < nodes.Length; index++)
            {
                var node = nodes[index];
                if (node == null)
                    continue;
                node.gameObject.SetActive(true);
                if (currentWorld == 1 && worldOneData != null && index < worldOneData.Count)
                {
                    node.Render(worldOneData[index]);
                    continue;
                }

                node.Render(new StageNodeData
                {
                    StageId = $"{currentWorld}-{index + 1}",
                    Label = $"{currentWorld}-{index + 1}",
                    State = StageNodeState.Locked,
                    Stars = 0,
                });
            }
        }

        public void ShowPreviousWorld() => ShowWorld(currentWorld - 1);
        public void ShowNextWorld() => ShowWorld(currentWorld + 1);

        private static bool HasTen(Sprite[] sprites)
        {
            if (sprites == null || sprites.Length != 10)
                return false;
            foreach (var sprite in sprites)
            {
                if (sprite == null)
                    return false;
            }
            return true;
        }

        private void RaiseNodeClicked(string stageId) => NodeClicked?.Invoke(stageId);
    }
}

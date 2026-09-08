using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TrickcalRevive.MainUI
{
    /// <summary>스테이지 선택 화면의 노드 하나를 상태에 맞게 표시한다.</summary>
    public sealed class StageNodeView : MonoBehaviour
    {
        private static readonly Color32 LockedTint = new Color32(145, 145, 145, 255);

        [SerializeField] private Image tile;
        [SerializeField] private Image tower;
        [SerializeField] private Image garland;
        [SerializeField] private Image mapDeco;
        [SerializeField] private Image numberPlate;
        [SerializeField] private Image lockIcon;
        [SerializeField] private Image[] stars;
        [SerializeField] private TMP_Text numberLabel;
        [SerializeField] private Button button;

        [SerializeField] private Sprite currentSprite;
        [SerializeField] private Sprite clearedSprite;
        [SerializeField] private Sprite unclearedSprite;
        [SerializeField] private Sprite lockedSprite;
        [SerializeField] private Sprite filledStarSprite;
        [SerializeField] private Sprite emptyStarSprite;

        public event Action<string> Clicked;

        private string stageId;

        public bool IsReady =>
            tile != null && tower != null && garland != null && mapDeco != null && numberPlate != null
            && lockIcon != null && numberLabel != null && button != null
            && stars != null && stars.Length == 3
            && currentSprite != null && clearedSprite != null
            && unclearedSprite != null && lockedSprite != null
            && filledStarSprite != null && emptyStarSprite != null;

        public void Configure(
            Image tileImage,
            Image towerImage,
            Image garlandImage,
            Image mapDecoImage,
            Image numberPlateImage,
            Image lockImage,
            Image[] starImages,
            TMP_Text number,
            Button nodeButton,
            Sprite current,
            Sprite cleared,
            Sprite uncleared,
            Sprite locked,
            Sprite filledStar,
            Sprite emptyStar)
        {
            tile = tileImage;
            tower = towerImage;
            garland = garlandImage;
            mapDeco = mapDecoImage;
            numberPlate = numberPlateImage;
            lockIcon = lockImage;
            stars = starImages;
            numberLabel = number;
            button = nodeButton;
            currentSprite = current;
            clearedSprite = cleared;
            unclearedSprite = uncleared;
            lockedSprite = locked;
            filledStarSprite = filledStar;
            emptyStarSprite = emptyStar;
        }

        private void OnEnable()
        {
            if (button != null)
                button.onClick.AddListener(RaiseClicked);
        }

        private void OnDisable()
        {
            if (button != null)
                button.onClick.RemoveListener(RaiseClicked);
        }

        public void Render(StageNodeData data)
        {
            stageId = data.StageId;
            var isLocked = data.State == StageNodeState.Locked;
            var contentTint = isLocked ? LockedTint : (Color32)Color.white;

            if (numberLabel != null)
            {
                numberLabel.text = FormatStageLabel(data.Label);
                numberLabel.color = contentTint;
            }

            if (tile != null)
            {
                tile.sprite = SpriteFor(data.State);
                tile.color = contentTint;
            }

            SetTint(garland, contentTint);
            SetTint(numberPlate, contentTint);

            if (mapDeco != null)
            {
                mapDeco.gameObject.SetActive(data.State == StageNodeState.Cleared);
                mapDeco.color = Color.white;
            }

            if (tower != null)
            {
                tower.gameObject.SetActive(!isLocked && data.Stars > 0);
                tower.color = contentTint;
            }

            if (lockIcon != null)
            {
                lockIcon.gameObject.SetActive(isLocked);
                lockIcon.color = Color.white;
            }

            if (stars != null)
            {
                for (var index = 0; index < stars.Length; index++)
                {
                    if (stars[index] == null)
                        continue;

                    stars[index].sprite = index < data.Stars ? filledStarSprite : emptyStarSprite;
                    stars[index].color = contentTint;
                }
            }

            if (button != null)
                button.interactable = !isLocked;
        }

        public void SetTheme(Sprite current, Sprite cleared, Sprite uncleared, Sprite locked)
        {
            if (current != null)
                currentSprite = current;
            if (cleared != null)
                clearedSprite = cleared;
            if (uncleared != null)
                unclearedSprite = uncleared;
            if (locked != null)
                lockedSprite = locked;
        }

        private Sprite SpriteFor(StageNodeState state)
        {
            switch (state)
            {
                case StageNodeState.Cleared: return clearedSprite;
                case StageNodeState.Uncleared: return unclearedSprite;
                case StageNodeState.Locked: return lockedSprite;
                default: return currentSprite;
            }
        }

        private static void SetTint(Graphic graphic, Color color)
        {
            if (graphic != null)
                graphic.color = color;
        }

        private static string FormatStageLabel(string label)
        {
            return string.IsNullOrEmpty(label)
                ? string.Empty
                : label.Replace(" ", string.Empty).Replace("-", " - ");
        }

        private void RaiseClicked() => Clicked?.Invoke(stageId);
    }
}

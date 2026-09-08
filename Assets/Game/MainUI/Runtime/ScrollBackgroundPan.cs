using UnityEngine;
using UnityEngine.UI;

namespace TrickcalRevive.MainUI
{
    /// <summary>
    /// 스테이지 리스트의 맵 배경을 ScrollRect와 함께 이동시킨다(원본처럼 맵이 타일과 같이 팬).
    /// 배경은 화면보다 크게(오버사이즈) 만들어 팬 시 가장자리가 드러나지 않게 한다.
    /// </summary>
    public sealed class ScrollBackgroundPan : MonoBehaviour
    {
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private RectTransform background;
        [SerializeField] private float factor = 1f;

        private Vector2 baseline;

        public void Configure(ScrollRect scroll, RectTransform bg, float panFactor = 1f)
        {
            scrollRect = scroll;
            background = bg;
            factor = panFactor;
            if (bg != null)
                baseline = bg.anchoredPosition;
        }

        private void OnEnable()
        {
            if (background != null)
                baseline = background.anchoredPosition;
            if (scrollRect != null)
            {
                scrollRect.onValueChanged.AddListener(OnScroll);
                RefreshPosition();
            }
        }

        private void OnDisable()
        {
            if (scrollRect != null)
                scrollRect.onValueChanged.RemoveListener(OnScroll);
        }

        private void OnScroll(Vector2 _)
        {
            RefreshPosition();
        }

        public void RefreshPosition()
        {
            if (scrollRect == null || scrollRect.content == null || background == null)
                return;
            background.anchoredPosition = baseline + scrollRect.content.anchoredPosition * factor;
        }
    }
}

using System.Collections;
using UnityEngine;

namespace TrickcalRevive.MainUI
{
    /// <summary>
    /// 중앙 팝업의 스케일 오버슈트 등장 효과. 원본 AnimationClip은 추출 자료에서
    /// 확인되지 않아 캡처의 "뽀잉" 인상을 결정론적으로 복원한 값이다.
    /// </summary>
    public sealed class PopupBounceView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private float duration = 0.28f;
        [SerializeField] private float startScale = 0.72f;
        [SerializeField] private float overshootScale = 1.08f;

        private Coroutine routine;

        public void Configure(CanvasGroup group, float seconds = 0.28f, float start = 0.72f, float overshoot = 1.08f)
        {
            canvasGroup = group;
            duration = Mathf.Max(0.01f, seconds);
            startScale = start;
            overshootScale = overshoot;
        }

        private void OnEnable()
        {
            if (Application.isPlaying)
                Play();
            else
            {
                transform.localScale = Vector3.one;
                if (canvasGroup != null)
                    canvasGroup.alpha = 1f;
            }
        }

        private void OnDisable()
        {
            if (routine != null)
                StopCoroutine(routine);
            routine = null;
        }

        public void Play()
        {
            if (!isActiveAndEnabled)
                return;
            if (routine != null)
                StopCoroutine(routine);
            routine = StartCoroutine(Animate());
        }

        private IEnumerator Animate()
        {
            transform.localScale = Vector3.one * startScale;
            if (canvasGroup != null)
                canvasGroup.alpha = 0f;

            // 팝업을 연 프레임의 누적 delta가 커도 시작 자세를 건너뛰지 않는다.
            // 최소 한 렌더 프레임 동안 중앙의 작은 스케일을 보장한다.
            yield return null;

            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var progress = Mathf.Clamp01(elapsed / duration);
                var scale = progress < 0.72f
                    ? Mathf.Lerp(startScale, overshootScale, EaseOutBack(progress / 0.72f))
                    : Mathf.Lerp(overshootScale, 1f, SmoothStep((progress - 0.72f) / 0.28f));
                transform.localScale = Vector3.one * scale;
                if (canvasGroup != null)
                    canvasGroup.alpha = Mathf.Clamp01(progress * 3f);
                yield return null;
            }

            transform.localScale = Vector3.one;
            if (canvasGroup != null)
                canvasGroup.alpha = 1f;
            routine = null;
        }

        private static float EaseOutBack(float value)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            var x = value - 1f;
            return 1f + c3 * x * x * x + c1 * x * x;
        }

        private static float SmoothStep(float value)
        {
            value = Mathf.Clamp01(value);
            return value * value * (3f - 2f * value);
        }
    }
}

using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TrickcalRevive.MainUI
{
    /// <summary>
    /// Reproduces the scale sequence serialized on the original main-lobby buttons.
    /// The source proves the scale values; the short interpolation durations are a
    /// deterministic reconstruction because no original tween duration was found.
    /// </summary>
    public sealed class LobbyButtonPressFeedback : MonoBehaviour,
        IPointerDownHandler,
        IPointerUpHandler,
        IPointerExitHandler
    {
        [SerializeField] private RectTransform target;
        [SerializeField] private Vector2 baseScale = Vector2.one;
        [SerializeField] private Vector2 pressedScale = new Vector2(0.9f, 1.2f);
        [SerializeField] private Vector2 releaseScale = new Vector2(1.1f, 0.9f);
        [SerializeField, Min(0f)] private float pressDuration = 0.07f;
        [SerializeField, Min(0f)] private float releaseDuration = 0.06f;
        [SerializeField, Min(0f)] private float settleDuration = 0.09f;

        private Coroutine transition;
        private bool pointerIsDown;

        public Vector2 BaseScale => baseScale;
        public Vector2 PressedScale => pressedScale;
        public Vector2 ReleaseScale => releaseScale;
        public Vector3 CurrentScale => Target.localScale;

        private RectTransform Target => target != null ? target : (RectTransform)transform;

        public void Configure(
            RectTransform scaleTarget,
            Vector2 normal,
            Vector2 pressed,
            Vector2 released)
        {
            target = scaleTarget;
            baseScale = normal;
            pressedScale = pressed;
            releaseScale = released;
            SetScale(baseScale);
        }

        private void OnEnable()
        {
            pointerIsDown = false;
            SetScale(baseScale);
        }

        private void OnDisable()
        {
            StopTransition();
            pointerIsDown = false;
            SetScale(baseScale);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            pointerIsDown = true;
            StartTransition(AnimateTo(pressedScale, pressDuration));
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!pointerIsDown)
                return;
            pointerIsDown = false;
            StartTransition(ReleaseThenSettle());
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!pointerIsDown)
                return;
            pointerIsDown = false;
            StartTransition(ReleaseThenSettle());
        }

        private IEnumerator ReleaseThenSettle()
        {
            yield return AnimateTo(releaseScale, releaseDuration);
            yield return AnimateTo(baseScale, settleDuration);
        }

        private IEnumerator AnimateTo(Vector2 destination, float duration)
        {
            var from = Target.localScale;
            var to = new Vector3(destination.x, destination.y, 1f);
            if (duration <= 0f)
            {
                Target.localScale = to;
                yield break;
            }

            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var progress = Mathf.Clamp01(elapsed / duration);
                Target.localScale = Vector3.LerpUnclamped(from, to, SmoothStep(progress));
                yield return null;
            }
            Target.localScale = to;
        }

        private void StartTransition(IEnumerator routine)
        {
            StopTransition();
            transition = StartCoroutine(routine);
        }

        private void StopTransition()
        {
            if (transition == null)
                return;
            StopCoroutine(transition);
            transition = null;
        }

        private void SetScale(Vector2 scale)
        {
            Target.localScale = new Vector3(scale.x, scale.y, 1f);
        }

        private static float SmoothStep(float value)
        {
            return value * value * (3f - 2f * value);
        }
    }
}

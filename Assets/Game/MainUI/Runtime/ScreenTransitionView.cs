using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace TrickcalRevive.MainUI
{
    /// <summary>
    /// 화면 전환 연출(04 영상의 속빈 별 와이프). 초록 풀스크린에 별 모양 구멍이 뚫려
    /// 뒤 화면이 보이고, 구멍이 줄며 닫힌 순간(peak) 실제 화면을 바꾼 뒤 다시 열린다.
    /// 구멍/색은 StarWipe 셰이더(_Progress 0=열림, 1=닫힘). 타이밍은 결정론적 추정값.
    /// </summary>
    public sealed class ScreenTransitionView : MonoBehaviour
    {
        private static readonly int ProgressId = Shader.PropertyToID("_Progress");
        private static readonly int CenterXId = Shader.PropertyToID("_CenterX");
        private static readonly int CenterYId = Shader.PropertyToID("_CenterY");

        [SerializeField] private GameObject overlay;
        [SerializeField] private Image cover;
        [SerializeField] private float coverSeconds = 0.5f;
        [SerializeField] private float revealSeconds = 0.5f;

        private bool playing;

        public bool IsReady => overlay != null && cover != null && cover.material != null;

        public void Configure(GameObject overlayRoot, Image coverImage)
        {
            overlay = overlayRoot;
            cover = coverImage;
            if (overlay != null)
                overlay.SetActive(false);
        }

        /// <summary>별 구멍이 닫히는 중심(화면 UV 0..1). 진입 버튼 위치에 맞춘다.</summary>
        public void SetHoleCenter(float u, float v)
        {
            if (cover != null && cover.material != null)
            {
                cover.material.SetFloat(CenterXId, u);
                cover.material.SetFloat(CenterYId, v);
            }
        }

        /// <summary>전환 연출을 재생하고, 구멍이 닫힌 순간 atPeak을 호출한다.</summary>
        public void Play(Action atPeak)
        {
            if (!IsReady || !isActiveAndEnabled || playing)
            {
                atPeak?.Invoke();
                return;
            }
            StartCoroutine(Run(atPeak));
        }

        private IEnumerator Run(Action atPeak)
        {
            playing = true;
            overlay.SetActive(true);
            yield return Animate(0f, 1f, coverSeconds);   // 구멍 닫힘
            atPeak?.Invoke();
            yield return Animate(1f, 0f, revealSeconds);   // 구멍 열림
            overlay.SetActive(false);
            playing = false;
        }

        private IEnumerator Animate(float from, float to, float seconds)
        {
            var elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                Apply(Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / seconds)));
                yield return null;
            }
            Apply(to);
        }

        private void Apply(float progress)
        {
            if (cover != null && cover.material != null)
                cover.material.SetFloat(ProgressId, progress);
        }

        /// <summary>정적 캡처/미리보기용 — 지정 진행도로 표시한다.</summary>
        public void PreviewAt(float progress)
        {
            if (overlay != null)
                overlay.SetActive(true);
            Apply(Mathf.Clamp01(progress));
        }
    }
}

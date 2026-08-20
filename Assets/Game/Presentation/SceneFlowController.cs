using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TrickcalRevive.Presentation
{
    public class SceneFlowController : MonoBehaviour, INavigationService
    {
        private NavigationHistory history;
        private IScreenNavigator screenNavigator;

        private void Awake()
        {
            if (FindObjectsByType<SceneFlowController>(FindObjectsSortMode.None).Length > 1)
            {
                Destroy(gameObject);
                return;
            }

            DontDestroyOnLoad(gameObject);
        }

        /// <summary>GameApplication이 켜질 때 한 번 부른다. Go/Back보다 먼저 호출돼야 한다.</summary>
        public void Configure(IPopupService popups, IScreenNavigator screens = null)
        {
            screenNavigator = screens;
            history = new NavigationHistory(popups, screens);
        }

        public void Go(string sceneId)
        {
            EnsureConfigured();

            if (!history.TryBeginTransition())
                return;

            history.RecordVisit(sceneId);
            SceneManager.LoadScene(sceneId);
            history.EndTransition();
        }

        public void ShowLoading(string type, string targetScene)
        {
            // 실제 로딩 화면(비주얼)은 UI 배치 이후 붙인다. 지금은 전투 진입 등에서
            // 아직 쓰이지 않으므로 뼈대만 둔다(02_로비화면전환_설계서 §2.1).
            throw new System.NotImplementedException();
        }

        public void Back()
        {
            EnsureConfigured();

            var result = history.ResolveBack();

            if (result.Type == BackResult.Kind.ScreenChanged)
            {
                screenNavigator?.GoBack();
                return;
            }

            if (result.Type != BackResult.Kind.Navigate)
                return;

            if (!history.TryBeginTransition())
                return;

            SceneManager.LoadScene(result.TargetScene);
            history.EndTransition();
        }

        public void LockBack()
        {
            EnsureConfigured();
            history.LockBack();
        }

        public void UnlockBack()
        {
            EnsureConfigured();
            history.UnlockBack();
        }

        /// <summary>
        /// <see cref="Configure"/> 없이 부르면 history가 null이라 어디서 잘못됐는지 안 보이는
        /// 참조 오류가 난다. 원인을 그대로 말해주는 예외로 바꾼다.
        /// </summary>
        private void EnsureConfigured()
        {
            if (history == null)
                throw new InvalidOperationException(
                    "SceneFlowController.Configure()가 호출되지 않았다. " +
                    "GameApplication이 루트 컨테이너를 만들 때 한 번 호출한다 — " +
                    "Login 씬을 거치지 않고 이 씬을 바로 열었는지 확인하라.");
        }
    }
}

using UnityEngine;
using UnityEngine.SceneManagement;

namespace TrickcalRevive.Presentation
{
    public class SceneFlowController : MonoBehaviour, INavigationService
    {
        private NavigationHistory history;

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
        public void Configure(IPopupService popups)
        {
            history = new NavigationHistory(popups);
        }

        public void Go(string sceneId)
        {
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
            var result = history.ResolveBack();
            if (result.Type != BackResult.Kind.Navigate)
                return;

            if (!history.TryBeginTransition())
                return;

            SceneManager.LoadScene(result.TargetScene);
            history.EndTransition();
        }

        public void LockBack() => history.LockBack();
        public void UnlockBack() => history.UnlockBack();
    }
}

using System.Collections.Generic;

namespace TrickcalRevive.Presentation
{
    /// <summary>
    /// 씬 방문 이력과 뒤로가기 판단만 한다. 유니티(SceneManager)를 몰라 유닛테스트 가능하다.
    /// 실제로 씬을 불러오는 건 SceneFlowController의 일이다.
    /// </summary>
    public sealed class NavigationHistory
    {
        private readonly Stack<string> scenes = new Stack<string>();
        private readonly IPopupService popupService;
        private bool backLocked;
        private bool isTransitioning;

        public NavigationHistory(IPopupService popupService)
        {
            this.popupService = popupService;
        }

        public void LockBack() => backLocked = true;
        public void UnlockBack() => backLocked = false;

        public bool TryBeginTransition()
        {
            if (isTransitioning)
                return false;
            isTransitioning = true;
            return true;
        }

        public void EndTransition() => isTransitioning = false;

        public void RecordVisit(string sceneId) => scenes.Push(sceneId);

        /// <summary>팝업이 열려있으면 그것부터 닫는다. 아니면 스택을 보고 이전 씬을 판단한다.</summary>
        public BackResult ResolveBack()
        {
            if (isTransitioning)
                return BackResult.None;

            if (popupService != null && popupService.IsAnyOpen)
            {
                popupService.CloseTop();
                return BackResult.PopupClosed;
            }

            if (backLocked || scenes.Count <= 1)
                return BackResult.None;

            scenes.Pop();
            return BackResult.Navigate(scenes.Peek());
        }
    }
}

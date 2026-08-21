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
        private readonly IScreenNavigator screenNavigator;
        private bool backLocked;
        private bool isTransitioning;

        public NavigationHistory(IPopupService popupService, IScreenNavigator screenNavigator = null)
        {
            this.popupService = popupService;
            this.screenNavigator = screenNavigator;
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

        /// <summary>팝업 → Main씬 내부 화면 → 씬 방문 스택 순서로 갈 곳을 찾는다.</summary>
        public BackResult ResolveBack()
        {
            if (isTransitioning)
                return BackResult.None;

            if (popupService != null && popupService.IsAnyOpen)
            {
                popupService.CloseTop();
                return BackResult.PopupClosed;
            }

            if (!backLocked && screenNavigator != null && screenNavigator.TryPeekPrevious(out var previousScreen))
                return BackResult.ScreenChanged(previousScreen);

            if (backLocked || scenes.Count <= 1)
                return BackResult.None;

            scenes.Pop();
            return BackResult.Navigate(scenes.Peek());
        }
    }
}

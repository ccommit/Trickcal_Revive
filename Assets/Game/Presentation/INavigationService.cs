namespace TrickcalRevive.Presentation
{
    /// <summary>화면 전환 창구. 구체 구현은 SceneFlowController.</summary>
    public interface INavigationService
    {
        void Go(string sceneId);
        void Back();
        void LockBack();
        void UnlockBack();
        void ShowLoading(string type, string targetScene);
    }
}

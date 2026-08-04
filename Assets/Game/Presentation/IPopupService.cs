namespace TrickcalRevive.Presentation
{
    /// <summary>지금 열려있는 팝업이 뭔지만 추적한다. 실제 표시·애니메이션은 MainUI가 한다.</summary>
    public interface IPopupService
    {
        void Open(string popupId);
        void Close(string popupId);
        void CloseTop();
        bool IsAnyOpen { get; }
        string TopPopupId { get; }
    }
}

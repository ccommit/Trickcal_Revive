using System.Collections.Generic;

namespace TrickcalRevive.Presentation
{
    public sealed class PopupService : IPopupService
    {
        private readonly Stack<string> openPopups = new Stack<string>();

        public void Open(string popupId) => openPopups.Push(popupId);

        public void Close(string popupId)
        {
            if (openPopups.Count > 0 && openPopups.Peek() == popupId)
                openPopups.Pop();
        }

        public void CloseTop()
        {
            if (openPopups.Count > 0)
                openPopups.Pop();
        }

        public bool IsAnyOpen => openPopups.Count > 0;
        public string TopPopupId => openPopups.Count > 0 ? openPopups.Peek() : null;
    }
}

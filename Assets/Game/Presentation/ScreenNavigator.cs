using System;
using System.Collections.Generic;

namespace TrickcalRevive.Presentation
{
    public sealed class ScreenNavigator : IScreenNavigator
    {
        private readonly Stack<string> history = new Stack<string>();

        public string CurrentScreen { get; private set; }
        public event Action<string> ScreenChanged;

        public void Show(string screenId)
        {
            if (CurrentScreen == screenId)
                return;

            if (CurrentScreen != null)
                history.Push(CurrentScreen);

            CurrentScreen = screenId;
            ScreenChanged?.Invoke(screenId);
        }

        public bool TryPeekPrevious(out string previousScreenId)
        {
            if (history.Count == 0)
            {
                previousScreenId = null;
                return false;
            }

            previousScreenId = history.Peek();
            return true;
        }

        public bool GoBack()
        {
            if (history.Count == 0)
                return false;

            CurrentScreen = history.Pop();
            ScreenChanged?.Invoke(CurrentScreen);
            return true;
        }

        public void Reset(string initialScreenId)
        {
            history.Clear();
            CurrentScreen = initialScreenId;
            ScreenChanged?.Invoke(initialScreenId);
        }
    }
}

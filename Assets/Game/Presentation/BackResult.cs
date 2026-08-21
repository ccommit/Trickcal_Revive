namespace TrickcalRevive.Presentation
{
    /// <summary>Back()이 실제로 뭘 해야 하는지.</summary>
    public readonly struct BackResult
    {
        public enum Kind { None, PopupClosed, ScreenChanged, Navigate }

        public Kind Type { get; }
        public string TargetScreen { get; }
        public string TargetScene { get; }

        private BackResult(Kind type, string targetScreen, string targetScene)
        {
            Type = type;
            TargetScreen = targetScreen;
            TargetScene = targetScene;
        }

        public static BackResult None { get; } = new BackResult(Kind.None, null, null);
        public static BackResult PopupClosed { get; } = new BackResult(Kind.PopupClosed, null, null);
        public static BackResult ScreenChanged(string screenId) => new BackResult(Kind.ScreenChanged, screenId, null);
        public static BackResult Navigate(string sceneId) => new BackResult(Kind.Navigate, null, sceneId);
    }
}

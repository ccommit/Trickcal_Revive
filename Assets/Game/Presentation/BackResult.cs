namespace TrickcalRevive.Presentation
{
    /// <summary>Back()이 실제로 뭘 해야 하는지.</summary>
    public readonly struct BackResult
    {
        public enum Kind { None, PopupClosed, Navigate }

        public Kind Type { get; }
        public string TargetScene { get; }

        private BackResult(Kind type, string targetScene)
        {
            Type = type;
            TargetScene = targetScene;
        }

        public static BackResult None { get; } = new BackResult(Kind.None, null);
        public static BackResult PopupClosed { get; } = new BackResult(Kind.PopupClosed, null);
        public static BackResult Navigate(string sceneId) => new BackResult(Kind.Navigate, sceneId);
    }
}

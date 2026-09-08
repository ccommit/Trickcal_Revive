using Spine.Unity;
using UnityEngine;

namespace TrickcalRevive.MainUI
{
    public sealed class TitleBackgroundPresenter : MonoBehaviour
    {
        public const string ConfirmedSkin = "Normal";
        public const string ConfirmedStartAnimation = "Start";
        public const string ConfirmedIdleAnimation = "Idle";

        [SerializeField] private SkeletonAnimation skeletonAnimation;

        public bool IsReady { get; private set; }
        public string LastError { get; private set; }

        public void Configure(SkeletonAnimation animation)
        {
            skeletonAnimation = animation;
        }

        private void Start()
        {
            InitializeAndPlay();
        }

        public bool InitializeAndPlay()
        {
            IsReady = false;
            LastError = string.Empty;
            if (skeletonAnimation == null || skeletonAnimation.skeletonDataAsset == null)
                return Fail("Title Spine is not configured.");

            skeletonAnimation.initialSkinName = ConfirmedSkin;
            skeletonAnimation.Initialize(true);
            var skeleton = skeletonAnimation.Skeleton;
            var data = skeleton?.Data;
            if (data == null)
                return Fail("Title Spine data could not be initialized.");
            if (data.FindSkin(ConfirmedSkin) == null)
                return Fail($"Title skin '{ConfirmedSkin}' is missing.");
            if (data.FindAnimation(ConfirmedStartAnimation) == null)
                return Fail($"Title animation '{ConfirmedStartAnimation}' is missing.");
            if (data.FindAnimation(ConfirmedIdleAnimation) == null)
                return Fail($"Title animation '{ConfirmedIdleAnimation}' is missing.");

            skeletonAnimation.AnimationState.SetAnimation(0, ConfirmedStartAnimation, false);
            skeletonAnimation.AnimationState.AddAnimation(0, ConfirmedIdleAnimation, true, 0f);
            IsReady = true;
            return true;
        }

        private bool Fail(string message)
        {
            LastError = message;
            Debug.LogError(message, this);
            return false;
        }
    }
}

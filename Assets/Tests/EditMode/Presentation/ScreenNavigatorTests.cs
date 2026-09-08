using NUnit.Framework;
using TrickcalRevive.Presentation;

namespace TrickcalRevive.Presentation.Tests
{
    public sealed class ScreenNavigatorTests
    {
        [Test]
        public void Show는_현재_화면을_스택에_쌓고_전환한다()
        {
            var nav = new ScreenNavigator();
            nav.Reset(ScreenIds.Lobby);

            nav.Show(ScreenIds.StageSelect);

            Assert.That(nav.CurrentScreen, Is.EqualTo(ScreenIds.StageSelect));
            Assert.That(nav.TryPeekPrevious(out var previous), Is.True);
            Assert.That(previous, Is.EqualTo(ScreenIds.Lobby));
        }

        [Test]
        public void Show는_screenId_변경_이벤트를_발행한다()
        {
            var nav = new ScreenNavigator();
            nav.Reset(ScreenIds.Lobby);
            string received = null;
            nav.ScreenChanged += id => received = id;

            nav.Show(ScreenIds.StageSelect);

            Assert.That(received, Is.EqualTo(ScreenIds.StageSelect));
        }

        [Test]
        public void GoBack은_스택을_pop만_하고_다시_쌓지_않는다()
        {
            var nav = new ScreenNavigator();
            nav.Reset(ScreenIds.Lobby);
            nav.Show(ScreenIds.StageSelect);
            nav.Show(ScreenIds.PartySetup);

            var wentBack = nav.GoBack();

            Assert.That(wentBack, Is.True);
            Assert.That(nav.CurrentScreen, Is.EqualTo(ScreenIds.StageSelect));
            Assert.That(nav.TryPeekPrevious(out var previous), Is.True);
            Assert.That(previous, Is.EqualTo(ScreenIds.Lobby));
        }

        [Test]
        public void Reset은_스택을_비우고_초기_화면에서_다시_시작한다()
        {
            var nav = new ScreenNavigator();
            nav.Show(ScreenIds.Lobby);
            nav.Show(ScreenIds.StageSelect);

            nav.Reset(ScreenIds.Lobby);

            Assert.That(nav.CurrentScreen, Is.EqualTo(ScreenIds.Lobby));
            Assert.That(nav.TryPeekPrevious(out _), Is.False);
        }

        [Test]
        public void 더_갈_곳_없으면_GoBack은_false를_돌려준다()
        {
            var nav = new ScreenNavigator();
            nav.Reset(ScreenIds.Lobby);

            Assert.That(nav.GoBack(), Is.False);
        }
    }
}

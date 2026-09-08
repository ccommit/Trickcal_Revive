using NUnit.Framework;
using TrickcalRevive.Presentation;

namespace TrickcalRevive.Presentation.Tests
{
    // 02_로비화면전환_설계서 §2.5: 팝업 → Main씬 내부 화면 → 씬 방문 스택 순서로 판단한다.
    public sealed class NavigationHistoryTests
    {
        [Test]
        public void 팝업이_열려있으면_화면이나_씬보다_우선한다()
        {
            var popups = new PopupService();
            popups.Open("Settings");
            var screens = new ScreenNavigator();
            screens.Reset(ScreenIds.Lobby);
            screens.Show(ScreenIds.StageSelect);
            var history = new NavigationHistory(popups, screens);
            history.RecordVisit("Login");
            history.RecordVisit("Main");

            var result = history.ResolveBack();

            Assert.That(result.Type, Is.EqualTo(BackResult.Kind.PopupClosed));
            Assert.That(popups.IsAnyOpen, Is.False, "팝업이 닫혔어야 한다");
        }

        [Test]
        public void 팝업_없고_이전_화면_있으면_ScreenChanged를_돌려준다()
        {
            var popups = new PopupService();
            var screens = new ScreenNavigator();
            screens.Reset(ScreenIds.Lobby);
            screens.Show(ScreenIds.StageSelect);
            var history = new NavigationHistory(popups, screens);
            history.RecordVisit("Login");
            history.RecordVisit("Main");

            var result = history.ResolveBack();

            Assert.That(result.Type, Is.EqualTo(BackResult.Kind.ScreenChanged));
            Assert.That(result.TargetScreen, Is.EqualTo(ScreenIds.Lobby));
        }

        [Test]
        public void 화면_스택도_없으면_씬_방문_스택을_본다()
        {
            var popups = new PopupService();
            var screens = new ScreenNavigator();
            screens.Reset(ScreenIds.Lobby);
            var history = new NavigationHistory(popups, screens);
            history.RecordVisit("Login");
            history.RecordVisit("Main");

            var result = history.ResolveBack();

            Assert.That(result.Type, Is.EqualTo(BackResult.Kind.Navigate));
            Assert.That(result.TargetScene, Is.EqualTo("Login"));
        }

        [Test]
        public void screenNavigator가_없어도_씬_뒤로가기는_그대로_동작한다()
        {
            var popups = new PopupService();
            var history = new NavigationHistory(popups);
            history.RecordVisit("Login");
            history.RecordVisit("Main");

            var result = history.ResolveBack();

            Assert.That(result.Type, Is.EqualTo(BackResult.Kind.Navigate));
            Assert.That(result.TargetScene, Is.EqualTo("Login"));
        }
    }
}

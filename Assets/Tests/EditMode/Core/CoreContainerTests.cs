using System;
using NUnit.Framework;
using TrickcalRevive.Core;
using UnityEngine.TestTools;

namespace TrickcalRevive.Core.Tests
{
    public sealed class DITests
    {
        private interface IThing { }
        private sealed class Thing : IThing { }
        private sealed class OtherThing { }

        [Test]
        public void 인터페이스로_등록하면_구현체를_돌려준다()
        {
            var container = new DI();
            var thing = new Thing();
            container.Register<IThing>(thing);

            Assert.That(container.Resolve<IThing>(), Is.SameAs(thing));
        }

        [Test]
        public void 약속표를_못_지키는_인스턴스는_등록_시점에_거른다()
        {
            var container = new DI();

            Assert.Throws<ArgumentException>(
                () => container.Register(typeof(IThing), new OtherThing()));
        }

        [Test]
        public void 등록되지_않은_타입은_null이_아니라_예외다()
        {
            var container = new DI();

            Assert.Throws<InvalidOperationException>(() => container.Resolve<IThing>());
        }

        [Test]
        public void 씬_컨테이너는_루트에_등록된_것을_이어받는다()
        {
            var root = new DI();
            var shared = new Thing();
            root.Register<IThing>(shared);

            var scene = new DI(root);

            Assert.That(scene.Resolve<IThing>(), Is.SameAs(shared));
        }

        [Test]
        public void 씬_컨테이너를_비워도_루트는_남는다()
        {
            var root = new DI();
            root.Register<IThing>(new Thing());
            var scene = new DI(root);
            scene.Register(new OtherThing());

            scene.Clear();

            Assert.That(scene.TryResolve<OtherThing>(out _), Is.False, "씬 등록은 지워져야 한다");
            Assert.That(scene.TryResolve<IThing>(out _), Is.True, "루트 등록은 남아야 한다");
        }

        [Test]
        public void 팩토리_등록은_Resolve할_때마다_새_인스턴스를_준다()
        {
            var container = new DI();
            container.RegisterFactory<IThing>(() => new Thing());

            var first = container.Resolve<IThing>();
            var second = container.Resolve<IThing>();

            Assert.That(first, Is.Not.SameAs(second));
        }

        [Test]
        public void 팩토리도_자식_컨테이너가_이어받는다()
        {
            var root = new DI();
            root.RegisterFactory<IThing>(() => new Thing());
            var scene = new DI(root);

            Assert.That(scene.TryResolve<IThing>(out var instance), Is.True);
            Assert.That(instance, Is.Not.Null);
        }
    }

    public sealed class EventBusTests
    {
        private sealed class CurrencyChanged
        {
            public int Amount;
        }

        private sealed class OtherEvent { }

        [Test]
        public void 구독한_타입의_이벤트만_받는다()
        {
            var bus = new EventBus();
            var received = 0;
            bus.Subscribe<CurrencyChanged>(_ => received++);

            bus.Publish(new OtherEvent());
            Assert.That(received, Is.Zero);

            bus.Publish(new CurrencyChanged { Amount = 100 });
            Assert.That(received, Is.EqualTo(1));
        }

        [Test]
        public void 발행_도중_구독을_해제해도_나머지_구독자가_건너뛰이지_않는다()
        {
            var bus = new EventBus();
            var secondCalled = false;
            Action<CurrencyChanged> second = _ => secondCalled = true;

            // 첫 번째 핸들러가 자기 자신을 해제해 목록을 바꾼다.
            Action<CurrencyChanged> first = null;
            first = _ => bus.Unsubscribe(first);

            bus.Subscribe(first);
            bus.Subscribe(second);

            bus.Publish(new CurrencyChanged());

            Assert.That(secondCalled, Is.True);
        }

        [Test]
        public void Clear는_남은_구독을_모두_버린다()
        {
            var bus = new EventBus();
            var received = 0;
            bus.Subscribe<CurrencyChanged>(_ => received++);

            bus.Clear();
            bus.Publish(new CurrencyChanged());

            Assert.That(received, Is.Zero);
        }

        [Test]
        public void 구독자_하나가_예외를_던져도_나머지는_호출된다()
        {
            var bus = new EventBus();
            var secondCalled = false;
            bus.Subscribe<CurrencyChanged>(_ => throw new InvalidOperationException("일부러 던짐"));
            bus.Subscribe<CurrencyChanged>(_ => secondCalled = true);

            LogAssert.Expect(UnityEngine.LogType.Exception, new System.Text.RegularExpressions.Regex("InvalidOperationException.*일부러 던짐"));
            Assert.DoesNotThrow(() => bus.Publish(new CurrencyChanged()));
            Assert.That(secondCalled, Is.True, "먼저 등록된 구독자가 예외를 던져도 다음 구독자는 호출돼야 한다");
        }

        [Test]
        public void SubscribeScoped를_Dispose하면_구독이_해제된다()
        {
            var bus = new EventBus();
            var received = 0;
            var subscription = bus.SubscribeScoped<CurrencyChanged>(_ => received++);

            bus.Publish(new CurrencyChanged());
            Assert.That(received, Is.EqualTo(1));

            subscription.Dispose();
            bus.Publish(new CurrencyChanged());

            Assert.That(received, Is.EqualTo(1), "Dispose 이후에는 더 이상 호출되면 안 된다");
        }
    }

    public sealed class StateMachineTests
    {
        private enum Phase { Ready, Preparation, Combat }

        [Test]
        public void 허용되지_않은_전이는_막힌다()
        {
            var machine = new StateMachine<Phase>(Phase.Ready);
            machine.AllowTransition(Phase.Ready, Phase.Preparation);

            Assert.That(machine.ChangeState(Phase.Combat), Is.False);
            Assert.That(machine.CurrentState, Is.EqualTo(Phase.Ready));

            Assert.That(machine.ChangeState(Phase.Preparation), Is.True);
            Assert.That(machine.CurrentState, Is.EqualTo(Phase.Preparation));
        }

        [Test]
        public void 상태가_바뀌면_이전_상태와_함께_알린다()
        {
            var machine = new StateMachine<Phase>(Phase.Ready);
            machine.AllowTransition(Phase.Ready, Phase.Preparation);

            Phase from = default, to = default;
            machine.StateChanged += (previous, next) => { from = previous; to = next; };

            machine.ChangeState(Phase.Preparation);

            Assert.That(from, Is.EqualTo(Phase.Ready));
            Assert.That(to, Is.EqualTo(Phase.Preparation));
        }

        [Test]
        public void FSM은_조건이_맞을_때_스스로_넘어간다()
        {
            var ready = true;
            var fsm = new FSM<Phase>(Phase.Ready);
            fsm.AddTransition(Phase.Ready, Phase.Preparation, () => ready);

            fsm.Tick(0.1f);
            Assert.That(fsm.CurrentState, Is.EqualTo(Phase.Preparation));

            // 조건이 남아 있어도 From이 다르면 다시 넘어가지 않는다.
            fsm.Tick(0.1f);
            Assert.That(fsm.CurrentState, Is.EqualTo(Phase.Preparation));
        }
    }

    public sealed class PoolTests
    {
        [Test]
        public void 반납한_인스턴스를_다시_꺼내_쓴다()
        {
            var pool = new Pool();
            var prefab = new UnityEngine.GameObject("prefab");

            var first = pool.Get(prefab);
            pool.Release(first);
            var second = pool.Get(prefab);

            Assert.That(second, Is.SameAs(first));

            UnityEngine.Object.DestroyImmediate(second);
            UnityEngine.Object.DestroyImmediate(prefab);
        }
    }
}

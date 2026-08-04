using System;
using System.Collections.Generic;

namespace TrickcalRevive.Core
{
    /// <summary>
    /// 발행-구독 전달자. 한 번의 변화에 반응할 곳이 둘 이상일 때만 쓴다.
    /// 받는 쪽이 하나뿐이면 직접 호출이 흐름을 읽기 쉽다.
    /// </summary>
    /// <remarks>
    /// 이벤트 종류를 문자열이 아니라 타입으로 구분한다. 문자열이면 오타가 조용히 통과하고,
    /// payload가 object라 받는 쪽이 매번 캐스팅해야 한다.
    /// </remarks>
    public sealed class EventBus
    {
        private readonly Dictionary<Type, List<Delegate>> handlersByEvent =
            new Dictionary<Type, List<Delegate>>();

        public void Subscribe<TEvent>(Action<TEvent> handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            if (!handlersByEvent.TryGetValue(typeof(TEvent), out var handlers))
            {
                handlers = new List<Delegate>();
                handlersByEvent.Add(typeof(TEvent), handlers);
            }
            handlers.Add(handler);
        }

        public void Unsubscribe<TEvent>(Action<TEvent> handler)
        {
            if (handler == null || !handlersByEvent.TryGetValue(typeof(TEvent), out var handlers))
                return;
            handlers.Remove(handler);
        }

        public void Publish<TEvent>(TEvent payload)
        {
            if (!handlersByEvent.TryGetValue(typeof(TEvent), out var handlers) || handlers.Count == 0)
                return;

            // 핸들러가 발행 도중 구독을 해제해도 나머지가 건너뛰이지 않도록 복사본을 돈다.
            foreach (var handler in handlers.ToArray())
            {
                try
                {
                    ((Action<TEvent>)handler)(payload);
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogException(ex);
                }
            }
        }

        /// <summary>
        /// 구독-해제 짝을 <see cref="EventSubscription.Dispose"/> 한 번으로 끝낸다.
        /// MonoBehaviour는 OnEnable에서 받고 OnDisable에서 Dispose()만 부르면 된다.
        /// </summary>
        public EventSubscription SubscribeScoped<TEvent>(Action<TEvent> handler)
        {
            Subscribe(handler);
            return new EventSubscription(() => Unsubscribe(handler));
        }

        /// <summary>
        /// 씬을 벗어날 때 남은 구독을 한 번에 버린다. 해제를 빠뜨린 구독이 파괴된
        /// 오브젝트를 계속 호출하는 사고를 막는 안전망이다.
        /// </summary>
        public void Clear()
        {
            handlersByEvent.Clear();
        }
    }

    /// <summary>
    /// <see cref="EventBus.SubscribeScoped{TEvent}"/>가 돌려주는 구독 핸들.
    /// Dispose 한 번으로 Unsubscribe를 대신한다.
    /// </summary>
    public sealed class EventSubscription : IDisposable
    {
        private Action unsubscribe;

        internal EventSubscription(Action unsubscribe)
        {
            this.unsubscribe = unsubscribe;
        }

        public void Dispose()
        {
            unsubscribe?.Invoke();
            unsubscribe = null;
        }
    }
}

using System;
using System.Collections.Generic;

namespace TrickcalRevive.Core
{
    /// <summary>
    /// 타입을 키로 인스턴스를 맡아두는 컨테이너. 등록은 한 곳에서 하고,
    /// 쓰는 쪽은 구현체가 아니라 인터페이스만 알면 되게 한다.
    /// </summary>
    /// <remarks>
    /// 씬마다 컨테이너를 새로 만든다. 씬 안에서만 사는 서비스는 그 컨테이너에,
    /// 씬을 넘겨 살아야 하는 마스터 데이터·세이브 같은 것은 루트 컨테이너에
    /// 등록하고 <see cref="parent"/>로 이어 받는다.
    ///
    /// 생성자 자동 주입은 없다. 등록 코드를 손으로 쓰는 대신 "무엇이 무엇에 의존하는지"가
    /// 설치 지점 한 곳에 다 드러나는 쪽을 택했다.
    /// </remarks>
    public sealed class DI
    {
        private readonly Dictionary<Type, object> instancesByType = new Dictionary<Type, object>();
        private readonly Dictionary<Type, Func<object>> factoriesByType = new Dictionary<Type, Func<object>>();
        private readonly DI parent;

        public DI(DI parent = null)
        {
            this.parent = parent;
        }

        public void Register(Type serviceType, object instance)
        {
            if (serviceType == null)
                throw new ArgumentNullException(nameof(serviceType));
            if (instance != null && !serviceType.IsInstanceOfType(instance))
                throw new ArgumentException($"{instance.GetType()} does not implement {serviceType}.", nameof(instance));

            instancesByType[serviceType] = instance;
        }

        public void Register<TService>(TService instance) => Register(typeof(TService), instance);

        /// <summary>
        /// 판마다 새로 만들어야 하는 서비스용. Resolve할 때마다 factory를 호출해 새 인스턴스를 준다.
        /// 인스턴스 등록(<see cref="Register{TService}"/>)과 달리 모든 소비자가 같은 인스턴스를 공유하지 않는다.
        /// </summary>
        public void RegisterFactory<TService>(Func<TService> factory)
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            factoriesByType[typeof(TService)] = () => factory();
        }

        public bool TryResolve(Type serviceType, out object instance)
        {
            if (instancesByType.TryGetValue(serviceType, out instance))
                return true;
            if (factoriesByType.TryGetValue(serviceType, out var factory))
            {
                instance = factory();
                return true;
            }
            if (parent != null)
                return parent.TryResolve(serviceType, out instance);

            instance = null;
            return false;
        }

        public object Resolve(Type serviceType)
        {
            if (TryResolve(serviceType, out var instance))
                return instance;

            // 등록을 빠뜨렸다는 사실을 여기서 알리지 않으면, 한참 뒤 엉뚱한 곳에서
            // NullReferenceException으로 터져 원인을 찾기 어려워진다.
            throw new InvalidOperationException($"No registration found for {serviceType}.");
        }

        public TService Resolve<TService>() => (TService)Resolve(typeof(TService));

        public bool TryResolve<TService>(out TService instance)
        {
            if (TryResolve(typeof(TService), out var found))
            {
                instance = (TService)found;
                return true;
            }

            instance = default;
            return false;
        }

        /// <summary>씬을 벗어날 때 그 씬 컨테이너만 비운다. 부모는 건드리지 않는다.</summary>
        public void Clear()
        {
            instancesByType.Clear();
            factoriesByType.Clear();
        }
    }
}

using System;
using UnityEngine;
using TrickcalRevive.Core;

namespace TrickcalRevive.App
{
    /// <summary>
    /// 씬마다 하나 붙여 그 씬이 쓸 서비스를 등록하고, 씬 안의 컴포넌트에 넣어준다.
    /// </summary>
    /// <remarks>
    /// MonoBehaviour는 new로 만들 수 없어 생성자 주입이 불가능하다. 그래서 서비스 로케이터
    /// (각자 DI.Instance.Resolve를 부르는 방식) 대신 이 방식을 골랐다. 서비스 로케이터는
    /// 구현이 더 쉽지만 "누가 무엇에 의존하는지"가 클래스 안에 숨어버려, 의존성 정리와
    /// 정반대로 간다. 여기서는 씬의 모든 의존이 InstallBindings 한 곳에 드러난다.
    ///
    /// 씬 컨테이너는 루트 컨테이너를 부모로 삼는다. 마스터 데이터·세이브처럼 씬을 넘겨
    /// 살아야 하는 것은 루트에 있고, 씬 전용 서비스만 여기서 만든다.
    /// </remarks>
    public abstract class SceneInstaller : MonoBehaviour
    {
        private DI container;

        protected DI Container => container;

        protected virtual void Awake()
        {
            // 로그만 남기고 넘어가면 등록이 통째로 빠진 채 씬이 계속 굴러가 원인을 못 찾는다.
            // 준비된 루트가 없으면 여기서 끊는다.
            var root = GameApplication.Ready
                ?? throw new InvalidOperationException(
                    $"{GetType().Name}: 루트 컨테이너가 준비되지 않았다. " +
                    "Login 씬을 거치지 않고 이 씬을 바로 열었는지 확인하라.");

            container = new DI(root.RootContainer);
            InstallBindings(container);
            InjectSceneObjects(container);
        }

        protected virtual void OnDestroy()
        {
            // 씬 전용 등록만 버린다. 루트는 건드리지 않는다.
            container?.Clear();
        }

        /// <summary>이 씬에서만 사는 서비스를 등록한다.</summary>
        protected abstract void InstallBindings(DI container);

        /// <summary>씬에 배치된 컴포넌트에 서비스를 넣어준다.</summary>
        protected abstract void InjectSceneObjects(DI container);
    }
}

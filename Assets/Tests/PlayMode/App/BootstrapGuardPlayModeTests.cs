using System.Collections;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

using TrickcalRevive.Core;

namespace TrickcalRevive.App.Tests
{
    // 리뷰 지적(SceneInstaller.cs): 루트 컨테이너가 준비되지 않은 상태로 씬 설치가 돌면
    // 로그만 남고 등록이 통째로 빠진 채 게임이 계속 굴러갔다. 이제는 그 자리에서 끊긴다.
    public sealed class BootstrapGuardPlayModeTests
    {
        private GameObject host;

        [SetUp]
        public void SetUp()
        {
            // 앞선 테스트가 Login 씬을 거쳐 DontDestroyOnLoad로 남긴 루트가 있으면
            // 이 검증들의 전제가 무너진다. 실행 순서에 기대지 않고 직접 치운다.
            foreach (var existing in Object.FindObjectsByType<GameApplication>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Object.DestroyImmediate(existing.gameObject);
            }
        }

        [TearDown]
        public void TearDown()
        {
            if (host != null)
                Object.DestroyImmediate(host);
        }

        [UnityTest]
        public IEnumerator GameApplication이_없으면_씬설치가_예외로_끊긴다()
        {
            Assert.That(GameApplication.Ready, Is.Null);
            LogAssert.Expect(LogType.Exception, new Regex("루트 컨테이너가 준비되지 않았다"));

            host = new GameObject(nameof(GameApplication이_없으면_씬설치가_예외로_끊긴다));
            var installer = host.AddComponent<ProbeSceneInstaller>();
            yield return null;

            Assert.That(installer.InstallCallCount, Is.Zero, "끊겼는데도 등록이 진행되면 반쪽 초기화가 그대로 남는다.");
        }

        [UnityTest]
        public IEnumerator 루트를_다_만들지_못한_GameApplication은_Ready에_잡히지_않는다()
        {
            // SceneFlowController 참조가 비어 있어 루트 구성이 중간에 끊기는 상황.
            LogAssert.Expect(LogType.Exception, new Regex("SceneFlowController가 연결되지 않았다"));

            host = new GameObject(nameof(루트를_다_만들지_못한_GameApplication은_Ready에_잡히지_않는다));
            host.AddComponent<GameApplication>();
            yield return null;

            Assert.That(GameApplication.Ready, Is.Null);
        }

        private sealed class ProbeSceneInstaller : SceneInstaller
        {
            public int InstallCallCount { get; private set; }

            protected override void InstallBindings(DI container) => InstallCallCount++;

            protected override void InjectSceneObjects(DI container)
            {
            }
        }
    }
}

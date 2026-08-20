using System;
using NUnit.Framework;
using UnityEngine;

namespace TrickcalRevive.Presentation.Tests
{
    // 리뷰 지적(SceneFlowController.cs:24): Configure() 이후에만 쓸 수 있는 구조인데
    // Go/Back/LockBack이 초기화 여부를 보장하지 않아 history null로 바로 실패했다.
    public sealed class SceneFlowControllerGuardTests
    {
        private GameObject host;
        private SceneFlowController controller;

        [SetUp]
        public void SetUp()
        {
            host = new GameObject(nameof(SceneFlowControllerGuardTests));
            controller = host.AddComponent<SceneFlowController>();
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(host);
        }

        [Test]
        public void Configure_전에_Go를_부르면_원인을_말하는_예외가_난다()
        {
            var exception = Assert.Throws<InvalidOperationException>(() => controller.Go("Main"));
            Assert.That(exception.Message, Does.Contain("Configure"));
        }

        [Test]
        public void Configure_전에_Back을_부르면_원인을_말하는_예외가_난다()
        {
            var exception = Assert.Throws<InvalidOperationException>(() => controller.Back());
            Assert.That(exception.Message, Does.Contain("Configure"));
        }

        [Test]
        public void Configure_전에_LockBack을_부르면_원인을_말하는_예외가_난다()
        {
            Assert.Throws<InvalidOperationException>(() => controller.LockBack());
        }

        [Test]
        public void Configure_전에_UnlockBack을_부르면_원인을_말하는_예외가_난다()
        {
            Assert.Throws<InvalidOperationException>(() => controller.UnlockBack());
        }

        [Test]
        public void Configure_후에는_잠금_전환이_통한다()
        {
            controller.Configure(new PopupService());

            Assert.DoesNotThrow(() => controller.LockBack());
            Assert.DoesNotThrow(() => controller.UnlockBack());
        }
    }
}

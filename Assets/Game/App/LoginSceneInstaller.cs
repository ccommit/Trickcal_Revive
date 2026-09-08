using UnityEngine;
using TrickcalRevive.Core;
using TrickcalRevive.Domain.Account;
using TrickcalRevive.Infra;
using TrickcalRevive.MainUI;
using TrickcalRevive.Presentation;

namespace TrickcalRevive.App
{
    // 계정 인증 관련 인터페이스는 전부 루트 컨테이너에 있다(09_공통기반_설계서 §2.2,
    // 결정 2026-08-05). 이 씬에서만 사는 서비스가 없어 InstallBindings는 비어 있다.
    public class LoginSceneInstaller : SceneInstaller
    {
        [SerializeField] private AuthController authController;
        [SerializeField] private LoginScreenView loginScreenView;

        protected override void InstallBindings(DI container)
        {
        }

        protected override void InjectSceneObjects(DI container)
        {
            authController.Configure(
                container.Resolve<IAccountAuthRepository>(),
                container.Resolve<ISessionService>(),
                container.Resolve<INavigationService>());
            authController.AttachView(loginScreenView);
        }
    }
}

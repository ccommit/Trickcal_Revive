using UnityEngine;
using TrickcalRevive.Core;
using TrickcalRevive.Domain.Account;
using TrickcalRevive.Domain.Inventory;
using TrickcalRevive.Infra;
using TrickcalRevive.Presentation;

namespace TrickcalRevive.App
{
    // PartyFormationValidator/CommandHistory(03_파티편성 스코프)는 아직 없어
    // InstallBindings가 비어 있다 — 파티 편성 이슈에서 채운다.
    public class MainSceneInstaller : SceneInstaller
    {
        [SerializeField] private LobbyController lobbyController;
        [SerializeField] private SettingsController settingsController;

        protected override void InstallBindings(DI container)
        {
        }

        protected override void InjectSceneObjects(DI container)
        {
            lobbyController.Configure(
                container.Resolve<IAccountRepository>(),
                container.Resolve<ICurrencyRepository>(),
                container.Resolve<IPopupService>());

            settingsController.Configure(container.Resolve<ISessionService>());
        }
    }
}

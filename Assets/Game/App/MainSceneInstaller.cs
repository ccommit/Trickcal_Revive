using UnityEngine;
using TrickcalRevive.Core;
using TrickcalRevive.Domain.Account;
using TrickcalRevive.Domain.Battle;
using TrickcalRevive.Domain.Character;
using TrickcalRevive.Domain.Inventory;
using TrickcalRevive.Domain.Stage;
using TrickcalRevive.Infra;
using TrickcalRevive.MainUI;
using TrickcalRevive.Presentation;

namespace TrickcalRevive.App
{
    public class MainSceneInstaller : SceneInstaller
    {
        [SerializeField] private LobbyController lobbyController;
        [SerializeField] private SettingsController settingsController;
        [SerializeField] private LobbyScreenView lobbyScreenView;
        [SerializeField] private StageSelectController stageSelectController;
        [SerializeField] private StageInfoPopupController stageInfoPopupController;
        [SerializeField] private PartySetupController partySetupController;

        protected override void InstallBindings(DI container)
        {
        }

        protected override void InjectSceneObjects(DI container)
        {
            var navigation = container.Resolve<INavigationService>();
            var screens = container.Resolve<IScreenNavigator>();
            var popups = container.Resolve<IPopupService>();
            var stageRepository = container.Resolve<IStageRepository>();
            var stageProgressRepository = container.Resolve<IStageProgressRepository>();
            var partyRepository = container.Resolve<IPartyRepository>();
            var ownedCharacterRepository = container.Resolve<IPlayerCharacterRepository>();

            // Main씬 진입 시 화면 방문 스택을 비우고 Lobby에서 새로 시작한다
            // (02_로비화면전환_설계서 §2.5).
            screens.Reset(ScreenIds.Lobby);

            lobbyController.Configure(
                container.Resolve<IAccountRepository>(),
                container.Resolve<ICurrencyRepository>(),
                popups,
                navigation,
                screens);
            settingsController.Configure(container.Resolve<ISessionService>());
            lobbyController.AttachView(lobbyScreenView);

            stageSelectController.Configure(stageRepository, stageProgressRepository, popups);

            stageInfoPopupController.Configure(stageRepository, stageProgressRepository, popups, screens);

            // PartyFormationValidator/BattleStartRequestBuilder/BattleContextBuilder는
            // PartySetupController 하나만 쓰는 씬 전용 헬퍼라 DI 등록 없이 바로 만든다
            // (09_공통기반_설계서 §2.2가 예고한 "MainSceneInstaller → PartyFormationValidator"
            // 자리, CommandHistory와 같은 이유로 팩토리 등록 없이 직접 구성).
            var formationValidator = new PartyFormationValidator();
            var requestBuilder = new BattleStartRequestBuilder(partyRepository);
            var contextBuilder = new BattleContextBuilder(stageRepository, partyRepository);
            partySetupController.Configure(
                partyRepository,
                ownedCharacterRepository,
                formationValidator,
                requestBuilder,
                contextBuilder);
        }
    }
}

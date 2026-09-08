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
        [SerializeField] private StageSelectView stageSelectView;
        [SerializeField] private ScreenPanelHost screenPanelHost;
        [SerializeField] private StageInfoPopupView stageInfoPopupView;
        [SerializeField] private PartySetupView partySetupView;
        [SerializeField] private ScreenTransitionView screenTransitionView;
        [SerializeField] private TopCurrencyController topCurrencyController;
        [SerializeField] private TopCurrencyPanelView topCurrencyPanelView;
        private IStageRepository stageRepository;

        protected override void InstallBindings(DI container)
        {
        }

        protected override void InjectSceneObjects(DI container)
        {
            var navigation = container.Resolve<INavigationService>();
            var screens = container.Resolve<IScreenNavigator>();
            var popups = container.Resolve<IPopupService>();
            stageRepository = container.Resolve<IStageRepository>();
            var stageProgressRepository = container.Resolve<IStageProgressRepository>();
            var partyRepository = container.Resolve<IPartyRepository>();
            var ownedCharacterRepository = container.Resolve<IPlayerCharacterRepository>();

            // Main씬 진입 시 화면 방문 스택을 비우고 Lobby에서 새로 시작한다
            // (02_로비화면전환_설계서 §2.5).
            screens.Reset(ScreenIds.Lobby);

            lobbyController.Configure(
                popups,
                navigation,
                screens);
            settingsController.Configure(container.Resolve<ISessionService>());
            lobbyController.AttachView(lobbyScreenView);

            stageSelectController.Configure(
                stageRepository,
                stageProgressRepository,
                popups);
            stageSelectController.AttachView(stageSelectView);

            stageInfoPopupController.Configure(stageRepository, stageProgressRepository, popups, screens);
            stageInfoPopupController.AttachView(stageInfoPopupView);

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
                contextBuilder,
                container.Resolve<ICharacterRepository>());
            partySetupController.AttachView(partySetupView);

            topCurrencyController.Configure(
                container.Resolve<ICurrencyRepository>(),
                container.Resolve<IAccountRepository>(),
                lobbyController.OpenSettingsMenu,
                () => screens.Reset(ScreenIds.Lobby),
                () => screens.GoBack());
            topCurrencyController.AttachView(topCurrencyPanelView);

            // Main씬 내부 화면 전환을 실제 패널 show/hide로 연결한다(Presentation↔MainUI 순환
            // 의존을 피해 설치자가 문자열 screenId로 브리지). GPT가 남긴 ScreenChanged 소비 지점.
            // 별 와이프(04 영상)는 로비→스테이지선택 진입에만 재생하고, 구멍이 닫힌 순간
            // 패널을 바꾼다. 뒤로가기·파티 전환 등 나머지는 즉시 전환(연출 없음).
            var lastScreen = screens.CurrentScreen;
            screens.ScreenChanged += screenId =>
            {
                var useStar = lastScreen == ScreenIds.Lobby && screenId == ScreenIds.StageSelect;
                lastScreen = screenId;
                if (useStar)
                    screenTransitionView.Play(() => ShowScreen(screenId));
                else
                    ShowScreen(screenId);
            };
            ShowScreen(screens.CurrentScreen);
        }

        private void ShowScreen(string screenId)
        {
            screenPanelHost.ShowScreen(screenId);
            topCurrencyController.Show(
                VisibleCurrenciesFor(screenId),
                screenId == ScreenIds.Lobby,
                PageTitleFor(screenId));
        }

        private string PageTitleFor(string screenId)
        {
            if (screenId == ScreenIds.StageSelect)
                return "스테이지 리스트";
            if (screenId == ScreenIds.PartySetup)
            {
                if (string.IsNullOrWhiteSpace(partySetupController.CurrentStageId))
                    return "파티 편성";
                var stage = stageRepository?.GetStage(partySetupController.CurrentStageId);
                if (stage == null)
                    return "파티 편성";
                if (string.IsNullOrWhiteSpace(stage.Name))
                    return stage.StageId ?? "파티 편성";
                return $"{stage.StageId}. {stage.Name} 시작!";
            }
            if (screenId == ScreenIds.Roster || screenId == ScreenIds.CharacterDetail)
                return "사도";
            if (screenId == ScreenIds.Gacha)
                return "모집";
            if (screenId == ScreenIds.Backpack)
                return "배낭";
            return string.Empty;
        }

        private static TopCurrencyVisibility VisibleCurrenciesFor(string screenId)
        {
            if (screenId == ScreenIds.Lobby)
                return TopCurrencyVisibility.All;
            if (screenId == ScreenIds.StageSelect || screenId == ScreenIds.PartySetup)
            {
                return TopCurrencyVisibility.Stamina
                    | TopCurrencyVisibility.Gold
                    | TopCurrencyVisibility.Elleaf;
            }
            return TopCurrencyVisibility.None;
        }
    }
}

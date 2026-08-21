using System;
using UnityEngine;
using TrickcalRevive.Core;
using TrickcalRevive.Domain.Account;
using TrickcalRevive.Domain.Character;
using TrickcalRevive.Domain.Growth;
using TrickcalRevive.Domain.Inventory;
using TrickcalRevive.Domain.Stage;
using TrickcalRevive.Infra;
using TrickcalRevive.Presentation;

namespace TrickcalRevive.App
{
    /// <summary>
    /// 게임이 켜질 때 가장 먼저 깨어나 루트 컨테이너를 만든다. 씬을 넘겨 살아야 하는
    /// 서비스만 여기서 등록하고, 씬 전용 서비스는 각 SceneInstaller가 맡는다.
    /// </summary>
    /// <remarks>
    /// Login 씬에 배치된다(인트로 부팅 씬은 아직 없음) — 항상 Login 화면부터 시작한다.
    /// </remarks>  
    [DefaultExecutionOrder(-1000)]
    public class GameApplication : MonoBehaviour
    {
        private static GameApplication instance;

        [SerializeField] private SceneFlowController sceneFlowController;

        /// <summary>
        /// 루트 컨테이너를 다 만든 살아있는 인스턴스. 준비 전이거나 중복으로 파괴되는
        /// 개체는 여기 잡히지 않는다 — <c>FindFirstObjectByType</c>은 파괴 예정인
        /// 개체도 찾아내므로 씬 설치는 반드시 이 속성을 거친다.
        /// </summary>
        public static GameApplication Ready => instance != null && instance.RootContainer != null ? instance : null;

        public DI RootContainer { get; private set; }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            RootContainer = BuildRootContainer();
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        private DI BuildRootContainer()
        {
            var container = new DI();

            var files = new JsonFileRepository();
            var session = new SessionService(files);
            var saveManager = new SaveManager(files, session);
            var masterData = new MasterDataRepository();
            var playerData = new PlayerDataRepository(files, session, saveManager);
            var accountAuth = new AccountAuthRepository(files, new PlaintextPasswordHasher());
            var popups = new PopupService();
            var screens = new ScreenNavigator();
            if (sceneFlowController == null)
                throw new InvalidOperationException(
                    "GameApplication에 SceneFlowController가 연결되지 않았다. " +
                    "GameApplication 오브젝트의 인스펙터에서 Scene Flow Controller를 지정하라.");

            sceneFlowController.Configure(popups, screens);

            container.Register<ISessionService>(session);
            container.Register<ISaveManager>(saveManager);
            container.Register<ICharacterRepository>(masterData);
            container.Register<IStageRepository>(masterData);
            container.Register<IGrowthRepository>(masterData);

            // PlayerDataRepository/AccountAuthRepository는 씬 특유의 상태가 없는
            // 무상태 데이터 접근자라 루트에 둔다(09_공통기반_설계서 §2.2, 결정 2026-08-05).
            // 씬 컨테이너는 루트만 부모로 삼고 씬↔씬으로는 안 이어지므로(§2.1),
            // MainSceneInstaller에만 등록하면 Battle/Login 씬이 이 서비스들을 못 쓴다.
            container.Register<IAccountRepository>(playerData);
            container.Register<IPlayerCharacterRepository>(playerData);
            container.Register<IPartyRepository>(playerData);
            container.Register<ICurrencyRepository>(playerData);
            container.Register<ICurrencyWallet>(playerData);
            container.Register<IInventoryRepository>(playerData);
            container.Register<IStageProgressRepository>(playerData);
            container.Register<IAccountAuthRepository>(accountAuth);

            container.Register<IPopupService>(popups);
            container.Register<INavigationService>(sceneFlowController);
            container.Register<IScreenNavigator>(screens);

            container.Register(new EventBus());

            return container;
        }
    }
}

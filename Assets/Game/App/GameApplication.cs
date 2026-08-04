using UnityEngine;
using TrickcalRevive.Core;
using TrickcalRevive.Domain.Account;
using TrickcalRevive.Domain.Character;
using TrickcalRevive.Domain.Growth;
using TrickcalRevive.Domain.Inventory;
using TrickcalRevive.Domain.Stage;
using TrickcalRevive.Infra;

namespace TrickcalRevive.App
{
    /// <summary>
    /// 게임이 켜질 때 가장 먼저 깨어나 루트 컨테이너를 만든다. 씬을 넘겨 살아야 하는
    /// 서비스만 여기서 등록하고, 씬 전용 서비스는 각 SceneInstaller가 맡는다.
    /// </summary>
    /// <remarks>
    /// 첫 화면 전환(로그인/로비 분기)은 02_로비화면전환 이슈에서 SceneFlowController와
    /// 함께 채운다. 지금은 루트 컨테이너 구성까지만.
    /// </remarks>
    public class GameApplication : MonoBehaviour
    {
        public DI RootContainer { get; private set; }

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            RootContainer = BuildRootContainer();
        }

        private DI BuildRootContainer()
        {
            var container = new DI();

            var files = new JsonFileRepository();
            var session = new SessionService(files);
            var saveManager = new SaveManager(files, session);
            var masterData = new MasterDataRepository();
            var playerData = new PlayerDataRepository(files, session, saveManager);
            var accountAuth = new AccountAuthRepository(files);

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

            container.Register(new EventBus());

            return container;
        }
    }
}

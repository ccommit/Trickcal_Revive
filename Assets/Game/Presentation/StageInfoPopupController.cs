using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TrickcalRevive.Data.Stage;
using TrickcalRevive.Domain.Stage;
using TrickcalRevive.MainUI;

namespace TrickcalRevive.Presentation
{
    // Show()/Close() 비주얼: 팝업 열기는 StageSelectController.OpenStageInfo가 IPopupService.Open()
    // + BuildPopup()으로, 닫기는 팝업 뷰의 닫기 버튼이 IPopupService.Close()로 처리한다
    // (02_로비화면전환_설계서 §2.3). 뷰 구동은 로비 설정팝업과 같이 컨트롤러가 직접 한다.
    public class StageInfoPopupController : MonoBehaviour
    {
        [SerializeField] private PartySetupController partySetupController;
        private IPopupService popups;
        private IScreenNavigator screenNavigator;
        private IStageRepository stageRepository;
        private IStageProgressRepository stageProgressRepository;
        private StageInfoPopupView view;

        public StageMasterData Stage { get; private set; }
        public PlayerStageProgressData Progress { get; private set; }

        public void Configure(
            IStageRepository stages,
            IStageProgressRepository progress,
            IPopupService popups,
            IScreenNavigator screens)
        {
            stageRepository = stages;
            stageProgressRepository = progress;
            this.popups = popups;
            screenNavigator = screens;
        }

        public void AttachView(StageInfoPopupView popupView)
        {
            DetachView();
            view = popupView;
            if (view == null)
                return;
            view.CloseRequested += HandleCloseRequested;
            view.DeckRequested += HandleDeckRequested;
        }

        public void BuildPopup(string stageId)
        {
            Stage = stageRepository.GetStage(stageId);
            Progress = stageProgressRepository.GetStageProgress(stageId);
            view?.Show(BuildData());
        }

        /// <summary>
        /// Show()는 페이로드가 없는 신호라, 어느 스테이지인지는 EnterForStage로 직접 넘긴
        /// 다음에 화면을 전환한다(02_로비화면전환_설계서 §2.4).
        /// </summary>
        public void EnterPartySetup()
        {
            partySetupController.EnterForStage(Stage.StageId);
            screenNavigator.Show(ScreenIds.PartySetup);
            popups.Close("StageInfo");
            view?.Hide();
        }

        private StageInfoData BuildData()
        {
            var stars = 0;
            if (Progress?.IsCleared ?? false) stars++;
            if (Progress?.Star1 ?? false) stars++;
            if (Progress?.Star2 ?? false) stars++;

            return new StageInfoData
            {
                StageName = Stage?.Name ?? string.Empty,
                Stars = stars,
                RecommendedPower = Stage?.RecommendedPower ?? 0,
                PersonalityText = Stage?.RecommendedPersonality ?? string.Empty,
                MonsterIds = MonsterIdsFrom(Stage?.WavesJson),
                Rewards = RewardsFrom(Stage?.RewardsJson),
            };
        }

        private static List<string> MonsterIdsFrom(string wavesJson)
        {
            var ids = new List<string>();
            if (string.IsNullOrEmpty(wavesJson))
                return ids;
            var waves = JsonUtility.FromJson<EnemyWaveListData>(wavesJson);
            if (waves?.Waves == null)
                return ids;
            foreach (var wave in waves.Waves)
            {
                if (wave?.Spawns == null)
                    continue;
                foreach (var spawn in wave.Spawns)
                {
                    if (spawn != null && !string.IsNullOrEmpty(spawn.CharacterId) && !ids.Contains(spawn.CharacterId))
                        ids.Add(spawn.CharacterId);
                }
            }
            return ids;
        }

        private static List<StageInfoRewardEntry> RewardsFrom(string rewardsJson)
        {
            var result = new List<StageInfoRewardEntry>();
            if (string.IsNullOrEmpty(rewardsJson))
                return result;
            var list = JsonUtility.FromJson<StageRewardListData>(rewardsJson);
            if (list?.Rewards == null)
                return result;
            result.AddRange(list.Rewards.Select(r => new StageInfoRewardEntry { RewardType = r.RewardType, Amount = r.Amount }));
            return result;
        }

        private void OnDestroy() => DetachView();

        private void DetachView()
        {
            if (view == null)
                return;
            view.CloseRequested -= HandleCloseRequested;
            view.DeckRequested -= HandleDeckRequested;
            view = null;
        }

        private void HandleCloseRequested()
        {
            popups.Close("StageInfo");
            view?.Hide();
        }

        private void HandleDeckRequested() => EnterPartySetup();
    }
}

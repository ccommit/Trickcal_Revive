using UnityEngine;
using TrickcalRevive.Data.Stage;
using TrickcalRevive.Domain.Stage;

namespace TrickcalRevive.Presentation
{
    // Show()/Close() 비주얼은 없다 — 팝업 열기는 StageSelectController.OpenStageInfo가
    // IPopupService.Open()으로, 닫기는 팝업 뷰의 닫기 버튼이 IPopupService.Close()로 직접
    // 처리한다(02_로비화면전환_설계서 §2.3, MainUI 몫).
    public class StageInfoPopupController : MonoBehaviour
    {
        [SerializeField] private PartySetupController partySetupController;
        private IPopupService popups;
        private IScreenNavigator screenNavigator;
        private IStageRepository stageRepository;
        private IStageProgressRepository stageProgressRepository;

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

        public void BuildPopup(string stageId)
        {
            Stage = stageRepository.GetStage(stageId);
            Progress = stageProgressRepository.GetStageProgress(stageId);
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
        }
    }
}

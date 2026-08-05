using System.Collections.Generic;
using UnityEngine;
using TrickcalRevive.Data.Stage;
using TrickcalRevive.Domain.Stage;

namespace TrickcalRevive.Presentation
{
    public class StageSelectController : MonoBehaviour
    {
        [SerializeField] private StageInfoPopupController stageInfoPopupController;
        private IPopupService popups;
        private IStageRepository stageRepository;
        private IStageProgressRepository stageProgressRepository;

        public string SelectedStageId { get; private set; }

        public void Configure(IStageRepository stages, IStageProgressRepository progress, IPopupService popups)
        {
            stageRepository = stages;
            stageProgressRepository = progress;
            this.popups = popups;
        }

        public List<StageMasterData> RenderStageNodes(string chapterId)
        {
            return stageRepository.GetStagesByChapter(chapterId);
        }

        public void SelectStage(string stageId)
        {
            SelectedStageId = stageId;
        }

        public bool CheckUnlocked(string stageId)
        {
            return stageProgressRepository.GetStageProgress(stageId)?.IsUnlocked ?? false;
        }

        public StageInfoPopupController OpenStageInfo(string stageId)
        {
            popups.Open("StageInfo");
            stageInfoPopupController.BuildPopup(stageId);
            return stageInfoPopupController;
        }
    }
}

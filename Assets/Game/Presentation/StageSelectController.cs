using System.Collections.Generic;
using UnityEngine;
using TrickcalRevive.Data.Stage;
using TrickcalRevive.Domain.Stage;
using TrickcalRevive.MainUI;

namespace TrickcalRevive.Presentation
{
    public class StageSelectController : MonoBehaviour
    {
        // fixture는 1챕터만 있다(RecoveryFixture). 챕터 전환은 후속.
        private const string DefaultChapterId = "1";

        [SerializeField] private StageInfoPopupController stageInfoPopupController;
        private IPopupService popups;
        private IStageRepository stageRepository;
        private IStageProgressRepository stageProgressRepository;
        private StageSelectView view;

        public string SelectedStageId { get; private set; }

        public void Configure(
            IStageRepository stages,
            IStageProgressRepository progress,
            IPopupService popups)
        {
            stageRepository = stages;
            stageProgressRepository = progress;
            this.popups = popups;
        }

        public void AttachView(StageSelectView stageSelectView)
        {
            DetachView();
            view = stageSelectView;
            if (view == null)
                return;

            view.NodeClicked += HandleNodeClicked;
            RefreshView();
        }

        public void RefreshView()
        {
            if (view == null)
                return;

            view.RenderNodes(BuildNodes(DefaultChapterId));

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

        private List<StageNodeData> BuildNodes(string chapterId)
        {
            var result = new List<StageNodeData>();
            foreach (var stage in RenderStageNodes(chapterId))
            {
                var progress = stageProgressRepository.GetStageProgress(stage.StageId);
                var unlocked = progress?.IsUnlocked ?? false;
                var cleared = progress?.IsCleared ?? false;
                StageNodeState state;
                if (!unlocked)
                    state = StageNodeState.Locked;
                else if (cleared)
                    state = StageNodeState.Cleared;
                else
                    state = StageNodeState.Current;

                var stars = 0;
                if (cleared) stars++;
                if (progress?.Star1 ?? false) stars++;
                if (progress?.Star2 ?? false) stars++;

                result.Add(new StageNodeData
                {
                    StageId = stage.StageId,
                    Label = $"{stage.ChapterId}-{stage.StageNo}",
                    State = state,
                    Stars = stars,
                });
            }
            return result;
        }

        private void OnDestroy() => DetachView();

        private void DetachView()
        {
            if (view == null)
                return;
            view.NodeClicked -= HandleNodeClicked;
            view = null;
        }

        private void HandleNodeClicked(string stageId)
        {
            SelectStage(stageId);
            OpenStageInfo(stageId);
        }

    }
}

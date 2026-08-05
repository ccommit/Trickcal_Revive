using System.Collections.Generic;
using UnityEngine;
using TrickcalRevive.Data.Stage;

namespace TrickcalRevive.Infra.SaveHandlers
{
    /// <summary>스테이지 진행도 저장 파일 하나를 전담한다. 여러 스테이지가 한 파일에 같이 들어간다.</summary>
    public sealed class StageProgressSaveHandler
    {
        private readonly IFileStore fileStore;

        public StageProgressSaveHandler(IFileStore fileStore)
        {
            this.fileStore = fileStore;
        }

        public List<PlayerStageProgressData> Load(string accountId)
        {
            var json = fileStore.LoadJson(AccountFilePaths.ProgressSave(accountId));
            if (string.IsNullOrEmpty(json))
                return new List<PlayerStageProgressData>();

            var wrapper = JsonUtility.FromJson<ListWrapper<PlayerStageProgressData>>(json);
            return wrapper?.Items ?? new List<PlayerStageProgressData>();
        }

        public void Save(string accountId, List<PlayerStageProgressData> data)
        {
            var wrapper = new ListWrapper<PlayerStageProgressData> { Items = data ?? new List<PlayerStageProgressData>() };
            fileStore.SaveJson(AccountFilePaths.ProgressSave(accountId), JsonUtility.ToJson(wrapper));
        }
    }
}

using System.Collections.Generic;
using UnityEngine;
using TrickcalRevive.Data.Party;

namespace TrickcalRevive.Infra.SaveHandlers
{
    /// <summary>파티 슬롯 저장 파일 하나를 전담한다. 여러 파티가 한 파일에 같이 들어간다.</summary>
    public sealed class PartySaveHandler
    {
        private readonly IFileStore fileStore;

        public PartySaveHandler(IFileStore fileStore)
        {
            this.fileStore = fileStore;
        }

        public List<PlayerPartySlotData> Load(string accountId)
        {
            var json = fileStore.LoadJson(AccountFilePaths.PartySave(accountId));
            if (string.IsNullOrEmpty(json))
                return new List<PlayerPartySlotData>();

            var wrapper = JsonUtility.FromJson<ListWrapper<PlayerPartySlotData>>(json);
            return wrapper?.Items ?? new List<PlayerPartySlotData>();
        }

        public void Save(string accountId, List<PlayerPartySlotData> data)
        {
            var wrapper = new ListWrapper<PlayerPartySlotData> { Items = data ?? new List<PlayerPartySlotData>() };
            fileStore.SaveJson(AccountFilePaths.PartySave(accountId), JsonUtility.ToJson(wrapper));
        }
    }
}

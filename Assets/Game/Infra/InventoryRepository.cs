using System;
using System.Collections.Generic;
using TrickcalRevive.Data.Inventory;
using TrickcalRevive.Domain.Inventory;

namespace TrickcalRevive.Infra
{
    /// <summary>배낭 저장은 07_인벤토리재화 이슈에서 채운다. 지금은 계약만 있다.</summary>
    public sealed class InventoryRepository : IInventoryRepository
    {
        public List<PlayerInventoryData> GetOwnedItems() => throw new NotImplementedException();
        public long GetItemCount(string itemId) => throw new NotImplementedException();
        public bool TryConsume(string itemId, long count) => throw new NotImplementedException();
        public long GetCharacterShardCount(string characterId) => throw new NotImplementedException();
        public bool TryConsumeCharacterShard(string characterId, int count) => throw new NotImplementedException();
    }
}

using System.Collections.Generic;
using TrickcalRevive.Data.Inventory;

namespace TrickcalRevive.Domain.Inventory
{
    // 배낭 화면과 승급(조각 소비)이 함께 쓴다.
    public interface IInventoryRepository
    {
        List<PlayerInventoryData> GetOwnedItems();
        long GetItemCount(string itemId);
        bool TryConsume(string itemId, long count);

        // 전용 사도 조각은 아이템 ID가 아니라 대상 사도로 찾는다.
        long GetCharacterShardCount(string characterId);
        bool TryConsumeCharacterShard(string characterId, int count);
    }
}

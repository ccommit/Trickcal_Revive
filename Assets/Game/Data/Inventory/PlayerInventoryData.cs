using System;

namespace TrickcalRevive.Data.Inventory
{
    // 수량형 아이템 한 줄. DB_설계서 3.3절 player_inventory_save 대응.
    // 골드·엘리프·마카롱·행동력은 여기가 아니라 계정 재화로 관리한다.
    [Serializable]
    public class PlayerInventoryData
    {
        public string InventoryId;
        public string AccountId;
        public string ItemId;

        // ticket / character_shard / common_shard
        public string ItemType;

        // 전용 조각일 때만 대상 사도. 아니면 빈 문자열.
        public string CharacterId;

        public long Count;
    }
}

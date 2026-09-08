using System.Collections.Generic;
using TrickcalRevive.Data.Character;

namespace TrickcalRevive.Domain.Character
{
    // 플레이어가 보유한 사도의 성장 상태. 마스터 데이터와 달리 읽고 쓴다.
    public interface IPlayerCharacterRepository
    {
        List<PlayerCharacterData> GetOwnedCharacters();
        void GrantCharacter(string characterId, int star);
        void AddCharacterShard(string characterId, int count);
    }
}

using System.Collections.Generic;
using TrickcalRevive.Data.Character;

namespace TrickcalRevive.Domain.Character
{
    // 사도·몬스터 원본 정보. 규칙책이라 읽기만 한다.
    // 공통 IRepository<T>를 씌우지 않는 이유가 여기 있다. Save나 Delete를 강제로
    // 물려받으면 "약속표에 있는데 못 지키는 메서드"가 생긴다.
    public interface ICharacterRepository
    {
        UnitMasterData GetCharacter(string characterId);
        List<UnitMasterData> GetPlayableCharacters();
        List<UnitMasterData> GetStarterCharacters();
    }
}

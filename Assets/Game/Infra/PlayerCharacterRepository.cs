using System;
using System.Collections.Generic;
using TrickcalRevive.Data.Character;
using TrickcalRevive.Domain.Character;

namespace TrickcalRevive.Infra
{
    public sealed class PlayerCharacterRepository : PlayerScopedRepository, IPlayerCharacterRepository
    {
        public PlayerCharacterRepository(ISessionService sessionService)
            : base(sessionService)
        {
        }

        // 가챠/스타터 지급은 아직 없다. UI 복구 검증을 위해 RecoveryFixture(정식 원본
        // 아님)의 사도 30명을 보유분으로 제공한다.
        public List<PlayerCharacterData> GetOwnedCharacters()
        {
            var accountId = CurrentAccountId;
            return accountId == null
                ? new List<PlayerCharacterData>()
                : Fixtures.RecoveryFixture.OwnedCharacters(accountId);
        }

        // 지급·조각 적립은 06_사도성장 이슈에서 채운다.
        public void GrantCharacter(string characterId, int star) => throw new NotImplementedException();
        public void AddCharacterShard(string characterId, int count) => throw new NotImplementedException();
    }
}

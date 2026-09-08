using System;
using System.Collections.Generic;
using TrickcalRevive.Data.Battle;
using TrickcalRevive.Data.Party;
using TrickcalRevive.Domain.Character;

namespace TrickcalRevive.Domain.Battle
{
    // "이 스테이지를 이 파티로 시작한다"는 신청서를 만든다.
    // 실제 스테이지·웨이브 로딩은 BattleContextBuilder가 로딩 화면에서 한다.
    public sealed class BattleStartRequestBuilder
    {
        private readonly IPartyRepository parties;

        public BattleStartRequestBuilder(IPartyRepository parties)
        {
            this.parties = parties ?? throw new ArgumentNullException(nameof(parties));
        }

        public BattleStartRequest Build(string stageId, string partyId, string battleOptions = null)
        {
            return new BattleStartRequest
            {
                RequestId = Guid.NewGuid().ToString("N"),
                StageId = stageId,
                PartyId = partyId,
                PartyMembers = parties.GetPartySlots(partyId) ?? new List<PlayerPartySlotData>(),
                BattleOptions = battleOptions ?? string.Empty,
                CreatedAt = DateTime.UtcNow,
            };
        }
    }
}

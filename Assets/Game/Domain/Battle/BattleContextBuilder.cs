using System;
using System.Collections.Generic;
using UnityEngine;
using TrickcalRevive.Data.Battle;
using TrickcalRevive.Data.Party;
using TrickcalRevive.Data.Stage;
using TrickcalRevive.Domain.Character;
using TrickcalRevive.Domain.Stage;

namespace TrickcalRevive.Domain.Battle
{
    /// <summary>신청서를 받아 전투에 필요한 것을 전부 담은 도시락을 싼다.</summary>
    public sealed class BattleContextBuilder
    {
        private readonly IStageRepository stages;
        private readonly IPartyRepository parties;

        public BattleContextBuilder(IStageRepository stages, IPartyRepository parties)
        {
            this.stages = stages ?? throw new ArgumentNullException(nameof(stages));
            this.parties = parties ?? throw new ArgumentNullException(nameof(parties));
        }

        /// <summary>스테이지를 못 찾으면 null. 호출부는 실패로 처리한다.</summary>
        public BattleContext Build(BattleStartRequest request)
        {
            if (request == null)
                return null;

            var stage = stages.GetStage(request.StageId);
            if (stage == null)
            {
                Debug.LogWarning($"스테이지를 찾지 못했다: {request.StageId}");
                return null;
            }

            return new BattleContext
            {
                ContextId = Guid.NewGuid().ToString("N"),
                Request = request,
                Stage = stage,
                Waves = ParseWaves(stage.WavesJson),
                RewardsJson = stage.RewardsJson,
                Party = LoadPartySlots(request.PartyId),
                CreatedAt = DateTime.UtcNow.Ticks,
            };
        }

        /// <summary>waves_json을 웨이브 목록으로 읽는다. 비어 있거나 깨졌으면 빈 목록.</summary>
        public static List<EnemyWaveData> ParseWaves(string wavesJson)
        {
            if (string.IsNullOrEmpty(wavesJson))
                return new List<EnemyWaveData>();

            try
            {
                var parsed = JsonUtility.FromJson<EnemyWaveListData>(wavesJson);
                return parsed?.Waves ?? new List<EnemyWaveData>();
            }
            catch (ArgumentException exception)
            {
                Debug.LogWarning($"waves_json을 읽지 못했다: {exception.Message}");
                return new List<EnemyWaveData>();
            }
        }

        public List<PlayerPartySlotData> LoadPartySlots(string partyId)
            => parties.GetPartySlots(partyId) ?? new List<PlayerPartySlotData>();
    }
}

using System;
using System.Collections.Generic;
using TrickcalRevive.Data.Character;
using TrickcalRevive.Data.Growth;
using TrickcalRevive.Data.Stage;
using TrickcalRevive.Domain.Character;
using TrickcalRevive.Domain.Growth;
using TrickcalRevive.Domain.Stage;
using TrickcalRevive.Infra.Fixtures;

namespace TrickcalRevive.Infra
{
    // character_master/stage_master는 RecoveryFixture(정식 원본 아님)로 채운다.
    // growth_table 등 나머지는 각 컨텐츠 이슈(06)에서 채운다.
    public class MasterDataRepository : ICharacterRepository, IStageRepository, IGrowthRepository
    {
        public UnitMasterData GetCharacter(string characterId) => RecoveryFixture.Character(characterId);
        public List<UnitMasterData> GetPlayableCharacters() => RecoveryFixture.PlayableCharacters();
        public List<UnitMasterData> GetStarterCharacters() => throw new NotImplementedException();
        public StageMasterData GetStage(string stageId) => RecoveryFixture.Stage(stageId);
        public List<StageMasterData> GetStagesByChapter(string chapterId) => RecoveryFixture.StagesByChapter(chapterId);
        public GrowthTableData GetGrowthStep(GrowthType growthType, int step) => throw new NotImplementedException();
    }
}

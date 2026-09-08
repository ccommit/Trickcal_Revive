using System;
using System.Collections.Generic;
using TrickcalRevive.Data.Character;
using TrickcalRevive.Data.Growth;
using TrickcalRevive.Data.Stage;
using TrickcalRevive.Domain.Character;
using TrickcalRevive.Domain.Growth;
using TrickcalRevive.Domain.Stage;

namespace TrickcalRevive.Infra
{
    // character_master/stage_master/growth_table 로드는 각 컨텐츠 이슈에서 채운다. 지금은 뼈대만.
    public class MasterDataRepository : ICharacterRepository, IStageRepository, IGrowthRepository
    {
        public UnitMasterData GetCharacter(string characterId) => throw new NotImplementedException();
        public List<UnitMasterData> GetPlayableCharacters() => throw new NotImplementedException();
        public List<UnitMasterData> GetStarterCharacters() => throw new NotImplementedException();
        public StageMasterData GetStage(string stageId) => throw new NotImplementedException();
        public List<StageMasterData> GetStagesByChapter(string chapterId) => throw new NotImplementedException();
        public GrowthTableData GetGrowthStep(GrowthType growthType, int step) => throw new NotImplementedException();
    }
}

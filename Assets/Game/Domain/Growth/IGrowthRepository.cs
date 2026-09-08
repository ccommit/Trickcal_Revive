using TrickcalRevive.Data.Growth;

namespace TrickcalRevive.Domain.Growth
{
    // 성장 비용표. 비용을 코드에 박지 않고 economy_master.growth_table에서 읽는다.
    public interface IGrowthRepository
    {
        // 해당 단계 정의가 없으면 null. 최대치에 도달했다는 뜻으로 쓴다.
        GrowthTableData GetGrowthStep(GrowthType growthType, int step);
    }
}

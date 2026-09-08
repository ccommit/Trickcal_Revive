namespace TrickcalRevive.Domain.Growth
{
    // 성장 종류. Data.GrowthTableData.GrowthType(string)과 매핑된다.
    public enum GrowthType
    {
        // 알 수 없는 값. 마스터 데이터에 오타나 새 코드가 들어와도 게임이 죽지 않게 하되,
        // 로드 시 검증 리포트에 남겨 조용히 넘어가지 않도록 한다.
        Unknown,

        CharacterLevel,
        CharacterStar,
        SkillLevel
    }
}

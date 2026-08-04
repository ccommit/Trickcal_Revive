namespace TrickcalRevive.Infra
{
    // 마지막으로 로그인한 계정만 기억한다. 계정 데이터 자체와는 다른 관심사다.
    public interface ISessionService
    {
        void SaveSession(string accountId);
        string GetSession();
        void ClearSession();
    }
}

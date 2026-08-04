using System;

namespace TrickcalRevive.Infra
{
    // 세션(마지막 로그인 계정)은 파일에 남는 저장 관심사라 Infra에 둔다.
    public class SessionService : ISessionService
    {
        public SessionService(JsonFileRepository jsonFileRepository)
        {
        }

        public void SaveSession(string accountId) => throw new NotImplementedException();
        public string GetSession() => throw new NotImplementedException();
        public void ClearSession() => throw new NotImplementedException();
    }
}

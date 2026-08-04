namespace TrickcalRevive.Infra
{
    // 세션(마지막 로그인 계정)은 파일에 남는 저장 관심사라 Infra에 둔다.
    public class SessionService : ISessionService
    {
        private const string SessionFilePath = "session.json";

        [System.Serializable]
        private class SessionData
        {
            public string AccountId;
        }

        private readonly JsonFileRepository jsonFileRepository;

        public SessionService(JsonFileRepository jsonFileRepository)
        {
            this.jsonFileRepository = jsonFileRepository;
        }

        public void SaveSession(string accountId)
        {
            var json = UnityEngine.JsonUtility.ToJson(new SessionData { AccountId = accountId });
            jsonFileRepository.SaveJson(SessionFilePath, json);
        }

        public string GetSession()
        {
            var json = jsonFileRepository.LoadJson(SessionFilePath);
            if (string.IsNullOrEmpty(json))
                return null;
            return UnityEngine.JsonUtility.FromJson<SessionData>(json).AccountId;
        }

        public void ClearSession()
        {
            jsonFileRepository.Delete(SessionFilePath);
        }
    }
}

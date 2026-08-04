namespace TrickcalRevive.Infra
{
    // 파일을 읽고 쓰는 일만 한다. 게임을 전혀 모른다.
    // 나중에 파일 대신 서버에 저장하게 되면 여기 구현만 갈아끼운다.
    public interface IFileStore
    {
        string LoadJson(string path);
        void SaveJson(string path, string data);
        bool Exists(string path);
        void Delete(string path);
    }
}

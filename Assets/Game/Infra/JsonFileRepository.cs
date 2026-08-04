using System;

namespace TrickcalRevive.Infra
{
    // 실제 파일 IO는 각 컨텐츠 이슈에서 채운다(08_저장_설계서). 지금은 뼈대만.
    public class JsonFileRepository : IFileStore
    {
        public string LoadJson(string path) => throw new NotImplementedException();
        public void SaveJson(string path, string data) => throw new NotImplementedException();
        public bool Exists(string path) => throw new NotImplementedException();
        public void Delete(string path) => throw new NotImplementedException();
    }
}

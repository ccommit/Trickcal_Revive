using System.IO;
using UnityEngine;

namespace TrickcalRevive.Infra
{
    public class JsonFileRepository : IFileStore
    {
        private readonly string rootDirectory;

        public JsonFileRepository() : this(Application.persistentDataPath)
        {
        }

        public JsonFileRepository(string rootDirectory)
        {
            this.rootDirectory = rootDirectory;
        }

        public string LoadJson(string path)
        {
            var fullPath = ResolvePath(path);
            return File.Exists(fullPath) ? File.ReadAllText(fullPath) : null;
        }

        public void SaveJson(string path, string data)
        {
            var fullPath = ResolvePath(path);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            File.WriteAllText(fullPath, data);
        }

        public bool Exists(string path)
        {
            return File.Exists(ResolvePath(path));
        }

        public void Delete(string path)
        {
            var fullPath = ResolvePath(path);
            if (File.Exists(fullPath))
                File.Delete(fullPath);
        }

        private string ResolvePath(string path) => Path.Combine(rootDirectory, path);
    }
}

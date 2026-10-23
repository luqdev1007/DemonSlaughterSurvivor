using Game.Core;
using System.IO;
using UnityEngine;

namespace Game.Services
{
    public sealed class FileSaveStorage : ISaveStorage
    {
        private const string TemporarySuffix = ".tmp";
        private const string BackupSuffix = ".bak";

        private readonly string _directory;

        public FileSaveStorage()
        {
            _directory = Application.persistentDataPath;
        }

        public bool TryRead(string fileName, out string content)
        {
            string path = Path.Combine(_directory, fileName);

            if (File.Exists(path) == false)
            {
                content = null;

                return false;
            }

            content = File.ReadAllText(path);

            return true;
        }

        public void Write(string fileName, string content)
        {
            string path = Path.Combine(_directory, fileName);
            string temporary = path + TemporarySuffix;

            File.WriteAllText(temporary, content);

            if (File.Exists(path))
            {
                File.Replace(temporary, path, path + BackupSuffix);

                return;
            }

            File.Move(temporary, path);
        }

        public void Delete(string fileName)
        {
            string path = Path.Combine(_directory, fileName);

            if (File.Exists(path))
                File.Delete(path);
        }
    }
}

using System.IO;
using UnityEngine;
using DemonSlaughter.Core.Save;

namespace DemonSlaughter.Infrastructure.Save
{
    public sealed class JsonSaveService : ISaveService
    {
        private readonly string _savePath;

        public JsonSaveService()
        {
            _savePath = Path.Combine(
                Application.persistentDataPath,
                "save.json");
        }

        public bool HasSave()
        {
            return File.Exists(_savePath);
        }

        public SaveData Load()
        {
            if (!HasSave())
            {
                return new SaveData();
            }

            string json = File.ReadAllText(_savePath);

            return JsonUtility.FromJson<SaveData>(json);
        }

        public void Save(SaveData saveData)
        {
            string json = JsonUtility.ToJson(
                saveData,
                true);

            File.WriteAllText(_savePath, json);
        }
    }
}
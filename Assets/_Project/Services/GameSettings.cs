using Game.Core;
using System;
using System.IO;
using UnityEngine;

namespace Game.Services
{
    public sealed class GameSettings : ISoundSettings, ICombatTextSettings, IHeroSelection
    {
        public const int CurrentVersion = 1;

        private const string FileName = "settings.json";

        private readonly string _path;
        private SettingsData _data;

        public GameSettings()
        {
            _path = Path.Combine(Application.persistentDataPath, FileName);
            _data = Load(_path);
            ApplySound();
        }

        public float MasterVolume => _data.MasterVolume;

        public bool IsMuted => _data.Muted;

        public bool DamageNumbersEnabled => _data.DamageNumbers;

        public float DamageNumberScale => _data.DamageNumberScale;

        public bool DpsVisible => _data.DpsVisible;

        public string SelectedCharacterId => _data.CharacterId;

        public void SetMasterVolume(float volume)
        {
            _data.MasterVolume = Mathf.Clamp01(volume);
            ApplySound();
            Save();
        }

        public void SetMuted(bool muted)
        {
            _data.Muted = muted;
            ApplySound();
            Save();
        }

        public void SetDamageNumbersEnabled(bool enabled)
        {
            _data.DamageNumbers = enabled;
            Save();
        }

        public void SetDamageNumberScale(float scale)
        {
            _data.DamageNumberScale = Mathf.Clamp(scale, CombatTextLimits.MinDamageNumberScale, CombatTextLimits.MaxDamageNumberScale);
            Save();
        }

        public void SetDpsVisible(bool visible)
        {
            _data.DpsVisible = visible;
            Save();
        }

        public void SelectCharacter(string characterId)
        {
            _data.CharacterId = characterId;
            Save();
        }

        private void ApplySound()
        {
            AudioListener.volume = _data.Muted ? 0f : _data.MasterVolume;
        }

        private void Save()
        {
            try
            {
                File.WriteAllText(_path, JsonUtility.ToJson(_data, true));
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Settings were not saved to '{_path}': {exception.Message}");
            }
        }

        private static SettingsData Load(string path)
        {
            if (File.Exists(path) == false)
                return SettingsData.Defaults();

            try
            {
                SettingsData data = JsonUtility.FromJson<SettingsData>(File.ReadAllText(path));

                if (data == null || data.Version != CurrentVersion)
                    return SettingsData.Defaults();

                data.MasterVolume = Mathf.Clamp01(data.MasterVolume);
                data.DamageNumberScale = Mathf.Clamp(data.DamageNumberScale, CombatTextLimits.MinDamageNumberScale, CombatTextLimits.MaxDamageNumberScale);

                return data;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Settings at '{path}' could not be read, defaults are used: {exception.Message}");

                return SettingsData.Defaults();
            }
        }

        [Serializable]
        private sealed class SettingsData
        {
            public int Version;
            public float MasterVolume;
            public bool Muted;
            public bool DamageNumbers;
            public float DamageNumberScale;
            public bool DpsVisible;
            public string CharacterId;

            public static SettingsData Defaults()
            {
                return new SettingsData
                {
                    Version = CurrentVersion,
                    MasterVolume = 1f,
                    Muted = false,
                    DamageNumbers = true,
                    DamageNumberScale = 1f,
                    DpsVisible = true
                };
            }
        }
    }
}

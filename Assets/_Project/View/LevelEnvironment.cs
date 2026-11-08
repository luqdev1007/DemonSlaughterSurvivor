using Game.Configs;
using System;
using UnityEngine;

namespace Game.View
{
    public sealed class LevelEnvironment : IDisposable
    {
        private GameObject _instance;

        public LevelEnvironment(LevelConfig level)
        {
            if (level == null)
                throw new ArgumentNullException(nameof(level));

            if (level.EnvironmentPrefab != null)
                _instance = UnityEngine.Object.Instantiate(level.EnvironmentPrefab);
        }

        public GameObject Instance => _instance;

        public void Dispose()
        {
            if (_instance == null)
                return;

            GameObject instance = _instance;
            _instance = null;

            if (Application.isPlaying)
                UnityEngine.Object.Destroy(instance);
            else
                UnityEngine.Object.DestroyImmediate(instance);
        }
    }
}

using Game.Configs;
using Game.Core;
using System;
using UnityEngine;

namespace Game.View
{
    public sealed class ThreatAtmosphere : IThreatSink, IDisposable
    {
        private readonly GameObject _instance;
        private readonly ThreatVolume _volume;

        public ThreatAtmosphere(LevelConfig level)
        {
            if (level.ThreatAtmospherePrefab == null)
                return;

            _instance = UnityEngine.Object.Instantiate(level.ThreatAtmospherePrefab);
            _volume = _instance.GetComponentInChildren<ThreatVolume>();

            if (_volume == null)
                throw new InvalidOperationException(
                    $"Threat atmosphere prefab '{level.ThreatAtmospherePrefab.name}' of level '{level.Id}' has no {nameof(ThreatVolume)}.");
        }

        public void SetThreat(float waveProgress, bool bossAlive)
        {
            if (_volume != null)
                _volume.SetTarget(waveProgress, bossAlive);
        }

        public void Dispose()
        {
            if (_instance != null)
                UnityEngine.Object.Destroy(_instance);
        }
    }
}

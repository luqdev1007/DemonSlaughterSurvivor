using Game.Configs;
using Game.Core;
using Game.Simulation.Components;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class PublishThreatSystem : IEcsRunSystem
    {
        private readonly EcsFilterInject<Inc<WaveState>> _timelines = default;
        private readonly EcsFilterInject<Inc<Boss>, Exc<Dead>> _bosses = default;

        private readonly EcsPoolInject<WaveState> _waveStates = default;

        private readonly EcsCustomInject<LevelConfig> _level = default;
        private readonly EcsCustomInject<IThreatSink> _sink = default;

        public void Run(IEcsSystems systems)
        {
            float progress = 0f;

            foreach (int entity in _timelines.Value)
                progress = Progress(_waveStates.Value.Get(entity));

            _sink.Value.SetThreat(progress, _bosses.Value.GetEntitiesCount() > 0);
        }

        private float Progress(in WaveState state)
        {
            IReadOnlyList<Wave> waves = _level.Value.Waves.Waves;

            if (waves.Count <= 1)
                return 0f;

            int index = Mathf.Clamp(state.Index, 0, waves.Count - 1);
            float duration = waves[index].DurationSeconds;
            float inWave = duration > 0f ? Mathf.Clamp01(state.Elapsed / duration) : 1f;

            return Mathf.Clamp01((index + inWave) / (waves.Count - 1));
        }
    }
}

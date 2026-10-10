using Game.Configs;
using Game.Core;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class StartRangedAttackSystem : IEcsInitSystem, IEcsRunSystem
    {
        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<RangedAttack, ChaseTarget, Position>, Exc<Dead>> _filter = default;

        private readonly EcsPoolInject<RangedAttack> _attacks = default;
        private readonly EcsPoolInject<ChaseTarget> _chaseTargets = default;
        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<Dead> _dead = default;
        private readonly EcsPoolInject<RangedWindupStarted> _started = default;

        private readonly EcsCustomInject<SimulationClock> _clock = default;
        private readonly EcsCustomInject<LevelConfig> _level = default;
        private readonly EcsCustomInject<IContentRegistry> _content = default;

        public void Init(IEcsSystems systems)
        {
            IReadOnlyList<EnemyConfig> registered = _content.Value.All<EnemyConfig>();

            for (int index = 0; index < registered.Count; index++)
                Validate(registered[index]);

            WaveTimelineConfig timeline = _level.Value.Waves;

            if (timeline == null || timeline.Waves == null)
                return;

            for (int waveIndex = 0; waveIndex < timeline.Waves.Count; waveIndex++)
            {
                Wave wave = timeline.Waves[waveIndex];

                if (wave == null)
                    continue;

                for (int entry = 0; entry < wave.Enemies.Count; entry++)
                    Validate(wave.Enemies[entry] == null ? null : wave.Enemies[entry].Enemy);

                for (int entry = 0; entry < wave.Guaranteed.Count; entry++)
                    Validate(wave.Guaranteed[entry] == null ? null : wave.Guaranteed[entry].Enemy);
            }
        }

        public void Run(IEcsSystems systems)
        {
            EcsWorld world = _world.Value;
            float delta = _clock.Value.Delta;

            foreach (int entity in _filter.Value)
            {
                ref RangedAttack attack = ref _attacks.Value.Get(entity);

                if (attack.WindupTicks > 0 || attack.CooldownTicks > 0)
                    continue;

                RangedAttackConfig config = attack.Config;

                if (RangedTarget.TryFindLiving(world, _chaseTargets.Value.Get(entity).Value, _positions.Value, _dead.Value, out Vector3 target) == false)
                    continue;

                if (RangedTarget.IsWithin(_positions.Value.Get(entity).Value, target, config.Range) == false)
                    continue;

                int ticks = Mathf.Max(1, Mathf.RoundToInt(config.WindupSeconds / delta));

                attack.WindupTicks = ticks;
                attack.PlaybackSpeed = config.ReleaseClipSeconds / (ticks * delta);

                if (_started.Value.Has(entity) == false)
                    _started.Value.Add(entity);
            }
        }

        private static void Validate(EnemyConfig enemy)
        {
            if (enemy == null || enemy.RangedAttack == null)
                return;

            enemy.RangedAttack.Validate(enemy);
        }
    }
}

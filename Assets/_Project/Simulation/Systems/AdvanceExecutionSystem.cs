using Game.Configs;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class AdvanceExecutionSystem : IEcsRunSystem
    {
        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<Execution>> _executions = default;

        private readonly EcsPoolInject<Execution> _executionPool = default;
        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<Health> _healths = default;
        private readonly EcsPoolInject<MaxHealth> _maxHealths = default;
        private readonly EcsPoolInject<Dead> _dead = default;
        private readonly EcsPoolInject<DamageEvent> _damageEvents = default;

        private readonly EcsCustomInject<SimulationClock> _clock = default;

        public void Run(IEcsSystems systems)
        {
            EcsWorld world = _world.Value;
            float delta = _clock.Value.Delta;

            foreach (int entity in _executions.Value)
            {
                ref Execution execution = ref _executionPool.Value.Get(entity);

                if (execution.Owner.Unpack(world, out int owner) == false)
                {
                    world.DelEntity(entity);

                    continue;
                }

                ExecutionConfig config = execution.Config;

                while (execution.NextHit < config.HitCount && execution.ElapsedTicks == HitTick(config, execution.NextHit, delta))
                {
                    Hit(world, owner, ref execution);
                    execution.NextHit++;
                }

                execution.ElapsedTicks++;

                if (execution.ElapsedTicks >= TotalTicks(config, delta))
                    world.DelEntity(entity);
            }
        }

        public static int TotalTicks(ExecutionConfig config, float delta)
        {
            return Mathf.Max(1, Mathf.RoundToInt(config.ClipSeconds / config.PlaybackSpeed / delta));
        }

        public static int HitTick(ExecutionConfig config, int hit, float delta)
        {
            return Mathf.RoundToInt(config.HitClipSeconds(hit) / config.PlaybackSpeed / delta);
        }

        private void Hit(EcsWorld world, int owner, ref Execution execution)
        {
            if (execution.Target.Unpack(world, out int target) == false || _dead.Value.Has(target) || _healths.Value.Has(target) == false)
                return;

            float current = _healths.Value.Get(target).Current;
            float max = _maxHealths.Value.Has(target) ? _maxHealths.Value.Get(target).Value : current;

            ExecutionConfig config = execution.Config;

            int eventEntity = world.NewEntity();

            ref DamageEvent damageEvent = ref _damageEvents.Value.Add(eventEntity);
            damageEvent.Target = world.PackEntity(target);
            damageEvent.Source = world.PackEntity(owner);
            damageEvent.SourcePosition = _positions.Value.Get(owner).Value;
            damageEvent.Amount = config.HitDamage(config.Level(execution.Level), current, max);
            damageEvent.Kind = DamageKind.Perk;
            damageEvent.IsSlash = true;
            damageEvent.SourcePerk = config;
        }
    }
}

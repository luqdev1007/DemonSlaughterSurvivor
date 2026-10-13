using Game.Configs;
using Game.Core;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class StartExecutionSystem : IEcsRunSystem
    {
        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<ExecutionCooldown>> _cooldowns = default;
        private readonly EcsFilterInject<Inc<Player, Position, Facing>, Exc<Dead, Dashing, SpecialAttack, PoweringUp>> _heroes = default;
        private readonly EcsFilterInject<Inc<Boss, Position, BodyRadius, Health>, Exc<Dead>> _bosses = default;
        private readonly EcsFilterInject<Inc<TakenPerk>> _taken = default;

        private readonly EcsPoolInject<ExecutionCooldown> _cooldownPool = default;
        private readonly EcsPoolInject<TakenPerk> _takenPool = default;
        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<Facing> _facings = default;
        private readonly EcsPoolInject<BodyRadius> _bodyRadii = default;
        private readonly EcsPoolInject<Execution> _executions = default;
        private readonly EcsPoolInject<ExecutionStarted> _started = default;
        private readonly EcsPoolInject<SpecialAttack> _specialAttacks = default;

        private readonly EcsCustomInject<IContentRegistry> _content = default;
        private readonly EcsCustomInject<SimulationClock> _clock = default;

        public void Run(IEcsSystems systems)
        {
            foreach (int entity in _cooldowns.Value)
            {
                ref ExecutionCooldown cooldown = ref _cooldownPool.Value.Get(entity);

                if (cooldown.RemainingTicks > 0)
                    cooldown.RemainingTicks--;
            }

            EcsWorld world = _world.Value;

            foreach (int hero in _heroes.Value)
            {
                if (TakenBehaviourLookup.TryFind(world, _taken.Value, _takenPool.Value, _content.Value, hero, out ExecutionConfig config, out int level) == false)
                    continue;

                if (_cooldownPool.Value.Has(hero) == false)
                    _cooldownPool.Value.Add(hero);

                ref ExecutionCooldown cooldown = ref _cooldownPool.Value.Get(hero);

                if (cooldown.RemainingTicks > 0)
                    continue;

                if (TryFindTarget(hero, config, out int target) == false)
                    continue;

                Start(world, hero, target, config, level);

                cooldown.RemainingTicks = Mathf.Max(1, Mathf.RoundToInt(config.Level(level).CooldownSeconds / _clock.Value.Delta));
            }
        }

        private bool TryFindTarget(int hero, ExecutionConfig config, out int target)
        {
            Vector3 origin = _positions.Value.Get(hero).Value;
            Vector3 facing = _facings.Value.Get(hero).Value;
            facing.y = 0f;

            target = -1;

            if (facing.sqrMagnitude <= Mathf.Epsilon)
                return false;

            facing.Normalize();

            float cosHalfAngle = Mathf.Cos(config.SectorHalfAngleDegrees * Mathf.Deg2Rad);
            float nearest = float.MaxValue;

            foreach (int boss in _bosses.Value)
            {
                Vector3 offset = _positions.Value.Get(boss).Value - origin;
                offset.y = 0f;

                float distance = offset.magnitude;

                if (distance > config.Reach + _bodyRadii.Value.Get(boss).Value)
                    continue;

                if (distance > Mathf.Epsilon && Vector3.Dot(offset / distance, facing) < cosHalfAngle - 1e-6f)
                    continue;

                if (distance >= nearest)
                    continue;

                nearest = distance;
                target = boss;
            }

            return target >= 0;
        }

        private void Start(EcsWorld world, int hero, int target, ExecutionConfig config, int level)
        {
            int entity = world.NewEntity();

            ref Execution execution = ref _executions.Value.Add(entity);
            execution.Owner = world.PackEntity(hero);
            execution.Target = world.PackEntity(target);
            execution.Config = config;
            execution.Level = level;
            execution.ElapsedTicks = 0;
            execution.NextHit = 0;

            _started.Value.Add(entity);

            ref SpecialAttack special = ref _specialAttacks.Value.Add(hero);
            special.Attack = world.PackEntity(entity);
            special.LocksMovement = config.LocksMovement;
        }
    }
}

using Game.Configs;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class ApplyDamageSystem : IEcsInitSystem, IEcsRunSystem
    {
        private const int TargetCapacityReserve = 16;

        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<DamageEvent>> _events = default;

        private readonly EcsPoolInject<DamageEvent> _damageEvents = default;
        private readonly EcsPoolInject<Health> _healths = default;
        private readonly EcsPoolInject<Invulnerable> _invulnerables = default;
        private readonly EcsPoolInject<HitInvulnerability> _hitInvulnerabilities = default;
        private readonly EcsPoolInject<DamageTaken> _damageTakens = default;
        private readonly EcsPoolInject<DamageApplied> _applied = default;
        private readonly EcsPoolInject<KillingBlow> _killingBlows = default;
        private readonly EcsPoolInject<Player> _players = default;

        private readonly EcsCustomInject<SimulationClock> _clock = default;
        private readonly EcsCustomInject<LevelConfig> _level = default;

        private Dictionary<int, int> _strongest;

        public void Init(IEcsSystems systems)
        {
            int capacity = WaveTimelineValidator.ResolveMaxLiveCap(_level.Value.Waves) + TargetCapacityReserve;

            _strongest = new Dictionary<int, int>(capacity);
        }

        public void Run(IEcsSystems systems)
        {
            _strongest.Clear();

            EcsWorld world = _world.Value;

            foreach (int entity in _events.Value)
            {
                ref DamageEvent damageEvent = ref _damageEvents.Value.Get(entity);

                if (damageEvent.Amount <= 0f)
                    continue;

                if (damageEvent.Target.Unpack(world, out int target) == false)
                    continue;

                if (_healths.Value.Has(target) == false)
                    continue;

                if (_invulnerables.Value.Has(target))
                    continue;

                if (_players.Value.Has(target) == false)
                {
                    if (_healths.Value.Get(target).Current <= 0f)
                        continue;

                    Apply(target, entity, ref damageEvent, _clock.Value.Delta);

                    continue;
                }

                if (_strongest.TryGetValue(target, out int previous))
                {
                    ref DamageEvent previousEvent = ref _damageEvents.Value.Get(previous);

                    if (damageEvent.Amount <= previousEvent.Amount)
                        continue;
                }

                _strongest[target] = entity;
            }

            float delta = _clock.Value.Delta;

            foreach (KeyValuePair<int, int> pair in _strongest)
            {
                ref DamageEvent damageEvent = ref _damageEvents.Value.Get(pair.Value);

                Apply(pair.Key, pair.Value, ref damageEvent, delta);
            }
        }

        private void Apply(int target, int eventEntity, ref DamageEvent damageEvent, float delta)
        {
            ref Health health = ref _healths.Value.Get(target);

            bool wasAlive = health.Current > 0f;

            float amount = damageEvent.Amount;

            if (_damageTakens.Value.Has(target))
                amount *= _damageTakens.Value.Get(target).Value;

            health.Current -= amount;

            ref DamageApplied applied = ref _applied.Value.Add(eventEntity);
            applied.Amount = amount;

            if (wasAlive && health.Current <= 0f && _killingBlows.Value.Has(target) == false)
            {
                ref KillingBlow killingBlow = ref _killingBlows.Value.Add(target);
                killingBlow.SourcePosition = damageEvent.SourcePosition;
            }

            GrantInvulnerability(target, delta);
        }

        private void GrantInvulnerability(int entity, float delta)
        {
            if (_hitInvulnerabilities.Value.Has(entity) == false)
                return;

            ref HitInvulnerability hitInvulnerability = ref _hitInvulnerabilities.Value.Get(entity);

            if (hitInvulnerability.Seconds <= 0f)
                return;

            Invulnerability.Grant(_invulnerables.Value, entity, Mathf.Max(1, Mathf.RoundToInt(hitInvulnerability.Seconds / delta)));
        }
    }
}

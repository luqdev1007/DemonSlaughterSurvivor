using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class DebugLogPlayerHealthSystem : IEcsRunSystem
    {
        private readonly EcsFilterInject<Inc<Player, Health>> _filter = default;

        private readonly EcsPoolInject<Health> _healths = default;
        private readonly EcsPoolInject<MaxHealth> _maxHealths = default;
        private readonly EcsPoolInject<Invulnerable> _invulnerables = default;
        private readonly EcsPoolInject<Dead> _deads = default;

        private readonly EcsCustomInject<SimulationClock> _clock = default;

        private float _lastHealth = float.NaN;
        private bool _lastInvulnerable;
        private bool _lastDead;

        public void Run(IEcsSystems systems)
        {
            foreach (int entity in _filter.Value)
            {
                ref Health health = ref _healths.Value.Get(entity);

                bool invulnerable = _invulnerables.Value.Has(entity);
                bool dead = _deads.Value.Has(entity);

                if (health.Current == _lastHealth && invulnerable == _lastInvulnerable && dead == _lastDead)
                    continue;

                float max = _maxHealths.Value.Has(entity) ? _maxHealths.Value.Get(entity).Value : 0f;

                int ticks = invulnerable ? _invulnerables.Value.Get(entity).RemainingTicks : 0;

                Debug.Log(
                    $"[block2] t={_clock.Value.Elapsed:F2} hp={health.Current:F1}/{max:F1} " +
                    $"iframes={(invulnerable ? ticks.ToString() : "-")} dead={dead}");

                _lastHealth = health.Current;
                _lastInvulnerable = invulnerable;
                _lastDead = dead;
            }
        }
    }
}

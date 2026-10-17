using Game.Simulation.Components;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

namespace Game.Simulation.Systems
{
    public sealed class AdvanceDashSystem : IEcsRunSystem
    {
        private readonly EcsFilterInject<Inc<Dashing, DashStats>> _filter = default;

        private readonly EcsPoolInject<Dashing> _dashes = default;
        private readonly EcsPoolInject<DashStats> _stats = default;
        private readonly EcsPoolInject<DashCooldown> _cooldowns = default;
        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<DashEndedEvent> _endedEvents = default;

        public void Run(IEcsSystems systems)
        {
            foreach (int entity in _filter.Value)
            {
                ref Dashing dashing = ref _dashes.Value.Get(entity);

                dashing.RemainingTicks -= 1;

                if (dashing.RemainingTicks > 0)
                    continue;

                ref DashStats stats = ref _stats.Value.Get(entity);

                ref DashCooldown cooldown = ref _cooldowns.Value.Add(entity);
                cooldown.Remaining = stats.Cooldown;

                ref DashEndedEvent ended = ref _endedEvents.Value.Has(entity) ? ref _endedEvents.Value.Get(entity) : ref _endedEvents.Value.Add(entity);
                ended.Start = dashing.Start;
                ended.End = _positions.Value.Has(entity) ? _positions.Value.Get(entity).Value : dashing.Start;
                ended.Direction = dashing.Direction;
                ended.Strength = dashing.Strength;
                ended.DamageMultiplier = dashing.DamageMultiplier;

                _dashes.Value.Del(entity);
            }
        }
    }
}

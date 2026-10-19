using Game.Simulation.Components;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

namespace Game.Simulation.Systems
{
    public sealed class TickSlowedSystem : IEcsRunSystem
    {
        private readonly EcsFilterInject<Inc<Slowed>> _slowed = default;

        private readonly EcsPoolInject<Slowed> _slowedPool = default;
        private readonly EcsPoolInject<MoveIntent> _intents = default;
        private readonly EcsPoolInject<Dead> _dead = default;

        public void Run(IEcsSystems systems)
        {
            foreach (int entity in _slowed.Value)
            {
                ref Slowed slowed = ref _slowedPool.Value.Get(entity);

                if (_intents.Value.Has(entity))
                    _intents.Value.Get(entity).Value *= 1f - slowed.Factor;

                if (--slowed.RemainingTicks <= 0 || _dead.Value.Has(entity))
                    _slowedPool.Value.Del(entity);
            }
        }
    }
}

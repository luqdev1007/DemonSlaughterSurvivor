using Game.Simulation.Components;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

namespace Game.Simulation.Systems
{
    public sealed class TickPendingFinishSystem : IEcsRunSystem
    {
        private readonly EcsFilterInject<Inc<PendingFinish>> _filter = default;

        private readonly EcsPoolInject<PendingFinish> _pendingFinishes = default;

        public void Run(IEcsSystems systems)
        {
            foreach (int entity in _filter.Value)
            {
                ref PendingFinish pending = ref _pendingFinishes.Value.Get(entity);

                pending.RemainingTicks -= 1;
            }
        }
    }
}

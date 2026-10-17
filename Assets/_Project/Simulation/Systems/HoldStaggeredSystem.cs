using Game.Simulation.Components;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class HoldStaggeredSystem : IEcsRunSystem
    {
        private readonly EcsFilterInject<Inc<Staggered>> _filter = default;

        private readonly EcsPoolInject<Staggered> _staggered = default;
        private readonly EcsPoolInject<MoveIntent> _intents = default;

        public void Run(IEcsSystems systems)
        {
            foreach (int entity in _filter.Value)
            {
                if (_intents.Value.Has(entity))
                    _intents.Value.Get(entity).Value = Vector3.zero;

                ref Staggered staggered = ref _staggered.Value.Get(entity);
                staggered.RemainingTicks--;

                if (staggered.RemainingTicks > 0)
                    continue;

                _staggered.Value.Del(entity);
            }
        }
    }
}

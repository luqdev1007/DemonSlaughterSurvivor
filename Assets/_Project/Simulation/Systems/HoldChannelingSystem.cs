using Game.Simulation.Components;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class HoldChannelingSystem : IEcsRunSystem
    {
        private readonly EcsFilterInject<Inc<Channeling, MoveIntent>> _filter = default;

        private readonly EcsPoolInject<MoveIntent> _intents = default;

        public void Run(IEcsSystems systems)
        {
            foreach (int entity in _filter.Value)
                _intents.Value.Get(entity).Value = Vector3.zero;
        }
    }
}

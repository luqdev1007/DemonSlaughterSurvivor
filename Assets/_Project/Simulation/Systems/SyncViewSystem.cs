using Game.Simulation.Components;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class SyncViewSystem : IEcsRunSystem
    {
        private readonly EcsFilterInject<Inc<Position, Facing, View>> _filter = default;

        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<Facing> _facings = default;
        private readonly EcsPoolInject<View> _views = default;

        public void Run(IEcsSystems systems)
        {
            foreach (var entity in _filter.Value)
            {
                ref var position = ref _positions.Value.Get(entity);
                ref var facing = ref _facings.Value.Get(entity);
                ref var view = ref _views.Value.Get(entity);

                view.Value.position = position.Value;

                if (facing.Value != Vector3.zero)
                    view.Value.rotation = Quaternion.LookRotation(facing.Value); 
            }
        }
    }
}

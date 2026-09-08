using Game.Core;
using Game.Simulation.Components;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using System;

namespace Game.Simulation.Systems
{
    public sealed class BindCameraTargetSystem : IEcsRunSystem
    {
        private readonly EcsFilterInject<Inc<Player, View>> _filter = default;

        private readonly EcsPoolInject<View> _views = default;

        private readonly EcsCustomInject<ICameraService> _camera = default;

        public void Run(IEcsSystems systems)
        {
            if (_filter.Value.GetEntitiesCount() == 0)
                return;

            throw new NotImplementedException();
        }
    }
}

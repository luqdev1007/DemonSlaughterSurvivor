using Game.Core;
using Game.Simulation.Components;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

namespace Game.Simulation.Systems
{
    public sealed class BindCameraTargetSystem : IEcsRunSystem
    {
        private readonly EcsFilterInject<Inc<Player, View>> _filter = default;

        private readonly EcsPoolInject<View> _views = default;

        private readonly EcsCustomInject<ICameraService> _camera = default;

        private bool _cameraBinded = false;

        public void Run(IEcsSystems systems)
        {
            if (_cameraBinded)
                return;

            foreach (var entity in _filter.Value)
            {
                ref View view = ref _views.Value.Get(entity);

                _camera.Value.SetFollowTarget(view.Value.Transform);
                _cameraBinded = true;
            }
        }
    }
}

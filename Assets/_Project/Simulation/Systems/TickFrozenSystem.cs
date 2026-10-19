using Game.Core;
using Game.Simulation.Components;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class TickFrozenSystem : IEcsRunSystem
    {
        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<Frozen>> _frozenFilter = default;
        private readonly EcsFilterInject<Inc<FrozenFx, Position>> _fxFilter = default;

        private readonly EcsPoolInject<Frozen> _frozen = default;
        private readonly EcsPoolInject<FrozenFx> _fxPool = default;
        private readonly EcsPoolInject<MoveIntent> _intents = default;
        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<Dead> _dead = default;
        private readonly EcsPoolInject<View> _views = default;

        private readonly EcsCustomInject<IViewFactory> _viewFactory = default;

        public void Run(IEcsSystems systems)
        {
            EcsWorld world = _world.Value;

            foreach (int entity in _frozenFilter.Value)
            {
                if (_intents.Value.Has(entity))
                    _intents.Value.Get(entity).Value = Vector3.zero;

                ref Frozen frozen = ref _frozen.Value.Get(entity);
                frozen.RemainingTicks--;

                if (frozen.RemainingTicks > 0 && _dead.Value.Has(entity) == false)
                    continue;

                _frozen.Value.Del(entity);

                if (_views.Value.Has(entity) && _views.Value.Get(entity).Value is IFreezeView view)
                    view.SetFrozen(false);
            }

            foreach (int fx in _fxFilter.Value)
            {
                if (_fxPool.Value.Get(fx).Owner.Unpack(world, out int owner) && _frozen.Value.Has(owner) && _positions.Value.Has(owner))
                {
                    _positions.Value.Get(fx).Value = _positions.Value.Get(owner).Value;

                    continue;
                }

                if (_views.Value.Has(fx))
                {
                    ref View view = ref _views.Value.Get(fx);

                    if (view.Value != null)
                        _viewFactory.Value.Release(view.Value);

                    view.Value = null;
                }

                world.DelEntity(fx);
            }
        }
    }
}

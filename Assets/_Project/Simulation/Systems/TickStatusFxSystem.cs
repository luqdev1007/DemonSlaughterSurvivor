using Game.Core;
using Game.Simulation.Components;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

namespace Game.Simulation.Systems
{
    public sealed class TickStatusFxSystem : IEcsRunSystem
    {
        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<StatusFx, Position>> _fx = default;

        private readonly EcsPoolInject<StatusFx> _fxPool = default;
        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<Burning> _burning = default;
        private readonly EcsPoolInject<ElementMark> _marks = default;
        private readonly EcsPoolInject<Dead> _dead = default;
        private readonly EcsPoolInject<View> _views = default;

        private readonly EcsCustomInject<IViewFactory> _viewFactory = default;

        public void Run(IEcsSystems systems)
        {
            EcsWorld world = _world.Value;

            foreach (int fx in _fx.Value)
            {
                ref StatusFx status = ref _fxPool.Value.Get(fx);

                if (status.Owner.Unpack(world, out int owner) && _dead.Value.Has(owner) == false && HasStatus(owner, status.Kind) && _positions.Value.Has(owner))
                {
                    _positions.Value.Get(fx).Value = _positions.Value.Get(owner).Value;

                    continue;
                }

                if (status.Owner.Unpack(world, out owner))
                    ClearFlag(owner, status.Kind);

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

        private bool HasStatus(int owner, StatusKind kind)
        {
            return kind == StatusKind.Burning ? _burning.Value.Has(owner) : _marks.Value.Has(owner);
        }

        private void ClearFlag(int owner, StatusKind kind)
        {
            if (kind == StatusKind.Burning && _burning.Value.Has(owner))
                _burning.Value.Get(owner).HasFx = false;

            if (kind == StatusKind.Marked && _marks.Value.Has(owner))
                _marks.Value.Get(owner).HasFx = false;
        }
    }
}

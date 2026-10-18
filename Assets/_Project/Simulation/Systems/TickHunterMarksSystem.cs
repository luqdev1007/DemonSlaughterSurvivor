using Game.Core;
using Game.Simulation.Components;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

namespace Game.Simulation.Systems
{
    public sealed class TickHunterMarksSystem : IEcsRunSystem
    {
        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<HunterMark>> _marked = default;
        private readonly EcsFilterInject<Inc<MarkFx, Position>> _fx = default;

        private readonly EcsPoolInject<HunterMark> _marks = default;
        private readonly EcsPoolInject<MarkFx> _markFx = default;
        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<Dead> _dead = default;
        private readonly EcsPoolInject<View> _views = default;

        private readonly EcsCustomInject<IViewFactory> _viewFactory = default;

        public void Run(IEcsSystems systems)
        {
            EcsWorld world = _world.Value;

            foreach (int enemy in _marked.Value)
            {
                ref HunterMark mark = ref _marks.Value.Get(enemy);

                if (--mark.RemainingTicks <= 0 || _dead.Value.Has(enemy))
                    _marks.Value.Del(enemy);
            }

            foreach (int fx in _fx.Value)
            {
                bool alive = _markFx.Value.Get(fx).Target.Unpack(world, out int target) && _marks.Value.Has(target) && _dead.Value.Has(target) == false;

                if (alive)
                {
                    _positions.Value.Get(fx).Value = _positions.Value.Get(target).Value;

                    continue;
                }

                if (_views.Value.Has(fx) && _views.Value.Get(fx).Value != null)
                    _viewFactory.Value.Release(_views.Value.Get(fx).Value);

                world.DelEntity(fx);
            }
        }
    }
}

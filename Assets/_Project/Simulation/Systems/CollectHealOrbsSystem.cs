using Game.Core;
using Game.Simulation.Components;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

namespace Game.Simulation.Systems
{
    public sealed class CollectHealOrbsSystem : IEcsRunSystem
    {
        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<HealOrb, GemFlight>> _flying = default;

        private readonly EcsPoolInject<HealOrb> _orbs = default;
        private readonly EcsPoolInject<GemFlight> _flights = default;
        private readonly EcsPoolInject<MaxHealth> _maxHealths = default;
        private readonly EcsPoolInject<Dead> _dead = default;
        private readonly EcsPoolInject<View> _views = default;
        private readonly EcsPoolInject<HealEvent> _healEvents = default;

        private readonly EcsCustomInject<IViewFactory> _viewFactory = default;

        public void Run(IEcsSystems systems)
        {
            EcsWorld world = _world.Value;

            foreach (int orb in _flying.Value)
            {
                ref GemFlight flight = ref _flights.Value.Get(orb);

                if (flight.ElapsedTicks < flight.TotalTicks)
                    continue;

                if (flight.Target.Unpack(world, out int hero) == false || _dead.Value.Has(hero) || _maxHealths.Value.Has(hero) == false)
                    continue;

                int eventEntity = world.NewEntity();

                ref HealEvent healEvent = ref _healEvents.Value.Add(eventEntity);
                healEvent.Target = flight.Target;
                healEvent.Amount = _orbs.Value.Get(orb).Fraction * _maxHealths.Value.Get(hero).Value;

                if (_views.Value.Has(orb))
                {
                    ref View view = ref _views.Value.Get(orb);

                    _viewFactory.Value.Release(view.Value);

                    view.Value = null;
                }

                world.DelEntity(orb);
            }
        }
    }
}

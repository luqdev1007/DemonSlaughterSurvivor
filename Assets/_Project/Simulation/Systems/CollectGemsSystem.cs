using Game.Core;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

namespace Game.Simulation.Systems
{
    public sealed class CollectGemsSystem : IEcsRunSystem
    {
        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<Gem, GemFlight>> _flying = default;

        private readonly EcsPoolInject<Gem> _gems = default;
        private readonly EcsPoolInject<GemFlight> _flights = default;
        private readonly EcsPoolInject<Experience> _experiences = default;
        private readonly EcsPoolInject<Dead> _dead = default;
        private readonly EcsPoolInject<View> _views = default;
        private readonly EcsPoolInject<CombatStats> _combatStats = default;

        private readonly EcsCustomInject<IViewFactory> _viewFactory = default;

        public void Run(IEcsSystems systems)
        {
            EcsWorld world = _world.Value;

            foreach (int gem in _flying.Value)
            {
                ref GemFlight flight = ref _flights.Value.Get(gem);

                if (flight.ElapsedTicks < flight.TotalTicks)
                    continue;

                if (flight.Target.Unpack(world, out int hero) == false || _dead.Value.Has(hero) || _experiences.Value.Has(hero) == false)
                    continue;

                Grant(hero, _gems.Value.Get(gem).Experience);

                if (_views.Value.Has(gem))
                {
                    ref View view = ref _views.Value.Get(gem);

                    _viewFactory.Value.Release(view.Value);

                    view.Value = null;
                }

                world.DelEntity(gem);
            }
        }

        private void Grant(int hero, int amount)
        {
            ref Experience experience = ref _experiences.Value.Get(hero);

            experience.Current += amount;

            float luck = _combatStats.Value.Has(hero) ? _combatStats.Value.Get(hero).Luck : 0f;

            if (luck <= 0f)
                return;

            experience.LuckCarry += amount * luck / ItemRoller.LuckScale;

            int bonus = (int)experience.LuckCarry;

            experience.Current += bonus;
            experience.LuckCarry -= bonus;
        }
    }
}

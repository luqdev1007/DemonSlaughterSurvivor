using Game.Core;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

namespace Game.Simulation.Systems
{
    public sealed class DropHealOrbsSystem : IEcsInitSystem, IEcsRunSystem
    {
        private const string RandomStreamId = "drop.heal_orb";

        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<Breakable, DiedEvent, HealOrbDrop, Position>> _dying = default;

        private readonly EcsPoolInject<HealOrbDrop> _drops = default;
        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<HealOrb> _orbs = default;
        private readonly EcsPoolInject<Velocity> _velocities = default;
        private readonly EcsPoolInject<View> _views = default;
        private readonly EcsPoolInject<NotIndexed> _notIndexed = default;

        private readonly EcsCustomInject<RunContext> _context = default;
        private readonly EcsCustomInject<IViewFactory> _viewFactory = default;

        private uint _random;

        public void Init(IEcsSystems systems)
        {
            _random = SimulationRandom.StreamState(_context.Value.Seed, RandomStreamId);
        }

        public void Run(IEcsSystems systems)
        {
            EcsWorld world = _world.Value;

            foreach (int breakable in _dying.Value)
            {
                ref HealOrbDrop drop = ref _drops.Value.Get(breakable);

                if (SimulationRandom.NextUnit(ref _random) >= drop.Chance)
                    continue;

                ref Position position = ref _positions.Value.Get(breakable);

                int orb = world.NewEntity();

                _orbs.Value.Add(orb).Fraction = drop.Config.HealFraction;
                _positions.Value.Add(orb).Value = position.Value;
                _velocities.Value.Add(orb);
                _notIndexed.Value.Add(orb);
                _views.Value.Add(orb).Value = _viewFactory.Value.Create(drop.Config.ViewPrefab, position.Value);
            }
        }
    }
}

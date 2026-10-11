using Game.Configs;
using Game.Core;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class StartGemFlightSystem : IEcsInitSystem, IEcsRunSystem
    {
        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<Player, Position, PickupRadius>, Exc<Dead>> _heroes = default;
        private readonly EcsFilterInject<Inc<Gem, Position>, Exc<GemFlight>> _resting = default;
        private readonly EcsFilterInject<Inc<HealOrb, Position>, Exc<GemFlight>> _restingOrbs = default;

        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<PickupRadius> _radii = default;
        private readonly EcsPoolInject<GemFlight> _flights = default;
        private readonly EcsPoolInject<Health> _healths = default;
        private readonly EcsPoolInject<MaxHealth> _maxHealths = default;

        private readonly EcsCustomInject<IContentRegistry> _content = default;
        private readonly EcsCustomInject<RunContext> _context = default;
        private readonly EcsCustomInject<SimulationClock> _clock = default;

        private ExperienceConfig _config;

        public void Init(IEcsSystems systems)
        {
            _config = ExperienceRules.Resolve(_content.Value, _context.Value);
        }

        public void Run(IEcsSystems systems)
        {
            EcsWorld world = _world.Value;

            foreach (int hero in _heroes.Value)
            {
                Vector3 heroPosition = _positions.Value.Get(hero).Value;
                float radius = _radii.Value.Get(hero).Value;
                float radiusSqr = radius * radius;

                foreach (int gem in _resting.Value)
                    TryStart(world, hero, heroPosition, radiusSqr, gem);

                if (IsWounded(hero) == false)
                    continue;

                foreach (int orb in _restingOrbs.Value)
                    TryStart(world, hero, heroPosition, radiusSqr, orb);
            }
        }

        private void TryStart(EcsWorld world, int hero, Vector3 heroPosition, float radiusSqr, int pickup)
        {
            Vector3 position = _positions.Value.Get(pickup).Value;

            float dx = position.x - heroPosition.x;
            float dz = position.z - heroPosition.z;

            if (dx * dx + dz * dz > radiusSqr)
                return;

            ref GemFlight flight = ref _flights.Value.Add(pickup);
            flight.Start = position;
            flight.TotalTicks = ExperienceRules.FlightTicks(_config, _clock.Value.Delta);
            flight.ElapsedTicks = 0;
            flight.Target = world.PackEntity(hero);
        }

        private bool IsWounded(int hero)
        {
            if (_healths.Value.Has(hero) == false || _maxHealths.Value.Has(hero) == false)
                return false;

            return _healths.Value.Get(hero).Current < _maxHealths.Value.Get(hero).Value;
        }
    }
}

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

        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<PickupRadius> _radii = default;
        private readonly EcsPoolInject<GemFlight> _flights = default;

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
                {
                    Vector3 position = _positions.Value.Get(gem).Value;

                    float dx = position.x - heroPosition.x;
                    float dz = position.z - heroPosition.z;

                    if (dx * dx + dz * dz > radiusSqr)
                        continue;

                    ref GemFlight flight = ref _flights.Value.Add(gem);
                    flight.Start = position;
                    flight.TotalTicks = ExperienceRules.FlightTicks(_config, _clock.Value.Delta);
                    flight.ElapsedTicks = 0;
                    flight.Target = world.PackEntity(hero);
                }
            }
        }
    }
}

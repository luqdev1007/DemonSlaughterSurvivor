using Game.Core;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class ApplyGemFlightSystem : IEcsInitSystem, IEcsRunSystem
    {
        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<Gem, GemFlight, Position, Velocity>> _flying = default;

        private readonly EcsPoolInject<GemFlight> _flights = default;
        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<Velocity> _velocities = default;
        private readonly EcsPoolInject<Dead> _dead = default;

        private readonly EcsCustomInject<IContentRegistry> _content = default;
        private readonly EcsCustomInject<RunContext> _context = default;
        private readonly EcsCustomInject<SimulationClock> _clock = default;

        private float _power;

        public void Init(IEcsSystems systems)
        {
            _power = ExperienceRules.Resolve(_content.Value, _context.Value).FlightPower;
        }

        public void Run(IEcsSystems systems)
        {
            EcsWorld world = _world.Value;
            float delta = _clock.Value.Delta;

            foreach (int gem in _flying.Value)
            {
                ref GemFlight flight = ref _flights.Value.Get(gem);
                ref Velocity velocity = ref _velocities.Value.Get(gem);

                if (flight.Target.Unpack(world, out int hero) == false || _dead.Value.Has(hero) || _positions.Value.Has(hero) == false)
                {
                    velocity.Value = Vector3.zero;

                    continue;
                }

                if (flight.ElapsedTicks < flight.TotalTicks)
                    flight.ElapsedTicks++;

                float progress = Mathf.Pow((float)flight.ElapsedTicks / flight.TotalTicks, _power);
                Vector3 desired = Vector3.LerpUnclamped(flight.Start, _positions.Value.Get(hero).Value, progress);

                velocity.Value = (desired - _positions.Value.Get(gem).Value) / delta;
            }
        }
    }
}

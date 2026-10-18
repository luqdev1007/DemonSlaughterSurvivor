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

        private readonly EcsFilterInject<Inc<GemFlight, Position, Velocity>> _flying = default;

        private readonly EcsPoolInject<GemFlight> _flights = default;
        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<Velocity> _velocities = default;
        private readonly EcsPoolInject<Dead> _dead = default;

        private readonly EcsCustomInject<IContentRegistry> _content = default;
        private readonly EcsCustomInject<RunContext> _context = default;
        private readonly EcsCustomInject<SimulationClock> _clock = default;

        private float _power;
        private float _anticipation;
        private float _arc;

        public void Init(IEcsSystems systems)
        {
            Game.Configs.ExperienceConfig config = ExperienceRules.Resolve(_content.Value, _context.Value);

            _power = config.FlightPower;
            _anticipation = config.FlightAnticipation;
            _arc = config.FlightArc;
        }

        public static float Ease(float t, float power, float anticipation)
        {
            if (anticipation <= 0f)
                return Mathf.Pow(t, power);

            return t * t * ((anticipation + 1f) * t - anticipation);
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

                float t = (float)flight.ElapsedTicks / flight.TotalTicks;
                float progress = Ease(t, _power, _anticipation);
                Vector3 target = _positions.Value.Get(hero).Value;
                Vector3 desired = Vector3.LerpUnclamped(flight.Start, target, progress);

                Vector3 across = Vector3.Cross(Vector3.up, target - flight.Start);

                if (across.sqrMagnitude > 1e-6f)
                    desired += across.normalized * (flight.Side * _arc * Mathf.Sin(Mathf.PI * t) * (1f - t));

                velocity.Value = (desired - _positions.Value.Get(gem).Value) / delta;
            }
        }
    }
}

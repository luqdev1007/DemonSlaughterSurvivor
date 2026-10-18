using Game.Configs;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class FaceLegsSystem : IEcsRunSystem
    {
        private readonly EcsFilterInject<Inc<Player, TorsoAim, Facing, Velocity, TurnSpeed>, Exc<Dead, Backleaping>> _heroes = default;

        private readonly EcsPoolInject<TorsoAim> _aims = default;
        private readonly EcsPoolInject<Facing> _facings = default;
        private readonly EcsPoolInject<Velocity> _velocities = default;
        private readonly EcsPoolInject<TurnSpeed> _turnSpeeds = default;

        private readonly EcsCustomInject<SimulationClock> _clock = default;

        public void Run(IEcsSystems systems)
        {
            float delta = _clock.Value.Delta;

            foreach (int hero in _heroes.Value)
            {
                ref TorsoAim aim = ref _aims.Value.Get(hero);
                ref Facing facing = ref _facings.Value.Get(hero);
                TorsoAimConfig config = aim.Config;

                Vector3 velocity = _velocities.Value.Get(hero).Value;
                velocity.y = 0f;

                if (velocity.sqrMagnitude > Mathf.Epsilon)
                {
                    aim.TurnSide = 0;

                    Vector3 moveDirection = velocity.normalized;
                    float angle = Vector3.Angle(moveDirection, aim.Direction);

                    if (aim.Backpedal == false && angle > config.TwistLimit)
                        aim.Backpedal = true;
                    else if (aim.Backpedal && angle < config.TwistLimit - config.BackpedalHysteresis)
                        aim.Backpedal = false;

                    Vector3 legs = aim.Backpedal ? -moveDirection : moveDirection;
                    facing.Value = YawTurn.Towards(facing.Value, legs, _turnSpeeds.Value.Get(hero).Value * delta);

                    continue;
                }

                aim.Backpedal = false;

                float twist = Vector3.SignedAngle(facing.Value, aim.Direction, Vector3.up);

                if (aim.TurnSide == 0 && Mathf.Abs(twist) <= config.TwistLimit)
                    continue;

                if (Mathf.Abs(twist) <= config.TurnInPlaceSettle)
                {
                    aim.TurnSide = 0;

                    continue;
                }

                aim.TurnSide = twist > 0f ? 1 : -1;

                facing.Value = YawTurn.Towards(facing.Value, aim.Direction, config.TurnInPlaceSpeed * delta);
            }
        }
    }
}

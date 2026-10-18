using Game.Configs;
using Game.Core;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class FaceAimSystem : IEcsRunSystem
    {
        private const float MinAimDistance = 0.05f;

        private readonly EcsFilterInject<Inc<Player, InputState, Aim, Position, Facing>, Exc<Dead, Dashing, TorsoAim>> _heroes = default;

        private readonly EcsPoolInject<InputState> _states = default;
        private readonly EcsPoolInject<Aim> _aims = default;
        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<Facing> _facings = default;
        private readonly EcsPoolInject<DashRequest> _dashRequests = default;

        private readonly EcsCustomInject<InputConfig> _config = default;
        private readonly EcsCustomInject<SimulationClock> _clock = default;

        public void Run(IEcsSystems systems)
        {
            InputConfig config = _config.Value;

            if (config.DefaultScheme != ControlScheme.MouseHold)
                return;

            float delta = _clock.Value.Delta;

            foreach (int hero in _heroes.Value)
            {
                ref InputState state = ref _states.Value.Get(hero);

                Vector3 direction = state.AimPoint - _positions.Value.Get(hero).Value;
                direction.y = 0f;

                if (state.HasAim == false || state.StandHeld == false || direction.sqrMagnitude < MinAimDistance * MinAimDistance)
                {
                    state.AimTurning = false;
                    state.AimAngularSpeed = 0f;
                    state.LastAimDirection = Vector3.zero;
                    state.PredictiveLead = 0f;

                    continue;
                }

                direction.Normalize();

                float raw = state.LastAimDirection == Vector3.zero ? 0f : Vector3.SignedAngle(state.LastAimDirection, direction, Vector3.up) / delta;
                state.AimAngularSpeed = Mathf.Lerp(state.AimAngularSpeed, raw, config.AngularSmoothing);
                state.LastAimDirection = direction;

                float speed = _aims.Value.Get(hero).TurnSpeed * (_dashRequests.Value.Has(hero) ? config.DashTurnBoost : 1f);

                ref Facing facing = ref _facings.Value.Get(hero);
                facing.Value = Vector3.RotateTowards(facing.Value, direction, speed * Mathf.Deg2Rad * delta, 0f);

                state.AimTurning = Vector3.Angle(facing.Value, direction) > config.AimAlignDegrees;

                float turn = state.AimAngularSpeed;

                if (Mathf.Abs(turn) < config.PredictiveTurnSpeed)
                {
                    state.PredictiveLead = 0f;

                    continue;
                }

                state.PredictiveLead = Mathf.Clamp(turn * config.PredictiveLeadSeconds, -config.PredictiveMaxLeadDegrees, config.PredictiveMaxLeadDegrees);
                state.PredictiveStretch = 1f + Mathf.Min(config.PredictiveMaxReachBonus, Mathf.Abs(turn) * config.PredictiveReachPerTurnSpeed);
                state.PredictiveSwing = turn > 0f ? config.TurnRightSwing : config.TurnLeftSwing;
            }
        }
    }
}

using Game.Configs;
using Game.Core;
using Game.Simulation.Components;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using System;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class ReadInputStateSystem : IEcsRunSystem
    {
        private readonly EcsFilterInject<Inc<Player, MoveIntent, InputState, Aim, Position>, Exc<Dead>> _filter = default;

        private readonly EcsPoolInject<MoveIntent> _intents = default;
        private readonly EcsPoolInject<InputState> _states = default;
        private readonly EcsPoolInject<Aim> _aims = default;
        private readonly EcsPoolInject<Position> _positions = default;

        private readonly EcsCustomInject<IInputService> _input = default;
        private readonly EcsCustomInject<IPointerInput> _pointer = default;
        private readonly EcsCustomInject<IAimInput> _aimInput = default;
        private readonly EcsCustomInject<InputConfig> _inputConfig = default;
        private readonly EcsCustomInject<LevelConfig> _level = default;

        public void Run(IEcsSystems systems)
        {
            ControlScheme scheme = _inputConfig.Value.DefaultScheme;

            bool hasAim = _aimInput.Value.TryGetAimPoint(out Vector3 aimPoint);
            bool standHeld = _pointer.Value.IsStandHeld;
            bool moveHeld = _pointer.Value.IsMoveHeld;

            Vector2 axis = _input.Value.MoveAxis;
            Vector3 keyboardDirection = Quaternion.Euler(0f, _level.Value.CameraYaw, 0f) * new Vector3(axis.x, 0f, axis.y).normalized;

            foreach (int entity in _filter.Value)
            {
                ref InputState state = ref _states.Value.Get(entity);
                state.AimPoint = aimPoint;
                state.HasAim = hasAim;
                state.StandHeld = standHeld;
                state.MoveHeld = moveHeld;

                ref MoveIntent intent = ref _intents.Value.Get(entity);

                intent.Value = scheme switch
                {
                    ControlScheme.Keyboard => keyboardDirection,
                    ControlScheme.MouseRun => ResolveMouseRun(entity, in state),
                    ControlScheme.MouseHold => state.MoveHeld ? ResolveCursorDirection(entity, in state) : Vector3.zero,
                    _ => throw new InvalidOperationException($"Unknown control scheme {scheme}.")
                };
            }
        }

        private Vector3 ResolveMouseRun(int entity, in InputState state)
        {
            if (state.StandHeld)
                return Vector3.zero;

            return ResolveCursorDirection(entity, in state);
        }

        private Vector3 ResolveCursorDirection(int entity, in InputState state)
        {
            if (state.HasAim == false)
                return Vector3.zero;

            Vector3 offset = state.AimPoint - _positions.Value.Get(entity).Value;
            offset.y = 0f;

            float deadZone = _aims.Value.Get(entity).DeadZone;

            if (offset.sqrMagnitude <= deadZone * deadZone)
                return Vector3.zero;

            return offset.normalized;
        }
    }
}

using Game.Configs;
using Game.Core;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class SyncEnemyActionViewSystem : IEcsRunSystem
    {
        private readonly EcsFilterInject<Inc<EnemyAction, View>, Exc<Dead>> _filter = default;

        private readonly EcsPoolInject<EnemyAction> _actions = default;
        private readonly EcsPoolInject<EnemyCharge> _charges = default;
        private readonly EcsPoolInject<EnemySpin> _spins = default;
        private readonly EcsPoolInject<EnemyHeavyStrike> _heavyStrikes = default;
        private readonly EcsPoolInject<EnemyJump> _jumps = default;
        private readonly EcsPoolInject<BodyRadius> _bodyRadii = default;
        private readonly EcsPoolInject<View> _views = default;

        private readonly EcsCustomInject<SimulationClock> _clock = default;

        public void Run(IEcsSystems systems)
        {
            float delta = _clock.Value.Delta;

            foreach (int entity in _filter.Value)
            {
                ref View view = ref _views.Value.Get(entity);

                if (view.Value == null)
                    continue;

                ref EnemyAction action = ref _actions.Value.Get(entity);

                ActionCue cue = new ActionCue { Pose = ActionPose.None, PlaybackSpeed = 1f };

                if (EnemyActionPhases.IsCharge(action.Phase) && _charges.Value.Has(entity))
                    DescribeCharge(entity, ref action, delta, ref cue);
                else if (EnemyActionPhases.IsSpin(action.Phase) && _spins.Value.Has(entity))
                    DescribeSpin(entity, ref action, delta, ref cue);
                else if (EnemyActionPhases.IsHeavyStrike(action.Phase) && _heavyStrikes.Value.Has(entity))
                    DescribeHeavyStrike(entity, ref action, delta, ref cue);
                else if (EnemyActionPhases.IsJump(action.Phase) && _jumps.Value.Has(entity))
                    DescribeJump(entity, ref action, delta, ref cue);

                view.Value.SetAction(cue);

                if (_jumps.Value.Has(entity))
                    view.Value.SetLift(_jumps.Value.Get(entity).Lift);
            }
        }

        private void DescribeSpin(int entity, ref EnemyAction action, float delta, ref ActionCue cue)
        {
            ref EnemySpin spin = ref _spins.Value.Get(entity);

            cue.MarkerRadius = spin.Config.Radius;
            cue.SpinDegreesPerSecond = spin.Config.SpinAngularSpeedDegrees;
            cue.ArmWobbleDegrees = spin.Config.ArmWobbleDegrees;

            switch (action.Phase)
            {
                case EnemyActionPhase.SpinRaise:
                    cue.Pose = ActionPose.SpinRaise;
                    cue.Marker = GroundMarker.SpinWarning;
                    cue.PlaybackSpeed = spin.Config.RaiseClipSeconds / (action.PhaseTotalTicks * delta);
                    break;
                case EnemyActionPhase.Spinning:
                    cue.Pose = ActionPose.Spinning;
                    cue.Marker = GroundMarker.SpinActive;
                    cue.Pulse = spin.PulseTotalTicks > 0 ? 1f - (float)spin.PulseTicks / spin.PulseTotalTicks : 0f;
                    break;
                default:
                    cue.Pose = ActionPose.Recover;
                    break;
            }
        }

        private void DescribeJump(int entity, ref EnemyAction action, float delta, ref ActionCue cue)
        {
            JumpAbilityConfig config = _jumps.Value.Get(entity).Config;
            bool air = action.Phase == EnemyActionPhase.JumpAir;
            float clipSeconds = air ? config.ImpactClipSeconds : config.ClipSeconds - config.ImpactClipSeconds;

            cue.Pose = air ? ActionPose.JumpAir : ActionPose.JumpRecover;
            cue.PlaybackSpeed = clipSeconds / (Mathf.Max(1, action.PhaseTotalTicks) * delta);
        }

        private void DescribeHeavyStrike(int entity, ref EnemyAction action, float delta, ref ActionCue cue)
        {
            ref EnemyHeavyStrike strike = ref _heavyStrikes.Value.Get(entity);
            HeavyStrikeAbilityConfig config = strike.Config;

            float castSeconds = config.WindupSeconds + config.LockSeconds;
            float castSpeed = config.ImpactClipSeconds / castSeconds;

            cue.MarkerOffset = config.StrikeDistance;

            switch (action.Phase)
            {
                case EnemyActionPhase.StrikeWindup:
                    float progress = action.PhaseTotalTicks > 0 ? 1f - (float)action.PhaseTicks / action.PhaseTotalTicks : 1f;
                    cue.Pose = ActionPose.StrikeWindup;
                    cue.PlaybackSpeed = castSpeed;
                    cue.Marker = GroundMarker.StrikeAim;
                    cue.MarkerRadius = config.Radius * Mathf.Lerp(config.MarkerStartScale, 1f, progress);
                    cue.TurnDegreesPerSecond = strike.TurnDegreesPerSecond;
                    break;
                case EnemyActionPhase.StrikeLock:
                    cue.Pose = ActionPose.StrikeLock;
                    cue.PlaybackSpeed = castSpeed;
                    cue.Marker = GroundMarker.StrikeLocked;
                    cue.MarkerRadius = config.Radius;
                    break;
                default:
                    cue.Pose = ActionPose.StrikeRecover;
                    cue.PlaybackSpeed = (config.ClipSeconds - config.ImpactClipSeconds) / config.RecoverSeconds;
                    break;
            }
        }

        private void DescribeCharge(int entity, ref EnemyAction action, float delta, ref ActionCue cue)
        {
            ref EnemyCharge charge = ref _charges.Value.Get(entity);

            float radius = _bodyRadii.Value.Has(entity) ? _bodyRadii.Value.Get(entity).Value : 0f;

            cue.MarkerLength = charge.Config.TelegraphLength;
            cue.MarkerWidth = radius * 2f;

            switch (action.Phase)
            {
                case EnemyActionPhase.ChargeWindup:
                    cue.Pose = ActionPose.ChargeWindup;
                    cue.Marker = GroundMarker.ChargeAim;
                    break;
                case EnemyActionPhase.ChargeLock:
                    cue.Pose = ActionPose.ChargeWindup;
                    cue.Marker = GroundMarker.ChargeLocked;
                    break;
                case EnemyActionPhase.ChargeRush:
                    cue.Pose = ActionPose.ChargeRush;
                    cue.PlaybackSpeed = charge.Config.RushClipSeconds / (action.PhaseTotalTicks * delta);
                    break;
                default:
                    cue.Pose = ActionPose.Recover;
                    break;
            }
        }
    }
}

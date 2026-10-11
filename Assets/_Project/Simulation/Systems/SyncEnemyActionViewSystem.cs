using Game.Core;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

namespace Game.Simulation.Systems
{
    public sealed class SyncEnemyActionViewSystem : IEcsRunSystem
    {
        private readonly EcsFilterInject<Inc<EnemyAction, View>, Exc<Dead>> _filter = default;

        private readonly EcsPoolInject<EnemyAction> _actions = default;
        private readonly EcsPoolInject<EnemyCharge> _charges = default;
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

                view.Value.SetAction(cue);
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

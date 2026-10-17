using Game.Simulation.Components;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

namespace Game.Simulation.Systems
{
    public sealed class PlayDashFinisherFeedbackSystem : IEcsRunSystem
    {
        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<DashFinisher, DashFinisherStarted>> _started = default;

        private readonly EcsPoolInject<DashFinisher> _finishers = default;
        private readonly EcsPoolInject<View> _views = default;

        public void Run(IEcsSystems systems)
        {
            EcsWorld world = _world.Value;

            foreach (int entity in _started.Value)
            {
                ref DashFinisher finisher = ref _finishers.Value.Get(entity);

                if (finisher.Owner.Unpack(world, out int owner) == false || _views.Value.Has(owner) == false)
                    continue;

                ref View view = ref _views.Value.Get(owner);

                if (view.Value == null)
                    continue;

                view.Value.PlayAttack(finisher.Trigger, finisher.PlaybackSpeed);
            }
        }
    }
}

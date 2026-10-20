using Game.Simulation.Components;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

namespace Game.Simulation.Systems
{
    public sealed class PlayFlameSwingFeedbackSystem : IEcsRunSystem
    {
        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<FlameSwing, FlameSwingStarted>> _started = default;

        private readonly EcsPoolInject<FlameSwing> _swings = default;
        private readonly EcsPoolInject<View> _views = default;

        public void Run(IEcsSystems systems)
        {
            EcsWorld world = _world.Value;

            foreach (int entity in _started.Value)
            {
                ref FlameSwing swing = ref _swings.Value.Get(entity);

                if (swing.Owner.Unpack(world, out int owner) == false || _views.Value.Has(owner) == false)
                    continue;

                ref View view = ref _views.Value.Get(owner);

                if (view.Value == null)
                    continue;

                view.Value.PlayAttack(swing.Config.AnimatorTrigger, swing.Config.PlaybackSpeed);
            }
        }
    }
}

using Game.Simulation.Components;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

namespace Game.Simulation.Systems
{
    public sealed class PlayLeapFeedbackSystem : IEcsRunSystem
    {
        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<HeroicLeap, LeapStarted>> _started = default;

        private readonly EcsPoolInject<HeroicLeap> _leaps = default;
        private readonly EcsPoolInject<View> _views = default;

        public void Run(IEcsSystems systems)
        {
            EcsWorld world = _world.Value;

            foreach (int entity in _started.Value)
            {
                ref HeroicLeap leap = ref _leaps.Value.Get(entity);

                if (leap.Owner.Unpack(world, out int owner) == false || _views.Value.Has(owner) == false)
                    continue;

                ref View view = ref _views.Value.Get(owner);

                if (view.Value == null)
                    continue;

                view.Value.PlayAttack(leap.Config.AnimatorTrigger, leap.Config.StateSpeed);
            }
        }
    }
}

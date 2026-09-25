using Game.Simulation.Components;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

namespace Game.Simulation.Systems
{
    public sealed class PlayHitFeedbackSystem : IEcsRunSystem
    {
        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<DamageEvent, DamageApplied>> _events = default;

        private readonly EcsPoolInject<DamageEvent> _damageEvents = default;
        private readonly EcsPoolInject<View> _views = default;

        public void Run(IEcsSystems systems)
        {
            EcsWorld world = _world.Value;

            foreach (int entity in _events.Value)
            {
                ref DamageEvent damageEvent = ref _damageEvents.Value.Get(entity);

                if (damageEvent.Target.Unpack(world, out int target) == false)
                    continue;

                if (_views.Value.Has(target) == false)
                    continue;

                ref View view = ref _views.Value.Get(target);

                if (view.Value == null)
                    continue;

                view.Value.PlayHit();
            }
        }
    }
}

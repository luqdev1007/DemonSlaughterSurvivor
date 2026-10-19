using Game.Core;
using Game.Simulation.Components;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

namespace Game.Simulation.Systems
{
    public sealed class PlayShieldFeedbackSystem : IEcsRunSystem
    {
        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<DamageEvent, ShieldAbsorbed>> _absorbed = default;

        private readonly EcsPoolInject<DamageEvent> _damageEvents = default;
        private readonly EcsPoolInject<ShieldAbsorbed> _absorbedPool = default;
        private readonly EcsPoolInject<View> _views = default;

        public void Run(IEcsSystems systems)
        {
            EcsWorld world = _world.Value;

            foreach (int entity in _absorbed.Value)
            {
                if (_damageEvents.Value.Get(entity).Target.Unpack(world, out int target) == false || _views.Value.Has(target) == false)
                    continue;

                if (_views.Value.Get(target).Value is IShieldView view)
                    view.FlashShield(_absorbedPool.Value.Get(entity).Amount);
            }
        }
    }
}

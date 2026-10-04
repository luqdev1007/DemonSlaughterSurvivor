using Game.Simulation.Components;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

namespace Game.Simulation.Systems
{
    public sealed class ExpireSpecialAttackSystem : IEcsRunSystem
    {
        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<SpecialAttack>> _filter = default;

        private readonly EcsPoolInject<SpecialAttack> _specials = default;

        public void Run(IEcsSystems systems)
        {
            EcsWorld world = _world.Value;

            foreach (int entity in _filter.Value)
            {
                if (_specials.Value.Get(entity).Attack.Unpack(world, out _))
                    continue;

                _specials.Value.Del(entity);
            }
        }
    }
}

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
        private readonly EcsPoolInject<Dead> _dead = default;
        private readonly EcsPoolInject<Dashing> _dashing = default;

        public void Run(IEcsSystems systems)
        {
            EcsWorld world = _world.Value;

            foreach (int entity in _filter.Value)
            {
                if (_specials.Value.Get(entity).Attack.Unpack(world, out int attack))
                {
                    if (_dead.Value.Has(entity) == false && _dashing.Value.Has(entity) == false)
                        continue;

                    world.DelEntity(attack);
                }

                _specials.Value.Del(entity);
            }
        }
    }
}

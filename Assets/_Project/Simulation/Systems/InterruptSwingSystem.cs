using Game.Simulation.Components;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

namespace Game.Simulation.Systems
{
    public sealed class InterruptSwingSystem : IEcsRunSystem
    {
        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<Swing>> _swings = default;

        private readonly EcsPoolInject<Swing> _swingPool = default;
        private readonly EcsPoolInject<Swinging> _swinging = default;
        private readonly EcsPoolInject<Dead> _dead = default;
        private readonly EcsPoolInject<Dashing> _dashing = default;

        public void Run(IEcsSystems systems)
        {
            EcsWorld world = _world.Value;

            foreach (int entity in _swings.Value)
            {
                ref Swing swing = ref _swingPool.Value.Get(entity);

                bool ownerAlive = swing.Owner.Unpack(world, out int owner);

                if (ownerAlive && _dead.Value.Has(owner) == false && _dashing.Value.Has(owner) == false)
                    continue;

                if (swing.Weapon.Unpack(world, out int weapon) && _swinging.Value.Has(weapon))
                    _swinging.Value.Del(weapon);

                world.DelEntity(entity);
            }
        }
    }
}

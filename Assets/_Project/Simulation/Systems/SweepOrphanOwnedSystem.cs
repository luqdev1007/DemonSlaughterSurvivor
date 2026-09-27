using Game.Simulation.Components;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

namespace Game.Simulation.Systems
{
    public sealed class SweepOrphanOwnedSystem : IEcsRunSystem
    {
        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<OwnerLink>> _owned = default;

        private readonly EcsPoolInject<OwnerLink> _ownerLinks = default;

        public void Run(IEcsSystems systems)
        {
            EcsWorld world = _world.Value;

            foreach (int entity in _owned.Value)
            {
                ref OwnerLink link = ref _ownerLinks.Value.Get(entity);

                if (link.Owner.Unpack(world, out int _))
                    continue;

                world.DelEntity(entity);
            }
        }
    }
}

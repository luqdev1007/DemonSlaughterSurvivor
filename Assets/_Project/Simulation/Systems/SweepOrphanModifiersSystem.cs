using Game.Simulation.Components;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

namespace Game.Simulation.Systems
{
    public sealed class SweepOrphanModifiersSystem : IEcsRunSystem
    {
        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<StatModifier>> _modifiers = default;

        private readonly EcsPoolInject<StatModifier> _modifierPool = default;

        public void Run(IEcsSystems systems)
        {
            EcsWorld world = _world.Value;

            foreach (int entity in _modifiers.Value)
            {
                ref StatModifier modifier = ref _modifierPool.Value.Get(entity);

                if (modifier.Target.Unpack(world, out int _))
                    continue;

                world.DelEntity(entity);
            }
        }
    }
}

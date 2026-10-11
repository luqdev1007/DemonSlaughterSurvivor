using Game.Simulation.Components;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

namespace Game.Simulation.Systems
{
    public sealed class ApplyEnemyChargeVelocitySystem : IEcsRunSystem
    {
        private readonly EcsFilterInject<Inc<EnemyCharge, Rushing, Velocity>, Exc<Dead>> _filter = default;

        private readonly EcsPoolInject<EnemyCharge> _charges = default;
        private readonly EcsPoolInject<Velocity> _velocities = default;

        public void Run(IEcsSystems systems)
        {
            foreach (int entity in _filter.Value)
            {
                ref EnemyCharge charge = ref _charges.Value.Get(entity);

                _velocities.Value.Get(entity).Value = charge.Direction * charge.Config.RushSpeed;
            }
        }
    }
}

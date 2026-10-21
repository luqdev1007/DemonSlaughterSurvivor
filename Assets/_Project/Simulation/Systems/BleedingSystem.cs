using Game.Simulation.Components;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class BleedingSystem : IEcsRunSystem
    {
        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<Bleeding>> _bleeding = default;

        private readonly EcsPoolInject<Bleeding> _bleedingPool = default;
        private readonly EcsPoolInject<DamageEvent> _damageEvents = default;
        private readonly EcsPoolInject<Health> _healths = default;
        private readonly EcsPoolInject<Dead> _dead = default;
        private readonly EcsPoolInject<Position> _positions = default;

        public void Run(IEcsSystems systems)
        {
            EcsWorld world = _world.Value;

            foreach (int entity in _bleeding.Value)
            {
                ref Bleeding bleeding = ref _bleedingPool.Value.Get(entity);

                if (_dead.Value.Has(entity) || _healths.Value.Has(entity) == false)
                {
                    _bleedingPool.Value.Del(entity);

                    continue;
                }

                if (--bleeding.TickCountdown <= 0)
                {
                    bleeding.TickCountdown = Mathf.Max(1, bleeding.TickTicks);

                    ref DamageEvent damage = ref _damageEvents.Value.Add(world.NewEntity());
                    damage.Target = world.PackEntity(entity);
                    damage.Source = bleeding.Source;
                    damage.SourcePosition = _positions.Value.Has(entity) ? _positions.Value.Get(entity).Value : Vector3.zero;
                    damage.Amount = bleeding.TickDamage;
                    damage.Kind = DamageKind.Perk;
                    damage.SourcePerk = bleeding.SourcePerk;
                }

                if (--bleeding.RemainingTicks <= 0)
                    _bleedingPool.Value.Del(entity);
            }
        }
    }
}

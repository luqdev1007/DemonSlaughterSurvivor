using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

namespace Game.Simulation.Systems
{
    public sealed class SweepEnemyChargeSystem : IEcsRunSystem
    {
        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<EnemyCharge, Rushing, Position, PreviousPosition, BodyRadius>, Exc<Dead>> _chargers = default;
        private readonly EcsFilterInject<Inc<Player, Position, PreviousPosition, BodyRadius, Health>, Exc<Dead, Airborne>> _targets = default;

        private readonly EcsPoolInject<EnemyCharge> _charges = default;
        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<PreviousPosition> _previousPositions = default;
        private readonly EcsPoolInject<BodyRadius> _bodyRadii = default;
        private readonly EcsPoolInject<DamageEvent> _damageEvents = default;

        public void Run(IEcsSystems systems)
        {
            EcsWorld world = _world.Value;

            foreach (int charger in _chargers.Value)
            {
                ref EnemyCharge charge = ref _charges.Value.Get(charger);

                if (charge.HasHit)
                    continue;

                float reach = _bodyRadii.Value.Get(charger).Value + charge.Config.HitRadiusExtra;

                foreach (int target in _targets.Value)
                {
                    float contact = reach + _bodyRadii.Value.Get(target).Value;

                    bool hit = RangedTarget.SweptHit(
                        _previousPositions.Value.Get(charger).Value,
                        _positions.Value.Get(charger).Value,
                        _previousPositions.Value.Get(target).Value,
                        _positions.Value.Get(target).Value,
                        contact);

                    if (hit == false)
                        continue;

                    int entity = world.NewEntity();

                    ref DamageEvent damageEvent = ref _damageEvents.Value.Add(entity);
                    damageEvent.Target = world.PackEntity(target);
                    damageEvent.Source = world.PackEntity(charger);
                    damageEvent.SourcePosition = _positions.Value.Get(charger).Value;
                    damageEvent.Amount = charge.Config.Damage;
                    damageEvent.Kind = DamageKind.EnemyAbility;

                    charge.HasHit = true;

                    break;
                }
            }
        }
    }
}

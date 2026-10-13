using Game.Configs;
using Game.Simulation.Components;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class ImpactEnemyHeavyStrikeSystem : IEcsRunSystem
    {
        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<EnemyHeavyStrike, Position>, Exc<Dead>> _strikers = default;
        private readonly EcsFilterInject<Inc<Player, Position, BodyRadius, Health>, Exc<Dead>> _targets = default;

        private readonly EcsPoolInject<EnemyHeavyStrike> _strikes = default;
        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<BodyRadius> _bodyRadii = default;
        private readonly EcsPoolInject<DamageEvent> _damageEvents = default;

        public void Run(IEcsSystems systems)
        {
            EcsWorld world = _world.Value;

            foreach (int striker in _strikers.Value)
            {
                ref EnemyHeavyStrike strike = ref _strikes.Value.Get(striker);

                if (strike.ImpactPending == false)
                    continue;

                strike.ImpactPending = false;

                Vector3 center = ResolveImpactPoint(_positions.Value.Get(striker).Value, strike.Direction, strike.Config);

                Hit(world, striker, center, strike.Config);
            }
        }

        public static Vector3 ResolveImpactPoint(Vector3 position, Vector3 direction, HeavyStrikeAbilityConfig config)
        {
            Vector3 center = position + direction * config.StrikeDistance;
            center.y = 0f;

            return center;
        }

        private void Hit(EcsWorld world, int striker, Vector3 center, HeavyStrikeAbilityConfig config)
        {
            foreach (int target in _targets.Value)
            {
                Vector3 position = _positions.Value.Get(target).Value;

                float deltaX = position.x - center.x;
                float deltaZ = position.z - center.z;
                float reach = config.Radius + _bodyRadii.Value.Get(target).Value;

                if (deltaX * deltaX + deltaZ * deltaZ > reach * reach)
                    continue;

                int entity = world.NewEntity();

                ref DamageEvent damageEvent = ref _damageEvents.Value.Add(entity);
                damageEvent.Target = world.PackEntity(target);
                damageEvent.Source = world.PackEntity(striker);
                damageEvent.SourcePosition = center;
                damageEvent.Amount = config.Damage;
                damageEvent.Kind = DamageKind.EnemyAbility;
            }
        }
    }
}

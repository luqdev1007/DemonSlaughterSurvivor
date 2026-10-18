using Game.Configs;
using Game.Simulation.Components;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class PulseEnemySpinSystem : IEcsRunSystem
    {
        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<EnemyAction, EnemySpin, Position>, Exc<Dead>> _spinners = default;
        private readonly EcsFilterInject<Inc<Player, Position, BodyRadius, Health>, Exc<Dead, Airborne>> _targets = default;

        private readonly EcsPoolInject<EnemyAction> _actions = default;
        private readonly EcsPoolInject<EnemySpin> _spins = default;
        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<BodyRadius> _bodyRadii = default;
        private readonly EcsPoolInject<DamageEvent> _damageEvents = default;

        public void Run(IEcsSystems systems)
        {
            EcsWorld world = _world.Value;

            foreach (int spinner in _spinners.Value)
            {
                if (_actions.Value.Get(spinner).Phase != EnemyActionPhase.Spinning)
                    continue;

                ref EnemySpin spin = ref _spins.Value.Get(spinner);

                if (spin.PulseTicks <= 0)
                {
                    Pulse(world, spinner, spin.Config);

                    spin.PulseTicks = spin.PulseTotalTicks;
                }

                spin.PulseTicks--;
            }
        }

        private void Pulse(EcsWorld world, int spinner, SpinAbilityConfig config)
        {
            Vector3 center = _positions.Value.Get(spinner).Value;

            foreach (int target in _targets.Value)
            {
                Vector3 position = _positions.Value.Get(target).Value;

                float deltaX = position.x - center.x;
                float deltaZ = position.z - center.z;
                float distance = Mathf.Sqrt(deltaX * deltaX + deltaZ * deltaZ);

                if (distance > config.Radius + _bodyRadii.Value.Get(target).Value)
                    continue;

                int entity = world.NewEntity();

                ref DamageEvent damageEvent = ref _damageEvents.Value.Add(entity);
                damageEvent.Target = world.PackEntity(target);
                damageEvent.Source = world.PackEntity(spinner);
                damageEvent.SourcePosition = center;
                damageEvent.Amount = config.DamageAt(distance);
                damageEvent.Kind = DamageKind.EnemyAbility;
            }
        }
    }
}

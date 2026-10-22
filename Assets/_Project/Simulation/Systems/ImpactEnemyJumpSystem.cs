using Game.Configs;
using Game.Core;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class ImpactEnemyJumpSystem : IEcsRunSystem
    {
        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<EnemyJump, Position>, Exc<Dead>> _jumpers = default;
        private readonly EcsFilterInject<Inc<Player, Position, BodyRadius, Health>, Exc<Dead, Airborne>> _targets = default;

        private readonly EcsPoolInject<EnemyJump> _jumps = default;
        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<BodyRadius> _bodyRadii = default;
        private readonly EcsPoolInject<DamageEvent> _damageEvents = default;
        private readonly EcsPoolInject<EnemySlamEvent> _slams = default;
        private readonly EcsPoolInject<LeapImpactFx> _impactFx = default;
        private readonly EcsPoolInject<NotIndexed> _notIndexed = default;
        private readonly EcsPoolInject<View> _views = default;

        private readonly EcsCustomInject<IViewFactory> _viewFactory = default;
        private readonly EcsCustomInject<SimulationClock> _clock = default;

        public void Run(IEcsSystems systems)
        {
            EcsWorld world = _world.Value;

            foreach (int jumper in _jumpers.Value)
            {
                ref EnemyJump jump = ref _jumps.Value.Get(jumper);

                if (jump.ImpactPending == false)
                    continue;

                jump.ImpactPending = false;

                Vector3 center = _positions.Value.Get(jumper).Value;
                center.y = 0f;

                Hit(world, jumper, center, jump.Config);
                Slam(world, center, jump.Config);
            }
        }

        private void Hit(EcsWorld world, int jumper, Vector3 center, JumpAbilityConfig config)
        {
            foreach (int target in _targets.Value)
            {
                Vector3 position = _positions.Value.Get(target).Value;

                float deltaX = position.x - center.x;
                float deltaZ = position.z - center.z;
                float reach = config.Radius + _bodyRadii.Value.Get(target).Value;

                if (deltaX * deltaX + deltaZ * deltaZ > reach * reach)
                    continue;

                ref DamageEvent damageEvent = ref _damageEvents.Value.Add(world.NewEntity());
                damageEvent.Target = world.PackEntity(target);
                damageEvent.Source = world.PackEntity(jumper);
                damageEvent.SourcePosition = center;
                damageEvent.Amount = config.Damage;
                damageEvent.Kind = DamageKind.EnemyAbility;
            }
        }

        private void Slam(EcsWorld world, Vector3 center, JumpAbilityConfig config)
        {
            _slams.Value.Add(world.NewEntity()).Position = center;

            if (config.ImpactFxPrefab == null)
                return;

            int fx = world.NewEntity();

            _impactFx.Value.Add(fx).RemainingTicks = Mathf.Max(1, Mathf.RoundToInt(config.ImpactFxSeconds / _clock.Value.Delta));
            _positions.Value.Add(fx).Value = center;
            _notIndexed.Value.Add(fx);
            _views.Value.Add(fx).Value = AreaScale.Apply(_viewFactory.Value.Create(config.ImpactFxPrefab, center), config.ImpactFxScale);
        }
    }
}

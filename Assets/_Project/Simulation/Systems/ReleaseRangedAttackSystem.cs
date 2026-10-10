using Game.Configs;
using Game.Core;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class ReleaseRangedAttackSystem : IEcsRunSystem
    {
        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<RangedAttack, ChaseTarget, Position, Facing>, Exc<Dead>> _filter = default;

        private readonly EcsPoolInject<RangedAttack> _attacks = default;
        private readonly EcsPoolInject<ChaseTarget> _chaseTargets = default;
        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<Facing> _facings = default;
        private readonly EcsPoolInject<BodyRadius> _bodyRadii = default;
        private readonly EcsPoolInject<Velocity> _velocities = default;
        private readonly EcsPoolInject<Projectile> _projectiles = default;
        private readonly EcsPoolInject<View> _views = default;
        private readonly EcsPoolInject<NotIndexed> _notIndexed = default;

        private readonly EcsCustomInject<IViewFactory> _viewFactory = default;
        private readonly EcsCustomInject<SimulationClock> _clock = default;

        public void Run(IEcsSystems systems)
        {
            EcsWorld world = _world.Value;
            float delta = _clock.Value.Delta;

            foreach (int entity in _filter.Value)
            {
                ref RangedAttack attack = ref _attacks.Value.Get(entity);

                if (attack.CooldownTicks > 0)
                    attack.CooldownTicks--;

                if (attack.WindupTicks <= 0)
                    continue;

                attack.WindupTicks--;

                if (attack.WindupTicks > 0)
                    continue;

                RangedAttackConfig config = attack.Config;

                attack.CooldownTicks = Mathf.Max(0, Mathf.RoundToInt(config.CooldownSeconds / delta));

                Shoot(world, entity, config, delta);
            }
        }

        private void Shoot(EcsWorld world, int archer, RangedAttackConfig config, float delta)
        {
            Vector3 origin = _positions.Value.Get(archer).Value;
            Vector3 direction = ResolveDirection(world, archer, origin);

            float muzzle = _bodyRadii.Value.Has(archer) ? _bodyRadii.Value.Get(archer).Value : 0f;
            Vector3 spawn = origin + direction * muzzle;

            int bolt = world.NewEntity();

            ref Projectile projectile = ref _projectiles.Value.Add(bolt);
            projectile.Source = world.PackEntity(archer);
            projectile.LastPosition = spawn;
            projectile.Damage = config.BoltDamage;
            projectile.Radius = config.BoltRadius;
            projectile.RemainingTicks = Mathf.Max(1, Mathf.RoundToInt(config.BoltRange / (config.BoltSpeed * delta)));

            _positions.Value.Add(bolt).Value = spawn;
            _velocities.Value.Add(bolt).Value = direction * config.BoltSpeed;
            _facings.Value.Add(bolt).Value = direction;
            _notIndexed.Value.Add(bolt);
            _views.Value.Add(bolt).Value = _viewFactory.Value.Create(config.BoltPrefab, spawn);
        }

        private Vector3 ResolveDirection(EcsWorld world, int archer, Vector3 origin)
        {
            ChaseTarget chaseTarget = _chaseTargets.Value.Get(archer);

            if (chaseTarget.Value.Unpack(world, out int target) && _positions.Value.Has(target))
            {
                Vector3 toTarget = _positions.Value.Get(target).Value - origin;
                toTarget.y = 0f;

                if (toTarget.sqrMagnitude > Mathf.Epsilon)
                    return toTarget.normalized;
            }

            Vector3 facing = _facings.Value.Get(archer).Value;
            facing.y = 0f;

            return facing.sqrMagnitude > Mathf.Epsilon ? facing.normalized : Vector3.forward;
        }
    }
}

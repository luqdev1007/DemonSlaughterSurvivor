using Game.Core;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class AdvanceProjectilesSystem : IEcsRunSystem
    {
        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<Projectile, Position>> _projectiles = default;
        private readonly EcsFilterInject<Inc<Player, Position, PreviousPosition, BodyRadius, Health>, Exc<Dead>> _targets = default;

        private readonly EcsPoolInject<Projectile> _projectilePool = default;
        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<PreviousPosition> _previousPositions = default;
        private readonly EcsPoolInject<BodyRadius> _bodyRadii = default;
        private readonly EcsPoolInject<DamageEvent> _damageEvents = default;
        private readonly EcsPoolInject<View> _views = default;

        private readonly EcsCustomInject<IViewFactory> _viewFactory = default;

        public void Run(IEcsSystems systems)
        {
            EcsWorld world = _world.Value;

            foreach (int entity in _projectiles.Value)
            {
                ref Projectile projectile = ref _projectilePool.Value.Get(entity);

                Vector3 from = projectile.LastPosition;
                Vector3 to = _positions.Value.Get(entity).Value;

                if (TryHit(world, ref projectile, from, to))
                {
                    Remove(world, entity);
                    continue;
                }

                if (projectile.RemainingTicks <= 0)
                {
                    Remove(world, entity);
                    continue;
                }

                projectile.RemainingTicks--;
                projectile.LastPosition = to;
            }
        }

        private bool TryHit(EcsWorld world, ref Projectile projectile, Vector3 from, Vector3 to)
        {
            foreach (int target in _targets.Value)
            {
                float contact = projectile.Radius + _bodyRadii.Value.Get(target).Value;

                Vector3 bodyFrom = _previousPositions.Value.Get(target).Value;
                Vector3 bodyTo = _positions.Value.Get(target).Value;

                if (RangedTarget.SweptHit(from, to, bodyFrom, bodyTo, contact) == false)
                    continue;

                int entity = world.NewEntity();

                ref DamageEvent damageEvent = ref _damageEvents.Value.Add(entity);
                damageEvent.Target = world.PackEntity(target);
                damageEvent.Source = projectile.Source;
                damageEvent.SourcePosition = to;
                damageEvent.Amount = projectile.Damage;
                damageEvent.Kind = DamageKind.Projectile;

                return true;
            }

            return false;
        }

        private void Remove(EcsWorld world, int entity)
        {
            if (_views.Value.Has(entity))
            {
                ref View view = ref _views.Value.Get(entity);

                if (view.Value != null)
                    _viewFactory.Value.Release(view.Value);

                view.Value = null;
            }

            world.DelEntity(entity);
        }
    }
}

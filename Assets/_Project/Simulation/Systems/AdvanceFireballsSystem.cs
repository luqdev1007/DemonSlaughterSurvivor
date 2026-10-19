using Game.Configs;
using Game.Core;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class AdvanceFireballsSystem : IEcsInitSystem, IEcsRunSystem
    {
        private const int CandidateCapacityReserve = 16;
        private const float FullCircleDegrees = 180f;

        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<Projectile, Fireball, Position>> _fireballs = default;

        private readonly EcsPoolInject<Projectile> _projectiles = default;
        private readonly EcsPoolInject<Fireball> _fireballData = default;
        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<PreviousPosition> _previousPositions = default;
        private readonly EcsPoolInject<BodyRadius> _bodyRadii = default;
        private readonly EcsPoolInject<Health> _healths = default;
        private readonly EcsPoolInject<Enemy> _enemies = default;
        private readonly EcsPoolInject<Breakable> _breakables = default;
        private readonly EcsPoolInject<Dead> _dead = default;
        private readonly EcsPoolInject<DamageEvent> _damageEvents = default;
        private readonly EcsPoolInject<AreaStrike> _strikes = default;
        private readonly EcsPoolInject<LeapImpactFx> _impacts = default;
        private readonly EcsPoolInject<NotIndexed> _notIndexed = default;
        private readonly EcsPoolInject<View> _views = default;

        private readonly EcsCustomInject<SpatialGrid> _grid = default;
        private readonly EcsCustomInject<SimulationClock> _clock = default;
        private readonly EcsCustomInject<EnemyMotionBounds> _motionBounds = default;
        private readonly EcsCustomInject<LevelConfig> _level = default;
        private readonly EcsCustomInject<IViewFactory> _viewFactory = default;

        private List<int> _candidates;

        public void Init(IEcsSystems systems)
        {
            _candidates = new List<int>(WaveTimelineValidator.ResolveMaxLiveCap(_level.Value.Waves) + CandidateCapacityReserve);
        }

        public void Run(IEcsSystems systems)
        {
            EcsWorld world = _world.Value;

            foreach (int fireball in _fireballs.Value)
            {
                ref Projectile projectile = ref _projectiles.Value.Get(fireball);
                StaffConfig config = _fireballData.Value.Get(fireball).Config;

                Vector3 from = projectile.LastPosition;
                Vector3 to = _positions.Value.Get(fireball).Value;

                if (TryHit(in projectile, from, to, out int target, out float time))
                {
                    Vector3 impact = Vector3.Lerp(from, to, time);

                    ref DamageEvent damageEvent = ref _damageEvents.Value.Add(world.NewEntity());
                    damageEvent.Target = world.PackEntity(target);
                    damageEvent.Source = projectile.Source;
                    damageEvent.SourcePosition = impact;
                    damageEvent.Amount = projectile.Damage;
                    damageEvent.Kind = DamageKind.Weapon;
                    damageEvent.IsMagic = true;

                    Explode(world, in projectile, config, impact);
                    Remove(world, fireball);

                    continue;
                }

                if (projectile.RemainingTicks <= 0)
                {
                    Explode(world, in projectile, config, to);
                    Remove(world, fireball);

                    continue;
                }

                projectile.RemainingTicks--;
                projectile.LastPosition = to;
            }
        }

        private void Explode(EcsWorld world, in Projectile projectile, StaffConfig config, Vector3 at)
        {
            ref AreaStrike strike = ref _strikes.Value.Add(world.NewEntity());
            strike.Owner = projectile.Source;
            strike.Origin = at;
            strike.Forward = Vector3.forward;
            strike.Reach = config.ExplosionRadius;
            strike.HalfAngleDegrees = FullCircleDegrees;
            strike.Damage = projectile.Damage * config.ExplosionDamageScale;
            strike.Kind = DamageKind.Weapon;
            strike.IsMagic = true;
            strike.PushSpeed = config.ExplosionPushSpeed;
            strike.PushSeconds = config.ExplosionPushSeconds;
            strike.PushArcHeight = config.ExplosionPushArcHeight;

            int fx = world.NewEntity();
            _impacts.Value.Add(fx).RemainingTicks = Mathf.Max(1, Mathf.RoundToInt(config.ExplosionFxSeconds / _clock.Value.Delta));
            _positions.Value.Add(fx).Value = at;
            _notIndexed.Value.Add(fx);
            _views.Value.Add(fx).Value = _viewFactory.Value.Create(config.ExplosionPrefab, at);
        }

        private bool TryHit(in Projectile projectile, Vector3 from, Vector3 to, out int hit, out float earliest)
        {
            hit = -1;
            earliest = float.MaxValue;

            Vector3 middle = (from + to) * 0.5f;
            float radius = Vector3.Distance(from, to) * 0.5f + projectile.Radius + _motionBounds.Value.MaxEnemyBodyRadius;

            _grid.Value.Query(middle, radius, _motionBounds.Value.CandidateSlack(_clock.Value.Delta), _candidates);

            for (int index = 0; index < _candidates.Count; index++)
            {
                int candidate = _candidates[index];

                if (IsTarget(candidate) == false)
                    continue;

                Vector3 bodyTo = _positions.Value.Get(candidate).Value;
                Vector3 bodyFrom = _previousPositions.Value.Has(candidate) ? _previousPositions.Value.Get(candidate).Value : bodyTo;
                float contact = projectile.Radius + _bodyRadii.Value.Get(candidate).Value;

                if (RangedTarget.SweptHit(from, to, bodyFrom, bodyTo, contact, out float time) == false || time >= earliest)
                    continue;

                earliest = time;
                hit = candidate;
            }

            return hit >= 0;
        }

        private bool IsTarget(int candidate)
        {
            if ((_enemies.Value.Has(candidate) == false && _breakables.Value.Has(candidate) == false) || _dead.Value.Has(candidate))
                return false;

            return _healths.Value.Has(candidate) && _bodyRadii.Value.Has(candidate);
        }

        private void Remove(EcsWorld world, int fireball)
        {
            if (_views.Value.Has(fireball))
            {
                ref View view = ref _views.Value.Get(fireball);

                if (view.Value != null)
                    _viewFactory.Value.Release(view.Value);

                view.Value = null;
            }

            world.DelEntity(fireball);
        }
    }
}

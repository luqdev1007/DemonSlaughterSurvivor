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

        public const string BurstStreamId = "stream.fire-burst";

        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<Projectile, Fireball, Position>> _fireballs = default;
        private readonly EcsFilterInject<Inc<TakenPerk>> _taken = default;

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
        private readonly EcsPoolInject<TakenPerk> _takenPool = default;
        private readonly EcsPoolInject<FireBurstRandom> _burstRandoms = default;
        private readonly EcsPoolInject<Velocity> _velocities = default;
        private readonly EcsPoolInject<Facing> _facings = default;

        private readonly EcsCustomInject<SpatialGrid> _grid = default;
        private readonly EcsCustomInject<SimulationClock> _clock = default;
        private readonly EcsCustomInject<EnemyMotionBounds> _motionBounds = default;
        private readonly EcsCustomInject<LevelConfig> _level = default;
        private readonly EcsCustomInject<IViewFactory> _viewFactory = default;
        private readonly EcsCustomInject<IContentRegistry> _content = default;
        private readonly EcsCustomInject<RunContext> _context = default;

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
                Fireball data = _fireballData.Value.Get(fireball);

                Vector3 from = projectile.LastPosition;
                Vector3 to = _positions.Value.Get(fireball).Value;

                if (TryHit(world, in projectile, in data, from, to, out int target, out float time))
                {
                    Vector3 impact = Vector3.Lerp(from, to, time);

                    ref DamageEvent damageEvent = ref _damageEvents.Value.Add(world.NewEntity());
                    damageEvent.Target = world.PackEntity(target);
                    damageEvent.Source = projectile.Source;
                    damageEvent.SourcePosition = impact;
                    damageEvent.Amount = projectile.Damage;
                    damageEvent.Kind = DamageKind.Weapon;
                    damageEvent.IsMagic = true;

                    Explode(world, in projectile, in data, impact, target);
                    Remove(world, fireball);

                    continue;
                }

                if (projectile.RemainingTicks <= 0)
                {
                    Explode(world, in projectile, in data, to, -1);
                    Remove(world, fireball);

                    continue;
                }

                projectile.RemainingTicks--;
                projectile.LastPosition = to;
            }
        }

        private void Explode(EcsWorld world, in Projectile projectile, in Fireball data, Vector3 at, int hit)
        {
            StaffConfig config = data.Config;

            ref AreaStrike strike = ref _strikes.Value.Add(world.NewEntity());
            strike.Owner = projectile.Source;
            strike.Origin = at;
            strike.Forward = Vector3.forward;
            strike.Reach = config.ExplosionRadius * data.BlastScale;
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

            if (data.Child == false)
                TryBurst(world, in projectile, in data, at, hit);
        }

        private void TryBurst(EcsWorld world, in Projectile projectile, in Fireball data, Vector3 at, int hit)
        {
            if (projectile.Source.Unpack(world, out int owner) == false)
                return;

            if (TakenBehaviourLookup.TryFind(world, _taken.Value, _takenPool.Value, _content.Value, owner, out FireBurstConfig burst, out int level) == false)
                return;

            if (_burstRandoms.Value.Has(owner) == false)
                _burstRandoms.Value.Add(owner).State = SimulationRandom.StreamState(_context.Value.Seed, BurstStreamId);

            ref uint state = ref _burstRandoms.Value.Get(owner).State;
            FireBurstLevel entry = burst.Level(level);

            if (SimulationRandom.NextUnit(ref state) >= entry.Chance)
                return;

            float delta = _clock.Value.Delta;
            float offset = SimulationRandom.NextUnit(ref state) * 360f;
            int ticks = Mathf.Max(1, Mathf.RoundToInt(burst.Range / (burst.Speed * delta)));

            for (int index = 0; index < entry.Count; index++)
            {
                Vector3 direction = Quaternion.AngleAxis(offset + 360f * index / entry.Count, Vector3.up) * Vector3.forward;
                Vector3 spawn = at + direction * burst.SpawnOffset;
                spawn.y = 0f;

                int child = world.NewEntity();

                ref Projectile childProjectile = ref _projectiles.Value.Add(child);
                childProjectile.Source = projectile.Source;
                childProjectile.LastPosition = spawn;
                childProjectile.Damage = projectile.Damage * burst.DamageScale;
                childProjectile.Radius = projectile.Radius;
                childProjectile.RemainingTicks = ticks;

                ref Fireball childData = ref _fireballData.Value.Add(child);
                childData.Config = data.Config;
                childData.BlastScale = burst.BlastScale;
                childData.Lift = data.Lift;
                childData.Child = true;
                childData.HasIgnore = hit >= 0;
                childData.Ignore = hit >= 0 ? world.PackEntity(hit) : default;

                _positions.Value.Add(child).Value = spawn;
                _velocities.Value.Add(child).Value = direction * burst.Speed;
                _facings.Value.Add(child).Value = direction;
                _notIndexed.Value.Add(child);

                IView view = _viewFactory.Value.Create(burst.FireballPrefab, spawn);
                _views.Value.Add(child).Value = view;

                if (view == null)
                    continue;

                view.SetLift(data.Lift);
                view.SetLift(data.Lift);
            }
        }

        private bool TryHit(EcsWorld world, in Projectile projectile, in Fireball data, Vector3 from, Vector3 to, out int hit, out float earliest)
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

                if (data.HasIgnore && data.Ignore.Unpack(world, out int ignored) && ignored == candidate)
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

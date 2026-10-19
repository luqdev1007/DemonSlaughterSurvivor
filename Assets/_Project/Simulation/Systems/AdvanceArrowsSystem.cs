using Game.Configs;
using Game.Core;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class AdvanceArrowsSystem : IEcsInitSystem, IEcsRunSystem
    {
        private const int CandidateCapacityReserve = 16;
        private const float FullCircleDegrees = 180f;

        public const string ExplosionStreamId = "stream.explosive-arrow";

        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<Projectile, Arrow, Position>> _arrows = default;
        private readonly EcsFilterInject<Inc<TakenPerk>> _taken = default;

        private readonly EcsPoolInject<Projectile> _projectiles = default;
        private readonly EcsPoolInject<Arrow> _arrowData = default;
        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<PreviousPosition> _previousPositions = default;
        private readonly EcsPoolInject<BodyRadius> _bodyRadii = default;
        private readonly EcsPoolInject<Health> _healths = default;
        private readonly EcsPoolInject<Enemy> _enemies = default;
        private readonly EcsPoolInject<Breakable> _breakables = default;
        private readonly EcsPoolInject<Dead> _dead = default;
        private readonly EcsPoolInject<DamageEvent> _damageEvents = default;
        private readonly EcsPoolInject<View> _views = default;
        private readonly EcsPoolInject<TakenPerk> _takenPool = default;
        private readonly EcsPoolInject<ExplosiveArrowRandom> _explosionRandoms = default;
        private readonly EcsPoolInject<Duality> _dualities = default;
        private readonly EcsPoolInject<AreaStrike> _strikes = default;
        private readonly EcsPoolInject<LeapImpactFx> _impacts = default;
        private readonly EcsPoolInject<NotIndexed> _notIndexed = default;
        private readonly EcsPoolInject<LightningChain> _chains = default;

        private readonly EcsCustomInject<IContentRegistry> _content = default;
        private readonly EcsCustomInject<RunContext> _context = default;

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

            foreach (int arrow in _arrows.Value)
            {
                ref Projectile projectile = ref _projectiles.Value.Get(arrow);
                ref Arrow data = ref _arrowData.Value.Get(arrow);

                Vector3 from = projectile.LastPosition;
                Vector3 to = _positions.Value.Get(arrow).Value;

                if (data.Piercing)
                {
                    Pierce(world, arrow, in projectile, ref data, from, to);
                }
                else if (TryHit(world, in projectile, from, to, out int target))
                {
                    Damage(world, target, in projectile, in data, to);
                    Remove(world, arrow);

                    continue;
                }

                if (projectile.RemainingTicks <= 0)
                {
                    Remove(world, arrow);

                    continue;
                }

                projectile.RemainingTicks--;
                projectile.LastPosition = to;
            }
        }

        private void Pierce(EcsWorld world, int arrow, in Projectile projectile, ref Arrow data, Vector3 from, Vector3 to)
        {
            Query(in projectile, from, to);

            for (int index = 0; index < _candidates.Count; index++)
            {
                int candidate = _candidates[index];

                if (IsTarget(candidate) == false || WasHit(world, in data, candidate))
                    continue;

                if (Sweep(in projectile, from, to, candidate, out _) == false)
                    continue;

                if (data.HitCount == 0 && _views.Value.Has(arrow) && _views.Value.Get(arrow).Value is IArrowView view)
                    view.MarkPierced();

                if (data.HitCount == data.Hits.Length)
                    Array.Resize(ref data.Hits, data.Hits.Length * 2);

                data.Hits[data.HitCount] = world.PackEntity(candidate);
                data.HitCount++;

                Damage(world, candidate, in projectile, in data, to);
            }
        }

        private static bool WasHit(EcsWorld world, in Arrow data, int candidate)
        {
            for (int index = 0; index < data.HitCount; index++)
            {
                if (data.Hits[index].Unpack(world, out int hit) && hit == candidate)
                    return true;
            }

            return false;
        }

        private void Damage(EcsWorld world, int target, in Projectile projectile, in Arrow data, Vector3 at)
        {
            ref DamageEvent damageEvent = ref _damageEvents.Value.Add(world.NewEntity());
            damageEvent.Target = world.PackEntity(target);
            damageEvent.Source = projectile.Source;
            damageEvent.SourcePosition = at;
            damageEvent.Amount = projectile.Damage;
            damageEvent.Kind = DamageKind.Weapon;
            damageEvent.CritChanceBonus = data.CritChanceBonus;

            TryExplode(world, in projectile, at);

            if (data.Lightning && data.HitCount <= 1)
                StartChain(world, target, in projectile);
        }

        private void StartChain(EcsWorld world, int target, in Projectile projectile)
        {
            if (projectile.Source.Unpack(world, out int owner) == false)
                return;

            if (TakenBehaviourLookup.TryFind(world, _taken.Value, _takenPool.Value, _content.Value, owner, out LightningArrowConfig config, out int level) == false)
                return;

            LightningArrowLevel entry = config.Level(level);

            ref LightningChain chain = ref _chains.Value.Add(world.NewEntity());
            chain.Config = config;
            chain.Owner = projectile.Source;
            chain.From = _positions.Value.Get(target).Value;
            chain.Hits = new EcsPackedEntity[entry.Jumps + 1];
            chain.Hits[0] = world.PackEntity(target);
            chain.HitCount = 1;
            chain.JumpsLeft = entry.Jumps;
            chain.Damage = projectile.Damage * entry.DamageScale;
        }

        private void TryExplode(EcsWorld world, in Projectile projectile, Vector3 at)
        {
            if (projectile.Source.Unpack(world, out int owner) == false)
                return;

            if (TakenBehaviourLookup.TryFind(world, _taken.Value, _takenPool.Value, _content.Value, owner, out ExplosiveArrowConfig config, out int level) == false)
                return;

            if (_explosionRandoms.Value.Has(owner) == false)
                _explosionRandoms.Value.Add(owner).State = SimulationRandom.StreamState(_context.Value.Seed, ExplosionStreamId);

            ExplosiveArrowLevel entry = config.Level(level);
            bool demonic = DualityForm.IsDemonic(_dualities.Value, owner);
            float chance = demonic ? Mathf.Min(1f, entry.Chance * config.DemonicChanceScale) : entry.Chance;

            if (SimulationRandom.NextUnit(ref _explosionRandoms.Value.Get(owner).State) >= chance)
                return;

            ref AreaStrike strike = ref _strikes.Value.Add(world.NewEntity());
            strike.Owner = projectile.Source;
            strike.Origin = at;
            strike.Forward = Vector3.forward;
            strike.Reach = config.Radius * (demonic ? config.DemonicRadiusScale : 1f);
            strike.HalfAngleDegrees = FullCircleDegrees;
            strike.Damage = projectile.Damage * entry.DamageScale * (demonic ? config.DemonicDamageScale : 1f);
            strike.Kind = DamageKind.Perk;
            strike.SourcePerk = config;
            strike.PushSpeed = config.PushSpeed;
            strike.PushSeconds = config.PushSeconds;
            strike.PushArcHeight = config.PushArcHeight;

            int fx = world.NewEntity();
            _impacts.Value.Add(fx).RemainingTicks = Mathf.Max(1, Mathf.RoundToInt(config.FxSeconds / _clock.Value.Delta));
            _positions.Value.Add(fx).Value = at;
            _notIndexed.Value.Add(fx);
            _views.Value.Add(fx).Value = _viewFactory.Value.Create(config.FxPrefab, at);
        }

        private void Query(in Projectile projectile, Vector3 from, Vector3 to)
        {
            Vector3 middle = (from + to) * 0.5f;
            float halfLength = Vector3.Distance(from, to) * 0.5f;
            float radius = halfLength + projectile.Radius + _motionBounds.Value.MaxEnemyBodyRadius;

            _grid.Value.Query(middle, radius, _motionBounds.Value.CandidateSlack(_clock.Value.Delta), _candidates);
        }

        private bool IsTarget(int candidate)
        {
            if ((_enemies.Value.Has(candidate) == false && _breakables.Value.Has(candidate) == false) || _dead.Value.Has(candidate))
                return false;

            return _healths.Value.Has(candidate) && _bodyRadii.Value.Has(candidate);
        }

        private bool Sweep(in Projectile projectile, Vector3 from, Vector3 to, int candidate, out float time)
        {
            Vector3 bodyTo = _positions.Value.Get(candidate).Value;
            Vector3 bodyFrom = _previousPositions.Value.Has(candidate) ? _previousPositions.Value.Get(candidate).Value : bodyTo;
            float contact = projectile.Radius + _bodyRadii.Value.Get(candidate).Value;

            return RangedTarget.SweptHit(from, to, bodyFrom, bodyTo, contact, out time);
        }

        private bool TryHit(EcsWorld world, in Projectile projectile, Vector3 from, Vector3 to, out int hit)
        {
            hit = -1;

            Query(in projectile, from, to);

            float earliest = float.MaxValue;

            for (int index = 0; index < _candidates.Count; index++)
            {
                int candidate = _candidates[index];

                if (IsTarget(candidate) == false || Sweep(in projectile, from, to, candidate, out float time) == false)
                    continue;

                if (time >= earliest)
                    continue;

                earliest = time;
                hit = candidate;
            }

            return hit >= 0;
        }

        private void Remove(EcsWorld world, int arrow)
        {
            if (_views.Value.Has(arrow))
            {
                ref View view = ref _views.Value.Get(arrow);

                if (view.Value != null)
                    _viewFactory.Value.Release(view.Value);

                view.Value = null;
            }

            world.DelEntity(arrow);
        }
    }
}

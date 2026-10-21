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
    public sealed class ChakramSystem : IEcsInitSystem, IEcsRunSystem
    {
        private const int CandidateCapacityReserve = 16;
        private const float BleedFxHeight = 1f;

        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<Player, Position, Facing>, Exc<Dead>> _heroes = default;
        private readonly EcsFilterInject<Inc<ChakramCooldown>> _cooldownFilter = default;
        private readonly EcsFilterInject<Inc<Chakram, Position>> _chakrams = default;
        private readonly EcsFilterInject<Inc<Enemy, Position>, Exc<Dead>> _enemies = default;
        private readonly EcsFilterInject<Inc<Bow, OwnerLink, WeaponDamage>> _bows = default;
        private readonly EcsFilterInject<Inc<TakenPerk>> _taken = default;

        private readonly EcsPoolInject<TakenPerk> _takenPool = default;
        private readonly EcsPoolInject<ChakramCooldown> _cooldowns = default;
        private readonly EcsPoolInject<Chakram> _chakramPool = default;
        private readonly EcsPoolInject<AbilityCooldownScale> _cooldownScales = default;
        private readonly EcsPoolInject<OwnerLink> _ownerLinks = default;
        private readonly EcsPoolInject<WeaponDamage> _damages = default;
        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<Facing> _facings = default;
        private readonly EcsPoolInject<Enemy> _enemyPool = default;
        private readonly EcsPoolInject<Breakable> _breakables = default;
        private readonly EcsPoolInject<Boss> _bosses = default;
        private readonly EcsPoolInject<Dead> _dead = default;
        private readonly EcsPoolInject<BodyRadius> _bodyRadii = default;
        private readonly EcsPoolInject<Slowed> _slowed = default;
        private readonly EcsPoolInject<Bleeding> _bleeding = default;
        private readonly EcsPoolInject<StatusFx> _statusFx = default;
        private readonly EcsPoolInject<DamageEvent> _damageEvents = default;
        private readonly EcsPoolInject<NotIndexed> _notIndexed = default;
        private readonly EcsPoolInject<View> _views = default;

        private readonly EcsCustomInject<IContentRegistry> _content = default;
        private readonly EcsCustomInject<SimulationClock> _clock = default;
        private readonly EcsCustomInject<IViewFactory> _viewFactory = default;
        private readonly EcsCustomInject<SpatialGrid> _grid = default;
        private readonly EcsCustomInject<EnemyMotionBounds> _motionBounds = default;
        private readonly EcsCustomInject<LevelConfig> _level = default;

        private List<int> _candidates;

        public void Init(IEcsSystems systems)
        {
            _candidates = new List<int>(WaveTimelineValidator.ResolveMaxLiveCap(_level.Value.Waves) + CandidateCapacityReserve);
        }

        public void Run(IEcsSystems systems)
        {
            EcsWorld world = _world.Value;
            float delta = _clock.Value.Delta;

            foreach (int hero in _cooldownFilter.Value)
            {
                if (--_cooldowns.Value.Get(hero).RemainingTicks <= 0)
                    _cooldowns.Value.Del(hero);
            }

            foreach (int hero in _heroes.Value)
            {
                if (_cooldowns.Value.Has(hero))
                    continue;

                if (TakenBehaviourLookup.TryFind(world, _taken.Value, _takenPool.Value, _content.Value, hero, out ChakramConfig config, out int level) == false)
                    continue;

                Vector3 origin = _positions.Value.Get(hero).Value;

                if (HasEnemyWithin(origin, config.TriggerRange) == false || TryFindBowDamage(world, hero, out float bowDamage) == false)
                    continue;

                ChakramLevel entry = config.Level(level);
                Vector3 facing = _facings.Value.Get(hero).Value;
                float facingDegrees = Mathf.Atan2(facing.x, facing.z) * Mathf.Rad2Deg;

                for (int index = 0; index < entry.Count; index++)
                    Spawn(world, hero, config, entry, bowDamage, origin, facingDegrees + 360f * index / entry.Count, delta);

                _cooldowns.Value.Add(hero).RemainingTicks =
                    Mathf.Max(1, Mathf.RoundToInt(entry.CooldownSeconds * AbilityCooldowns.Scale(_cooldownScales.Value, hero) / delta));
            }

            foreach (int chakram in _chakrams.Value)
                Advance(world, chakram, delta);
        }

        private bool HasEnemyWithin(Vector3 origin, float radius)
        {
            foreach (int enemy in _enemies.Value)
            {
                Vector3 offset = _positions.Value.Get(enemy).Value - origin;

                if (offset.x * offset.x + offset.z * offset.z <= radius * radius)
                    return true;
            }

            return false;
        }

        private bool TryFindBowDamage(EcsWorld world, int hero, out float damage)
        {
            foreach (int bow in _bows.Value)
            {
                if (_ownerLinks.Value.Get(bow).Owner.Unpack(world, out int owner) == false || owner != hero)
                    continue;

                damage = _damages.Value.Get(bow).Value;

                return true;
            }

            damage = 0f;

            return false;
        }

        private void Spawn(EcsWorld world, int hero, ChakramConfig config, ChakramLevel entry, float bowDamage, Vector3 origin, float startDegrees, float delta)
        {
            int entity = world.NewEntity();

            ref Chakram chakram = ref _chakramPool.Value.Add(entity);
            chakram.Config = config;
            chakram.Owner = world.PackEntity(hero);
            chakram.StartAngleDegrees = startDegrees;
            chakram.TotalTicks = Mathf.Max(1, Mathf.RoundToInt(config.LifeSeconds / delta));
            chakram.Damage = bowDamage * entry.DamageScale;
            chakram.BleedTickDamage = bowDamage * entry.BleedDamageScale * config.BleedTickSeconds;
            chakram.BleedTicks = Mathf.RoundToInt(entry.BleedSeconds / delta);

            Vector3 at = Place(origin, config, startDegrees, 0f);

            _positions.Value.Add(entity).Value = at;
            _facings.Value.Add(entity).Value = Vector3.forward;
            _notIndexed.Value.Add(entity);
            _views.Value.Add(entity).Value = _viewFactory.Value.Create(config.ChakramPrefab, at);
        }

        private static Vector3 Place(Vector3 center, ChakramConfig config, float startDegrees, float progress)
        {
            float radius = Mathf.Lerp(config.StartRadius, config.EndRadius, progress);
            float angle = (startDegrees + 360f * config.Turns * progress) * Mathf.Deg2Rad;

            return new Vector3(center.x + Mathf.Sin(angle) * radius, 0f, center.z + Mathf.Cos(angle) * radius);
        }

        private void Advance(EcsWorld world, int entity, float delta)
        {
            ref Chakram chakram = ref _chakramPool.Value.Get(entity);
            ChakramConfig config = chakram.Config;

            if (chakram.Owner.Unpack(world, out int owner) == false || _dead.Value.Has(owner) || chakram.ElapsedTicks >= chakram.TotalTicks)
            {
                Remove(world, entity);

                return;
            }

            chakram.ElapsedTicks++;

            ref Position position = ref _positions.Value.Get(entity);
            Vector3 from = position.Value;
            Vector3 to = Place(_positions.Value.Get(owner).Value, config, chakram.StartAngleDegrees, chakram.ElapsedTicks / (float)chakram.TotalTicks);

            position.Value = to;
            chakram.SpinDegrees += config.SpinDegreesPerSecond * delta;
            _facings.Value.Get(entity).Value = Quaternion.AngleAxis(chakram.SpinDegrees, Vector3.up) * Vector3.forward;

            Cut(world, ref chakram, owner, from, to, delta);
        }

        private void Cut(EcsWorld world, ref Chakram chakram, int owner, Vector3 from, Vector3 to, float delta)
        {
            ChakramConfig config = chakram.Config;
            Vector3 middle = (from + to) * 0.5f;
            float halfStep = Vector3.Distance(from, to) * 0.5f;
            int slowTicks = Mathf.Max(1, Mathf.RoundToInt(config.SlowSeconds / delta));

            _grid.Value.Query(middle, halfStep + config.HitRadius + _motionBounds.Value.MaxEnemyBodyRadius, _motionBounds.Value.CandidateSlack(delta), _candidates);

            for (int index = 0; index < _candidates.Count; index++)
            {
                int candidate = _candidates[index];
                bool enemy = _enemyPool.Value.Has(candidate);

                if (enemy == false && _breakables.Value.Has(candidate) == false)
                    continue;

                if (_dead.Value.Has(candidate) || _bodyRadii.Value.Has(candidate) == false)
                    continue;

                float reach = config.HitRadius + _bodyRadii.Value.Get(candidate).Value;

                if (FlatSquaredDistanceToSegment(_positions.Value.Get(candidate).Value, from, to) > reach * reach)
                    continue;

                EcsPackedEntity packed = world.PackEntity(candidate);

                if (AlreadyHit(ref chakram, packed))
                    continue;

                RememberHit(ref chakram, packed);

                ref DamageEvent damage = ref _damageEvents.Value.Add(world.NewEntity());
                damage.Target = packed;
                damage.Source = world.PackEntity(owner);
                damage.SourcePosition = middle;
                damage.Amount = chakram.Damage;
                damage.Kind = DamageKind.Perk;
                damage.IsSlash = true;
                damage.SourcePerk = config;

                if (enemy == false)
                    continue;

                Bleed(world, candidate, ref chakram, owner);

                if (_bosses.Value.Has(candidate))
                    continue;

                ref Slowed slowed = ref _slowed.Value.Has(candidate) ? ref _slowed.Value.Get(candidate) : ref _slowed.Value.Add(candidate);
                slowed.Factor = Mathf.Max(slowed.Factor, config.Slow);
                slowed.RemainingTicks = Mathf.Max(slowed.RemainingTicks, slowTicks);
            }
        }

        private void Bleed(EcsWorld world, int target, ref Chakram chakram, int owner)
        {
            if (chakram.BleedTicks <= 0 || chakram.BleedTickDamage <= 0f)
                return;

            ChakramConfig config = chakram.Config;
            bool fresh = _bleeding.Value.Has(target) == false;
            ref Bleeding bleeding = ref fresh ? ref _bleeding.Value.Add(target) : ref _bleeding.Value.Get(target);
            int tickTicks = Mathf.Max(1, Mathf.RoundToInt(config.BleedTickSeconds / _clock.Value.Delta));

            bleeding.SourcePerk = config;
            bleeding.Source = world.PackEntity(owner);
            bleeding.TickDamage = Mathf.Max(fresh ? 0f : bleeding.TickDamage, chakram.BleedTickDamage);
            bleeding.TickTicks = tickTicks;
            bleeding.RemainingTicks = Mathf.Max(fresh ? 0 : bleeding.RemainingTicks, chakram.BleedTicks);

            if (fresh)
                bleeding.TickCountdown = tickTicks;

            if (bleeding.HasFx || config.BleedFxPrefab == null)
                return;

            bleeding.HasFx = true;
            SpawnFx(world, target, config.BleedFxPrefab);
        }

        private void SpawnFx(EcsWorld world, int target, GameObject prefab)
        {
            Vector3 at = _positions.Value.Get(target).Value;
            int fx = world.NewEntity();

            ref StatusFx status = ref _statusFx.Value.Add(fx);
            status.Owner = world.PackEntity(target);
            status.Kind = StatusKind.Bleeding;
            status.Height = BleedFxHeight;

            _positions.Value.Add(fx).Value = at;
            _facings.Value.Add(fx).Value = Vector3.forward;
            _notIndexed.Value.Add(fx);

            IView view = _viewFactory.Value.Create(prefab, at);
            _views.Value.Add(fx).Value = view;

            if (view == null)
                return;

            view.SetLift(BleedFxHeight);
            view.SetLift(BleedFxHeight);
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

        private static float FlatSquaredDistanceToSegment(Vector3 point, Vector3 from, Vector3 to)
        {
            float segmentX = to.x - from.x;
            float segmentZ = to.z - from.z;
            float lengthSquared = segmentX * segmentX + segmentZ * segmentZ;
            float t = lengthSquared > 1e-8f ? Mathf.Clamp01(((point.x - from.x) * segmentX + (point.z - from.z) * segmentZ) / lengthSquared) : 0f;
            float x = point.x - (from.x + segmentX * t);
            float z = point.z - (from.z + segmentZ * t);

            return x * x + z * z;
        }

        private static bool AlreadyHit(ref Chakram chakram, in EcsPackedEntity candidate)
        {
            for (int index = 0; index < chakram.HitCount; index++)
            {
                if (chakram.Hits[index].EqualsTo(candidate))
                    return true;
            }

            return false;
        }

        private static void RememberHit(ref Chakram chakram, in EcsPackedEntity candidate)
        {
            if (chakram.HitCount == chakram.Hits.Length)
                Array.Resize(ref chakram.Hits, chakram.Hits.Length * 2);

            chakram.Hits[chakram.HitCount] = candidate;
            chakram.HitCount++;
        }
    }
}

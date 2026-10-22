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
    public sealed class KillShotSystem : IEcsInitSystem, IEcsRunSystem
    {
        private const int CandidateCapacityReserve = 16;
        private const float RangeAllowance = 1.5f;

        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<KillShotCooldown>> _cooldownFilter = default;
        private readonly EcsFilterInject<Inc<Player, Position, TorsoAim, InputState>, Exc<Dead>> _heroes = default;
        private readonly EcsFilterInject<Inc<Enemy, Position, BodyRadius, Health>, Exc<Dead>> _enemies = default;
        private readonly EcsFilterInject<Inc<KillShot, Position>> _shots = default;
        private readonly EcsFilterInject<Inc<Bow, OwnerLink, WeaponDamage>> _bows = default;
        private readonly EcsFilterInject<Inc<TakenPerk>> _taken = default;

        private readonly EcsPoolInject<TakenPerk> _takenPool = default;
        private readonly EcsPoolInject<KillShotCooldown> _cooldowns = default;
        private readonly EcsPoolInject<KillShotAim> _aimPool = default;
        private readonly EcsPoolInject<KillShot> _shotPool = default;
        private readonly EcsPoolInject<AbilityCooldownScale> _cooldownScales = default;
        private readonly EcsPoolInject<TorsoAim> _torsoAims = default;
        private readonly EcsPoolInject<InputState> _inputs = default;
        private readonly EcsPoolInject<Backleaping> _backleaps = default;
        private readonly EcsPoolInject<Bow> _bowPool = default;
        private readonly EcsPoolInject<OwnerLink> _ownerLinks = default;
        private readonly EcsPoolInject<WeaponDamage> _damages = default;
        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<Facing> _facings = default;
        private readonly EcsPoolInject<Enemy> _enemyPool = default;
        private readonly EcsPoolInject<Elite> _elites = default;
        private readonly EcsPoolInject<Boss> _bosses = default;
        private readonly EcsPoolInject<Breakable> _breakables = default;
        private readonly EcsPoolInject<Dead> _dead = default;
        private readonly EcsPoolInject<BodyRadius> _bodyRadii = default;
        private readonly EcsPoolInject<Health> _healths = default;
        private readonly EcsPoolInject<MaxHealth> _maxHealths = default;
        private readonly EcsPoolInject<Height> _heights = default;
        private readonly EcsPoolInject<DamageEvent> _damageEvents = default;
        private readonly EcsPoolInject<LeapImpactFx> _impactFx = default;
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
                Aim(world, hero, delta);

            foreach (int shot in _shots.Value)
                Advance(world, shot, delta);
        }

        private void Aim(EcsWorld world, int hero, float delta)
        {
            if (TakenBehaviourLookup.TryFind(world, _taken.Value, _takenPool.Value, _content.Value, hero, out KillShotConfig config, out int level) == false
                || _cooldowns.Value.Has(hero)
                || _backleaps.Value.Has(hero)
                || _inputs.Value.Get(hero).StandHeld == false
                || _torsoAims.Value.Get(hero).Manual == false
                || TryFindTarget(hero, config, _torsoAims.Value.Get(hero).Desired, out int target) == false)
            {
                if (_aimPool.Value.Has(hero))
                    _aimPool.Value.Del(hero);

                return;
            }

            EcsPackedEntity packed = world.PackEntity(target);

            if (_aimPool.Value.Has(hero) == false)
                _aimPool.Value.Add(hero);

            ref KillShotAim aim = ref _aimPool.Value.Get(hero);

            if (aim.Target.EqualsTo(packed))
            {
                aim.Ticks++;
            }
            else
            {
                aim.Target = packed;
                aim.Ticks = 1;
            }

            if (aim.Ticks < Mathf.Max(1, Mathf.RoundToInt(config.AimSeconds / delta)))
                return;

            if (TryFindBow(world, hero, out BowConfig bow, out float bowDamage) == false)
                return;

            _aimPool.Value.Del(hero);

            KillShotLevel entry = config.Level(level);

            Fire(world, hero, target, config, entry, bowDamage, delta);

            _cooldowns.Value.Add(hero).RemainingTicks =
                Mathf.Max(1, Mathf.RoundToInt(entry.CooldownSeconds * AbilityCooldowns.Scale(_cooldownScales.Value, hero) / delta));

            if (_views.Value.Has(hero) && _views.Value.Get(hero).Value != null)
                _views.Value.Get(hero).Value.PlayAttack(bow.ReleaseTrigger, 1f);
        }

        private bool TryFindTarget(int hero, KillShotConfig config, Vector3 aim, out int target)
        {
            target = -1;

            aim.y = 0f;

            if (aim.sqrMagnitude <= Mathf.Epsilon)
                return false;

            aim.Normalize();

            Vector3 origin = _positions.Value.Get(hero).Value;
            float best = float.MaxValue;

            foreach (int enemy in _enemies.Value)
            {
                if (_elites.Value.Has(enemy) == false && _bosses.Value.Has(enemy) == false)
                    continue;

                Vector3 offset = _positions.Value.Get(enemy).Value - origin;
                offset.y = 0f;

                float distance = offset.magnitude;

                if (distance <= Mathf.Epsilon || distance > config.Range)
                    continue;

                float widening = Mathf.Asin(Mathf.Min(1f, _bodyRadii.Value.Get(enemy).Value / distance)) * Mathf.Rad2Deg;
                float deviation = Vector3.Angle(aim, offset) - widening;

                if (deviation > config.AimHalfAngleDegrees || deviation >= best)
                    continue;

                best = deviation;
                target = enemy;
            }

            return target >= 0;
        }

        private bool TryFindBow(EcsWorld world, int hero, out BowConfig config, out float damage)
        {
            foreach (int bow in _bows.Value)
            {
                if (_ownerLinks.Value.Get(bow).Owner.Unpack(world, out int owner) == false || owner != hero)
                    continue;

                config = _bowPool.Value.Get(bow).Config;
                damage = _damages.Value.Get(bow).Value;

                return true;
            }

            config = null;
            damage = 0f;

            return false;
        }

        private void Fire(EcsWorld world, int hero, int target, KillShotConfig config, KillShotLevel entry, float bowDamage, float delta)
        {
            Vector3 origin = _positions.Value.Get(hero).Value;
            Vector3 direction = _positions.Value.Get(target).Value - origin;
            direction.y = 0f;
            direction = direction.sqrMagnitude > Mathf.Epsilon ? direction.normalized : Vector3.forward;

            Vector3 spawn = origin + direction * config.MuzzleDistance;
            int entity = world.NewEntity();

            ref KillShot shot = ref _shotPool.Value.Add(entity);
            shot.Config = config;
            shot.Owner = world.PackEntity(hero);
            shot.Target = world.PackEntity(target);
            shot.Direction = direction;
            shot.Damage = bowDamage * entry.DamageScale;
            shot.PierceDamage = bowDamage * entry.PierceDamageScale;
            shot.ExecuteThreshold = entry.ExecuteThreshold;
            shot.RemainingTicks = Mathf.Max(1, Mathf.CeilToInt(config.Range * RangeAllowance / (config.Speed * delta)));

            _positions.Value.Add(entity).Value = spawn;
            _facings.Value.Add(entity).Value = direction;
            _notIndexed.Value.Add(entity);

            IView view = _viewFactory.Value.Create(config.CometPrefab, spawn);
            _views.Value.Add(entity).Value = view;

            float lift = _heights.Value.Has(hero) ? _heights.Value.Get(hero).Value : 0f;

            if (lift > 0f && view != null)
            {
                view.SetLift(lift);
                view.SetLift(lift);
            }
        }

        private void Advance(EcsWorld world, int entity, float delta)
        {
            ref KillShot shot = ref _shotPool.Value.Get(entity);

            if (shot.RemainingTicks <= 0)
            {
                Remove(world, entity);

                return;
            }

            shot.RemainingTicks--;

            ref Position position = ref _positions.Value.Get(entity);
            Vector3 from = position.Value;
            float step = shot.Config.Speed * delta;
            bool homing = shot.Target.Unpack(world, out int target) && _dead.Value.Has(target) == false && _positions.Value.Has(target);

            if (homing)
            {
                Vector3 offset = _positions.Value.Get(target).Value - from;
                offset.y = 0f;

                float distance = offset.magnitude;
                float reach = shot.Config.HitRadius + _bodyRadii.Value.Get(target).Value;

                if (distance > Mathf.Epsilon)
                    shot.Direction = offset / distance;

                if (distance - reach <= step)
                {
                    Vector3 contact = from + shot.Direction * Mathf.Max(0f, distance - reach);

                    Pierce(world, ref shot, from, contact, target);
                    Finish(world, in shot, target, contact);
                    Remove(world, entity);

                    return;
                }
            }

            Vector3 to = from + shot.Direction * step;

            position.Value = to;
            _facings.Value.Get(entity).Value = shot.Direction;

            Pierce(world, ref shot, from, to, homing ? target : -1);
        }

        private void Pierce(EcsWorld world, ref KillShot shot, Vector3 from, Vector3 to, int target)
        {
            if (shot.PierceDamage <= 0f)
                return;

            KillShotConfig config = shot.Config;
            Vector3 middle = (from + to) * 0.5f;
            float halfStep = Vector3.Distance(from, to) * 0.5f;

            _grid.Value.Query(middle, halfStep + config.HitRadius + _motionBounds.Value.MaxEnemyBodyRadius, _motionBounds.Value.CandidateSlack(_clock.Value.Delta), _candidates);

            for (int index = 0; index < _candidates.Count; index++)
            {
                int candidate = _candidates[index];

                if (candidate == target)
                    continue;

                if (_enemyPool.Value.Has(candidate) == false && _breakables.Value.Has(candidate) == false)
                    continue;

                if (_dead.Value.Has(candidate) || _bodyRadii.Value.Has(candidate) == false)
                    continue;

                float reach = config.HitRadius + _bodyRadii.Value.Get(candidate).Value;

                if (FlatSquaredDistanceToSegment(_positions.Value.Get(candidate).Value, from, to) > reach * reach)
                    continue;

                EcsPackedEntity packed = world.PackEntity(candidate);

                if (AlreadyHit(ref shot, packed))
                    continue;

                RememberHit(ref shot, packed);

                ref DamageEvent damage = ref _damageEvents.Value.Add(world.NewEntity());
                damage.Target = packed;
                damage.Source = shot.Owner;
                damage.SourcePosition = from;
                damage.Amount = shot.PierceDamage;
                damage.Kind = DamageKind.Perk;
                damage.SourcePerk = config;
            }
        }

        private void Finish(EcsWorld world, in KillShot shot, int target, Vector3 at)
        {
            KillShotConfig config = shot.Config;
            bool boss = _bosses.Value.Has(target);

            ref DamageEvent damage = ref _damageEvents.Value.Add(world.NewEntity());
            damage.Target = world.PackEntity(target);
            damage.Source = shot.Owner;
            damage.SourcePosition = at;
            damage.Amount = shot.Damage * (boss ? config.BossCritScale : 1f);
            damage.Kind = DamageKind.Perk;
            damage.ForceCrit = true;
            damage.SourcePerk = config;

            if (boss == false && _maxHealths.Value.Has(target))
                damage.Execute = _healths.Value.Get(target).Current < _maxHealths.Value.Get(target).Value * shot.ExecuteThreshold;

            if (config.ImpactPrefab == null)
                return;

            Vector3 point = _positions.Value.Get(target).Value;
            int fx = world.NewEntity();

            _impactFx.Value.Add(fx).RemainingTicks = Mathf.Max(1, Mathf.RoundToInt(config.ImpactSeconds / _clock.Value.Delta));
            _positions.Value.Add(fx).Value = point;
            _notIndexed.Value.Add(fx);
            _views.Value.Add(fx).Value = _viewFactory.Value.Create(config.ImpactPrefab, point);
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

        private static bool AlreadyHit(ref KillShot shot, in EcsPackedEntity candidate)
        {
            for (int index = 0; index < shot.HitCount; index++)
            {
                if (shot.Hits[index].EqualsTo(candidate))
                    return true;
            }

            return false;
        }

        private static void RememberHit(ref KillShot shot, in EcsPackedEntity candidate)
        {
            if (shot.HitCount == shot.Hits.Length)
                Array.Resize(ref shot.Hits, shot.Hits.Length * 2);

            shot.Hits[shot.HitCount] = candidate;
            shot.HitCount++;
        }
    }
}

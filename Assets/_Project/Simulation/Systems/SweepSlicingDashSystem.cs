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
    public sealed class SweepSlicingDashSystem : IEcsInitSystem, IEcsRunSystem
    {
        private const int CandidateCapacityReserve = 16;

        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<Player, Dashing, Position, PreviousPosition>, Exc<Dead>> _heroes = default;
        private readonly EcsFilterInject<Inc<TakenPerk>> _taken = default;
        private readonly EcsFilterInject<Inc<Weapon, OwnerLink, WeaponDamage>> _weapons = default;
        private readonly EcsFilterInject<Inc<SlicingDashCooldown>> _cooldowns = default;

        private readonly EcsPoolInject<TakenPerk> _takenPool = default;
        private readonly EcsPoolInject<Dashing> _dashes = default;
        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<PreviousPosition> _previousPositions = default;
        private readonly EcsPoolInject<BodyRadius> _bodyRadii = default;
        private readonly EcsPoolInject<Enemy> _enemies = default;
        private readonly EcsPoolInject<Dead> _dead = default;
        private readonly EcsPoolInject<Velocity> _velocities = default;
        private readonly EcsPoolInject<NotPushable> _notPushables = default;
        private readonly EcsPoolInject<Pushed> _pushes = default;
        private readonly EcsPoolInject<DamageEvent> _damageEvents = default;
        private readonly EcsPoolInject<OwnerLink> _ownerLinks = default;
        private readonly EcsPoolInject<WeaponDamage> _weaponDamages = default;
        private readonly EcsPoolInject<SlicingDashRoll> _rolls = default;
        private readonly EcsPoolInject<SlicingDashCooldown> _cooldownPool = default;
        private readonly EcsPoolInject<AbilityCooldownScale> _cooldownScales = default;
        private readonly EcsPoolInject<AreaStrike> _strikes = default;
        private readonly EcsPoolInject<UpperSlashCue> _cues = default;

        private readonly EcsCustomInject<IContentRegistry> _content = default;
        private readonly EcsCustomInject<SpatialGrid> _grid = default;
        private readonly EcsCustomInject<SimulationClock> _clock = default;
        private readonly EcsCustomInject<EnemyMotionBounds> _motionBounds = default;
        private readonly EcsCustomInject<LevelConfig> _level = default;

        private readonly HashSet<int> _hitThisDash = new HashSet<int>();

        private List<int> _candidates;

        public void Init(IEcsSystems systems)
        {
            _candidates = new List<int>(WaveTimelineValidator.ResolveMaxLiveCap(_level.Value.Waves) + CandidateCapacityReserve);
        }

        public void Run(IEcsSystems systems)
        {
            foreach (int entity in _cooldowns.Value)
            {
                ref SlicingDashCooldown cooldown = ref _cooldownPool.Value.Get(entity);

                if (cooldown.RemainingTicks > 0)
                    cooldown.RemainingTicks--;
            }

            EcsWorld world = _world.Value;

            foreach (int hero in _heroes.Value)
            {
                ref Dashing dashing = ref _dashes.Value.Get(hero);

                if (dashing.RemainingTicks == dashing.TotalTicks)
                    Roll(world, hero, in dashing);

                if (_rolls.Value.Has(hero) == false)
                    continue;

                if (TakenBehaviourLookup.TryFind(world, _taken.Value, _takenPool.Value, _content.Value, hero, out SlicingDashConfig config, out int level) == false)
                    continue;

                Cue(hero, config, in dashing);

                Sweep(world, hero, config, config.Level(level), in dashing);
            }
        }

        private void Cue(int hero, SlicingDashConfig config, in Dashing dashing)
        {
            ref SlicingDashRoll roll = ref _rolls.Value.Get(hero);

            if (roll.Cued)
                return;

            int lead = Mathf.RoundToInt(config.FinalImpactClipSeconds / config.FinalPlaybackSpeed / _clock.Value.Delta);

            if (dashing.RemainingTicks > lead)
                return;

            roll.Cued = true;

            ref UpperSlashCue cue = ref _cues.Value.Has(hero) ? ref _cues.Value.Get(hero) : ref _cues.Value.Add(hero);
            cue.Trigger = config.FinalTrigger;
            cue.PlaybackSpeed = config.FinalPlaybackSpeed;
        }

        private void ScheduleFinalStrike(EcsWorld world, int hero, SlicingDashConfig config, SlicingDashLevel entry, in Dashing dashing, ref SlicingDashRoll roll)
        {
            int entity = world.NewEntity();

            ref AreaStrike strike = ref _strikes.Value.Add(entity);
            strike.Owner = world.PackEntity(hero);
            strike.DelayTicks = Mathf.Max(0, dashing.TotalTicks - 1);
            strike.FollowOwner = true;
            strike.Reach = config.FinalReach * DashChargeRules.Scale(config.ReachAtFullCharge, dashing.Strength);
            strike.HalfAngleDegrees = config.FinalHalfAngleDegrees;
            strike.Damage = ResolveWeaponDamage(world, hero) * entry.FinalDamageShare * DashChargeRules.DamageMultiplier(in dashing);
            strike.Kind = DamageKind.Perk;
            strike.SourcePerk = config;
            strike.IsSlash = true;
            strike.PushSpeed = config.FinalPushSpeed;
            strike.PushSeconds = config.FinalPushSeconds;
            strike.PushArcHeight = config.FinalPushArcHeight;

            roll.Strike = world.PackEntity(entity);
        }

        private void Roll(EcsWorld world, int hero, in Dashing dashing)
        {
            _hitThisDash.Clear();
            _rolls.Value.Del(hero);

            if (TakenBehaviourLookup.TryFind(world, _taken.Value, _takenPool.Value, _content.Value, hero, out SlicingDashConfig config, out int level) == false)
                return;

            if (_cooldownPool.Value.Has(hero) == false)
                _cooldownPool.Value.Add(hero);

            ref SlicingDashCooldown cooldown = ref _cooldownPool.Value.Get(hero);

            if (cooldown.RemainingTicks > 0)
                return;

            float seconds = config.Level(level).CooldownSeconds * AbilityCooldowns.Scale(_cooldownScales.Value, hero);

            cooldown.RemainingTicks = Mathf.Max(0, Mathf.RoundToInt(seconds / _clock.Value.Delta));

            ref SlicingDashRoll roll = ref _rolls.Value.Add(hero);
            roll.Level = level;
            roll.Cued = false;

            ScheduleFinalStrike(world, hero, config, config.Level(level), in dashing, ref roll);
        }

        private void Sweep(EcsWorld world, int hero, SlicingDashConfig config, SlicingDashLevel entry, in Dashing dashing)
        {
            Vector3 from = _previousPositions.Value.Get(hero).Value;
            Vector3 to = _positions.Value.Get(hero).Value;
            Vector3 direction = PushFromPoint.Direction(Vector3.zero, dashing.Direction, Vector3.forward);
            Vector3 right = Vector3.Cross(Vector3.up, direction);
            Vector3 side = config.SwordSide == SwordSide.Right ? right : -right;

            float heroRadius = _bodyRadii.Value.Has(hero) ? _bodyRadii.Value.Get(hero).Value : 0f;
            float width = config.CorridorWidth * DashChargeRules.Scale(config.ReachAtFullCharge, dashing.Strength);
            float damage = ResolveWeaponDamage(world, hero) * entry.CorridorDamageShare * DashChargeRules.DamageMultiplier(in dashing);
            int pushTicks = Mathf.Max(1, Mathf.RoundToInt(config.CorridorPushSeconds / _clock.Value.Delta));
            bool pushes = config.CorridorPushSpeed > 0f && config.CorridorPushSeconds > 0f;

            Vector3 middle = (from + to) * 0.5f;
            float halfLength = Vector3.Distance(new Vector3(from.x, 0f, from.z), new Vector3(to.x, 0f, to.z)) * 0.5f;
            float radius = halfLength + width + _motionBounds.Value.MaxEnemyBodyRadius;

            _grid.Value.Query(middle, radius, _motionBounds.Value.CandidateSlack(_clock.Value.Delta), _candidates);

            for (int index = 0; index < _candidates.Count; index++)
            {
                int candidate = _candidates[index];

                if (_enemies.Value.Has(candidate) == false || _dead.Value.Has(candidate) || _bodyRadii.Value.Has(candidate) == false)
                    continue;

                if (_hitThisDash.Contains(candidate))
                    continue;

                Vector3 position = _positions.Value.Get(candidate).Value;
                Vector3 offset = position - ClosestPointOnSegment(position, from, to);
                offset.y = 0f;

                float lateral = Vector3.Dot(offset, side);

                if (lateral < -heroRadius)
                    continue;

                if (offset.magnitude > width + _bodyRadii.Value.Get(candidate).Value)
                    continue;

                _hitThisDash.Add(candidate);

                if (damage > 0f)
                {
                    ref DamageEvent damageEvent = ref _damageEvents.Value.Add(world.NewEntity());
                    damageEvent.Target = world.PackEntity(candidate);
                    damageEvent.Source = world.PackEntity(hero);
                    damageEvent.SourcePosition = position - offset;
                    damageEvent.Amount = damage;
                    damageEvent.Kind = DamageKind.Perk;
                    damageEvent.IsSlash = true;
                    damageEvent.SourcePerk = config;
                }

                if (pushes && _velocities.Value.Has(candidate) && _notPushables.Value.Has(candidate) == false)
                {
                    PushFromPoint.Apply(_pushes.Value, candidate, side, config.CorridorPushSpeed, pushTicks);
                    _pushes.Value.Get(candidate).ArcHeight = config.CorridorPushArcHeight;
                }
            }
        }

        private float ResolveWeaponDamage(EcsWorld world, int hero)
        {
            foreach (int weapon in _weapons.Value)
            {
                if (_ownerLinks.Value.Get(weapon).Owner.Unpack(world, out int owner) && owner == hero)
                    return _weaponDamages.Value.Get(weapon).Value;
            }

            return 0f;
        }

        private static Vector3 ClosestPointOnSegment(Vector3 point, Vector3 from, Vector3 to)
        {
            Vector3 segment = new Vector3(to.x - from.x, 0f, to.z - from.z);
            float squared = segment.sqrMagnitude;

            if (squared <= 1e-8f)
                return new Vector3(from.x, point.y, from.z);

            float t = Mathf.Clamp01(((point.x - from.x) * segment.x + (point.z - from.z) * segment.z) / squared);

            return new Vector3(from.x + segment.x * t, point.y, from.z + segment.z * t);
        }
    }
}

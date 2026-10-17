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
    public sealed class StartDashFinisherSystem : IEcsInitSystem, IEcsRunSystem
    {
        private const int CandidateCapacityReserve = 16;

        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<Player, DashEndedEvent>, Exc<Dead, SpecialAttack>> _heroes = default;
        private readonly EcsFilterInject<Inc<AgileDashCooldown>> _cooldowns = default;
        private readonly EcsFilterInject<Inc<TakenPerk>> _taken = default;
        private readonly EcsFilterInject<Inc<Weapon, OwnerLink, WeaponDamage>> _weapons = default;

        private readonly EcsPoolInject<TakenPerk> _takenPool = default;
        private readonly EcsPoolInject<DashEndedEvent> _endedEvents = default;
        private readonly EcsPoolInject<SlicingDashRoll> _slicingRolls = default;
        private readonly EcsPoolInject<AgileDashCooldown> _cooldownPool = default;
        private readonly EcsPoolInject<OwnerLink> _ownerLinks = default;
        private readonly EcsPoolInject<WeaponDamage> _weaponDamages = default;
        private readonly EcsPoolInject<DashFinisher> _finishers = default;
        private readonly EcsPoolInject<DashFinisherStarted> _started = default;
        private readonly EcsPoolInject<SpecialAttack> _specialAttacks = default;
        private readonly EcsPoolInject<AreaStrike> _strikes = default;
        private readonly EcsPoolInject<Invulnerable> _invulnerables = default;
        private readonly EcsPoolInject<ProjectilePassThrough> _passThroughs = default;
        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<Enemy> _enemies = default;
        private readonly EcsPoolInject<Dead> _dead = default;
        private readonly EcsPoolInject<AbilityCooldownScale> _cooldownScales = default;

        private readonly EcsCustomInject<IContentRegistry> _content = default;
        private readonly EcsCustomInject<SimulationClock> _clock = default;
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
            foreach (int entity in _cooldowns.Value)
            {
                ref AgileDashCooldown cooldown = ref _cooldownPool.Value.Get(entity);

                if (cooldown.RemainingTicks > 0)
                    cooldown.RemainingTicks--;
            }

            EcsWorld world = _world.Value;

            foreach (int hero in _heroes.Value)
            {
                ref DashEndedEvent ended = ref _endedEvents.Value.Get(hero);

                if (TryStartAgile(world, hero, in ended) && _slicingRolls.Value.Has(hero)
                    && _slicingRolls.Value.Get(hero).Strike.Unpack(world, out int pendingStrike))
                    world.DelEntity(pendingStrike);

                _slicingRolls.Value.Del(hero);
            }
        }

        private bool TryStartAgile(EcsWorld world, int hero, in DashEndedEvent ended)
        {
            if (TakenBehaviourLookup.TryFind(world, _taken.Value, _takenPool.Value, _content.Value, hero, out AgileDashConfig config, out int level) == false)
                return false;

            if (_cooldownPool.Value.Has(hero) == false)
                _cooldownPool.Value.Add(hero);

            ref AgileDashCooldown cooldown = ref _cooldownPool.Value.Get(hero);

            if (cooldown.RemainingTicks > 0)
                return false;

            if (CountEnemiesNear(_positions.Value.Get(hero).Value, config.TriggerRadius, config.MinEnemies) < config.MinEnemies)
                return false;

            AgileDashLevel entry = config.Level(level);
            float delta = _clock.Value.Delta;

            cooldown.RemainingTicks = Mathf.Max(0, Mathf.RoundToInt(entry.CooldownSeconds * AbilityCooldowns.Scale(_cooldownScales.Value, hero) / delta));

            int finisher = StartFinisher(world, hero, config.Trigger, config.PlaybackSpeed, config.ClipSeconds, config.LocksMovement);

            int window = Mathf.RoundToInt(config.WindowSeconds / delta);

            if (window > 0)
            {
                Invulnerability.Grant(_invulnerables.Value, hero, window + Invulnerability.TicksSpentBeforeDamage);

                ref ProjectilePassThrough passThrough = ref _passThroughs.Value.Has(hero) ? ref _passThroughs.Value.Get(hero) : ref _passThroughs.Value.Add(hero);
                passThrough.RemainingTicks = Mathf.Max(passThrough.RemainingTicks, window + Invulnerability.TicksSpentBeforeDamage);
            }

            ref AreaStrike strike = ref AddStrike(world, hero, finisher, config.ImpactClipSeconds / config.PlaybackSpeed);
            strike.Reach = config.Reach * DashChargeRules.Scale(config.ReachAtFullCharge, ended.Strength);
            strike.HalfAngleDegrees = config.HalfAngleDegrees;
            strike.Damage = ResolveWeaponDamage(world, hero) * entry.DamageShare * DamageMultiplier(in ended);
            strike.SourcePerk = config;
            strike.PushSpeed = config.PushSpeed;
            strike.PushSeconds = config.PushSeconds;
            strike.PushArcHeight = config.PushArcHeight;

            return true;
        }

        private int StartFinisher(EcsWorld world, int hero, string trigger, float speed, float clipSeconds, bool locksMovement)
        {
            int finisher = world.NewEntity();

            ref DashFinisher dashFinisher = ref _finishers.Value.Add(finisher);
            dashFinisher.Owner = world.PackEntity(hero);
            dashFinisher.RemainingTicks = Mathf.Max(1, Mathf.RoundToInt(clipSeconds / speed / _clock.Value.Delta));
            dashFinisher.Trigger = trigger;
            dashFinisher.PlaybackSpeed = speed;

            _started.Value.Add(finisher);

            ref SpecialAttack special = ref _specialAttacks.Value.Add(hero);
            special.Attack = world.PackEntity(finisher);
            special.LocksMovement = locksMovement;

            return finisher;
        }

        private ref AreaStrike AddStrike(EcsWorld world, int hero, int finisher, float impactSeconds)
        {
            ref AreaStrike strike = ref _strikes.Value.Add(world.NewEntity());
            strike.Owner = world.PackEntity(hero);
            strike.Anchor = world.PackEntity(finisher);
            strike.Anchored = true;
            strike.DelayTicks = Mathf.Max(0, Mathf.RoundToInt(impactSeconds / _clock.Value.Delta));
            strike.FollowOwner = true;
            strike.Kind = DamageKind.Perk;
            strike.IsSlash = true;

            return ref strike;
        }

        private int CountEnemiesNear(Vector3 origin, float radius, int enough)
        {
            float squaredRadius = radius * radius;
            int count = 0;

            _grid.Value.Query(origin, radius, _motionBounds.Value.CandidateSlack(_clock.Value.Delta), _candidates);

            for (int index = 0; index < _candidates.Count; index++)
            {
                int candidate = _candidates[index];

                if (_enemies.Value.Has(candidate) == false || _dead.Value.Has(candidate))
                    continue;

                Vector3 position = _positions.Value.Get(candidate).Value;
                float x = position.x - origin.x;
                float z = position.z - origin.z;

                if (x * x + z * z > squaredRadius)
                    continue;

                count++;

                if (count >= enough)
                    return count;
            }

            return count;
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

        private static float DamageMultiplier(in DashEndedEvent ended)
        {
            return ended.DamageMultiplier > 0f ? ended.DamageMultiplier : 1f;
        }
    }
}

using Game.Configs;
using Game.Core;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class StartDashFinisherSystem : IEcsRunSystem
    {
        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<Player, DashEndedEvent>, Exc<Dead, SpecialAttack>> _heroes = default;
        private readonly EcsFilterInject<Inc<TakenPerk>> _taken = default;
        private readonly EcsFilterInject<Inc<Weapon, OwnerLink, WeaponDamage>> _weapons = default;

        private readonly EcsPoolInject<TakenPerk> _takenPool = default;
        private readonly EcsPoolInject<DashEndedEvent> _endedEvents = default;
        private readonly EcsPoolInject<SlicingDashRoll> _slicingRolls = default;
        private readonly EcsPoolInject<OwnerLink> _ownerLinks = default;
        private readonly EcsPoolInject<WeaponDamage> _weaponDamages = default;
        private readonly EcsPoolInject<DashFinisher> _finishers = default;
        private readonly EcsPoolInject<DashFinisherStarted> _started = default;
        private readonly EcsPoolInject<SpecialAttack> _specialAttacks = default;
        private readonly EcsPoolInject<AreaStrike> _strikes = default;

        private readonly EcsCustomInject<IContentRegistry> _content = default;
        private readonly EcsCustomInject<SimulationClock> _clock = default;

        public void Run(IEcsSystems systems)
        {
            EcsWorld world = _world.Value;

            foreach (int hero in _heroes.Value)
            {
                bool slicing = _slicingRolls.Value.Has(hero);

                _slicingRolls.Value.Del(hero);

                if (slicing == false)
                    continue;

                if (TakenBehaviourLookup.TryFind(world, _taken.Value, _takenPool.Value, _content.Value, hero, out SlicingDashConfig config, out int level) == false)
                    continue;

                StartFinalSlash(world, hero, config, config.Level(level), in _endedEvents.Value.Get(hero));
            }
        }

        private void StartFinalSlash(EcsWorld world, int hero, SlicingDashConfig config, SlicingDashLevel entry, in DashEndedEvent ended)
        {
            float delta = _clock.Value.Delta;
            float speed = config.FinalPlaybackSpeed;

            int finisher = world.NewEntity();

            ref DashFinisher dashFinisher = ref _finishers.Value.Add(finisher);
            dashFinisher.Owner = world.PackEntity(hero);
            dashFinisher.RemainingTicks = Mathf.Max(1, Mathf.RoundToInt(config.FinalClipSeconds / speed / delta));
            dashFinisher.Trigger = config.FinalTrigger;
            dashFinisher.PlaybackSpeed = speed;

            _started.Value.Add(finisher);

            ref SpecialAttack special = ref _specialAttacks.Value.Add(hero);
            special.Attack = world.PackEntity(finisher);
            special.LocksMovement = config.FinalLocksMovement;

            ref AreaStrike strike = ref _strikes.Value.Add(world.NewEntity());
            strike.Owner = world.PackEntity(hero);
            strike.Anchor = world.PackEntity(finisher);
            strike.Anchored = true;
            strike.DelayTicks = Mathf.Max(0, Mathf.RoundToInt(config.FinalImpactClipSeconds / speed / delta));
            strike.FollowOwner = true;
            strike.Reach = config.FinalReach * DashChargeRules.Scale(config.ReachAtFullCharge, ended.Strength);
            strike.HalfAngleDegrees = config.FinalHalfAngleDegrees;
            strike.Damage = ResolveWeaponDamage(world, hero) * entry.FinalDamageShare * (ended.DamageMultiplier > 0f ? ended.DamageMultiplier : 1f);
            strike.Kind = DamageKind.Perk;
            strike.IsSlash = true;
            strike.PushSpeed = config.FinalPushSpeed;
            strike.PushSeconds = config.FinalPushSeconds;
            strike.PushArcHeight = config.FinalPushArcHeight;
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
    }
}

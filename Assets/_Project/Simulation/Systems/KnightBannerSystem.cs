using Game.Configs;
using Game.Core;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using System;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class KnightBannerSystem : IEcsRunSystem
    {
        public const string DamageParameter = "damage";

        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<Player, Position, PreviousPosition, BodyRadius>, Exc<Dead>> _heroes = default;
        private readonly EcsFilterInject<Inc<Enemy, Position, BodyRadius, Health>, Exc<Dead>> _targets = default;
        private readonly EcsFilterInject<Inc<Weapon, WeaponDamage, OwnerLink>> _weapons = default;
        private readonly EcsFilterInject<Inc<TakenPerk>> _taken = default;

        private readonly EcsPoolInject<TakenPerk> _takenPool = default;
        private readonly EcsPoolInject<KnightBanner> _banners = default;
        private readonly EcsPoolInject<Dashing> _dashes = default;
        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<PreviousPosition> _previousPositions = default;
        private readonly EcsPoolInject<BodyRadius> _bodyRadii = default;
        private readonly EcsPoolInject<WeaponDamage> _weaponDamages = default;
        private readonly EcsPoolInject<OwnerLink> _ownerLinks = default;
        private readonly EcsPoolInject<DamageEvent> _damageEvents = default;
        private readonly EcsPoolInject<Pushed> _pushes = default;
        private readonly EcsPoolInject<NotPushable> _notPushables = default;
        private readonly EcsPoolInject<AbilityCooldownScale> _cooldownScales = default;

        private readonly EcsCustomInject<IContentRegistry> _content = default;
        private readonly EcsCustomInject<SimulationClock> _clock = default;
        private readonly EcsCustomInject<Tuning> _tuning = default;

        public void Run(IEcsSystems systems)
        {
            EcsWorld world = _world.Value;

            foreach (int hero in _heroes.Value)
            {
                if (TakenBehaviourLookup.TryFind(world, _taken.Value, _takenPool.Value, _content.Value, hero, out KnightBannerConfig config, out int level) == false)
                    continue;

                if (_banners.Value.Has(hero) == false)
                    _banners.Value.Add(hero);

                ref KnightBanner banner = ref _banners.Value.Get(hero);

                if (banner.CooldownTicks > 0)
                    banner.CooldownTicks--;

                bool dashing = _dashes.Value.Has(hero);

                if (dashing && banner.WasDashing == false && banner.CooldownTicks <= 0)
                    Arm(world, hero, config, level, ref banner);

                if (dashing == false)
                    banner.Charging = false;

                banner.WasDashing = dashing;

                if (banner.Charging)
                    Ram(world, hero, config, ref banner);
            }
        }

        private void Arm(EcsWorld world, int hero, KnightBannerConfig config, int level, ref KnightBanner banner)
        {
            if (TryFindWeapon(world, hero, out int weapon) == false)
                return;

            float damage = _weaponDamages.Value.Get(weapon).Value * config.WeaponDamageShare(level);

            if (_tuning.Value != null)
            {
                damage = _tuning.Value.Resolve(config, DamageParameter, damage);
                damage = _tuning.Value.ResolveTags(config.Tags, DamageParameter, damage);
            }

            float seconds = config.CooldownSeconds(level) * AbilityCooldowns.Scale(_cooldownScales.Value, hero);

            banner.Charging = true;
            banner.Damage = damage;
            banner.HitCount = 0;
            banner.CooldownTicks = Mathf.Max(1, Mathf.RoundToInt(seconds / _clock.Value.Delta));
        }

        private void Ram(EcsWorld world, int hero, KnightBannerConfig config, ref KnightBanner banner)
        {
            Vector3 from = _previousPositions.Value.Get(hero).Value;
            Vector3 to = _positions.Value.Get(hero).Value;
            Vector3 forward = _dashes.Value.Get(hero).Direction;
            float heroRadius = _bodyRadii.Value.Get(hero).Value;
            int pushTicks = Mathf.Max(1, Mathf.RoundToInt(config.PushSeconds / _clock.Value.Delta));

            foreach (int target in _targets.Value)
            {
                if (WasHit(world, ref banner, target))
                    continue;

                Vector3 position = _positions.Value.Get(target).Value;
                Vector3 closest = ClosestPointOnSegment(position, from, to);
                float reach = heroRadius + _bodyRadii.Value.Get(target).Value + config.HitReach;
                float x = position.x - closest.x;
                float z = position.z - closest.z;

                if (x * x + z * z >= reach * reach)
                    continue;

                ref DamageEvent damage = ref _damageEvents.Value.Add(world.NewEntity());
                damage.Target = world.PackEntity(target);
                damage.Source = world.PackEntity(hero);
                damage.SourcePosition = from;
                damage.Amount = banner.Damage;
                damage.Kind = DamageKind.Perk;
                damage.SourcePerk = config;

                Remember(world, ref banner, target);

                if (_notPushables.Value.Has(target) || config.PushSpeed <= 0f)
                    continue;

                Vector3 away = PushFromPoint.Direction(closest, position, forward);
                Vector3 direction = (away + new Vector3(forward.x, 0f, forward.z)).normalized;

                PushFromPoint.Apply(_pushes.Value, target, direction, config.PushSpeed, pushTicks);
            }
        }

        private bool WasHit(EcsWorld world, ref KnightBanner banner, int target)
        {
            for (int index = 0; index < banner.HitCount; index++)
            {
                if (banner.Hits[index].Unpack(world, out int hit) && hit == target)
                    return true;
            }

            return false;
        }

        private void Remember(EcsWorld world, ref KnightBanner banner, int target)
        {
            if (banner.HitCount == banner.Hits.Length)
                Array.Resize(ref banner.Hits, banner.Hits.Length * 2);

            banner.Hits[banner.HitCount] = world.PackEntity(target);
            banner.HitCount++;
        }

        private bool TryFindWeapon(EcsWorld world, int hero, out int weapon)
        {
            foreach (int entity in _weapons.Value)
            {
                if (_ownerLinks.Value.Get(entity).Owner.Unpack(world, out int owner) == false || owner != hero)
                    continue;

                weapon = entity;

                return true;
            }

            weapon = -1;

            return false;
        }

        private static Vector3 ClosestPointOnSegment(Vector3 point, Vector3 from, Vector3 to)
        {
            Vector3 segment = to - from;
            segment.y = 0f;

            float lengthSquared = segment.sqrMagnitude;

            if (lengthSquared <= 1e-8f)
                return from;

            Vector3 offset = point - from;
            offset.y = 0f;

            float t = Mathf.Clamp01(Vector3.Dot(offset, segment) / lengthSquared);

            return from + segment * t;
        }
    }
}

using Game.Configs;
using Game.Core;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class ReleaseStaffCastSystem : IEcsRunSystem
    {
        private const float MinAimDistance = 0.05f;

        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<Staff, StaffCast, OwnerLink, WeaponDamage>> _casts = default;
        private readonly EcsFilterInject<Inc<Staff, FireballVolley>> _volleys = default;
        private readonly EcsFilterInject<Inc<FireballVolleyCooldown>> _volleyCooldowns = default;
        private readonly EcsFilterInject<Inc<TakenPerk>> _taken = default;

        private readonly EcsPoolInject<Staff> _staffs = default;
        private readonly EcsPoolInject<StaffCast> _castPool = default;
        private readonly EcsPoolInject<OwnerLink> _ownerLinks = default;
        private readonly EcsPoolInject<WeaponDamage> _damages = default;
        private readonly EcsPoolInject<TorsoAim> _aims = default;
        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<Velocity> _velocities = default;
        private readonly EcsPoolInject<Facing> _facings = default;
        private readonly EcsPoolInject<Projectile> _projectiles = default;
        private readonly EcsPoolInject<Fireball> _fireballs = default;
        private readonly EcsPoolInject<NotIndexed> _notIndexed = default;
        private readonly EcsPoolInject<Dead> _dead = default;
        private readonly EcsPoolInject<View> _views = default;
        private readonly EcsPoolInject<FireballVolley> _volleyPool = default;
        private readonly EcsPoolInject<FireballVolleyCooldown> _volleyCooldownPool = default;
        private readonly EcsPoolInject<AbilityCooldownScale> _cooldownScales = default;
        private readonly EcsPoolInject<TakenPerk> _takenPool = default;

        private readonly EcsCustomInject<IViewFactory> _viewFactory = default;
        private readonly EcsCustomInject<SimulationClock> _clock = default;
        private readonly EcsCustomInject<IContentRegistry> _content = default;

        public void Run(IEcsSystems systems)
        {
            EcsWorld world = _world.Value;
            float delta = _clock.Value.Delta;

            foreach (int hero in _volleyCooldowns.Value)
            {
                if (--_volleyCooldownPool.Value.Get(hero).RemainingTicks <= 0)
                    _volleyCooldownPool.Value.Del(hero);
            }

            foreach (int staff in _volleys.Value)
                AdvanceVolley(world, staff, delta);

            foreach (int staff in _casts.Value)
            {
                StaffConfig config = _staffs.Value.Get(staff).Config;
                bool hasOwner = _ownerLinks.Value.Get(staff).Owner.Unpack(world, out int owner);

                if (hasOwner == false || _dead.Value.Has(owner) || _aims.Value.Has(owner) == false)
                {
                    _castPool.Value.Del(staff);

                    if (hasOwner)
                        Cue(owner, config.CancelTrigger);

                    continue;
                }

                ref StaffCast cast = ref _castPool.Value.Get(staff);
                cast.RemainingTicks--;

                if (cast.RemainingTicks > 0)
                    continue;

                int variant = cast.Variant;
                _castPool.Value.Del(staff);

                float damage = _damages.Value.Get(staff).Value;

                Fire(world, config, owner, variant, 0f, damage, delta);
                TryStartVolley(world, staff, owner, variant, damage, delta);
            }
        }

        private void Fire(EcsWorld world, StaffConfig config, int owner, int variant, float yawOffset, float damage, float delta)
        {
            Vector3 origin = _positions.Value.Get(owner).Value;
            ref TorsoAim aim = ref _aims.Value.Get(owner);
            Vector3 forward = ResolveDirection(world, in aim, origin);
            Vector3 muzzle = config.Cast(variant).Muzzle;
            Vector3 right = new Vector3(forward.z, 0f, -forward.x);
            Vector3 spawn = origin + right * muzzle.x + forward * muzzle.z;
            Vector3 direction = Quaternion.AngleAxis(yawOffset, Vector3.up) * ResolveDirection(world, in aim, spawn);

            Launch(world, config, owner, spawn, direction, muzzle.y, damage, delta);
        }

        private void TryStartVolley(EcsWorld world, int staff, int owner, int variant, float damage, float delta)
        {
            if (_volleyCooldownPool.Value.Has(owner) || _volleyPool.Value.Has(staff))
                return;

            if (TakenBehaviourLookup.TryFind(world, _taken.Value, _takenPool.Value, _content.Value, owner, out FireballVolleyConfig config, out int level) == false)
                return;

            FireballVolleyLevel entry = config.Level(level);

            ref FireballVolley volley = ref _volleyPool.Value.Add(staff);
            volley.Owner = world.PackEntity(owner);
            volley.Variant = variant;
            volley.Remaining = entry.ExtraFireballs;
            volley.TicksToNext = Mathf.Max(1, Mathf.RoundToInt(config.IntervalSeconds / delta));
            volley.Index = 0;
            volley.Damage = damage;

            _volleyCooldownPool.Value.Add(owner).RemainingTicks =
                Mathf.Max(1, Mathf.RoundToInt(entry.CooldownSeconds * AbilityCooldowns.Scale(_cooldownScales.Value, owner) / delta));
        }

        private void AdvanceVolley(EcsWorld world, int staff, float delta)
        {
            ref FireballVolley volley = ref _volleyPool.Value.Get(staff);

            if (volley.Owner.Unpack(world, out int owner) == false || _dead.Value.Has(owner) || _aims.Value.Has(owner) == false)
            {
                _volleyPool.Value.Del(staff);

                return;
            }

            if (--volley.TicksToNext > 0)
                return;

            StaffConfig config = _staffs.Value.Get(staff).Config;

            if (TakenBehaviourLookup.TryFind(world, _taken.Value, _takenPool.Value, _content.Value, owner, out FireballVolleyConfig volleyConfig, out int _) == false)
            {
                _volleyPool.Value.Del(staff);

                return;
            }

            volley.Index++;

            float side = volley.Index % 2 == 1 ? 1f : -1f;
            float yaw = side * volleyConfig.SpreadDegrees * ((volley.Index + 1) / 2);

            Fire(world, config, owner, volley.Variant, yaw, volley.Damage, delta);

            volley.Remaining--;
            volley.TicksToNext = Mathf.Max(1, Mathf.RoundToInt(volleyConfig.IntervalSeconds / delta));

            if (volley.Remaining <= 0)
                _volleyPool.Value.Del(staff);
        }

        private Vector3 ResolveDirection(EcsWorld world, in TorsoAim aim, Vector3 origin)
        {
            if (aim.Manual)
                return aim.Desired;

            if (aim.HasTarget && RangedTarget.TryFindLiving(world, aim.Target, _positions.Value, _dead.Value, out Vector3 target))
            {
                Vector3 offset = target - origin;
                offset.y = 0f;

                if (offset.sqrMagnitude > MinAimDistance * MinAimDistance)
                    return offset.normalized;
            }

            return aim.Direction;
        }

        private void Launch(EcsWorld world, StaffConfig config, int owner, Vector3 spawn, Vector3 direction, float height, float damage, float delta)
        {
            int fireball = world.NewEntity();

            ref Projectile projectile = ref _projectiles.Value.Add(fireball);
            projectile.Source = world.PackEntity(owner);
            projectile.LastPosition = spawn;
            projectile.Damage = damage;
            projectile.Radius = config.FireballRadius;
            projectile.RemainingTicks = Mathf.Max(1, Mathf.RoundToInt(config.FireballRange / (config.FireballSpeed * delta)));

            ref Fireball data = ref _fireballs.Value.Add(fireball);
            data.Config = config;
            data.BlastScale = 1f;
            data.Lift = height;
            _positions.Value.Add(fireball).Value = spawn;
            _velocities.Value.Add(fireball).Value = direction * config.FireballSpeed;
            _facings.Value.Add(fireball).Value = direction;
            _notIndexed.Value.Add(fireball);
            IView view = _viewFactory.Value.Create(config.FireballPrefab, spawn);
            _views.Value.Add(fireball).Value = view;

            if (view == null)
                return;

            view.SetLift(height);
            view.SetLift(height);
        }

        private void Cue(int owner, string trigger)
        {
            if (_views.Value.Has(owner) == false || _views.Value.Get(owner).Value == null)
                return;

            _views.Value.Get(owner).Value.PlayAttack(trigger, 1f);
        }
    }
}

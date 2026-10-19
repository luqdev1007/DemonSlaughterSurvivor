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

        private readonly EcsCustomInject<IViewFactory> _viewFactory = default;
        private readonly EcsCustomInject<SimulationClock> _clock = default;

        public void Run(IEcsSystems systems)
        {
            EcsWorld world = _world.Value;
            float delta = _clock.Value.Delta;

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

                Vector3 origin = _positions.Value.Get(owner).Value;
                ref TorsoAim aim = ref _aims.Value.Get(owner);
                Vector3 forward = ResolveDirection(world, in aim, origin);
                Vector3 muzzle = config.Cast(variant).Muzzle;
                Vector3 right = new Vector3(forward.z, 0f, -forward.x);
                Vector3 spawn = origin + right * muzzle.x + forward * muzzle.z;
                Vector3 direction = ResolveDirection(world, in aim, spawn);

                Launch(world, config, owner, spawn, direction, muzzle.y, _damages.Value.Get(staff).Value, delta);
            }
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

            _fireballs.Value.Add(fireball).Config = config;
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

using Game.Configs;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class StartStaffCastSystem : IEcsRunSystem
    {
        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<Staff, WeaponReady, WeaponCooldown, AttackSpeed, OwnerLink>, Exc<StaffCast>> _staffs = default;

        private readonly EcsPoolInject<Staff> _staffPool = default;
        private readonly EcsPoolInject<WeaponReady> _readies = default;
        private readonly EcsPoolInject<WeaponCooldown> _cooldowns = default;
        private readonly EcsPoolInject<AttackSpeed> _attackSpeeds = default;
        private readonly EcsPoolInject<OwnerLink> _ownerLinks = default;
        private readonly EcsPoolInject<StaffCast> _casts = default;
        private readonly EcsPoolInject<TorsoAim> _aims = default;
        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<Dead> _dead = default;
        private readonly EcsPoolInject<View> _views = default;

        private readonly EcsCustomInject<SimulationClock> _clock = default;

        public void Run(IEcsSystems systems)
        {
            EcsWorld world = _world.Value;
            float delta = _clock.Value.Delta;

            foreach (int staff in _staffs.Value)
            {
                ref WeaponReady ready = ref _readies.Value.Get(staff);

                if (ready.Remaining > 0f)
                    continue;

                if (_ownerLinks.Value.Get(staff).Owner.Unpack(world, out int owner) == false || _dead.Value.Has(owner))
                    continue;

                if (_aims.Value.Has(owner) == false)
                    continue;

                StaffConfig config = _staffPool.Value.Get(staff).Config;
                ref TorsoAim aim = ref _aims.Value.Get(owner);

                if (HasShot(world, in aim, _positions.Value.Get(owner).Value, config) == false)
                    continue;

                if (Vector3.Angle(aim.Direction, aim.Desired) > config.AimToleranceDegrees)
                    continue;

                float attackSpeed = _attackSpeeds.Value.Get(staff).Value;
                int ticks = Mathf.Max(1, Mathf.RoundToInt(config.CastSeconds / attackSpeed / delta));
                float playbackSpeed = config.ReleaseClipSeconds / (ticks * delta);

                _casts.Value.Add(staff).RemainingTicks = ticks;

                ready.Remaining = _cooldowns.Value.Get(staff).Value / attackSpeed;

                if (_views.Value.Has(owner) && _views.Value.Get(owner).Value != null)
                    _views.Value.Get(owner).Value.PlayAttack(config.CastTrigger, playbackSpeed);
            }
        }

        private bool HasShot(EcsWorld world, in TorsoAim aim, Vector3 position, StaffConfig config)
        {
            if (aim.Manual)
                return true;

            if (aim.HasTarget == false)
                return false;

            if (RangedTarget.TryFindLiving(world, aim.Target, _positions.Value, _dead.Value, out Vector3 target) == false)
                return false;

            return RangedTarget.IsWithin(position, target, config.Range);
        }
    }
}

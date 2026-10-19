using Game.Configs;
using Game.Core;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class StartStaffCastSystem : IEcsRunSystem
    {
        public const string StreamId = "stream.staff-cast";

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
        private readonly EcsPoolInject<StaffCastPick> _picks = default;

        private readonly EcsCustomInject<SimulationClock> _clock = default;
        private readonly EcsCustomInject<RunContext> _context = default;

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
                int variant = Pick(staff, config);
                StaffCastVariant cast = config.Cast(variant);
                float playbackSpeed = cast.ReleaseClipSeconds / (ticks * delta);

                ref StaffCast staffCast = ref _casts.Value.Add(staff);
                staffCast.RemainingTicks = ticks;
                staffCast.Variant = variant;

                ready.Remaining = _cooldowns.Value.Get(staff).Value / attackSpeed;

                if (_views.Value.Has(owner) && _views.Value.Get(owner).Value != null)
                    _views.Value.Get(owner).Value.PlayAttack(cast.Trigger, playbackSpeed);
            }
        }

        private int Pick(int staff, StaffConfig config)
        {
            if (_picks.Value.Has(staff) == false)
                _picks.Value.Add(staff).State = SimulationRandom.StreamState(_context.Value.Seed, StreamId);

            ref StaffCastPick pick = ref _picks.Value.Get(staff);

            int allowed = 0;

            for (int index = 0; index < config.CastCount; index++)
            {
                if (IsAllowed(in pick, config.Cast(index)))
                    allowed++;
            }

            bool anyHand = allowed == 0;

            if (anyHand)
                allowed = config.CastCount;

            int chosen = Mathf.Min(allowed - 1, (int)(SimulationRandom.NextUnit(ref pick.State) * allowed));
            int variant = 0;

            for (int index = 0; index < config.CastCount; index++)
            {
                if (anyHand == false && IsAllowed(in pick, config.Cast(index)) == false)
                    continue;

                if (chosen == 0)
                {
                    variant = index;

                    break;
                }

                chosen--;
            }

            pick.HasLast = true;
            pick.LastLeft = config.Cast(variant).LeftHand;

            return variant;
        }

        private static bool IsAllowed(in StaffCastPick pick, StaffCastVariant cast)
        {
            return pick.HasLast == false || cast.LeftHand != pick.LastLeft;
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

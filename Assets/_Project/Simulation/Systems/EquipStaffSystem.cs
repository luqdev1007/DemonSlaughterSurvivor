using Game.Configs;
using Game.Core;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

namespace Game.Simulation.Systems
{
    public sealed class EquipStaffSystem : IEcsInitSystem
    {
        private const float BaseAttackSpeed = 1f;

        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<Player>> _players = default;

        private readonly EcsPoolInject<Staff> _staffs = default;
        private readonly EcsPoolInject<WeaponDamage> _damages = default;
        private readonly EcsPoolInject<WeaponCooldown> _cooldowns = default;
        private readonly EcsPoolInject<AttackSpeed> _attackSpeeds = default;
        private readonly EcsPoolInject<WeaponReady> _readies = default;
        private readonly EcsPoolInject<OwnerLink> _ownerLinks = default;

        private readonly EcsCustomInject<RunContext> _context = default;
        private readonly EcsCustomInject<IContentRegistry> _content = default;
        private readonly EcsCustomInject<StatModifiers> _statModifiers = default;

        public void Init(IEcsSystems systems)
        {
            StaffConfig config = _content.Value.Get<CharacterConfig>(_context.Value.CharacterId).Staff;

            if (config == null)
                return;

            config.Validate();

            EcsWorld world = _world.Value;

            foreach (int player in _players.Value)
            {
                int staff = world.NewEntity();

                _staffs.Value.Add(staff).Config = config;

                ref WeaponDamage damage = ref _damages.Value.Add(staff);
                damage.Base = config.Damage;
                damage.Value = config.Damage;

                ref WeaponCooldown cooldown = ref _cooldowns.Value.Add(staff);
                cooldown.Base = config.CooldownSeconds;
                cooldown.Value = config.CooldownSeconds;

                ref AttackSpeed attackSpeed = ref _attackSpeeds.Value.Add(staff);
                attackSpeed.Base = BaseAttackSpeed;
                attackSpeed.Value = BaseAttackSpeed;

                _readies.Value.Add(staff).Remaining = 0f;
                _ownerLinks.Value.Add(staff).Owner = world.PackEntity(player);

                _statModifiers.Value.MarkDirty(staff);
            }
        }
    }
}

using Game.Configs;
using Game.Core;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using System;

namespace Game.Simulation.Systems
{
    public sealed class EquipStartingWeaponSystem : IEcsInitSystem
    {
        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<Player>> _players = default;

        private readonly EcsPoolInject<Weapon> _weapons = default;
        private readonly EcsPoolInject<WeaponDamage> _damages = default;
        private readonly EcsPoolInject<WeaponCooldown> _cooldowns = default;
        private readonly EcsPoolInject<WeaponReady> _readies = default;
        private readonly EcsPoolInject<SwingRandom> _randoms = default;
        private readonly EcsPoolInject<OwnerLink> _ownerLinks = default;

        private readonly EcsCustomInject<RunContext> _context = default;
        private readonly EcsCustomInject<IContentRegistry> _content = default;
        private readonly EcsCustomInject<StatModifiers> _statModifiers = default;

        public void Init(IEcsSystems systems)
        {
            CharacterConfig character = _content.Value.Get<CharacterConfig>(_context.Value.CharacterId);
            WeaponConfig config = character.StartingWeapon;

            if (config == null)
                return;

            Validate(config);

            EcsWorld world = _world.Value;

            foreach (int player in _players.Value)
            {
                int weapon = world.NewEntity();

                ref Weapon component = ref _weapons.Value.Add(weapon);
                component.Config = config;

                ref WeaponDamage damage = ref _damages.Value.Add(weapon);
                damage.Base = config.Damage;
                damage.Value = config.Damage;

                ref WeaponCooldown cooldown = ref _cooldowns.Value.Add(weapon);
                cooldown.Base = config.CooldownSeconds;
                cooldown.Value = config.CooldownSeconds;

                ref WeaponReady ready = ref _readies.Value.Add(weapon);
                ready.Remaining = 0f;

                ref SwingRandom random = ref _randoms.Value.Add(weapon);
                random.State = SimulationRandom.StreamState(_context.Value.Seed, config.Id);

                ref OwnerLink link = ref _ownerLinks.Value.Add(weapon);
                link.Owner = world.PackEntity(player);

                _statModifiers.Value.MarkDirty(weapon);
            }
        }

        private static void Validate(WeaponConfig config)
        {
            if (string.IsNullOrEmpty(config.Id))
                throw new InvalidOperationException($"{nameof(WeaponConfig)} '{config.name}' has no id.");

            if (config.CooldownSeconds <= 0f)
                throw new InvalidOperationException(
                    $"{nameof(WeaponConfig)} '{config.Id}' has cooldown {config.CooldownSeconds}; it must be positive.");

            if (config.VariantCount == 0)
                throw new InvalidOperationException($"{nameof(WeaponConfig)} '{config.Id}' has no swing variants.");

            if (config.TriggerCoverage.IsBaked == false || config.TriggerCoverage.CoveredCount == 0)
                throw new InvalidOperationException(
                    $"{nameof(WeaponConfig)} '{config.Id}' has an empty trigger coverage, so it would never swing. " +
                    "Run Game/Bake Weapon Swings.");

            for (int index = 0; index < config.VariantCount; index++)
            {
                SwingVariant variant = config.Variant(index);
                string label = $"{nameof(WeaponConfig)} '{config.Id}' variant '{variant.AnimatorTrigger}'";

                if (variant.Bake == null || variant.Bake.SampleCount == 0)
                    throw new InvalidOperationException($"{label} is not baked. Run Game/Bake Weapon Swings.");

                if (variant.PlaybackSpeed <= 0f)
                    throw new InvalidOperationException($"{label} has playback speed {variant.PlaybackSpeed}; it must be positive.");

                if (variant.WindowEnd <= variant.WindowStart)
                    throw new InvalidOperationException($"{label} has an empty hit window.");

                bool halfAngleValid = variant.HalfAngleDegrees > 0f
                    && (variant.HalfAngleDegrees <= 90f || variant.HalfAngleDegrees >= SwingGeometry.FullCircleHalfAngle);

                if (halfAngleValid == false)
                    throw new InvalidOperationException(
                        $"{label} has half angle {variant.HalfAngleDegrees}. Only (0, 90] or {SwingGeometry.FullCircleHalfAngle} and above are supported: " +
                        "a wider but partial sector is not convex and the blade clip would be wrong.");
            }
        }
    }
}

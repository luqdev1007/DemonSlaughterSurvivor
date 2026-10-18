using Game.Configs;
using Game.Core;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class StartBerserkSystem : IEcsInitSystem, IEcsRunSystem
    {
        public const string SourceId = "ult.berserk";

        private readonly EcsFilterInject<Inc<Player, UltimateRequest, UltimateCharge>, Exc<BerserkMode, Dead, Dashing, SpecialAttack>> _filter = default;

        private readonly EcsPoolInject<UltimateRequest> _requests = default;
        private readonly EcsPoolInject<UltimateCharge> _charges = default;
        private readonly EcsPoolInject<BerserkMode> _modes = default;
        private readonly EcsPoolInject<PoweringUp> _poweringUps = default;
        private readonly EcsPoolInject<Invulnerable> _invulnerables = default;

        private readonly EcsCustomInject<SimulationClock> _clock = default;
        private readonly EcsCustomInject<RunContext> _context = default;
        private readonly EcsCustomInject<IContentRegistry> _content = default;
        private readonly EcsCustomInject<StatModifiers> _statModifiers = default;

        private BerserkAbilityConfig _config;

        public void Init(IEcsSystems systems)
        {
            CharacterConfig character = _content.Value.Get<CharacterConfig>(_context.Value.CharacterId);

            _config = character.Ultimate;

            if (_config == null)
                return;

            Validate(_config);
        }

        public void Run(IEcsSystems systems)
        {
            if (_config == null)
                return;

            foreach (int hero in _filter.Value)
            {
                ref UltimateCharge charge = ref _charges.Value.Get(hero);

                if (charge.Value < charge.Max)
                    continue;

                int ticks = Mathf.Max(1, Mathf.RoundToInt(_config.DurationSeconds / _clock.Value.Delta));

                ref BerserkMode mode = ref _modes.Value.Add(hero);
                mode.RemainingTicks = ticks;
                mode.TotalTicks = ticks;

                charge.Value = charge.Max;

                IReadOnlyList<StatModifierEntry> modifiers = _config.Modifiers;

                for (int index = 0; index < modifiers.Count; index++)
                {
                    StatModifierEntry entry = modifiers[index];

                    _statModifiers.Value.Add(hero, entry.Stat, entry.Op, entry.Value, SourceId);
                }

                StartPowerUp(hero);

                _requests.Value.Del(hero);
            }
        }

        private void StartPowerUp(int hero)
        {
            if (_config.PowerUpSeconds <= 0f)
                return;

            int ticks = Mathf.Max(1, Mathf.RoundToInt(_config.PowerUpSeconds / _clock.Value.Delta));

            ref PoweringUp powerUp = ref _poweringUps.Value.Add(hero);
            powerUp.RemainingTicks = ticks;
            powerUp.TotalTicks = ticks;

            Invulnerability.Grant(_invulnerables.Value, hero, ticks + Invulnerability.TicksSpentBeforeDamage);
        }

        private static void Validate(BerserkAbilityConfig config)
        {
            if (config.DurationSeconds <= 0f)
                throw new InvalidOperationException(
                    $"{nameof(BerserkAbilityConfig)} '{config.Id}' has duration {config.DurationSeconds}; it must be positive.");

            if (config.PowerUpSeconds < 0f)
                throw new InvalidOperationException(
                    $"{nameof(BerserkAbilityConfig)} '{config.Id}' has power-up {config.PowerUpSeconds}; it must be zero or positive.");

            if (config.PowerUpSeconds >= config.DurationSeconds)
                throw new InvalidOperationException(
                    $"{nameof(BerserkAbilityConfig)} '{config.Id}' has power-up {config.PowerUpSeconds} s, not shorter than the mode " +
                    $"({config.DurationSeconds} s); the stance would eat the whole ultimate.");

            IReadOnlyList<StatModifierEntry> modifiers = config.Modifiers;

            if (modifiers == null)
                return;

            for (int index = 0; index < modifiers.Count; index++)
            {
                StatModifierEntry entry = modifiers[index];

                if (entry == null)
                    throw new InvalidOperationException($"{nameof(BerserkAbilityConfig)} '{config.Id}' has an empty modifier slot {index}.");

                if (entry.Stat == StatId.MaxHealth)
                    throw new InvalidOperationException(
                        $"{nameof(BerserkAbilityConfig)} '{config.Id}' modifies {nameof(StatId.MaxHealth)}. " +
                        "Temporary effects must not target it: MaxHealth grows by a delta, so every activation would heal.");

                for (int other = 0; other < index; other++)
                {
                    if (modifiers[other] != null && modifiers[other].Stat == entry.Stat)
                        throw new InvalidOperationException(
                            $"{nameof(BerserkAbilityConfig)} '{config.Id}' lists {entry.Stat} twice. " +
                            $"Both would share the source '{SourceId}', so the second would silently replace the first.");
                }
            }
        }
    }
}

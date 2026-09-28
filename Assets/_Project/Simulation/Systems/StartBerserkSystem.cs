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

        private readonly EcsFilterInject<Inc<Player, UltimateRequest, UltimateCharge>, Exc<BerserkMode, Dead>> _filter = default;

        private readonly EcsPoolInject<UltimateRequest> _requests = default;
        private readonly EcsPoolInject<UltimateCharge> _charges = default;
        private readonly EcsPoolInject<BerserkMode> _modes = default;

        private readonly EcsCustomInject<SimulationClock> _clock = default;
        private readonly EcsCustomInject<RunContext> _context = default;
        private readonly EcsCustomInject<IContentRegistry> _content = default;
        private readonly EcsCustomInject<StatModifiers> _statModifiers = default;

        private BerserkAbilityConfig _config;

        public void Init(IEcsSystems systems)
        {
            CharacterConfig character = _content.Value.Get<CharacterConfig>(_context.Value.CharacterId);

            _config = character.Ultimate;

            Validate(character, _config);
        }

        public void Run(IEcsSystems systems)
        {
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

                _requests.Value.Del(hero);
            }
        }

        private static void Validate(CharacterConfig character, BerserkAbilityConfig config)
        {
            if (config == null)
                throw new InvalidOperationException(
                    $"{nameof(CharacterConfig)} '{character.Id}' has no ultimate assigned, " +
                    $"but {nameof(StartBerserkSystem)} is registered and reads its duration and modifiers.");

            if (config.DurationSeconds <= 0f)
                throw new InvalidOperationException(
                    $"{nameof(BerserkAbilityConfig)} '{config.Id}' has duration {config.DurationSeconds}; it must be positive.");

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

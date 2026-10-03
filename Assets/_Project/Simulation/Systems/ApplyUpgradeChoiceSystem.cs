using Game.Configs;
using Game.Core;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using System;
using System.Collections.Generic;

namespace Game.Simulation.Systems
{
    public sealed class ApplyUpgradeChoiceSystem : IEcsRunSystem
    {
        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<Player, PendingChoice, PendingLevelUps>> _choosing = default;
        private readonly EcsFilterInject<Inc<TakenPerk>> _taken = default;

        private readonly EcsPoolInject<PendingChoice> _choices = default;
        private readonly EcsPoolInject<PendingLevelUps> _pending = default;
        private readonly EcsPoolInject<TakenPerk> _takenPool = default;

        private readonly EcsCustomInject<IContentRegistry> _content = default;
        private readonly EcsCustomInject<StatModifiers> _statModifiers = default;
        private readonly EcsCustomInject<UpgradeChoiceGate> _gate = default;
        private readonly EcsCustomInject<IUpgradeChoiceInput> _input = default;
        private readonly EcsCustomInject<IUpgradeChoiceSink> _sink = default;

        public void Run(IEcsSystems systems)
        {
            if (_input.Value.TryConsume(out int index) == false)
                return;

            foreach (int hero in _choosing.Value)
            {
                ref PendingChoice choice = ref _choices.Value.Get(hero);

                if (index < 0 || index >= choice.Count)
                    continue;

                Take(hero, Pick(choice, index));

                _choices.Value.Del(hero);
                _pending.Value.Get(hero).Count--;

                _sink.Value.Hide();
                _gate.Value.Release();
            }
        }

        private void Take(int hero, string perkId)
        {
            PerkConfig perk = _content.Value.Get<PerkConfig>(perkId);
            EcsWorld world = _world.Value;

            int entity = TakenPerkLookup.Find(world, _taken.Value, _takenPool.Value, hero, perkId);

            if (entity < 0)
            {
                entity = world.NewEntity();

                ref TakenPerk created = ref _takenPool.Value.Add(entity);
                created.Owner = world.PackEntity(hero);
                created.PerkId = perkId;
                created.Level = 0;
            }

            ref TakenPerk taken = ref _takenPool.Value.Get(entity);

            int level = taken.Level + 1;

            if (level > perk.MaxLevel)
                throw new InvalidOperationException(
                    $"{nameof(PerkConfig)} '{perkId}' was chosen at level {taken.Level}, which is already its max level {perk.MaxLevel}. " +
                    $"{nameof(OfferUpgradesSystem)} must not offer a perk at its max level.");

            _statModifiers.Value.RemoveBySource(hero, perkId);

            IReadOnlyList<StatModifierEntry> modifiers = perk.Level(level).Modifiers;

            for (int modifier = 0; modifier < modifiers.Count; modifier++)
            {
                StatModifierEntry entry = modifiers[modifier];

                _statModifiers.Value.Add(hero, entry.Stat, entry.Op, entry.Value, perkId);
            }

            taken.Level = level;
        }

        private static string Pick(in PendingChoice choice, int index)
        {
            switch (index)
            {
                case 0:
                    return choice.First;
                case 1:
                    return choice.Second;
                default:
                    return choice.Third;
            }
        }
    }
}

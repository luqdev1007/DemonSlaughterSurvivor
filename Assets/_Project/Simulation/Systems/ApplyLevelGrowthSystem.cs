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
    public sealed class ApplyLevelGrowthSystem : IEcsRunSystem
    {
        public const string ModifierSource = "growth.level";

        private readonly EcsFilterInject<Inc<Player, Experience>, Exc<Dead>> _heroes = default;

        private readonly EcsPoolInject<Experience> _experiences = default;
        private readonly EcsPoolInject<LevelGrowthApplied> _applied = default;

        private readonly EcsCustomInject<IContentRegistry> _content = default;
        private readonly EcsCustomInject<RunContext> _context = default;
        private readonly EcsCustomInject<StatModifiers> _statModifiers = default;

        private IReadOnlyList<StatModifierEntry> _growth;

        public void Run(IEcsSystems systems)
        {
            foreach (int hero in _heroes.Value)
            {
                int level = _experiences.Value.Get(hero).Level;

                if (_applied.Value.Has(hero) && _applied.Value.Get(hero).Level == level)
                    continue;

                if (_applied.Value.Has(hero) == false)
                    _applied.Value.Add(hero);

                _applied.Value.Get(hero).Level = level;

                IReadOnlyList<StatModifierEntry> growth = ResolveGrowth();
                int levels = Mathf.Max(0, level - 1);

                for (int index = 0; index < growth.Count; index++)
                {
                    StatModifierEntry entry = growth[index];

                    _statModifiers.Value.Add(hero, entry.Stat, entry.Op, Value(entry, levels), ModifierSource);
                }
            }
        }

        public static float Value(StatModifierEntry entry, int levels)
        {
            if (entry.Op != StatOp.More)
                return entry.Value * levels;

            if (entry.Value <= -1f)
                throw new InvalidOperationException(
                    $"Level growth of {entry.Stat} is More {entry.Value}: a per-level More of -1 or below zeroes or flips the stat at level 2.");

            return Mathf.Pow(1f + entry.Value, levels) - 1f;
        }

        private IReadOnlyList<StatModifierEntry> ResolveGrowth()
        {
            if (_growth == null)
                _growth = _content.Value.Get<CharacterConfig>(_context.Value.CharacterId).LevelGrowth;

            return _growth;
        }
    }
}

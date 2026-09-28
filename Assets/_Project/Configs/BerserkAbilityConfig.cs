using Game.Core;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(fileName = "BerserkAbilityConfig", menuName = "Game/Content/Berserk Ability Config")]
    public sealed class BerserkAbilityConfig : ContentConfig
    {
        [SerializeField] private float _durationSeconds = 8f;
        [SerializeField] private StatModifierEntry[] _modifiers;

        public float DurationSeconds => _durationSeconds;

        public IReadOnlyList<StatModifierEntry> Modifiers => _modifiers;
    }

    [Serializable]
    public sealed class StatModifierEntry
    {
        [SerializeField] private StatId _stat;
        [SerializeField] private StatOp _op;
        [SerializeField] private float _value;

        public StatId Stat => _stat;

        public StatOp Op => _op;

        public float Value => _value;
    }
}

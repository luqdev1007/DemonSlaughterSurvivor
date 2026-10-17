using Game.Configs;
using System.Collections.Generic;
using UnityEngine;

namespace Game.UI
{
    public sealed class PerkCardContent
    {
        public readonly List<Sprite> ComboIcons = new List<Sprite>();

        public string Hotkey;
        public string Title;
        public Sprite Icon;
        public Rarity Rarity;
        public int NextLevel;
        public int MaxLevel;
        public string Description;
        public string Stats;
        public string Requires;

        public bool IsCombo => ComboIcons.Count > 0;
    }
}

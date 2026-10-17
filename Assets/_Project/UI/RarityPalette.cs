using Game.Configs;
using System;
using UnityEngine;

namespace Game.UI
{
    [Serializable]
    public sealed class RarityPalette
    {
        [SerializeField] private Color[] _colors =
        {
            new Color(0.82f, 0.82f, 0.82f),
            new Color(0.30f, 0.62f, 1f),
            new Color(0.70f, 0.38f, 1f),
            new Color(1f, 0.64f, 0.18f)
        };

        [SerializeField] private string[] _names = { "Обычный", "Редкий", "Эпический", "Легендарный" };

        public Color Color(Rarity rarity)
        {
            int index = (int)rarity;

            return index >= 0 && index < _colors.Length ? _colors[index] : UnityEngine.Color.white;
        }

        public string Name(Rarity rarity)
        {
            int index = (int)rarity;

            return index >= 0 && index < _names.Length ? _names[index] : rarity.ToString();
        }
    }
}

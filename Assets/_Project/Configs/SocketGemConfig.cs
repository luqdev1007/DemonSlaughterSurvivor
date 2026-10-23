using System.Collections.Generic;
using UnityEngine;

namespace Game.Configs
{
    public enum GemColor
    {
        Red = 0,
        Blue = 1,
        Green = 2
    }

    [CreateAssetMenu(fileName = "SocketGemConfig", menuName = "Game/Meta/Socket Gem Config")]
    public sealed class SocketGemConfig : ContentConfig
    {
        [SerializeField] private GemColor _color;
        [SerializeField] private string _displayName;
        [SerializeField, TextArea] private string _description;
        [SerializeField] private int _menuOrder;
        [SerializeField] private Sprite _icon;
        [SerializeField] private MetaEffect _effect = new MetaEffect();

        public GemColor Color => _color;
        public string DisplayName => _displayName;
        public string Description => _description;
        public int MenuOrder => _menuOrder;
        public Sprite Icon => _icon;
        public MetaEffect Effect => _effect ?? new MetaEffect();

        public void Validate(List<string> errors)
        {
            Effect.Validate(Id, errors);
        }
    }
}

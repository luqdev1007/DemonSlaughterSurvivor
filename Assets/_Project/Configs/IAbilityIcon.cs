using Game.Core;
using UnityEngine;

namespace Game.Configs
{
    public interface IAbilityIcon : IContentEntry
    {
        Sprite Icon { get; }
    }
}

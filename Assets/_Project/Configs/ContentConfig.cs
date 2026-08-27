using Game.Core;
using UnityEngine;

namespace Game.Configs
{
    public abstract class ContentConfig : ScriptableObject, IContentEntry
    {
        [field: SerializeField] public string Id { get; private set; }
    }
}

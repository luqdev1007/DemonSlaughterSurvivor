using UnityEngine;

namespace Game.Configs
{
    public sealed class LevelConfig : ContentConfig
    {
        [field: SerializeField] public string SceneName { get; private set; }
    }
}

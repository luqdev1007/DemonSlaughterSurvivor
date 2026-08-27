using UnityEngine;

namespace Game.Configs
{
    /// <summary>
    /// Scenes of the boot flow. They are not content: no id in a save, no drop table, no DLC.
    /// Arena scenes live in LevelConfig and are reached through the content registry instead.
    /// </summary>
    [CreateAssetMenu(fileName = "ScenesConfig", menuName = "Game/Scenes Config")]
    public sealed class ScenesConfig : ScriptableObject
    {
        [SerializeField] private string _mainMenuScene;

        public string MainMenuScene => _mainMenuScene;
    }
}

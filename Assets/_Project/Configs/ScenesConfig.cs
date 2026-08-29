using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(fileName = "ScenesConfig", menuName = "Game/Scenes Config")]
    public sealed class ScenesConfig : ScriptableObject
    {
        [SerializeField] private string _mainMenuScene;

        public string MainMenuScene => _mainMenuScene;
    }
}

using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(fileName = "Level", menuName = "Game/Content/Level")]
    public sealed class LevelConfig : ContentConfig
    {
        [SerializeField] private string _sceneName;
        [SerializeField] private float _cameraYaw;

        public float CameraYaw => _cameraYaw;
        public string SceneName => _sceneName;
    }
}

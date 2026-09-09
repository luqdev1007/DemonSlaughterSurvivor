using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(fileName = "Level", menuName = "Game/Content/Level")]
    public sealed class LevelConfig : ContentConfig
    {
        [SerializeField] private string _sceneName;

        [Header("Camera")]
        [SerializeField] private GameObject _cameraRigPrefab;
        [SerializeField] private float _cameraYaw;
        [SerializeField] private float _cameraPitch = 50f;
        [SerializeField] private float _cameraDistance = 16f;

        [Header("Enemies")]
        [SerializeField] private EnemyConfig _startingEnemy;
        [SerializeField] private int _startingEnemyCount;
        [SerializeField] private float _enemySpawnRadius = 12f;

        public string SceneName => _sceneName;

        public GameObject CameraRigPrefab => _cameraRigPrefab;

        public float CameraYaw => _cameraYaw;

        public float CameraPitch => _cameraPitch;

        public float CameraDistance => _cameraDistance;

        public EnemyConfig StartingEnemy => _startingEnemy;

        public int StartingEnemyCount => _startingEnemyCount;

        public float EnemySpawnRadius => _enemySpawnRadius;
    }
}

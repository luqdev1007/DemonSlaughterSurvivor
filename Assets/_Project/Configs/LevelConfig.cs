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

        [Header("Arena")]
        [SerializeField] private float _arenaRadius = 30f;
        [SerializeField] private float _spatialCellSize = 1f;

        [Header("Waves")]
        [SerializeField] private WaveTimelineConfig _waves;

        [Header("Feedback")]
        [SerializeField] private FeedbackConfig _feedback;
        [SerializeField] private GameObject _hudPrefab;

        public string SceneName => _sceneName;

        public GameObject CameraRigPrefab => _cameraRigPrefab;

        public float CameraYaw => _cameraYaw;

        public float CameraPitch => _cameraPitch;

        public float CameraDistance => _cameraDistance;

        public float ArenaRadius => _arenaRadius;

        public float SpatialCellSize => _spatialCellSize;

        public WaveTimelineConfig Waves => _waves;

        public FeedbackConfig Feedback => _feedback;

        public GameObject HudPrefab => _hudPrefab;
    }
}

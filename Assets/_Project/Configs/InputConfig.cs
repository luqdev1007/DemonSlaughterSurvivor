using Game.Core;
using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(fileName = "InputConfig", menuName = "Game/Input Config")]
    public sealed class InputConfig : ScriptableObject
    {
        [SerializeField] private float _bufferSeconds = 0.12f;
        [SerializeField] private ControlScheme _defaultScheme = ControlScheme.MouseRun;

        public float BufferSeconds => _bufferSeconds;

        public ControlScheme DefaultScheme => _defaultScheme;
    }
}

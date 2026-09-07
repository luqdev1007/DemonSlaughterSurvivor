using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(fileName = "InputConfig", menuName = "Game/Input Config")]
    public sealed class InputConfig : ScriptableObject
    {
        [SerializeField] private float _bufferSeconds = 0.12f;

        public float BufferSeconds => _bufferSeconds;
    }
}

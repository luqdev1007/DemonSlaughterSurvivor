using UnityEngine;

namespace Game.View
{
    public sealed class Turntable : MonoBehaviour
    {
        [SerializeField] private float _degreesPerSecond = 10f;

        private void Update()
        {
            transform.Rotate(0f, _degreesPerSecond * Time.unscaledDeltaTime, 0f, Space.World);
        }
    }
}

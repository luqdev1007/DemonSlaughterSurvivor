using UnityEngine;

namespace Game.View
{
    public sealed class SpinFx : MonoBehaviour
    {
        [SerializeField] private float _degreesPerSecond = 1440f;
        [SerializeField, Range(0f, 45f)] private float _tiltDegrees = 14f;
        [SerializeField] private float _wobbleDegreesPerSecond = 260f;

        private float _spin;
        private float _wobble;

        private void Update()
        {
            float deltaTime = Time.deltaTime;

            _spin = Mathf.Repeat(_spin + _degreesPerSecond * deltaTime, 360f);
            _wobble = Mathf.Repeat(_wobble + _wobbleDegreesPerSecond * deltaTime, 360f);

            transform.localRotation = Quaternion.AngleAxis(_wobble, Vector3.up)
                * Quaternion.AngleAxis(_tiltDegrees, Vector3.right)
                * Quaternion.AngleAxis(_spin - _wobble, Vector3.up);
        }
    }
}

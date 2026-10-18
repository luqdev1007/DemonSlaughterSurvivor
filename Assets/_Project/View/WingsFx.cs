using UnityEngine;

namespace Game.View
{
    public sealed class WingsFx : MonoBehaviour
    {
        [SerializeField] private Transform[] _wings;
        [SerializeField, Min(0.01f)] private float _openSeconds = 0.12f;
        [SerializeField, Min(0.01f)] private float _closeSeconds = 0.25f;
        [SerializeField, Range(0f, 90f)] private float _flapDegrees = 18f;
        [SerializeField, Min(0f)] private float _flapFrequency = 6f;

        private Vector3[] _openScales;
        private Quaternion[] _restRotations;
        private float _openness;
        private bool _showing;

        public void Show(bool value)
        {
            _showing = value;

            if (value)
                SetVisible(true);
        }

        public void HideImmediate()
        {
            _showing = false;
            _openness = 0f;
            Apply(0f);
            SetVisible(false);
        }

        private void Awake()
        {
            if (_wings == null)
                _wings = new Transform[0];

            _openScales = new Vector3[_wings.Length];
            _restRotations = new Quaternion[_wings.Length];

            for (int index = 0; index < _wings.Length; index++)
            {
                _openScales[index] = _wings[index].localScale;
                _restRotations[index] = _wings[index].localRotation;
            }

            HideImmediate();
        }

        private void LateUpdate()
        {
            float target = _showing ? 1f : 0f;

            if (Mathf.Approximately(_openness, target) && _showing == false)
                return;

            float seconds = _showing ? _openSeconds : _closeSeconds;
            _openness = Mathf.MoveTowards(_openness, target, Time.deltaTime / seconds);

            Apply(_openness);

            if (_showing == false && _openness <= 0f)
                SetVisible(false);
        }

        private void Apply(float openness)
        {
            if (_openScales == null)
                return;

            float flap = Mathf.Sin(Time.time * _flapFrequency * Mathf.PI * 2f) * _flapDegrees * openness;

            for (int index = 0; index < _wings.Length; index++)
            {
                float side = index % 2 == 0 ? 1f : -1f;

                _wings[index].localScale = _openScales[index] * openness;
                _wings[index].localRotation = _restRotations[index] * Quaternion.AngleAxis(flap * side, Vector3.up);
            }
        }

        private void SetVisible(bool visible)
        {
            for (int index = 0; index < _wings.Length; index++)
            {
                if (_wings[index].gameObject.activeSelf != visible)
                    _wings[index].gameObject.SetActive(visible);
            }
        }
    }
}

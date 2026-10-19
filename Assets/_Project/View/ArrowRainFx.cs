using System;
using UnityEngine;

namespace Game.View
{
    public sealed class ArrowRainFx : MonoBehaviour
    {
        [SerializeField] private Transform[] _arrows = Array.Empty<Transform>();
        [SerializeField] private GameObject[] _streaks = Array.Empty<GameObject>();
        [SerializeField, Min(0.02f)] private float _fallSeconds = 0.4f;
        [SerializeField, Min(0.02f)] private float _flightSeconds = 0.3f;
        [SerializeField, Min(0f)] private float _landJitterSeconds = 0.12f;
        [SerializeField, Min(0f)] private float _startHeight = 9f;
        [SerializeField, Min(0f)] private float _spread = 1.1f;
        [SerializeField, Min(0f)] private float _driftBack = 1.5f;
        [SerializeField, Min(0f)] private float _buryDepth = 0.25f;
        [SerializeField, Range(0f, 45f)] private float _tiltDegrees = 14f;

        private Vector3[] _landings;
        private float[] _landTimes;
        private Quaternion[] _rotations;
        private Vector3 _drift;
        private float _time;
        private System.Random _random;

        private void Awake()
        {
            _landings = new Vector3[_arrows.Length];
            _landTimes = new float[_arrows.Length];
            _rotations = new Quaternion[_arrows.Length];
            _random = new System.Random(GetInstanceID());
        }

        private void OnEnable()
        {
            _time = 0f;

            float yaw = Next() * 360f;
            _drift = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward * _driftBack;

            for (int index = 0; index < _arrows.Length; index++)
            {
                float angle = Next() * Mathf.PI * 2f;
                float distance = Mathf.Sqrt(Next()) * _spread;

                _landings[index] = new Vector3(Mathf.Cos(angle) * distance, -_buryDepth, Mathf.Sin(angle) * distance);
                _landTimes[index] = _fallSeconds - Next() * _landJitterSeconds;

                Vector3 down = (-Vector3.up * _startHeight - _drift).normalized;
                Quaternion tilt = Quaternion.Euler((Next() - 0.5f) * _tiltDegrees, (Next() - 0.5f) * _tiltDegrees, 0f);
                _rotations[index] = Quaternion.LookRotation(down) * tilt;
            }

            Apply();
        }

        private void Update()
        {
            _time += Time.deltaTime;

            Apply();
        }

        private void Apply()
        {
            for (int index = 0; index < _arrows.Length; index++)
            {
                float progress = Mathf.Clamp01((_time - _landTimes[index] + _flightSeconds) / _flightSeconds);
                float eased = progress * progress;
                Vector3 start = _landings[index] + Vector3.up * _startHeight + _drift;
                Transform arrow = _arrows[index];

                arrow.localPosition = Vector3.LerpUnclamped(start, _landings[index], eased);
                arrow.localRotation = _rotations[index];

                if (index < _streaks.Length && _streaks[index] != null && _streaks[index].activeSelf != progress < 1f)
                    _streaks[index].SetActive(progress < 1f);
            }
        }

        private float Next()
        {
            return (float)_random.NextDouble();
        }
    }
}

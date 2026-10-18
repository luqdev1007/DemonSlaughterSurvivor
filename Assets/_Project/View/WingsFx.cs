using System;
using UnityEngine;

namespace Game.View
{
    public sealed class WingsFx : MonoBehaviour
    {
        private const int Leap = 1;
        private const int Hover = 2;
        private const int Fall = 3;
        private const int Hop = 4;
        private const int Dive = 5;

        [SerializeField] private Transform _model;
        [SerializeField] private Transform[] _right = Array.Empty<Transform>();
        [SerializeField] private Transform[] _left = Array.Empty<Transform>();

        [Header("Poses (yaw: + folds back, roll: + raises), per bone from shoulder to tip")]
        [SerializeField] private Vector2[] _folded = { new Vector2(70f, -35f), new Vector2(80f, -10f), new Vector2(70f, 0f), new Vector2(50f, 0f) };
        [SerializeField] private Vector2[] _spread = { new Vector2(0f, 10f), Vector2.zero, Vector2.zero, Vector2.zero };
        [SerializeField] private Vector2[] _windUp = { new Vector2(30f, 45f), new Vector2(10f, 10f), Vector2.zero, Vector2.zero };
        [SerializeField] private Vector2[] _forward = { new Vector2(-75f, -10f), new Vector2(-25f, 0f), new Vector2(-20f, 0f), new Vector2(-15f, 0f) };
        [SerializeField] private Vector2[] _down = { new Vector2(-10f, -65f), new Vector2(0f, -20f), new Vector2(0f, -10f), Vector2.zero };

        [Header("Timing")]
        [SerializeField, Min(0.01f)] private float _windUpSeconds = 0.08f;
        [SerializeField, Min(0.01f)] private float _strokeSeconds = 0.14f;
        [SerializeField, Min(0.01f)] private float _recoverSeconds = 0.25f;
        [SerializeField, Min(0.01f)] private float _appearSeconds = 0.1f;
        [SerializeField, Min(0.01f)] private float _vanishSeconds = 0.25f;
        [SerializeField, Min(1f)] private float _sharpness = 22f;

        [Header("Hover")]
        [SerializeField, Min(0f)] private float _flapFrequency = 2.2f;
        [SerializeField, Min(0f)] private float _flapDegrees = 18f;
        [SerializeField, Min(0f)] private float _rippleDegrees = 9f;
        [SerializeField, Range(0f, 1f)] private float _fallFold = 0.45f;

        private Quaternion[] _restRight;
        private Quaternion[] _restLeft;
        private Vector2[] _current;
        private Vector2[] _target;
        private Vector3 _modelScale = Vector3.one;
        private int _phase;
        private float _phaseTime;
        private float _visibility;

        public void SetPhase(int phase)
        {
            if (phase == _phase)
                return;

            _phase = phase;
            _phaseTime = 0f;

            if (phase != 0 && _model != null && _model.gameObject.activeSelf == false)
            {
                _model.gameObject.SetActive(true);
                Copy(_folded, _current);
            }
        }

        public void HideImmediate()
        {
            _phase = 0;
            _phaseTime = 0f;
            _visibility = 0f;

            if (_current != null)
                Copy(_folded, _current);

            if (_model != null)
            {
                _model.localScale = Vector3.zero;
                _model.gameObject.SetActive(false);
            }
        }

        private void Awake()
        {
            _restRight = Capture(_right);
            _restLeft = Capture(_left);
            _current = new Vector2[Mathf.Max(_right.Length, _left.Length)];
            _target = new Vector2[_current.Length];

            if (_model != null)
                _modelScale = _model.localScale;

            HideImmediate();
        }

        private void LateUpdate()
        {
            if (_model == null || _model.gameObject.activeSelf == false)
                return;

            float deltaTime = Time.deltaTime;
            _phaseTime += deltaTime;

            float targetVisibility = _phase != 0 ? 1f : 0f;
            float seconds = _phase != 0 ? _appearSeconds : _vanishSeconds;
            _visibility = Mathf.MoveTowards(_visibility, targetVisibility, deltaTime / seconds);
            _model.localScale = _modelScale * _visibility;

            if (_phase == 0 && _visibility <= 0f)
            {
                _model.gameObject.SetActive(false);

                return;
            }

            ResolveTarget();

            float blend = 1f - Mathf.Exp(-_sharpness * deltaTime);

            for (int index = 0; index < _current.Length; index++)
                _current[index] = Vector2.Lerp(_current[index], _target[index], blend);

            Apply(_right, _restRight, 1f);
            Apply(_left, _restLeft, -1f);
        }

        private void ResolveTarget()
        {
            switch (_phase)
            {
                case Leap:
                    Stroke(_forward);
                    break;

                case Hop:
                    Stroke(_down);
                    break;

                case Hover:
                    Copy(_spread, _target);
                    AddFlap(1f);
                    break;

                case Fall:
                    Mix(_spread, _folded, _fallFold);
                    AddFlap(0.3f);
                    break;

                default:
                    Copy(_folded, _target);
                    break;
            }
        }

        private void Stroke(Vector2[] strokeEnd)
        {
            float time = _phaseTime;

            if (time < _windUpSeconds)
            {
                Mix(_folded, _windUp, time / _windUpSeconds);

                return;
            }

            time -= _windUpSeconds;

            if (time < _strokeSeconds)
            {
                float progress = time / _strokeSeconds;
                Mix(_windUp, strokeEnd, progress * progress);

                return;
            }

            time -= _strokeSeconds;

            if (_phase == Hop)
            {
                Copy(strokeEnd, _target);

                return;
            }

            Mix(strokeEnd, _spread, Mathf.Clamp01(time / _recoverSeconds));
        }

        private void AddFlap(float strength)
        {
            float angle = _phaseTime * _flapFrequency * Mathf.PI * 2f;

            if (_target.Length > 0)
                _target[0].y += Mathf.Sin(angle) * _flapDegrees * strength;

            if (_target.Length > 2)
                _target[2].x += Mathf.Sin(angle - 0.8f) * _rippleDegrees * 0.6f * strength;

            if (_target.Length > 3)
                _target[3].y += Mathf.Sin(angle - 1.4f) * _rippleDegrees * strength;
        }

        private void Apply(Transform[] chain, Quaternion[] rest, float side)
        {
            Vector3 up = transform.up;
            Vector3 forward = transform.forward;

            for (int index = 0; index < chain.Length; index++)
                chain[index].localRotation = rest[index];

            for (int index = 0; index < chain.Length; index++)
            {
                Vector2 pose = _current[index];
                Quaternion yaw = Quaternion.AngleAxis(pose.x * side, up);
                Quaternion roll = Quaternion.AngleAxis(pose.y * side, forward);

                chain[index].rotation = yaw * roll * chain[index].rotation;
            }
        }

        private void Mix(Vector2[] from, Vector2[] to, float t)
        {
            for (int index = 0; index < _target.Length; index++)
                _target[index] = Vector2.LerpUnclamped(At(from, index), At(to, index), t);
        }

        private static void Copy(Vector2[] from, Vector2[] to)
        {
            for (int index = 0; index < to.Length; index++)
                to[index] = At(from, index);
        }

        private static Vector2 At(Vector2[] pose, int index)
        {
            return pose != null && index < pose.Length ? pose[index] : Vector2.zero;
        }

        private static Quaternion[] Capture(Transform[] chain)
        {
            Quaternion[] rest = new Quaternion[chain.Length];

            for (int index = 0; index < chain.Length; index++)
                rest[index] = chain[index].localRotation;

            return rest;
        }
    }
}

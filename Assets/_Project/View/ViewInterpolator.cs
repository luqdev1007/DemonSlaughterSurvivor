using Game.Core;
using System;
using UnityEngine;

namespace Game.View
{
    public sealed class ViewInterpolator : MonoBehaviour, IView
    {
        private const float TickSeconds = 1f / 60f;
        private const float FallSeconds = 0.4f;
        private const float FallDegrees = 90f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private static readonly int DeathTriggerId = Animator.StringToHash("Death");

        private static readonly Color FlashColor = new Color(1f, 0.35f, 0.35f, 1f);
        private static readonly Color FlashEmission = new Color(0.8f, 0.1f, 0.1f, 1f);

        private Vector3 _previousPosition;
        private Vector3 _currentPosition;
        private float _lastSyncTime;
        private bool _hasPosition;

        private Quaternion _rotation = Quaternion.identity;

        private Renderer[] _renderers;
        private Animator _animator;
        private MaterialPropertyBlock _flashBlock;
        private bool _hasDeathTrigger;
        private float _hitFlashSeconds;
        private float _blinkSeconds;
        private Action<ViewInterpolator> _onRetired;

        private bool _isFlashing;
        private float _flashEndTime;

        private bool _isBlinking;
        private bool _renderersVisible = true;

        private bool _isFalling;
        private float _fallStartTime;

        private bool _isRetiring;
        private float _retireTime;

        public Transform Transform => transform;

        internal void Configure(Animator animator, float hitFlashSeconds, float blinkSeconds, Action<ViewInterpolator> onRetired)
        {
            _animator = animator;
            _renderers = GetComponentsInChildren<Renderer>(true);
            _flashBlock = new MaterialPropertyBlock();
            _flashBlock.SetColor(BaseColorId, FlashColor);
            _flashBlock.SetColor(ColorId, FlashColor);
            _flashBlock.SetColor(EmissionColorId, FlashEmission);
            _hasDeathTrigger = HasTrigger(animator, DeathTriggerId);
            _hitFlashSeconds = hitFlashSeconds;
            _blinkSeconds = blinkSeconds;
            _onRetired = onRetired;
        }

        public void SetPosition(Vector3 position)
        {
            if (_hasPosition == false)
            {
                _previousPosition = position;
                _currentPosition = position;
                _lastSyncTime = Time.time;
                _hasPosition = true;

                transform.position = position;

                return;
            }

            _previousPosition = _currentPosition;
            _currentPosition = position;
            _lastSyncTime = Time.time;
        }

        public void ResetInterpolation()
        {
            _previousPosition = Vector3.zero;
            _currentPosition = Vector3.zero;
            _lastSyncTime = 0f;
            _hasPosition = false;
        }

        public void SetRotation(Quaternion rotation)
        {
            _rotation = rotation;

            transform.rotation = ResolveRotation();
        }

        public void PlayHit()
        {
            if (_hitFlashSeconds <= 0f)
                return;

            _isFlashing = true;
            _flashEndTime = Time.time + _hitFlashSeconds;

            ApplyFlash(true);
        }

        public void SetInvulnerable(bool value)
        {
            if (_isBlinking == value)
                return;

            _isBlinking = value;

            if (value == false)
                SetRenderersVisible(true);
        }

        public void PlayDeath()
        {
            if (_hasDeathTrigger)
            {
                _animator.SetTrigger(DeathTriggerId);

                return;
            }

            if (_isFalling)
                return;

            _isFalling = true;
            _fallStartTime = Time.time;
        }

        internal void BeginRetire(float seconds)
        {
            _isRetiring = true;
            _retireTime = Time.time + seconds;

            PlayDeath();
        }

        internal void ResetFeedback()
        {
            if (_isFlashing)
                ApplyFlash(false);

            _isFlashing = false;
            _isBlinking = false;
            _isFalling = false;
            _isRetiring = false;
            _rotation = Quaternion.identity;

            SetRenderersVisible(true);
        }

        private void Update()
        {
            float time = Time.time;

            if (_hasPosition)
                transform.position = ResolvePosition();

            if (_isFalling)
                transform.rotation = ResolveRotation();

            if (_isFlashing && time >= _flashEndTime)
            {
                _isFlashing = false;

                ApplyFlash(false);
            }

            if (_isBlinking && _blinkSeconds > 0f)
                SetRenderersVisible(Mathf.Repeat(time, _blinkSeconds) < _blinkSeconds * 0.5f);

            if (_isRetiring && time >= _retireTime)
            {
                _isRetiring = false;

                _onRetired?.Invoke(this);
            }
        }

        private Vector3 ResolvePosition()
        {
            float phase = Mathf.Clamp01((Time.time - _lastSyncTime) / TickSeconds);

            return Vector3.Lerp(_previousPosition, _currentPosition, phase);
        }

        private Quaternion ResolveRotation()
        {
            if (_isFalling == false)
                return _rotation;

            float progress = Mathf.Clamp01((Time.time - _fallStartTime) / FallSeconds);

            return _rotation * Quaternion.Euler(-FallDegrees * progress * progress, 0f, 0f);
        }

        private void ApplyFlash(bool on)
        {
            if (_renderers == null)
                return;

            for (int index = 0; index < _renderers.Length; index++)
            {
                Renderer target = _renderers[index];

                if (target == null)
                    continue;

                target.SetPropertyBlock(on ? _flashBlock : null);
            }
        }

        private void SetRenderersVisible(bool visible)
        {
            if (_renderersVisible == visible)
                return;

            _renderersVisible = visible;

            if (_renderers == null)
                return;

            for (int index = 0; index < _renderers.Length; index++)
            {
                Renderer target = _renderers[index];

                if (target == null)
                    continue;

                target.enabled = visible;
            }
        }

        private static bool HasTrigger(Animator animator, int id)
        {
            if (animator == null || animator.runtimeAnimatorController == null)
                return false;

            AnimatorControllerParameter[] parameters = animator.parameters;

            for (int index = 0; index < parameters.Length; index++)
            {
                if (parameters[index].nameHash == id && parameters[index].type == AnimatorControllerParameterType.Trigger)
                    return true;
            }

            return false;
        }
    }
}

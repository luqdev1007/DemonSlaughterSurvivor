using Game.Core;
using System;
using UnityEngine;

namespace Game.View
{
    public sealed class ViewInterpolator : MonoBehaviour, IView
    {
        private const float TickSeconds = 1f / 60f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private static readonly int DissolveAmountId = Shader.PropertyToID("_DissolveAmount");
        private static readonly int DeathTriggerId = Animator.StringToHash("Death");
        private static readonly int IsDashingId = Animator.StringToHash("IsDashing");
        private static readonly int IsRunningId = Animator.StringToHash("IsRunning");

        private static readonly Color FlashColor = new Color(1f, 0.35f, 0.35f, 1f);
        private static readonly Color FlashEmission = new Color(0.8f, 0.1f, 0.1f, 1f);

        private Vector3 _previousPosition;
        private Vector3 _currentPosition;
        private float _lastSyncTime;
        private bool _hasPosition;

        private Renderer[] _renderers;
        private Material[][] _originalMaterials;
        private Material[][] _dissolveMaterials;
        private Animator _animator;
        private MaterialPropertyBlock _block;
        private bool _hasDeathTrigger;
        private bool _hasDashingParameter;
        private bool _isDashing;
        private bool _hasRunningParameter;
        private bool _isRunning;
        private float _hitFlashSeconds;
        private float _blinkSeconds;
        private float _dissolveSeconds;
        private Func<Material, Material> _dissolveMaterialFor;
        private Action<ViewInterpolator> _onRetired;

        private bool _isFlashing;
        private float _flashEndTime;

        private bool _isBlinking;
        private bool _renderersVisible = true;

        private bool _isDissolving;
        private float _dissolveStartTime;
        private float _dissolveAmount;

        private bool _isRetiring;
        private float _retireTime;

        private bool _isKnockedBack;
        private bool _isAirborne;
        private Vector3 _knockbackVelocity;
        private Vector3 _knockbackOffset;
        private float _knockbackGravity;
        private float _knockbackFriction;

        public Transform Transform => transform;

        internal void Configure(
            Animator animator,
            float hitFlashSeconds,
            float blinkSeconds,
            float dissolveSeconds,
            Func<Material, Material> dissolveMaterialFor,
            Action<ViewInterpolator> onRetired)
        {
            _animator = animator;
            _renderers = GetComponentsInChildren<Renderer>(true);
            _block = new MaterialPropertyBlock();
            _hasDeathTrigger = HasParameter(animator, DeathTriggerId, AnimatorControllerParameterType.Trigger);
            _hasDashingParameter = HasParameter(animator, IsDashingId, AnimatorControllerParameterType.Bool);
            _hasRunningParameter = HasParameter(animator, IsRunningId, AnimatorControllerParameterType.Bool);
            _hitFlashSeconds = hitFlashSeconds;
            _blinkSeconds = blinkSeconds;
            _dissolveSeconds = dissolveSeconds;
            _dissolveMaterialFor = dissolveMaterialFor;
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
            transform.rotation = rotation;
        }

        public void PlayHit()
        {
            if (_hitFlashSeconds <= 0f || _isDissolving)
                return;

            _isFlashing = true;
            _flashEndTime = Time.time + _hitFlashSeconds;

            ApplyBlock();
        }

        public void SetInvulnerable(bool value)
        {
            if (_isBlinking == value)
                return;

            _isBlinking = value;

            if (value == false)
                SetRenderersVisible(true);
        }

        public void SetDashing(bool value)
        {
            if (_hasDashingParameter == false || _isDashing == value)
                return;

            _isDashing = value;

            _animator.SetBool(IsDashingId, value);
        }

        public void SetRunning(bool value)
        {
            if (_hasRunningParameter == false || _isRunning == value)
                return;

            _isRunning = value;

            _animator.SetBool(IsRunningId, value);
        }

        public void PlayDeath()
        {
            if (_hasDeathTrigger == false)
                return;

            _animator.SetTrigger(DeathTriggerId);
        }

        public void Dissolve()
        {
            if (_isDissolving || _dissolveMaterialFor == null)
                return;

            if (_dissolveMaterials == null)
                BuildDissolveMaterials();

            _isFlashing = false;
            _isDissolving = true;
            _dissolveStartTime = Time.time;
            _dissolveAmount = 0f;

            SwapMaterials(_dissolveMaterials);
            ApplyBlock();
        }

        internal void BeginRetire(float seconds, Vector3 knockbackVelocity, float gravity, float friction)
        {
            _isRetiring = true;
            _retireTime = Time.time + seconds;

            PlayDeath();

            if (knockbackVelocity == Vector3.zero)
                return;

            _isKnockedBack = true;
            _isAirborne = knockbackVelocity.y > 0f;
            _knockbackVelocity = knockbackVelocity;
            _knockbackOffset = Vector3.zero;
            _knockbackGravity = gravity;
            _knockbackFriction = friction;
        }

        internal void ResetFeedback()
        {
            if (_isDissolving)
                SwapMaterials(_originalMaterials);

            _isFlashing = false;
            _isDissolving = false;
            _dissolveAmount = 0f;
            _isBlinking = false;
            _isDashing = false;
            _isRunning = false;
            _isRetiring = false;
            _isKnockedBack = false;
            _isAirborne = false;
            _knockbackVelocity = Vector3.zero;
            _knockbackOffset = Vector3.zero;

            ApplyBlock();
            SetRenderersVisible(true);
        }

        private void Update()
        {
            float time = Time.time;

            if (_isKnockedBack)
                AdvanceKnockback(Time.deltaTime);

            if (_hasPosition)
                transform.position = ResolvePosition() + _knockbackOffset;

            if (_isFlashing && time >= _flashEndTime)
            {
                _isFlashing = false;

                ApplyBlock();
            }

            if (_isDissolving && _dissolveAmount < 1f)
            {
                _dissolveAmount = _dissolveSeconds > 0f ? Mathf.Clamp01((time - _dissolveStartTime) / _dissolveSeconds) : 1f;

                ApplyBlock();
            }

            if (_isBlinking && _blinkSeconds > 0f)
                SetRenderersVisible(Mathf.Repeat(time, _blinkSeconds) < _blinkSeconds * 0.5f);

            if (_isRetiring && time >= _retireTime)
            {
                _isRetiring = false;

                _onRetired?.Invoke(this);
            }
        }

        private void AdvanceKnockback(float deltaTime)
        {
            if (_isAirborne)
            {
                _knockbackVelocity.y -= _knockbackGravity * deltaTime;
                _knockbackOffset += _knockbackVelocity * deltaTime;

                if (_knockbackOffset.y > 0f || _knockbackVelocity.y > 0f)
                    return;

                _knockbackOffset.y = 0f;
                _knockbackVelocity.y = 0f;
                _isAirborne = false;

                return;
            }

            float speed = _knockbackVelocity.magnitude;
            float slowdown = _knockbackFriction * deltaTime;

            if (speed <= slowdown)
            {
                _knockbackVelocity = Vector3.zero;
                _isKnockedBack = false;

                return;
            }

            _knockbackVelocity *= (speed - slowdown) / speed;
            _knockbackOffset += _knockbackVelocity * deltaTime;
        }

        private Vector3 ResolvePosition()
        {
            float phase = Mathf.Clamp01((Time.time - _lastSyncTime) / TickSeconds);

            return Vector3.Lerp(_previousPosition, _currentPosition, phase);
        }

        private void BuildDissolveMaterials()
        {
            _originalMaterials = new Material[_renderers.Length][];
            _dissolveMaterials = new Material[_renderers.Length][];

            for (int index = 0; index < _renderers.Length; index++)
            {
                Material[] originals = _renderers[index].sharedMaterials;
                Material[] dissolves = new Material[originals.Length];

                for (int slot = 0; slot < originals.Length; slot++)
                    dissolves[slot] = _dissolveMaterialFor(originals[slot]);

                _originalMaterials[index] = originals;
                _dissolveMaterials[index] = dissolves;
            }
        }

        private void ApplyBlock()
        {
            if (_renderers == null)
                return;

            _block.Clear();

            if (_isFlashing)
            {
                _block.SetColor(BaseColorId, FlashColor);
                _block.SetColor(ColorId, FlashColor);
                _block.SetColor(EmissionColorId, FlashEmission);
            }

            if (_isDissolving)
                _block.SetFloat(DissolveAmountId, _dissolveAmount);

            MaterialPropertyBlock block = _isFlashing || _isDissolving ? _block : null;

            for (int index = 0; index < _renderers.Length; index++)
            {
                Renderer target = _renderers[index];

                if (target == null)
                    continue;

                target.SetPropertyBlock(block);
            }
        }

        private void SwapMaterials(Material[][] materials)
        {
            if (materials == null)
                return;

            for (int index = 0; index < _renderers.Length; index++)
            {
                Renderer target = _renderers[index];

                if (target == null)
                    continue;

                target.sharedMaterials = materials[index];
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

        private static bool HasParameter(Animator animator, int id, AnimatorControllerParameterType type)
        {
            if (animator == null || animator.runtimeAnimatorController == null)
                return false;

            AnimatorControllerParameter[] parameters = animator.parameters;

            for (int index = 0; index < parameters.Length; index++)
            {
                if (parameters[index].nameHash == id && parameters[index].type == type)
                    return true;
            }

            return false;
        }
    }
}

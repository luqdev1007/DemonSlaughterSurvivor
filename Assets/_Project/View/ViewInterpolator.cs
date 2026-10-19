using Game.Core;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.View
{
    public sealed class ViewInterpolator : MonoBehaviour, IView, IDashChargeView, ICastInterruptView, ISwingStyleView, IEliteView, IPickupView, IRuneAuraView, IAimView, IAirView, IArrowView, ILightningView, ITeleportView, IFreezeView, IBeamView, IAreaView, IShieldView
    {
        private static readonly int IsChargingId = Animator.StringToHash("IsCharging");
        private static readonly int ChargeRunId = Animator.StringToHash("ChargeRun");
        private static readonly int ChargedDashId = Animator.StringToHash("ChargedDash");

        private const float TickSeconds = 1f / 60f;

        private static readonly int DissolveAmountId = Shader.PropertyToID("_DissolveAmount");
        private static readonly int DeathTriggerId = Animator.StringToHash("Death");
        private static readonly int IsDashingId = Animator.StringToHash("IsDashing");
        private static readonly int IsRunningId = Animator.StringToHash("IsRunning");
        private static readonly int AttackSpeedId = Animator.StringToHash("AttackSpeed");
        private static readonly int IsSpecialId = Animator.StringToHash("IsSpecial");
        private static readonly int IsPoweringUpId = Animator.StringToHash("IsPoweringUp");
        private static readonly int PowerUpTimeId = Animator.StringToHash("PowerUpTime");
        private static readonly int ActionPhaseId = Animator.StringToHash("ActionPhase");
        private static readonly int PushReactionTriggerId = Animator.StringToHash("PushReaction");
        private static readonly int PushReactionSpeedId = Animator.StringToHash("PushReactionSpeed");
        private static readonly int AirPhaseId = Animator.StringToHash("AirPhase");
        private static readonly int AirSpeedId = Animator.StringToHash("AirSpeed");
        private const string BackleapCue = "Backleap";

        [SerializeField] private BerserkLightningFx _berserkFx;
        [SerializeField] private SwordTrail _swordTrail;
        [SerializeField] private PickupFx _pickupFx;
        [SerializeField] private RuneAuraFx _runeAura;
        [SerializeField] private AimRig _aimRig;
        [SerializeField] private HoverRig _hoverRig;
        [SerializeField] private DeathRayFx _beamFx;
        [SerializeField] private WingsFx _wings;
        [SerializeField] private TrailRenderer _pierceTrail;
        [SerializeField] private LightningBoltFx _lightningBolt;
        [SerializeField] private EnergyShieldFx _shieldFx;

        private Vector3 _baseScale = Vector3.one;
        private bool _hasBaseScale;
        private Dictionary<GameObject, GameObject> _eliteFx;
        private GameObject _activeEliteFx;
        [SerializeField] private BossKnightView _actionView;
        [SerializeField] private DashChargeIndicator _chargeIndicator;
        [SerializeField] private ParticleSystem _dazeFx;

        private bool _hasChargeParameters;
        private bool _hasChargedDashParameter;
        private bool _isCharging;

        private Vector3 _previousPosition;
        private Vector3 _currentPosition;
        private float _lastSyncTime;
        private bool _hasPosition;
        private float _previousLift;
        private float _currentLift;

        private Renderer[] _renderers;
        private Material[][] _savedMaterials;
        private Material[][] _flashMaterials;
        private readonly List<Material> _materialScratch = new List<Material>();
        private Material[][] _originalMaterials;
        private Material[][] _dissolveMaterials;
        private Animator _animator;
        private MaterialPropertyBlock _block;
        private bool _hasDeathTrigger;
        private bool _hasDashingParameter;
        private bool _isDashing;
        private bool _hasRunningParameter;
        private bool _isRunning;
        private bool _hasSpecialParameter;
        private bool _isSpecial;
        private bool _hasPowerUpParameters;
        private bool _isPoweringUp;
        private bool _isDead;
        private bool _hasAttackSpeedParameter;
        private bool _hasActionPhaseParameter;
        private bool _hasPushReactionParameters;
        private bool _hasAirParameters;
        private int _airPhase;
        private int _actionPhase;
        private HitFeedback _hit;
        private float _blinkSeconds;
        private float _dissolveSeconds;
        private Func<Material, Material> _dissolveMaterialFor;
        private Action<ViewInterpolator> _onRetired;
        private Action<ViewInterpolator, Vector3> _onHit;
        private Action<ViewInterpolator, ViewCue, string> _onCue;

        private bool _isFlashing;
        private float _flashEndTime;
        private float _nextFlashTime;

        private bool _isPulsing;
        private float _pulseStartTime;
        private float _nextPulseTime;
        private Vector3 _pulseDirection;
        private Vector3 _pulseOffset;
        private Vector3 _pulseRestScale;

        private bool _isBlinking;
        private bool _renderersVisible = true;

        private bool _isDissolving;
        private float _dissolveStartTime;
        private float _dissolveAmount;

        private bool _isRetiring;
        private float _retireTime;
        private float _animatorOffTime;
        private float _decaySeconds;
        private float _sinkDepth;
        private float _sinkOffset;

        private bool _isBerserkSuppressed;
        private bool _isFrozen;

        private bool _isKnockedBack;
        private bool _isAirborne;
        private Vector3 _knockbackVelocity;
        private Vector3 _knockbackOffset;
        private float _knockbackGravity;
        private float _knockbackFriction;

        public Transform Transform => transform;

        internal void Configure(
            Animator animator,
            in HitFeedback hit,
            float blinkSeconds,
            float dissolveSeconds,
            Func<Material, Material> dissolveMaterialFor,
            Action<ViewInterpolator> onRetired,
            Action<ViewInterpolator, Vector3> onHit,
            Action<ViewInterpolator, ViewCue, string> onCue)
        {
            _animator = animator;
            _renderers = CollectOwnRenderers();
            _block = new MaterialPropertyBlock();
            _hasDeathTrigger = HasParameter(animator, DeathTriggerId, AnimatorControllerParameterType.Trigger);
            _hasDashingParameter = HasParameter(animator, IsDashingId, AnimatorControllerParameterType.Bool);
            _hasRunningParameter = HasParameter(animator, IsRunningId, AnimatorControllerParameterType.Bool);
            _hasAttackSpeedParameter = HasParameter(animator, AttackSpeedId, AnimatorControllerParameterType.Float);
            _hasActionPhaseParameter = HasParameter(animator, ActionPhaseId, AnimatorControllerParameterType.Int);
            _hasSpecialParameter = HasParameter(animator, IsSpecialId, AnimatorControllerParameterType.Bool);
            _hasPowerUpParameters = HasParameter(animator, IsPoweringUpId, AnimatorControllerParameterType.Bool)
                && HasParameter(animator, PowerUpTimeId, AnimatorControllerParameterType.Float);
            _hasPushReactionParameters = HasParameter(animator, PushReactionTriggerId, AnimatorControllerParameterType.Trigger)
                && HasParameter(animator, PushReactionSpeedId, AnimatorControllerParameterType.Float);
            _hasChargeParameters = HasParameter(animator, IsChargingId, AnimatorControllerParameterType.Bool)
                && HasParameter(animator, ChargeRunId, AnimatorControllerParameterType.Bool);
            _hasChargedDashParameter = HasParameter(animator, ChargedDashId, AnimatorControllerParameterType.Bool);
            _hasAirParameters = HasParameter(animator, AirPhaseId, AnimatorControllerParameterType.Int)
                && HasParameter(animator, AirSpeedId, AnimatorControllerParameterType.Float);
            _hit = hit;
            _blinkSeconds = blinkSeconds;
            _dissolveSeconds = dissolveSeconds;
            _dissolveMaterialFor = dissolveMaterialFor;
            _onRetired = onRetired;
            _onHit = onHit;
            _onCue = onCue;

            BuildFlashMaterials();
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

        public void SetAreaScale(float scale)
        {
            if (_hasBaseScale == false)
            {
                _baseScale = transform.localScale;
                _hasBaseScale = true;
            }

            transform.localScale = _baseScale * scale;
        }

        public void SetShieldStrength(float strength)
        {
            if (_shieldFx != null && _isDead == false)
                _shieldFx.SetStrength(strength);
        }

        public void FlashShield(float strength)
        {
            if (_shieldFx != null && _isDead == false)
                _shieldFx.Flash();
        }

        public void SetBeam(float charge, bool firing)
        {
            if (_beamFx != null)
                _beamFx.Show(charge, firing);
        }

        public void SetFrozen(bool value)
        {
            if (_animator == null || _isDead || _isFrozen == value)
                return;

            _isFrozen = value;
            _animator.speed = value ? 0f : 1f;
        }

        public void Teleport(Vector3 position)
        {
            _hasPosition = false;
            SetPosition(position);
        }

        public void SetRune(bool active, Color color, Texture glyph, float fraction, float remainingSeconds)
        {
            if (_runeAura != null)
                _runeAura.Show(active, color, glyph, fraction, remainingSeconds);
        }

        public void SetAim(float twistDegrees, Vector2 localVelocity, int turnSide)
        {
            if (_isDead)
                return;

            if (_hoverRig != null)
                _hoverRig.SetVelocity(localVelocity);

            if (_aimRig != null)
                _aimRig.SetAim(twistDegrees, localVelocity, turnSide);
        }

        public void SetAir(int phase, float playbackSpeed, bool wings)
        {
            if (_isDead)
            {
                phase = 0;
                wings = false;
            }

            if (_wings != null)
                _wings.SetPhase(wings ? phase : 0);

            if (_airPhase == phase)
                return;

            if (_airPhase == 0 && phase != 0)
                _onCue?.Invoke(this, ViewCue.Attack, BackleapCue);

            _airPhase = phase;

            if (_hasAirParameters == false)
                return;

            _animator.SetFloat(AirSpeedId, playbackSpeed);
            _animator.SetInteger(AirPhaseId, phase);
        }

        public void Strike(Vector3 from, Vector3 to)
        {
            if (_lightningBolt != null)
                _lightningBolt.Strike(from, to);
        }

        public void MarkPierced()
        {
            if (_pierceTrail != null)
                _pierceTrail.emitting = true;
        }

        public void SetPickupFlight(float progress)
        {
            if (_pickupFx != null)
                _pickupFx.SetFlight(progress);
        }

        public void SetLift(float height)
        {
            _previousLift = _currentLift;
            _currentLift = height;
        }

        public void PlayPushReaction(float seconds)
        {
            if (_isDead || _hasPushReactionParameters == false || seconds <= 0f)
                return;

            _animator.SetFloat(PushReactionSpeedId, 1f / seconds);
            _animator.SetTrigger(PushReactionTriggerId);
        }

        internal Renderer[] BodyRenderers => _renderers;

        public void PlayCastInterrupted()
        {
            if (_isDead || _dazeFx == null)
                return;

            _dazeFx.Clear(true);
            _dazeFx.Play(true);
        }

        public void ResetInterpolation()
        {
            _previousPosition = Vector3.zero;
            _currentPosition = Vector3.zero;
            _previousLift = 0f;
            _currentLift = 0f;
            _lastSyncTime = 0f;
            _hasPosition = false;
        }

        public void SetRotation(Quaternion rotation)
        {
            transform.rotation = rotation;
        }

        public void PlayHit(Vector3 direction)
        {
            if (_isDead)
                return;

            _onHit?.Invoke(this, direction);

            StartFlash();
            StartPulse(direction);
        }

        public void PlayWeaponHit()
        {
            _onCue?.Invoke(this, ViewCue.WeaponHit, null);

            if (_swordTrail != null)
                _swordTrail.PlayBlood();
        }

        public void SetInvulnerable(bool value)
        {
            if (_isBlinking == value)
                return;

            _isBlinking = value;

            if (value == false)
                SetRenderersVisible(true);
        }

        public void SetElite(float scale, GameObject vfxPrefab, Vector3 vfxOffset)
        {
            if (_hasBaseScale == false)
            {
                _baseScale = transform.localScale;
                _hasBaseScale = true;
            }

            transform.localScale = _baseScale * scale;

            if (vfxPrefab == null)
                return;

            _eliteFx ??= new Dictionary<GameObject, GameObject>();

            if (_eliteFx.TryGetValue(vfxPrefab, out GameObject fx) == false)
            {
                fx = Instantiate(vfxPrefab, transform);
                fx.transform.localPosition = vfxOffset / Mathf.Max(scale, 1e-3f);
                fx.transform.localRotation = Quaternion.identity;

                if (fx.GetComponent<NotBody>() == null)
                    fx.AddComponent<NotBody>();

                _eliteFx.Add(vfxPrefab, fx);
            }

            fx.SetActive(true);
            _activeEliteFx = fx;
        }

        public void SetSwingStyle(float reach, bool sharp)
        {
            if (_swordTrail != null)
                _swordTrail.SetStyle(reach, sharp);
        }

        public void SetBerserk(bool value)
        {
            if (_swordTrail != null)
                _swordTrail.SetBerserk(value);

            if (_berserkFx == null)
                return;

            _berserkFx.SetShowing(value && _isBerserkSuppressed == false);
        }

        public void SetPowerUp(bool active, float progress)
        {
            if (_isDead || _hasPowerUpParameters == false)
                return;

            if (active)
                _animator.SetFloat(PowerUpTimeId, progress);

            if (_isPoweringUp == active)
                return;

            _isPoweringUp = active;

            _animator.SetBool(IsPoweringUpId, active);
        }

        public void SetDashing(bool value)
        {
            if (_isDead || _hasDashingParameter == false || _isDashing == value)
                return;

            _isDashing = value;

            _animator.SetBool(IsDashingId, value);

            if (value)
                _onCue?.Invoke(this, ViewCue.DashStarted, null);
        }

        public void SetSpecialAttack(bool value)
        {
            if (_isDead || _hasSpecialParameter == false || _isSpecial == value)
                return;

            _isSpecial = value;

            _animator.SetBool(IsSpecialId, value);
        }

        public void SetDashCharge(bool charging, float strength, bool runStyle, float dashStrength)
        {
            if (_isDead)
                charging = false;

            if (_hasChargedDashParameter)
                _animator.SetBool(ChargedDashId, dashStrength > 0f);

            if (_chargeIndicator != null)
            {
                if (charging)
                    _chargeIndicator.Show(strength);
                else
                    _chargeIndicator.Hide();
            }

            if (_hasChargeParameters == false || _isCharging == charging)
                return;

            _isCharging = charging;

            _animator.SetBool(ChargeRunId, runStyle);
            _animator.SetBool(IsChargingId, charging);
        }

        public void SetRunning(bool value)
        {
            if (_isDead || _hasRunningParameter == false || _isRunning == value)
                return;

            _isRunning = value;

            if (_actionView != null)
                _actionView.SetMoving(value);

            _animator.SetBool(IsRunningId, value);
        }

        public void PlayAttack(string trigger, float speed)
        {
            if (_isDead || _hasAttackSpeedParameter == false)
                return;

            _animator.SetFloat(AttackSpeedId, speed);
            _animator.SetTrigger(Animator.StringToHash(trigger));

            _onCue?.Invoke(this, ViewCue.Attack, trigger);
        }

        public void SetAction(in ActionCue cue)
        {
            if (_isDead)
                return;

            if (_actionView != null)
                _actionView.Apply(cue);

            if (_hasActionPhaseParameter == false)
                return;

            int phase = (int)cue.Pose;

            if (_hasAttackSpeedParameter)
                _animator.SetFloat(AttackSpeedId, cue.PlaybackSpeed > 0f ? cue.PlaybackSpeed : 1f);

            if (_actionPhase == phase)
                return;

            _actionPhase = phase;

            _animator.SetInteger(ActionPhaseId, phase);
        }

        public void PlayDeath()
        {
            if (_actionView != null)
                _actionView.ResetImmediate();

            if (_hoverRig != null)
                _hoverRig.SetVelocity(Vector2.zero);

            Thaw();

            if (_shieldFx != null)
                _shieldFx.HideImmediate();

            if (_hasDeathTrigger == false || _isDead)
                return;

            _isDead = true;

            if (_hasRunningParameter)
                _animator.SetBool(IsRunningId, false);

            if (_hasDashingParameter)
                _animator.SetBool(IsDashingId, false);

            if (_hasSpecialParameter)
                _animator.SetBool(IsSpecialId, false);

            if (_hasPowerUpParameters)
                _animator.SetBool(IsPoweringUpId, false);

            if (_hasPushReactionParameters)
                _animator.ResetTrigger(PushReactionTriggerId);

            if (_dazeFx != null)
                _dazeFx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            _isRunning = false;
            _isDashing = false;
            _isSpecial = false;
            _isPoweringUp = false;

            _animator.SetTrigger(DeathTriggerId);

            _onCue?.Invoke(this, ViewCue.Death, null);
        }

        public void Dissolve()
        {
            _isBerserkSuppressed = true;

            if (_berserkFx != null)
                _berserkFx.SetShowing(false);

            if (_isDissolving || _dissolveMaterialFor == null)
                return;

            EndFlash();

            if (_dissolveMaterials == null)
                BuildDissolveMaterials();

            _isDissolving = true;
            _dissolveStartTime = Time.time;
            _dissolveAmount = 0f;

            SwapMaterials(_dissolveMaterials);
            ApplyBlock();
        }

        internal void InheritLift(ViewInterpolator source)
        {
            float lift = source.ResolveLift();

            _previousLift = lift;
            _currentLift = lift;
        }

        internal bool IsRetiring => _isRetiring;

        internal float RetireTime => _retireTime;

        internal void BeginRetire(float seconds, float deathPoseSeconds, float decaySeconds, float sinkDepth, Vector3 knockbackVelocity, float gravity, float friction)
        {
            EndFlash();
            EndPulse();

            _isRetiring = true;
            _retireTime = Time.time + seconds;
            _animatorOffTime = Time.time + deathPoseSeconds;
            _decaySeconds = Mathf.Min(decaySeconds, seconds);
            _sinkDepth = sinkDepth;
            _sinkOffset = 0f;

            if (_activeEliteFx != null)
            {
                _activeEliteFx.SetActive(false);
                _activeEliteFx = null;
            }
            _knockbackGravity = gravity;
            _knockbackFriction = friction;

            PlayDeath();

            float lift = ResolveLift();

            _previousLift = 0f;
            _currentLift = 0f;

            if (knockbackVelocity == Vector3.zero && lift <= 0f)
                return;

            _isKnockedBack = true;
            _isAirborne = knockbackVelocity.y > 0f || lift > 0f;
            _knockbackVelocity = knockbackVelocity;
            _knockbackOffset = Vector3.up * lift;
        }

        private void Thaw()
        {
            if (_isFrozen == false)
                return;

            _isFrozen = false;

            if (_animator != null)
                _animator.speed = 1f;
        }

        internal void DecayBy(float time)
        {
            if (_isRetiring && _retireTime > time + _decaySeconds)
                _retireTime = time + _decaySeconds;
        }


        internal void ResetFeedback()
        {
            if (_animator != null && _animator.enabled == false)
                _animator.enabled = true;

            _sinkOffset = 0f;

            if (_aimRig != null)
                _aimRig.ResetAim();

            if (_hoverRig != null)
                _hoverRig.ResetHover();

            Thaw();

            if (_wings != null)
                _wings.HideImmediate();

            if (_shieldFx != null)
                _shieldFx.HideImmediate();

            if (_pierceTrail != null)
            {
                _pierceTrail.emitting = false;
                _pierceTrail.Clear();
            }

            _airPhase = 0;

            if (_hasBaseScale)
                transform.localScale = _baseScale;

            if (_activeEliteFx != null)
            {
                _activeEliteFx.SetActive(false);
                _activeEliteFx = null;
            }

            EndFlash();
            _nextFlashTime = 0f;

            EndPulse();
            _nextPulseTime = 0f;

            if (_isDissolving)
                SwapMaterials(_originalMaterials);

            _isDissolving = false;
            _dissolveAmount = 0f;
            _isBlinking = false;
            _isDashing = false;
            _isRunning = false;
            _isSpecial = false;
            _isPoweringUp = false;
            _isDead = false;
            _isRetiring = false;
            _isKnockedBack = false;
            _isAirborne = false;
            _knockbackVelocity = Vector3.zero;
            _knockbackOffset = Vector3.zero;
            _isBerserkSuppressed = false;
            _actionPhase = 0;
            _isCharging = false;

            if (_chargeIndicator != null)
                _chargeIndicator.Hide();

            if (_dazeFx != null)
                _dazeFx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            if (_actionView != null)
                _actionView.ResetImmediate();

            if (_berserkFx != null)
                _berserkFx.ResetImmediate();

            if (_swordTrail != null)
            {
                _swordTrail.SetBerserk(false);
                _swordTrail.ResetTrail();
            }

            ApplyBlock();
            SetRenderersVisible(true);
        }

        internal int ActiveSlot { get; set; } = -1;

        internal void Tick(float time, float deltaTime)
        {
            if (_isKnockedBack)
                AdvanceKnockback(deltaTime);

            if (_isRetiring)
                AdvanceCorpse(time);

            if (_hasPosition)
            {
                if (_isPulsing)
                    AdvancePulse(time);

                transform.position = ResolvePosition() + Vector3.up * (ResolveLift() + _sinkOffset) + _knockbackOffset + _pulseOffset;
            }

            if (_isFlashing && time >= _flashEndTime)
                EndFlash();

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

        private void AdvanceCorpse(float time)
        {
            if (_animator != null && _animator.enabled && time >= _animatorOffTime && _isKnockedBack == false)
                _animator.enabled = false;

            float decayStart = _retireTime - _decaySeconds;

            if (_decaySeconds > 0f && time > decayStart)
                _sinkOffset = -_sinkDepth * Mathf.Clamp01((time - decayStart) / _decaySeconds);
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

        private float ResolveLift()
        {
            float phase = Mathf.Clamp01((Time.time - _lastSyncTime) / TickSeconds);

            return Mathf.Lerp(_previousLift, _currentLift, phase);
        }

        private Renderer[] CollectOwnRenderers()
        {
            Renderer[] all = GetComponentsInChildren<Renderer>(true);

            int count = 0;

            for (int index = 0; index < all.Length; index++)
            {
                if (IsBody(all[index]))
                    count++;
            }

            if (count == all.Length)
                return all;

            Renderer[] own = new Renderer[count];
            int next = 0;

            for (int index = 0; index < all.Length; index++)
            {
                if (IsBody(all[index]) == false)
                    continue;

                own[next] = all[index];
                next++;
            }

            return own;
        }

        private bool IsBody(Renderer renderer)
        {
            if (_berserkFx != null && renderer.transform.IsChildOf(_berserkFx.transform))
                return false;

            return renderer.GetComponentInParent<NotBody>(true) == null;
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

            if (_isDissolving)
                _block.SetFloat(DissolveAmountId, _dissolveAmount);

            MaterialPropertyBlock block = _isDissolving ? _block : null;

            for (int index = 0; index < _renderers.Length; index++)
            {
                Renderer target = _renderers[index];

                if (target == null)
                    continue;

                target.SetPropertyBlock(block);
            }
        }

        private void BuildFlashMaterials()
        {
            if (_hit.FlashMaterial == null || _renderers == null)
            {
                _flashMaterials = null;
                _savedMaterials = null;

                return;
            }

            _flashMaterials = new Material[_renderers.Length][];
            _savedMaterials = new Material[_renderers.Length][];

            for (int index = 0; index < _renderers.Length; index++)
            {
                int slots = _renderers[index].sharedMaterials.Length;
                Material[] flash = new Material[slots];

                for (int slot = 0; slot < slots; slot++)
                    flash[slot] = _hit.FlashMaterial;

                _flashMaterials[index] = flash;
                _savedMaterials[index] = new Material[slots];
            }
        }

        private void StartFlash()
        {
            if (_flashMaterials == null || _isDissolving || _isRetiring)
                return;

            float time = Time.time;

            if (time < _nextFlashTime)
                return;

            _nextFlashTime = time + _hit.FlashCooldownSeconds;
            _flashEndTime = time + _hit.FlashSeconds;

            if (_isFlashing)
                return;

            SaveMaterials();
            SwapMaterials(_flashMaterials);

            _isFlashing = true;
        }

        private void EndFlash()
        {
            if (_isFlashing == false)
                return;

            _isFlashing = false;

            SwapMaterials(_savedMaterials);
        }

        private void StartPulse(Vector3 direction)
        {
            if (_hit.PulseSeconds <= 0f || _isRetiring)
                return;

            float time = Time.time;

            if (time < _nextPulseTime)
                return;

            _nextPulseTime = time + _hit.PulseCooldownSeconds;

            if (_isPulsing == false)
                _pulseRestScale = transform.localScale;

            direction.y = 0f;

            _isPulsing = true;
            _pulseStartTime = time;
            _pulseDirection = direction.sqrMagnitude > 1e-8f ? direction.normalized : Vector3.zero;
        }

        private void AdvancePulse(float time)
        {
            float progress = (time - _pulseStartTime) / _hit.PulseSeconds;

            if (progress >= 1f)
            {
                EndPulse();

                return;
            }

            float kick = Mathf.Sin(Mathf.PI * Mathf.Sqrt(Mathf.Max(0f, progress)));
            float squash = _hit.Squash * kick;

            _pulseOffset = _pulseDirection * (_hit.RecoilDistance * kick);
            transform.localScale = Vector3.Scale(_pulseRestScale, new Vector3(1f + squash * 0.5f, 1f - squash, 1f + squash * 0.5f));
        }

        private void EndPulse()
        {
            if (_isPulsing == false)
                return;

            _isPulsing = false;
            _pulseOffset = Vector3.zero;

            transform.localScale = _pulseRestScale;
        }

        private void SaveMaterials()
        {
            for (int index = 0; index < _renderers.Length; index++)
            {
                Renderer target = _renderers[index];

                if (target == null)
                    continue;

                target.GetSharedMaterials(_materialScratch);

                Material[] saved = _savedMaterials[index];
                int count = Mathf.Min(saved.Length, _materialScratch.Count);

                for (int slot = 0; slot < count; slot++)
                    saved[slot] = _materialScratch[slot];
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

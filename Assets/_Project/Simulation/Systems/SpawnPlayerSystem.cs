using Game.Configs;
using Game.Core;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class SpawnPlayerSystem : IEcsInitSystem
    {
        private const float BaseDamageTaken = 1f;

        private static readonly Vector3 StartPosition = Vector3.zero;

        private readonly EcsWorldInject _world = default;

        private readonly EcsPoolInject<Player> _players = default;
        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<Facing> _facings = default;
        private readonly EcsPoolInject<MoveIntent> _intents = default;
        private readonly EcsPoolInject<MoveSpeed> _speeds = default;
        private readonly EcsPoolInject<TurnSpeed> _turnSpeeds = default;
        private readonly EcsPoolInject<InputState> _inputStates = default;
        private readonly EcsPoolInject<Aim> _aims = default;
        private readonly EcsPoolInject<BodyRadius> _bodyRadii = default;
        private readonly EcsPoolInject<PickupRadius> _pickupRadii = default;
        private readonly EcsPoolInject<LifeSteal> _lifeSteals = default;
        private readonly EcsPoolInject<Experience> _experiences = default;
        private readonly EcsPoolInject<Health> _healths = default;
        private readonly EcsPoolInject<MaxHealth> _maxHealths = default;
        private readonly EcsPoolInject<HitInvulnerability> _hitInvulnerabilities = default;
        private readonly EcsPoolInject<DamageTaken> _damageTakens = default;
        private readonly EcsPoolInject<Velocity> _velocities = default;
        private readonly EcsPoolInject<PreviousPosition> _previousPositions = default;
        private readonly EcsPoolInject<DashStats> _dashStats = default;
        private readonly EcsPoolInject<UltimateCharge> _charges = default;
        private readonly EcsPoolInject<RageState> _rageStates = default;
        private readonly EcsPoolInject<View> _views = default;
        private readonly EcsPoolInject<AbilityCooldownScale> _cooldownScales = default;
        private readonly EcsPoolInject<CombatStats> _combatStats = default;
        private readonly EcsPoolInject<TorsoAim> _torsoAims = default;
        private readonly EcsPoolInject<Height> _heights = default;
        private readonly EcsPoolInject<BackleapStats> _backleaps = default;

        private readonly EcsCustomInject<RunContext> _context = default;
        private readonly EcsCustomInject<IContentRegistry> _content = default;
        private readonly EcsCustomInject<IViewFactory> _viewFactory = default;
        private readonly EcsCustomInject<StatModifiers> _statModifiers = default;

        public void Init(IEcsSystems systems)
        {
            CharacterConfig character = _content.Value.Get<CharacterConfig>(_context.Value.CharacterId);
            DashAbilityConfig dash = character.Dash;

            if (dash != null && dash.AccelerationPower < 1f)
                throw new InvalidOperationException(
                    $"{nameof(DashAbilityConfig)} '{dash.Id}' has {nameof(DashAbilityConfig.AccelerationPower)} {dash.AccelerationPower}. " +
                    "The dash must not slow down towards its end: use 1 for a constant speed and more for a stronger run-up.");

            RageConfig rage = character.Rage;

            if (rage != null)
                ValidateRage(rage);

            if (character.TorsoAim != null)
                character.TorsoAim.Validate($"{nameof(CharacterConfig)} '{character.Id}'");

            if (character.Backleap != null)
                character.Backleap.Validate();

            int entity = _world.Value.NewEntity();

            _players.Value.Add(entity);

            ref Position position = ref _positions.Value.Add(entity);
            position.Value = StartPosition;

            ref PreviousPosition previousPosition = ref _previousPositions.Value.Add(entity);
            previousPosition.Value = StartPosition;

            ref Facing facing = ref _facings.Value.Add(entity);
            facing.Value = Vector3.forward;

            ref MoveSpeed speed = ref _speeds.Value.Add(entity);
            speed.Base = character.MoveSpeed;
            speed.Value = character.MoveSpeed;

            ref TurnSpeed turnSpeed = ref _turnSpeeds.Value.Add(entity);
            turnSpeed.Value = character.TurnSpeed;

            _inputStates.Value.Add(entity);

            ref Aim aim = ref _aims.Value.Add(entity);
            aim.DeadZone = character.AimDeadZone;
            aim.TurnSpeed = character.AimTurnSpeed;

            ref BodyRadius bodyRadius = ref _bodyRadii.Value.Add(entity);
            bodyRadius.Value = character.BodyRadius;

            ref PickupRadius pickupRadius = ref _pickupRadii.Value.Add(entity);
            pickupRadius.Base = character.PickupRadius;
            pickupRadius.Value = character.PickupRadius;

            ref Experience experience = ref _experiences.Value.Add(entity);
            experience.Level = 1;
            experience.Current = 0;

            ref MaxHealth maxHealth = ref _maxHealths.Value.Add(entity);
            maxHealth.Base = character.MaxHealth;
            maxHealth.Value = character.MaxHealth;

            ref Health health = ref _healths.Value.Add(entity);
            health.Current = character.MaxHealth;

            ref AbilityCooldownScale cooldownScale = ref _cooldownScales.Value.Add(entity);
            cooldownScale.Base = 1f;
            cooldownScale.Value = 1f;

            ref CombatStats combat = ref _combatStats.Value.Add(entity);
            combat.CritChanceBase = character.CritChance;
            combat.CritChance = character.CritChance;
            combat.CritMultiplier = character.CritMultiplier;
            combat.AttackPowerBase = character.AttackPower;
            combat.AttackPower = character.AttackPower;
            combat.SpellPowerBase = character.SpellPower;
            combat.SpellPower = character.SpellPower;
            combat.ArmorBase = character.Armor;
            combat.Armor = character.Armor;
            combat.MagicResistBase = character.MagicResist;
            combat.MagicResist = character.MagicResist;
            combat.DefenseScale = character.DefenseScale;
            combat.EvasionBase = character.Evasion;
            combat.Evasion = character.Evasion;
            combat.MaxEvasion = character.MaxEvasion;
            combat.LuckBase = character.Luck;
            combat.Luck = character.Luck;
            combat.DamageDealtBase = 1f;
            combat.DamageDealt = 1f;

            ref LifeSteal lifeSteal = ref _lifeSteals.Value.Add(entity);
            lifeSteal.OnKillBase = character.LifeOnKill;
            lifeSteal.OnKill = character.LifeOnKill;
            lifeSteal.OnHitBase = character.LifeOnHit;
            lifeSteal.OnHit = character.LifeOnHit;

            ref HitInvulnerability hitInvulnerability = ref _hitInvulnerabilities.Value.Add(entity);
            hitInvulnerability.Seconds = character.HitInvulnerabilitySeconds;

            ref DamageTaken damageTaken = ref _damageTakens.Value.Add(entity);
            damageTaken.Base = BaseDamageTaken;
            damageTaken.Value = BaseDamageTaken;

            if (dash != null)
            {
                ref DashStats dashStats = ref _dashStats.Value.Add(entity);
                dashStats.Distance = dash.Distance;
                dashStats.Duration = dash.Duration;
                dashStats.CooldownBase = dash.Cooldown;
                dashStats.Cooldown = dash.Cooldown;
                dashStats.Direction = dash.Direction;
                dashStats.AccelerationPower = dash.AccelerationPower;
                dashStats.InvulnerabilitySeconds = dash.InvulnerabilitySeconds;
                dashStats.PushSpeed = dash.PushSpeed;
                dashStats.PushSeconds = dash.PushSeconds;
                dashStats.PushArcHeight = dash.PushArcHeight;
                dashStats.PushReach = dash.PushReach;
                dashStats.PushEdgeScale = dash.PushEdgeScale;
                dashStats.PushForwardTiltDegrees = dash.PushForwardTiltDegrees;
                dashStats.PushSpreadDegrees = dash.PushSpreadDegrees;
                dashStats.ChargeEnabled = dash.ChargeEnabled;
                dashStats.TapSeconds = dash.TapSeconds;
                dashStats.MaxChargeSeconds = dash.MaxChargeSeconds;
                dashStats.ChargeCurve = dash.ChargeCurve;
                dashStats.DistanceAtFullCharge = dash.DistanceAtFullCharge;
                dashStats.SpeedAtFullCharge = dash.SpeedAtFullCharge;
                dashStats.DamageAtFullCharge = dash.DamageAtFullCharge;
                dashStats.ChargeMoveSpeedMore = dash.ChargeMoveSpeedMore;
                dashStats.ChargeRunAnimation = dash.ChargeRunAnimation;
            }

            if (rage != null)
            {
                ref UltimateCharge charge = ref _charges.Value.Add(entity);
                charge.Value = 0f;
                charge.Max = rage.Max;

                _rageStates.Value.Add(entity);
            }

            ref Height height = ref _heights.Value.Add(entity);
            height.Value = 0f;
            height.Clearance = character.AirborneClearance;

            if (character.Backleap != null)
                _backleaps.Value.Add(entity).Config = character.Backleap;

            if (character.TorsoAim != null)
            {
                ref TorsoAim torso = ref _torsoAims.Value.Add(entity);
                torso.Config = character.TorsoAim;
                torso.Direction = Vector3.forward;
                torso.Desired = Vector3.forward;
            }

            _intents.Value.Add(entity);
            _velocities.Value.Add(entity);

            ref View view = ref _views.Value.Add(entity);
            view.Value = _viewFactory.Value.Create(character.ViewPrefab, StartPosition);

            StatModifiers statModifiers = _statModifiers.Value;

            IReadOnlyList<StatModifierSpec> startModifiers = _context.Value.StartModifiers;

            for (int index = 0; index < startModifiers.Count; index++)
            {
                StatModifierSpec spec = startModifiers[index];

                statModifiers.Add(entity, spec.Stat, spec.Op, spec.Value, spec.SourceId);
            }

            statModifiers.MarkDirty(entity);
        }

        private static void ValidateRage(RageConfig rage)
        {
            if (rage.Max <= 0f)
                throw new InvalidOperationException(
                    $"{nameof(RageConfig)} '{rage.Id}' has {nameof(RageConfig.Max)} {rage.Max}. The ultimate could never charge.");

            if (rage.DealtPerHit < 0f || rage.ReceivedScale < 0f)
                throw new InvalidOperationException(
                    $"{nameof(RageConfig)} '{rage.Id}' has a negative gain: {nameof(RageConfig.DealtPerHit)} {rage.DealtPerHit}, " +
                    $"{nameof(RageConfig.ReceivedScale)} {rage.ReceivedScale}. Fighting must never drain rage.");

            if (rage.DecayDelaySeconds < 0f || rage.DecayPerSecond < 0f)
                throw new InvalidOperationException(
                    $"{nameof(RageConfig)} '{rage.Id}' has a negative decay: {nameof(RageConfig.DecayDelaySeconds)} {rage.DecayDelaySeconds}, " +
                    $"{nameof(RageConfig.DecayPerSecond)} {rage.DecayPerSecond}. A negative decay would fill rage out of combat.");
        }
    }
}

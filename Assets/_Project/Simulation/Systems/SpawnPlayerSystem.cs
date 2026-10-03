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
        private readonly EcsPoolInject<BodyRadius> _bodyRadii = default;
        private readonly EcsPoolInject<PickupRadius> _pickupRadii = default;
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

        private readonly EcsCustomInject<RunContext> _context = default;
        private readonly EcsCustomInject<IContentRegistry> _content = default;
        private readonly EcsCustomInject<IViewFactory> _viewFactory = default;
        private readonly EcsCustomInject<StatModifiers> _statModifiers = default;

        public void Init(IEcsSystems systems)
        {
            CharacterConfig character = _content.Value.Get<CharacterConfig>(_context.Value.CharacterId);
            DashAbilityConfig dash = character.Dash;

            if (dash.AccelerationPower < 1f)
                throw new InvalidOperationException(
                    $"{nameof(DashAbilityConfig)} '{dash.Id}' has {nameof(DashAbilityConfig.AccelerationPower)} {dash.AccelerationPower}. " +
                    "The dash must not slow down towards its end: use 1 for a constant speed and more for a stronger run-up.");

            RageConfig rage = character.Rage;

            ValidateRage(character, rage);

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

            ref HitInvulnerability hitInvulnerability = ref _hitInvulnerabilities.Value.Add(entity);
            hitInvulnerability.Seconds = character.HitInvulnerabilitySeconds;

            ref DamageTaken damageTaken = ref _damageTakens.Value.Add(entity);
            damageTaken.Base = BaseDamageTaken;
            damageTaken.Value = BaseDamageTaken;

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

            ref UltimateCharge charge = ref _charges.Value.Add(entity);
            charge.Value = 0f;
            charge.Max = rage.Max;

            _rageStates.Value.Add(entity);

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

        private static void ValidateRage(CharacterConfig character, RageConfig rage)
        {
            if (rage == null)
                throw new InvalidOperationException(
                    $"{nameof(CharacterConfig)} '{character.Id}' has no {nameof(RageConfig)} assigned. " +
                    "The ultimate charge needs a resource to fill.");

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

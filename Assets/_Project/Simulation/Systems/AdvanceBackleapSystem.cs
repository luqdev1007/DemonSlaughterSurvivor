using Game.Configs;
using Game.Core;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class AdvanceBackleapSystem : IEcsRunSystem
    {
        private const float FullCircleDegrees = 180f;

        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<Backleaping, BackleapStats, Height, Velocity, Position>> _filter = default;

        private readonly EcsPoolInject<Backleaping> _backleaps = default;
        private readonly EcsPoolInject<BackleapStats> _stats = default;
        private readonly EcsPoolInject<BackleapCooldown> _cooldowns = default;
        private readonly EcsPoolInject<Height> _heights = default;
        private readonly EcsPoolInject<Velocity> _velocities = default;
        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<Facing> _facings = default;
        private readonly EcsPoolInject<AbilityCooldownScale> _cooldownScales = default;
        private readonly EcsPoolInject<AreaStrike> _strikes = default;
        private readonly EcsPoolInject<MoveIntent> _intents = default;

        private readonly EcsCustomInject<SimulationClock> _clock = default;

        public void Run(IEcsSystems systems)
        {
            float delta = _clock.Value.Delta;

            foreach (int hero in _filter.Value)
            {
                ref Backleaping leap = ref _backleaps.Value.Get(hero);
                ref BackleapStats stats = ref _stats.Value.Get(hero);
                ref Height height = ref _heights.Value.Get(hero);
                BackleapConfig config = stats.Config;

                leap.Ticks++;

                float progress = Mathf.Clamp01((float)leap.Ticks / leap.PhaseTicks);

                switch (leap.Phase)
                {
                    case BackleapPhase.Leap:
                        height.Value = config.LeapHeight * Mathf.Sin(progress * Mathf.PI * 0.5f);
                        SetVelocity(hero, leap.Direction * leap.Speed);

                        if (leap.Ticks < leap.PhaseTicks)
                            break;

                        if (stats.Held && config.MaxHoverSeconds > 0f)
                        {
                            leap.Glided = true;
                            Enter(ref leap, BackleapPhase.Hover, StartBackleapSystem.Ticks(config.MaxHoverSeconds, delta));
                        }
                        else
                            EnterFall(ref leap, height.Value, config, delta);

                        break;

                    case BackleapPhase.Hover:
                        height.Value = config.LeapHeight;
                        Glide(hero, ref leap, config, delta);

                        if (stats.Held && leap.Ticks < leap.PhaseTicks)
                            break;

                        EnterFall(ref leap, height.Value, config, delta);

                        break;

                    case BackleapPhase.Fall:
                        height.Value = leap.FallFrom * (1f - progress);

                        if (leap.Glided)
                            Glide(hero, ref leap, config, delta);
                        else
                            SetVelocity(hero, Vector3.zero);

                        if (leap.Ticks < leap.PhaseTicks)
                            break;

                        Land(hero, ref height, config);

                        break;

                    case BackleapPhase.Hop:
                        height.Value = config.HopHeight * Mathf.Sin(progress * Mathf.PI * 0.5f);
                        SetVelocity(hero, Vector3.zero);

                        if (leap.Ticks < leap.PhaseTicks)
                            break;

                        Enter(ref leap, BackleapPhase.Dive, StartBackleapSystem.Ticks(config.DiveSeconds, delta));

                        break;

                    case BackleapPhase.Dive:
                        height.Value = config.HopHeight * (1f - progress) * (1f - progress);
                        SetVelocity(hero, Vector3.zero);

                        if (leap.Ticks < leap.PhaseTicks)
                            break;

                        Strike(hero, config);
                        Land(hero, ref height, config);

                        break;
                }
            }
        }

        private static void Enter(ref Backleaping leap, BackleapPhase phase, int ticks)
        {
            leap.Phase = phase;
            leap.Ticks = 0;
            leap.PhaseTicks = ticks;
        }

        private static void EnterFall(ref Backleaping leap, float from, BackleapConfig config, float delta)
        {
            leap.FallFrom = from;
            Enter(ref leap, BackleapPhase.Fall, StartBackleapSystem.Ticks(config.FallSeconds, delta));
        }

        private void Land(int hero, ref Height height, BackleapConfig config)
        {
            height.Value = 0f;

            SetVelocity(hero, Vector3.zero);
            _backleaps.Value.Del(hero);

            ref BackleapCooldown cooldown = ref _cooldowns.Value.Has(hero) ? ref _cooldowns.Value.Get(hero) : ref _cooldowns.Value.Add(hero);
            cooldown.Remaining = config.CooldownSeconds * AbilityCooldowns.Scale(_cooldownScales.Value, hero);
        }

        private void Strike(int hero, BackleapConfig config)
        {
            EcsWorld world = _world.Value;

            ref AreaStrike strike = ref _strikes.Value.Add(world.NewEntity());
            strike.Owner = world.PackEntity(hero);
            strike.Origin = _positions.Value.Get(hero).Value;
            strike.Forward = _facings.Value.Has(hero) ? _facings.Value.Get(hero).Value : Vector3.forward;
            strike.Reach = config.ImpactRadius;
            strike.HalfAngleDegrees = FullCircleDegrees;
            strike.Damage = config.ImpactDamage;
            strike.Kind = DamageKind.Weapon;
            strike.PushSpeed = config.ImpactPushSpeed;
            strike.PushSeconds = config.ImpactPushSeconds;
            strike.PushArcHeight = config.ImpactPushArcHeight;
        }

        private void Glide(int hero, ref Backleaping leap, BackleapConfig config, float delta)
        {
            ref Velocity velocity = ref _velocities.Value.Get(hero);
            Vector3 heading = leap.Direction;

            if (_intents.Value.Has(hero))
            {
                Vector3 intent = _intents.Value.Get(hero).Value;
                intent.y = 0f;

                if (intent.sqrMagnitude > Mathf.Epsilon)
                    heading = intent.normalized;
            }

            velocity.Value = Vector3.MoveTowards(velocity.Value, heading * config.GlideSpeed, config.GlideSteering * delta);

            Vector3 flat = velocity.Value;
            flat.y = 0f;

            if (flat.sqrMagnitude > Mathf.Epsilon)
                leap.Direction = flat.normalized;
        }

        private void SetVelocity(int hero, Vector3 velocity)
        {
            _velocities.Value.Get(hero).Value = velocity;
        }
    }
}

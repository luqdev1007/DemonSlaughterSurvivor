using Game.Configs;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class AccumulateDualitySystem : IEcsRunSystem
    {
        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<Player, Duality, Health, MaxHealth>, Exc<Dead>> _heroes = default;
        private readonly EcsFilterInject<Inc<DamageEvent, DamageApplied>> _applied = default;

        private readonly EcsPoolInject<Duality> _dualities = default;
        private readonly EcsPoolInject<Health> _healths = default;
        private readonly EcsPoolInject<MaxHealth> _maxHealths = default;
        private readonly EcsPoolInject<DamageEvent> _damageEvents = default;
        private readonly EcsPoolInject<DamageApplied> _appliedPool = default;
        private readonly EcsPoolInject<Backleaping> _backleaps = default;
        private readonly EcsPoolInject<TorsoAim> _aims = default;
        private readonly EcsPoolInject<MoveIntent> _intents = default;

        private readonly EcsCustomInject<SimulationClock> _clock = default;

        public void Run(IEcsSystems systems)
        {
            EcsWorld world = _world.Value;
            float delta = _clock.Value.Delta;

            foreach (int hero in _heroes.Value)
            {
                ref Duality duality = ref _dualities.Value.Get(hero);
                DualityConfig config = duality.Config;
                float current = _healths.Value.Get(hero).Current;
                float maxHealth = _maxHealths.Value.Get(hero).Value;

                if (duality.LastHealth < 0f)
                    duality.LastHealth = current;

                float taken = 0f;
                float critGain = 0f;

                foreach (int entity in _applied.Value)
                {
                    ref DamageEvent damageEvent = ref _damageEvents.Value.Get(entity);

                    if (damageEvent.Target.Unpack(world, out int target) && target == hero)
                        taken += _appliedPool.Value.Get(entity).Amount;
                    else if (damageEvent.IsCrit && damageEvent.Source.Unpack(world, out int source) && source == hero)
                        critGain += config.PerCrit;
                }

                float demonicGain = Mathf.Min(critGain, config.MaxCritGainPerTick);
                float humanGain = 0f;

                if (maxHealth > 0f)
                {
                    demonicGain += taken / maxHealth * config.DamageTakenScale;

                    float healed = current - (duality.LastHealth - taken);

                    if (healed > 0f)
                        humanGain += healed / maxHealth * config.HealScale;
                }

                bool leaping = _backleaps.Value.Has(hero);

                if (leaping && duality.WasLeaping == false)
                    demonicGain += config.PerBackleap;

                if (taken > 0f)
                    duality.CalmTicks = 0;
                else
                    duality.CalmTicks++;

                if (duality.CalmTicks * delta >= config.CalmDelaySeconds)
                    humanGain += config.CalmPerSecond * delta;

                if (leaping == false && IsAimingStill(hero))
                    humanGain += config.AimStillPerSecond * delta;

                float demonic = Mathf.Clamp01(duality.Demonic + demonicGain - humanGain);
                float passiveFloor = 1f - config.PassiveHumanCeiling;

                if (demonic > passiveFloor)
                    demonic = Mathf.Max(passiveFloor, demonic - config.PassiveHumanPerSecond * delta);

                duality.Demonic = demonic;
                duality.LastHealth = current;
                duality.WasLeaping = leaping;
            }
        }

        private bool IsAimingStill(int hero)
        {
            if (_aims.Value.Has(hero) == false || _aims.Value.Get(hero).Manual == false)
                return false;

            if (_intents.Value.Has(hero) == false)
                return true;

            Vector3 intent = _intents.Value.Get(hero).Value;
            intent.y = 0f;

            return intent.sqrMagnitude <= Mathf.Epsilon;
        }
    }
}

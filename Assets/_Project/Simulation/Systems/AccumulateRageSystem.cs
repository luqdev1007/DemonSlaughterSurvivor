using Game.Configs;
using Game.Core;
using Game.Simulation.Components;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using System;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class AccumulateRageSystem : IEcsInitSystem, IEcsRunSystem
    {
        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<Player, UltimateCharge, RageState, MaxHealth>, Exc<Dead>> _heroes = default;
        private readonly EcsFilterInject<Inc<DamageEvent, DamageApplied>> _applied = default;

        private readonly EcsPoolInject<UltimateCharge> _charges = default;
        private readonly EcsPoolInject<RageState> _states = default;
        private readonly EcsPoolInject<MaxHealth> _maxHealths = default;
        private readonly EcsPoolInject<DamageEvent> _damageEvents = default;

        private readonly EcsCustomInject<RunContext> _context = default;
        private readonly EcsCustomInject<IContentRegistry> _content = default;

        private float _dealtPerHit;
        private float _receivedScale;

        public void Init(IEcsSystems systems)
        {
            CharacterConfig character = _content.Value.Get<CharacterConfig>(_context.Value.CharacterId);
            RageConfig rage = character.Rage;

            if (rage == null)
                throw new InvalidOperationException(
                    $"{nameof(CharacterConfig)} '{character.Id}' has no {nameof(RageConfig)} assigned, " +
                    $"but {nameof(AccumulateRageSystem)} is registered and reads its gain numbers.");

            _dealtPerHit = rage.DealtPerHit;
            _receivedScale = rage.ReceivedScale;
        }

        public void Run(IEcsSystems systems)
        {
            EcsWorld world = _world.Value;

            foreach (int hero in _heroes.Value)
            {
                ref UltimateCharge charge = ref _charges.Value.Get(hero);
                ref RageState state = ref _states.Value.Get(hero);
                ref MaxHealth maxHealth = ref _maxHealths.Value.Get(hero);

                float gain = 0f;
                bool inCombat = false;

                foreach (int entity in _applied.Value)
                {
                    ref DamageEvent damageEvent = ref _damageEvents.Value.Get(entity);

                    if (damageEvent.Source.Unpack(world, out int source) && source == hero)
                    {
                        gain += _dealtPerHit;
                        inCombat = true;
                    }

                    if (damageEvent.Target.Unpack(world, out int target) && target == hero)
                    {
                        if (maxHealth.Value > 0f)
                            gain += damageEvent.Amount / maxHealth.Value * _receivedScale;

                        inCombat = true;
                    }
                }

                if (inCombat == false)
                    continue;

                state.TicksSinceCombat = 0;
                charge.Value = Mathf.Clamp(charge.Value + gain, 0f, charge.Max);
            }
        }
    }
}

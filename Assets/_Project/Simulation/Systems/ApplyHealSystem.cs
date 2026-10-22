using Game.Simulation.Components;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class ApplyHealSystem : IEcsRunSystem
    {
        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<HealEvent>> _events = default;

        private readonly EcsPoolInject<HealEvent> _healEvents = default;
        private readonly EcsPoolInject<Health> _healths = default;
        private readonly EcsPoolInject<MaxHealth> _maxHealths = default;
        private readonly EcsPoolInject<Dead> _dead = default;
        private readonly EcsPoolInject<LifeSteal> _lifeSteals = default;

        public void Run(IEcsSystems systems)
        {
            EcsWorld world = _world.Value;

            foreach (int entity in _events.Value)
            {
                ref HealEvent healEvent = ref _healEvents.Value.Get(entity);

                if (healEvent.Amount <= 0f)
                    continue;

                if (healEvent.Target.Unpack(world, out int target) == false)
                    continue;

                if (_dead.Value.Has(target) || _healths.Value.Has(target) == false || _maxHealths.Value.Has(target) == false)
                    continue;

                ref Health health = ref _healths.Value.Get(target);

                float max = _maxHealths.Value.Get(target).Value;

                if (health.Current <= 0f || health.Current >= max)
                    continue;

                float amount = healEvent.Amount;

                if (healEvent.LifeSteal && _lifeSteals.Value.Has(target))
                    amount *= 1f + Mathf.Max(0f, _lifeSteals.Value.Get(target).ScaleBonus);

                health.Current = Mathf.Min(health.Current + amount, max);
            }
        }
    }
}

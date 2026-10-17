using Game.Core;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

namespace Game.Simulation.Systems
{
    public sealed class ReadDashInputSystem : IEcsRunSystem
    {
        private readonly EcsFilterInject<Inc<Player, DashStats>, Exc<Dead>> _filter = default;
        private readonly EcsFilterInject<Inc<DashCharge, Dead>> _deadCharges = default;

        private readonly EcsPoolInject<DashRequest> _requests = default;
        private readonly EcsPoolInject<DashCharge> _charges = default;
        private readonly EcsPoolInject<DashStats> _stats = default;
        private readonly EcsPoolInject<Dashing> _dashes = default;
        private readonly EcsPoolInject<DashCooldown> _cooldowns = default;

        private readonly EcsCustomInject<IInputService> _input = default;
        private readonly EcsCustomInject<IDashInput> _dashInput = default;
        private readonly EcsCustomInject<StatModifiers> _statModifiers = default;
        private readonly EcsCustomInject<SimulationClock> _clock = default;

        public void Run(IEcsSystems systems)
        {
            bool pressed = _input.Value.ConsumeDashPressed();
            bool released = _dashInput.Value.ConsumeDashReleased();
            bool held = _dashInput.Value.IsDashHeld;

            foreach (int entity in _filter.Value)
            {
                if (pressed && _charges.Value.Has(entity) == false)
                {
                    if (_dashes.Value.Has(entity) || _cooldowns.Value.Has(entity))
                        Request(entity, 0f);
                    else
                        BeginCharge(entity);
                }

                if (_charges.Value.Has(entity) == false)
                    continue;

                ref DashCharge charge = ref _charges.Value.Get(entity);

                if (released == false && held)
                {
                    charge.Ticks++;

                    continue;
                }

                Release(entity, charge.Ticks);
            }

            foreach (int entity in _deadCharges.Value)
                EndCharge(entity);
        }

        private void BeginCharge(int entity)
        {
            _charges.Value.Add(entity).Ticks = 0;

            float slow = _stats.Value.Get(entity).ChargeMoveSpeedMore;

            if (slow < 0f)
                _statModifiers.Value.Add(entity, StatId.MoveSpeed, StatOp.More, slow, DashChargeRules.SlowSourceId);
        }

        private void Release(int entity, int ticks)
        {
            Request(entity, DashChargeRules.Strength(in _stats.Value.Get(entity), ticks, _clock.Value.Delta));

            EndCharge(entity);
        }

        private void Request(int entity, float strength)
        {
            ref DashRequest request = ref _requests.Value.Has(entity) ? ref _requests.Value.Get(entity) : ref _requests.Value.Add(entity);
            request.Age = 0f;
            request.Strength = strength;
        }

        private void EndCharge(int entity)
        {
            _charges.Value.Del(entity);
            _statModifiers.Value.RemoveBySource(entity, DashChargeRules.SlowSourceId);
        }
    }
}

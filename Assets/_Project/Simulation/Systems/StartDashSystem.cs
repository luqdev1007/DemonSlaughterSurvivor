using Game.Core;
using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using System;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class StartDashSystem : IEcsRunSystem
    {
        private readonly EcsFilterInject<Inc<DashRequest, DashStats, Facing>, Exc<Dashing, DashCooldown>> _filter = default;

        private readonly EcsPoolInject<DashRequest> _requests = default;
        private readonly EcsPoolInject<DashStats> _stats = default;
        private readonly EcsPoolInject<Facing> _facings = default;
        private readonly EcsPoolInject<Dashing> _dashes = default;

        private readonly EcsCustomInject<SimulationClock> _clock = default;

        public void Run(IEcsSystems systems)
        {
            foreach (var entity in _filter.Value)
            {
                ref Dashing dashing = ref _dashes.Value.Add(entity);

                ref Facing facing = ref _facings.Value.Get(entity);
                ref DashStats stats = ref _stats.Value.Get(entity);

                dashing.Direction = stats.Direction switch
                {
                    DashDirection.Forward => facing.Value.normalized,
                    DashDirection.Backward => -facing.Value.normalized,
                    _ => throw new NotImplementedException()
                };

                float delta = _clock.Value.Delta;
                int ticks = Mathf.Max(1, Mathf.RoundToInt(stats.Duration / delta));

                dashing.Speed = stats.Distance / (ticks * delta);
                dashing.RemainingTicks = ticks;

                _requests.Value.Del(entity);
            }
        }
    }
}

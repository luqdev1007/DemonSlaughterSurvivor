using Game.Simulation.Components;
using Game.Simulation.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

namespace Game.Simulation.Systems
{
    public sealed class AdvanceGemScatterSystem : IEcsRunSystem
    {
        private const float MainArcShare = 0.78f;
        private const float BounceHeightShare = 0.18f;

        private readonly EcsFilterInject<Inc<GemScatter, Position, Velocity>> _scattering = default;

        private readonly EcsPoolInject<GemScatter> _scatters = default;
        private readonly EcsPoolInject<Position> _positions = default;
        private readonly EcsPoolInject<Velocity> _velocities = default;

        private readonly EcsCustomInject<SimulationClock> _clock = default;

        public void Run(IEcsSystems systems)
        {
            float delta = _clock.Value.Delta;

            foreach (int gem in _scattering.Value)
            {
                ref GemScatter scatter = ref _scatters.Value.Get(gem);
                ref Velocity velocity = ref _velocities.Value.Get(gem);

                if (scatter.ElapsedTicks >= scatter.TotalTicks)
                {
                    velocity.Value = Vector3.zero;
                    _scatters.Value.Del(gem);

                    continue;
                }

                scatter.ElapsedTicks++;

                float t = Mathf.Clamp01((float)scatter.ElapsedTicks / scatter.TotalTicks);
                Vector3 desired = Sample(in scatter, t);

                velocity.Value = (desired - _positions.Value.Get(gem).Value) / delta;
            }
        }

        public static Vector3 Sample(in GemScatter scatter, float t)
        {
            float travel = 1f - (1f - t) * (1f - t);
            Vector3 point = Vector3.Lerp(scatter.Start, scatter.End, travel);

            if (t < MainArcShare)
            {
                float u = t / MainArcShare;
                point.y = scatter.Height * 4f * u * (1f - u);
            }
            else
            {
                float u = (t - MainArcShare) / (1f - MainArcShare);
                point.y = scatter.Height * BounceHeightShare * 4f * u * (1f - u);
            }

            return point;
        }
    }
}

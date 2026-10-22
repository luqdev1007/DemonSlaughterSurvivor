using Game.Configs;
using Game.Core;
using Game.Simulation.Components;
using Leopotam.EcsLite;
using UnityEngine;

namespace Game.Simulation.Services
{
    public readonly struct FlameWaveShape
    {
        public readonly float StartRadius;
        public readonly float MaxRadius;
        public readonly float HalfAngleDegrees;
        public readonly float Thickness;
        public readonly float Speed;
        public readonly int PillarCount;
        public readonly float Scale;

        private FlameWaveShape(float startRadius, float maxRadius, float halfAngleDegrees, float thickness, float speed, int pillarCount, float scale)
        {
            StartRadius = startRadius;
            MaxRadius = maxRadius;
            HalfAngleDegrees = halfAngleDegrees;
            Thickness = thickness;
            Speed = speed;
            PillarCount = pillarCount;
            Scale = scale;
        }

        public static FlameWaveShape Of(FlameSwingConfig config, FlameSwingLevel entry, float halfAngleDegrees, int pillarCount, float scale)
        {
            return new FlameWaveShape(
                config.StartRadius * scale,
                entry.Distance * scale,
                halfAngleDegrees,
                config.Thickness * scale,
                config.Speed * scale,
                pillarCount,
                scale);
        }
    }

    public static class FlameWaves
    {
        private const float FullCircleHalfAngle = 180f;

        public static void Spawn(
            EcsWorld world,
            int owner,
            FlameSwingConfig config,
            Vector3 origin,
            Vector3 direction,
            float damage,
            in FlameWaveShape shape,
            EcsPool<FlameWave> waves,
            EcsPool<FlameWavePillar> pillars,
            EcsPool<Position> positions,
            EcsPool<Facing> facings,
            EcsPool<NotIndexed> notIndexed,
            EcsPool<View> views,
            IViewFactory viewFactory)
        {
            origin.y = 0f;

            int wave = world.NewEntity();

            ref FlameWave data = ref waves.Add(wave);
            data.Owner = world.PackEntity(owner);
            data.Config = config;
            data.Origin = origin;
            data.Direction = direction;
            data.Radius = shape.StartRadius;
            data.MaxRadius = shape.MaxRadius;
            data.Damage = damage;
            data.HalfAngleDegrees = shape.HalfAngleDegrees;
            data.Thickness = shape.Thickness;
            data.Speed = shape.Speed;

            EcsPackedEntity packedWave = world.PackEntity(wave);
            int count = shape.PillarCount;
            bool ring = shape.HalfAngleDegrees >= FullCircleHalfAngle;

            for (int index = 0; index < count; index++)
            {
                float angle = PillarAngle(index, count, shape.HalfAngleDegrees, ring);
                Vector3 outward = Quaternion.Euler(0f, angle, 0f) * direction;
                Vector3 point = origin + outward * shape.StartRadius;

                int pillar = world.NewEntity();

                ref FlameWavePillar pillarData = ref pillars.Add(pillar);
                pillarData.Wave = packedWave;
                pillarData.AngleDegrees = angle;

                positions.Add(pillar).Value = point;
                facings.Add(pillar).Value = outward;
                notIndexed.Add(pillar);
                views.Add(pillar).Value = AreaScale.Apply(viewFactory.Create(config.PillarPrefab, point), shape.Scale);
            }
        }

        private static float PillarAngle(int index, int count, float halfAngleDegrees, bool ring)
        {
            if (ring)
                return 360f * index / count;

            return count == 1 ? 0f : Mathf.Lerp(-halfAngleDegrees, halfAngleDegrees, index / (float)(count - 1));
        }
    }
}

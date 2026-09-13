using System;
using System.Collections.Generic;

namespace Game.Configs
{
    public static class WaveTimelineValidator
    {
        public static void Validate(WaveTimelineConfig timeline, float spatialCellSize)
        {
            if (timeline == null)
                return;

            IReadOnlyList<Wave> waves = timeline.Waves;

            if (waves == null)
                return;

            for (int waveIndex = 0; waveIndex < waves.Count; waveIndex++)
            {
                Wave wave = waves[waveIndex];

                if (wave == null)
                    throw new InvalidOperationException(
                        $"{nameof(WaveTimelineConfig)} '{timeline.Id}' has an empty wave slot at index {waveIndex}.");

                IReadOnlyList<WeightedEnemy> weighted = wave.Enemies;

                for (int entryIndex = 0; entryIndex < weighted.Count; entryIndex++)
                {
                    WeightedEnemy entry = weighted[entryIndex];

                    ValidateEnemy(timeline, entry == null ? null : entry.Enemy, spatialCellSize, waveIndex, entryIndex, "weighted");
                }

                IReadOnlyList<GuaranteedSpawn> guaranteed = wave.Guaranteed;

                for (int entryIndex = 0; entryIndex < guaranteed.Count; entryIndex++)
                {
                    GuaranteedSpawn entry = guaranteed[entryIndex];

                    ValidateEnemy(timeline, entry == null ? null : entry.Enemy, spatialCellSize, waveIndex, entryIndex, "guaranteed");
                }
            }
        }

        public static int ResolveMaxGuaranteedCount(WaveTimelineConfig timeline)
        {
            if (timeline == null)
                return 0;

            IReadOnlyList<Wave> waves = timeline.Waves;

            if (waves == null)
                return 0;

            int max = 0;

            for (int index = 0; index < waves.Count; index++)
            {
                int count = waves[index].Guaranteed.Count;

                if (count > max)
                    max = count;
            }

            return max;
        }

        public static int ResolveMaxLiveCap(WaveTimelineConfig timeline)
        {
            if (timeline == null)
                return 0;

            IReadOnlyList<Wave> waves = timeline.Waves;

            if (waves == null)
                return 0;

            int max = 0;

            for (int index = 0; index < waves.Count; index++)
            {
                int cap = waves[index].LiveCap;

                if (cap > max)
                    max = cap;
            }

            return max;
        }

        private static void ValidateEnemy(
            WaveTimelineConfig timeline,
            EnemyConfig enemy,
            float spatialCellSize,
            int waveIndex,
            int entryIndex,
            string kind)
        {
            if (enemy == null)
                throw new InvalidOperationException(
                    $"{nameof(WaveTimelineConfig)} '{timeline.Id}' has no {nameof(EnemyConfig)} assigned " +
                    $"in wave {waveIndex}, {kind} entry {entryIndex}.");

            if (enemy.ViewPrefab == null)
                throw new InvalidOperationException(
                    $"{nameof(EnemyConfig)} '{enemy.Id}' has no view prefab assigned " +
                    $"(wave {waveIndex}, {kind} entry {entryIndex}).");

            if (enemy.SeparationRadius < 0f)
                throw new InvalidOperationException(
                    $"{nameof(EnemyConfig)} '{enemy.Id}' has a negative {nameof(EnemyConfig.SeparationRadius)} " +
                    $"of {enemy.SeparationRadius} (wave {waveIndex}, {kind} entry {entryIndex}). " +
                    "A negative radius makes the neighbour query return nothing, which disables separation without any message. " +
                    $"Set {nameof(EnemyConfig.SeparationRadius)} to zero to disable separation on purpose.");

            if (enemy.SeparationRadius > spatialCellSize)
                throw new InvalidOperationException(
                    $"{nameof(EnemyConfig)} '{enemy.Id}' has {nameof(EnemyConfig.SeparationRadius)} {enemy.SeparationRadius}, " +
                    $"which is greater than {nameof(LevelConfig)}.{nameof(LevelConfig.SpatialCellSize)} {spatialCellSize} " +
                    $"(wave {waveIndex}, {kind} entry {entryIndex}). " +
                    "The spatial index walks one ring of cells around the center and rejects a query radius wider than one cell. " +
                    $"Either raise {nameof(LevelConfig)}.{nameof(LevelConfig.SpatialCellSize)} in the level config " +
                    $"or lower {nameof(EnemyConfig.SeparationRadius)} in the enemy config.");

            if (enemy.SeparationStrength < 0f || enemy.SeparationStrength >= 1f)
                throw new InvalidOperationException(
                    $"{nameof(EnemyConfig)} '{enemy.Id}' has {nameof(EnemyConfig.SeparationStrength)} {enemy.SeparationStrength}, " +
                    $"which is outside the allowed range [0, 1) (wave {waveIndex}, {kind} entry {entryIndex}). " +
                    "At a strength of one or more the push can cancel or reverse the direction towards the target, " +
                    "and the crowd stalls in a ring instead of reaching the player. " +
                    $"Set {nameof(EnemyConfig.SeparationStrength)} to zero to disable separation on purpose.");
        }
    }
}

using System;
using System.Collections.Generic;

namespace Game.Configs
{
    public static class WaveTimelineValidator
    {
        public static void Validate(WaveTimelineConfig timeline)
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

                    ValidateEnemy(timeline, entry == null ? null : entry.Enemy, waveIndex, entryIndex, "weighted");
                }

                IReadOnlyList<GuaranteedSpawn> guaranteed = wave.Guaranteed;

                for (int entryIndex = 0; entryIndex < guaranteed.Count; entryIndex++)
                {
                    GuaranteedSpawn entry = guaranteed[entryIndex];

                    ValidateEnemy(timeline, entry == null ? null : entry.Enemy, waveIndex, entryIndex, "guaranteed");
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

        private static void ValidateEnemy(WaveTimelineConfig timeline, EnemyConfig enemy, int waveIndex, int entryIndex, string kind)
        {
            if (enemy == null)
                throw new InvalidOperationException(
                    $"{nameof(WaveTimelineConfig)} '{timeline.Id}' has no {nameof(EnemyConfig)} assigned " +
                    $"in wave {waveIndex}, {kind} entry {entryIndex}.");

            if (enemy.ViewPrefab == null)
                throw new InvalidOperationException(
                    $"{nameof(EnemyConfig)} '{enemy.Id}' has no view prefab assigned " +
                    $"(wave {waveIndex}, {kind} entry {entryIndex}).");
        }
    }
}

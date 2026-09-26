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

                ValidateWave(timeline, wave, waveIndex);

                IReadOnlyList<WeightedEnemy> weighted = wave.Enemies;

                for (int entryIndex = 0; entryIndex < weighted.Count; entryIndex++)
                {
                    WeightedEnemy entry = weighted[entryIndex];

                    ValidateEnemy(timeline, entry == null ? null : entry.Enemy, waveIndex, entryIndex, "weighted");
                    ValidateWeight(timeline, entry, waveIndex, entryIndex);
                }

                IReadOnlyList<GuaranteedSpawn> guaranteed = wave.Guaranteed;

                for (int entryIndex = 0; entryIndex < guaranteed.Count; entryIndex++)
                {
                    GuaranteedSpawn entry = guaranteed[entryIndex];

                    ValidateEnemy(timeline, entry == null ? null : entry.Enemy, waveIndex, entryIndex, "guaranteed");
                    ValidateGuaranteed(timeline, entry, waveIndex, entryIndex);
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

        public static float ResolveMaxBodyRadius(WaveTimelineConfig timeline)
        {
            if (timeline == null)
                return 0f;

            IReadOnlyList<Wave> waves = timeline.Waves;

            if (waves == null)
                return 0f;

            float max = 0f;

            for (int index = 0; index < waves.Count; index++)
            {
                Wave wave = waves[index];

                if (wave == null)
                    continue;

                IReadOnlyList<WeightedEnemy> weighted = wave.Enemies;

                for (int entryIndex = 0; entryIndex < weighted.Count; entryIndex++)
                {
                    WeightedEnemy entry = weighted[entryIndex];

                    max = ResolveLargerRadius(max, entry == null ? null : entry.Enemy);
                }

                IReadOnlyList<GuaranteedSpawn> guaranteed = wave.Guaranteed;

                for (int entryIndex = 0; entryIndex < guaranteed.Count; entryIndex++)
                {
                    GuaranteedSpawn entry = guaranteed[entryIndex];

                    max = ResolveLargerRadius(max, entry == null ? null : entry.Enemy);
                }
            }

            return max;
        }

        public static float ResolveMaxMoveSpeed(WaveTimelineConfig timeline)
        {
            if (timeline == null)
                return 0f;

            IReadOnlyList<Wave> waves = timeline.Waves;

            if (waves == null)
                return 0f;

            float max = 0f;

            for (int index = 0; index < waves.Count; index++)
            {
                Wave wave = waves[index];

                if (wave == null)
                    continue;

                IReadOnlyList<WeightedEnemy> weighted = wave.Enemies;

                for (int entryIndex = 0; entryIndex < weighted.Count; entryIndex++)
                {
                    WeightedEnemy entry = weighted[entryIndex];

                    max = ResolveFasterSpeed(max, entry == null ? null : entry.Enemy);
                }

                IReadOnlyList<GuaranteedSpawn> guaranteed = wave.Guaranteed;

                for (int entryIndex = 0; entryIndex < guaranteed.Count; entryIndex++)
                {
                    GuaranteedSpawn entry = guaranteed[entryIndex];

                    max = ResolveFasterSpeed(max, entry == null ? null : entry.Enemy);
                }
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

        private static float ResolveLargerRadius(float max, EnemyConfig enemy)
        {
            if (enemy == null)
                return max;

            if (enemy.BodyRadius > max)
                return enemy.BodyRadius;

            return max;
        }

        private static float ResolveFasterSpeed(float max, EnemyConfig enemy)
        {
            if (enemy == null)
                return max;

            if (enemy.MoveSpeed > max)
                return enemy.MoveSpeed;

            return max;
        }

        private static void ValidateWave(WaveTimelineConfig timeline, Wave wave, int waveIndex)
        {
            if (wave.DurationSeconds < 0f)
                throw new InvalidOperationException(
                    $"{nameof(WaveTimelineConfig)} '{timeline.Id}' wave {waveIndex} has a negative " +
                    $"{nameof(Wave.DurationSeconds)} of {wave.DurationSeconds}. " +
                    "A negative duration ends the wave on the tick it starts, so the wave is skipped without any message.");

            if (wave.SpawnRatePerSecond < 0f)
                throw new InvalidOperationException(
                    $"{nameof(WaveTimelineConfig)} '{timeline.Id}' wave {waveIndex} has a negative " +
                    $"{nameof(Wave.SpawnRatePerSecond)} of {wave.SpawnRatePerSecond}. " +
                    "A negative rate drives the spawn accumulator down without a floor, so the wave spawns nothing " +
                    "and reports nothing. Set the rate to zero to disable spawning on purpose.");

            if (wave.RefillRatePerSecond < 0f)
                throw new InvalidOperationException(
                    $"{nameof(WaveTimelineConfig)} '{timeline.Id}' wave {waveIndex} has a negative " +
                    $"{nameof(Wave.RefillRatePerSecond)} of {wave.RefillRatePerSecond}. " +
                    "A negative rate drives the refill accumulator down without a floor, so the refill never fires " +
                    "and reports nothing. Set the rate to zero to disable refilling on purpose.");

            if (wave.LiveCap < 0)
                throw new InvalidOperationException(
                    $"{nameof(WaveTimelineConfig)} '{timeline.Id}' wave {waveIndex} has a negative " +
                    $"{nameof(Wave.LiveCap)} of {wave.LiveCap}. " +
                    "A negative cap is below any possible live count, so the wave spawns nothing.");

            if (wave.LiveFloor < 0)
                throw new InvalidOperationException(
                    $"{nameof(WaveTimelineConfig)} '{timeline.Id}' wave {waveIndex} has a negative " +
                    $"{nameof(Wave.LiveFloor)} of {wave.LiveFloor}. " +
                    "A negative floor can never be reached, so refilling never starts.");

            if (wave.LiveFloor > wave.LiveCap)
                throw new InvalidOperationException(
                    $"{nameof(WaveTimelineConfig)} '{timeline.Id}' wave {waveIndex} has {nameof(Wave.LiveFloor)} " +
                    $"{wave.LiveFloor} above {nameof(Wave.LiveCap)} {wave.LiveCap}. " +
                    "Refilling tops the crowd up to the floor, so a floor above the cap lets the refill push the live " +
                    "count past the cap silently. Keep the floor at or below the cap.");
        }

        private static void ValidateWeight(WaveTimelineConfig timeline, WeightedEnemy entry, int waveIndex, int entryIndex)
        {
            if (entry == null)
                return;

            if (entry.Weight < 0f)
                throw new InvalidOperationException(
                    $"{nameof(WaveTimelineConfig)} '{timeline.Id}' has a negative {nameof(WeightedEnemy.Weight)} " +
                    $"of {entry.Weight} in wave {waveIndex}, weighted entry {entryIndex}. " +
                    "Weights are summed and sampled by a running total, so a negative weight shifts the sampling of " +
                    "every other entry and can make the pick fall through. Set the weight to zero to exclude the entry.");
        }

        private static void ValidateGuaranteed(WaveTimelineConfig timeline, GuaranteedSpawn entry, int waveIndex, int entryIndex)
        {
            if (entry == null)
                return;

            if (entry.OffsetSeconds < 0f)
                throw new InvalidOperationException(
                    $"{nameof(WaveTimelineConfig)} '{timeline.Id}' has a negative {nameof(GuaranteedSpawn.OffsetSeconds)} " +
                    $"of {entry.OffsetSeconds} in wave {waveIndex}, guaranteed entry {entryIndex}. " +
                    "The offset is compared against the time elapsed inside the wave, so a negative offset fires the " +
                    "event on the first tick of the wave rather than where it was meant to land.");

            if (entry.Count < 0)
                throw new InvalidOperationException(
                    $"{nameof(WaveTimelineConfig)} '{timeline.Id}' has a negative {nameof(GuaranteedSpawn.Count)} " +
                    $"of {entry.Count} in wave {waveIndex}, guaranteed entry {entryIndex}. " +
                    "A negative count spawns nothing and still marks the event as fired, so it disappears without a message. " +
                    "Set the count to zero to disable the event on purpose.");
        }

        private static void ValidateEnemy(
            WaveTimelineConfig timeline,
            EnemyConfig enemy,
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

            if (enemy.MaxHealth <= 0f)
                throw new InvalidOperationException(
                    $"{nameof(EnemyConfig)} '{enemy.Id}' has {nameof(EnemyConfig.MaxHealth)} {enemy.MaxHealth}, " +
                    $"which is not above zero (wave {waveIndex}, {kind} entry {entryIndex}). " +
                    "Such an enemy is born dead: it is marked dead on its first tick without any blow, " +
                    "so it dies without a killing blow and without a knockback, and never reaches the player.");

            if (enemy.BodyRadius < 0f)
                throw new InvalidOperationException(
                    $"{nameof(EnemyConfig)} '{enemy.Id}' has a negative {nameof(EnemyConfig.BodyRadius)} " +
                    $"of {enemy.BodyRadius} (wave {waveIndex}, {kind} entry {entryIndex}). " +
                    "The contact distance is the sum of the two body radii, so a negative radius shrinks it below the " +
                    "real gap and the enemy never reports an overlap: it walks through the player without any message. " +
                    $"Set {nameof(EnemyConfig.BodyRadius)} to zero to disable the contact radius on purpose.");

            if (enemy.SeparationRadius < 0f)
                throw new InvalidOperationException(
                    $"{nameof(EnemyConfig)} '{enemy.Id}' has a negative {nameof(EnemyConfig.SeparationRadius)} " +
                    $"of {enemy.SeparationRadius} (wave {waveIndex}, {kind} entry {entryIndex}). " +
                    "A negative radius makes the neighbour query return nothing, which disables separation without any message. " +
                    $"Set {nameof(EnemyConfig.SeparationRadius)} to zero to disable separation on purpose.");

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

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Configs
{
    public static class ViewPrewarmPlan
    {
        public static void Resolve(WaveTimelineConfig timeline, Dictionary<GameObject, int> into)
        {
            if (into == null)
                throw new ArgumentNullException(nameof(into));

            into.Clear();

            if (timeline == null || timeline.Waves == null)
                return;

            Dictionary<EnemyConfig, int> peaks = new Dictionary<EnemyConfig, int>();
            Dictionary<EnemyConfig, int> guaranteed = new Dictionary<EnemyConfig, int>();

            for (int waveIndex = 0; waveIndex < timeline.Waves.Count; waveIndex++)
            {
                Wave wave = timeline.Waves[waveIndex];

                if (wave == null)
                    continue;

                for (int entry = 0; entry < wave.Enemies.Count; entry++)
                {
                    WeightedEnemy weighted = wave.Enemies[entry];

                    if (weighted == null || weighted.Enemy == null || weighted.Weight <= 0f)
                        continue;

                    Raise(peaks, weighted.Enemy, wave.LiveCap);
                }

                guaranteed.Clear();

                for (int entry = 0; entry < wave.Guaranteed.Count; entry++)
                {
                    GuaranteedSpawn spawn = wave.Guaranteed[entry];

                    if (spawn == null || spawn.Enemy == null || spawn.Count <= 0)
                        continue;

                    guaranteed.TryGetValue(spawn.Enemy, out int count);
                    guaranteed[spawn.Enemy] = count + spawn.Count;
                }

                foreach (KeyValuePair<EnemyConfig, int> pair in guaranteed)
                    Raise(peaks, pair.Key, pair.Value);
            }

            int cap = timeline.PrewarmCapPerEnemy;

            foreach (KeyValuePair<EnemyConfig, int> pair in peaks)
            {
                EnemyConfig enemy = pair.Key;
                int count = Mathf.Min(pair.Value, cap);

                if (count <= 0)
                    continue;

                Add(into, enemy.ViewPrefab, count);

                if (enemy.DismemberedViewPrefab != null)
                    Add(into, enemy.DismemberedViewPrefab, Mathf.CeilToInt(count * enemy.DismemberChance));

                if (enemy.RangedAttack != null)
                    Add(into, enemy.RangedAttack.BoltPrefab, count);
            }
        }

        private static void Raise(Dictionary<EnemyConfig, int> peaks, EnemyConfig enemy, int count)
        {
            if (peaks.TryGetValue(enemy, out int peak) && peak >= count)
                return;

            peaks[enemy] = count;
        }

        private static void Add(Dictionary<GameObject, int> into, GameObject prefab, int count)
        {
            if (prefab == null || count <= 0)
                return;

            into.TryGetValue(prefab, out int existing);
            into[prefab] = existing + count;
        }
    }
}

using Game.Configs;
using System;
using System.Collections.Generic;

namespace Game.Simulation.Services
{
    public sealed class WeightedEnemyPicker
    {
        public EnemyConfig Pick(IReadOnlyList<WeightedEnemy> entries, float roll)
        {
            float total = 0f;

            for (int i = 0; i < entries.Count; i++)
            {
                float weight = entries[i].Weight;

                if (weight > 0f)
                    total += weight;
            }

            if (total <= 0f)
                return null;

            float target = roll * total;
            float cursor = 0f;
            WeightedEnemy entry = null;

            for (int i = 0; i < entries.Count; i++)
            {
                float weight = entries[i].Weight;

                if (weight <= 0f)
                    continue;

                cursor += weight;
                entry = entries[i];

                if (cursor > target)
                    return entries[i].Enemy;
            }

            return entry.Enemy;
        }
    }
}

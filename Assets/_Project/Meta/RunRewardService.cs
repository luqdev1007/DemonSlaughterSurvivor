using Game.Configs;
using Game.Core;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Meta
{
    public sealed class RunRewardService : IRunRewardService
    {
        private readonly MetaProgress _progress;
        private readonly IContentRegistry _content;

        public RunRewardService(MetaProgress progress, IContentRegistry content)
        {
            _progress = progress;
            _content = content;
        }

        public RunRewards Grant(RunReport report)
        {
            RunRewards rewards = Compute(report);

            _progress.GrantRewards(rewards, report.Victory ? report.BossesKilled : System.Array.Empty<string>());

            return rewards;
        }

        public RunRewards Compute(RunReport report)
        {
            float coefficient = 0f;
            int bossBonus = 0;

            _content.TryGet(report.LevelId, out LevelConfig level);

            if (level != null)
                coefficient = level.MemoryShardsPerEffectiveDamage;

            string bossId = report.BossesKilled.Count > 0 ? report.BossesKilled[0] : null;

            if (bossId != null && level != null)
                bossBonus = level.BossMemoryShards;

            int fromDamage = Mathf.RoundToInt(report.EffectiveDamage * coefficient);

            List<MagicShardLine> lines = new List<MagicShardLine>();
            IReadOnlyList<RunRewardsConfig> configs = _content.All<RunRewardsConfig>();
            RunRewardsConfig rewardsConfig = configs.Count > 0 ? configs[0] : null;

            for (int rarity = 0; rarity < report.ItemsByRarity.Count; rarity++)
            {
                int count = report.ItemsByRarity[rarity];

                if (count <= 0)
                    continue;

                int weight = rewardsConfig == null ? 0 : rewardsConfig.MagicShards((Rarity)rarity);
                lines.Add(new MagicShardLine(rarity, count, weight));
            }

            string relicId = null;
            bool repeat = false;

            if (report.Victory && bossId != null)
            {
                RelicConfig relic = RelicFor(bossId);

                if (relic != null)
                {
                    relicId = relic.Id;
                    repeat = _progress.OwnsRelic(relic.Id);
                }
            }

            return new RunRewards(report.EffectiveDamage, coefficient, fromDamage, bossId, bossBonus, lines, relicId, repeat);
        }

        private RelicConfig RelicFor(string bossId)
        {
            foreach (RelicConfig relic in _content.All<RelicConfig>())
            {
                if (relic.Boss != null && relic.Boss.Id == bossId)
                    return relic;
            }

            return null;
        }
    }
}

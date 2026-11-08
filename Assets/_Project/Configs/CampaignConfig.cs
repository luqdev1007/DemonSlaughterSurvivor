using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Configs
{
    [Serializable]
    public sealed class CampaignLevel
    {
        [SerializeField] private LevelConfig _level;
        [SerializeField] private LevelConfig[] _prerequisites;
        [SerializeField] private Sprite _preview;

        public CampaignLevel(LevelConfig level, LevelConfig[] prerequisites, Sprite preview = null)
        {
            _level = level;
            _prerequisites = prerequisites;
            _preview = preview;
        }

        public LevelConfig Level => _level;
        public IReadOnlyList<LevelConfig> Prerequisites => _prerequisites ?? Array.Empty<LevelConfig>();
        public Sprite Preview => _preview;
    }

    [CreateAssetMenu(fileName = "Campaign", menuName = "Game/Content/Campaign")]
    public sealed class CampaignConfig : ContentConfig
    {
        public const string FirstLevelId = "level.gameplay";

        [SerializeField] private CampaignLevel[] _levels;

        public IReadOnlyList<CampaignLevel> Levels => _levels ?? Array.Empty<CampaignLevel>();

        public void Validate()
        {
            Validate(Levels);
        }

        public static void Validate(IReadOnlyList<CampaignLevel> levels)
        {
            if (levels == null || levels.Count == 0)
                throw new InvalidOperationException("Campaign has no levels.");

            Dictionary<string, int> indices = new Dictionary<string, int>(StringComparer.Ordinal);

            for (int index = 0; index < levels.Count; index++)
            {
                CampaignLevel entry = levels[index];
                LevelConfig level = entry?.Level;

                if (level == null)
                    throw new InvalidOperationException($"Campaign level at index {index} is null.");

                if (string.IsNullOrWhiteSpace(level.Id))
                    throw new InvalidOperationException($"Campaign level at index {index} has no ID.");

                if (level.Id == "level.stress")
                    throw new InvalidOperationException("Stress level cannot be part of the campaign.");

                if (indices.TryAdd(level.Id, index) == false)
                    throw new InvalidOperationException($"Campaign has duplicate level ID '{level.Id}'.");
            }

            if (levels[0].Level.Id != FirstLevelId)
                throw new InvalidOperationException($"The first campaign level must be '{FirstLevelId}'.");

            if (levels[0].Prerequisites.Count != 0)
                throw new InvalidOperationException("The first campaign level must have no prerequisites.");

            for (int index = 0; index < levels.Count; index++)
            {
                HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);

                foreach (LevelConfig prerequisite in levels[index].Prerequisites)
                {
                    if (prerequisite == null || string.IsNullOrWhiteSpace(prerequisite.Id) || indices.TryGetValue(prerequisite.Id, out int prerequisiteIndex) == false ||
                        ReferenceEquals(levels[prerequisiteIndex].Level, prerequisite) == false)
                        throw new InvalidOperationException($"Campaign level '{levels[index].Level.Id}' references a level outside the campaign.");

                    if (seen.Add(prerequisite.Id) == false)
                        throw new InvalidOperationException($"Campaign level '{levels[index].Level.Id}' has a duplicate prerequisite '{prerequisite.Id}'.");

                    if (prerequisiteIndex >= index)
                        throw new InvalidOperationException($"Campaign level '{levels[index].Level.Id}' has a cyclic or forward prerequisite '{prerequisite.Id}'.");
                }
            }
        }
    }
}

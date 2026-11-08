using Game.Configs;
using Game.Core;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Game.Simulation.Tests
{
    public sealed class CampaignCatalogTests
    {
        private const string CampaignPath = "Assets/_Project/Configs/Campaign.asset";
        private const string DatabasePath = "Assets/_Project/Configs/ContentDatabase.asset";
        private const string TrollPath = "Assets/_Project/Configs/Levels/Level_TrollLair.asset";

        private readonly List<LevelConfig> _created = new List<LevelConfig>();

        [TearDown]
        public void TearDown()
        {
            foreach (LevelConfig level in _created)
                UnityEngine.Object.DestroyImmediate(level);

            _created.Clear();
        }

        [Test]
        public void CatalogAssetHasTwoRegisteredCampaignLevels()
        {
            CampaignConfig campaign = AssetDatabase.LoadAssetAtPath<CampaignConfig>(CampaignPath);
            LevelConfig troll = AssetDatabase.LoadAssetAtPath<LevelConfig>(TrollPath);
            ContentDatabase database = AssetDatabase.LoadAssetAtPath<ContentDatabase>(DatabasePath);

            Assert.IsNotNull(campaign);
            Assert.IsNotNull(troll);
            Assert.IsNotNull(database);
            Assert.DoesNotThrow(campaign.Validate);
            Assert.AreEqual(2, campaign.Levels.Count);
            Assert.AreEqual(CampaignConfig.FirstLevelId, campaign.Levels[0].Level.Id);
            Assert.AreSame(troll, campaign.Levels[1].Level);
            Assert.AreEqual("level.troll_lair", troll.Id);
            Assert.AreEqual("Логово троллей", troll.DisplayName);
            Assert.AreEqual("Gameplay", troll.SceneName);
            Assert.IsNotNull(troll.EnvironmentPrefab);
            Assert.IsNotNull(troll.Waves);
            Assert.AreEqual(1, campaign.Levels[1].Prerequisites.Count);
            Assert.AreSame(campaign.Levels[0].Level, campaign.Levels[1].Prerequisites[0]);

            ContentRegistry registry = new ContentRegistry(database.Entries);
            Assert.AreSame(campaign, registry.Get<CampaignConfig>(campaign.Id));
            Assert.AreSame(troll, registry.Get<LevelConfig>(troll.Id));
            Assert.AreSame(campaign.Levels[0].Level, registry.Get<LevelConfig>(CampaignConfig.FirstLevelId));
            Assert.IsNotNull(registry.Get<LevelConfig>("level.stress"));
        }

        [Test]
        public void RejectsMissingFirstLevelAndNullEntry()
        {
            LevelConfig first = Level(CampaignConfig.FirstLevelId);
            LevelConfig second = Level("level.other");

            Assert.Throws<InvalidOperationException>(() => CampaignConfig.Validate(new[] { Entry(second) }));
            Assert.Throws<InvalidOperationException>(() => CampaignConfig.Validate(new[] { Entry(first), null }));
        }

        [Test]
        public void RejectsDuplicateLevelAndPrerequisite()
        {
            LevelConfig first = Level(CampaignConfig.FirstLevelId);
            LevelConfig second = Level("level.other");

            Assert.Throws<InvalidOperationException>(() => CampaignConfig.Validate(new[] { Entry(first), Entry(first) }));
            Assert.Throws<InvalidOperationException>(() => CampaignConfig.Validate(new[] { Entry(first), Entry(second, first, first) }));
        }

        [Test]
        public void RejectsMissingPrerequisiteAndCycle()
        {
            LevelConfig first = Level(CampaignConfig.FirstLevelId);
            LevelConfig second = Level("level.other");
            LevelConfig outsider = Level("level.outside");

            Assert.Throws<InvalidOperationException>(() => CampaignConfig.Validate(new[] { Entry(first), Entry(second, outsider) }));
            Assert.Throws<InvalidOperationException>(() => CampaignConfig.Validate(new[] { Entry(first), Entry(second, second) }));
            Assert.Throws<InvalidOperationException>(() => CampaignConfig.Validate(new[] { Entry(first, second), Entry(second, first) }));
        }

        [Test]
        public void RejectsStressLevel()
        {
            LevelConfig first = Level(CampaignConfig.FirstLevelId);
            LevelConfig stress = Level("level.stress");

            Assert.Throws<InvalidOperationException>(() => CampaignConfig.Validate(new[] { Entry(first), Entry(stress, first) }));
        }

        private LevelConfig Level(string id)
        {
            LevelConfig level = ScriptableObject.CreateInstance<LevelConfig>();
            typeof(ContentConfig).GetField("_id", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(level, id);
            _created.Add(level);

            return level;
        }

        private static CampaignLevel Entry(LevelConfig level, params LevelConfig[] prerequisites)
        {
            return new CampaignLevel(level, prerequisites);
        }
    }
}

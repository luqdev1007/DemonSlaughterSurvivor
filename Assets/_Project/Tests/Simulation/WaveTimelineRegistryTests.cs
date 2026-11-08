using Game.Configs;
using Game.Core;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Simulation.Tests
{
    public sealed class WaveTimelineRegistryTests
    {
        private const string DatabasePath = "Assets/_Project/Configs/ContentDatabase.asset";
        private const string CastlePath = "Assets/_Project/Configs/Waves/Waves_Gameplay.asset";
        private const string LairPath = "Assets/_Project/Configs/Waves/Waves_TrollLair.asset";

        private readonly List<Object> _owned = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (Object owned in _owned)
            {
                if (owned != null)
                    Object.DestroyImmediate(owned);
            }

            _owned.Clear();
        }

        [Test]
        public void EveryTimelineEnemyOfEveryLevelIsInTheContentDatabase()
        {
            ContentDatabase database = AssetDatabase.LoadAssetAtPath<ContentDatabase>(DatabasePath);

            Assert.IsNotNull(database, $"No ContentDatabase at '{DatabasePath}'.");

            ContentRegistry registry = new ContentRegistry(database.Entries);
            IReadOnlyList<LevelConfig> levels = registry.All<LevelConfig>();

            Assert.Greater(levels.Count, 0, "The database registers no level, so nothing was checked.");

            for (int index = 0; index < levels.Count; index++)
                Assert.DoesNotThrow(() => WaveTimelineValidator.Validate(levels[index].Waves, registry), $"level '{levels[index].Id}'");
        }

        [Test]
        public void EnemyMissingFromTheRegistryFailsWithItsIdAndPlace()
        {
            EnemyConfig enemy = CreateEnemy("enemy.test_unregistered");
            WaveTimelineConfig timeline = CreateTimeline("waves.test", enemy, guaranteed: true);
            ContentRegistry registry = new ContentRegistry(new IContentEntry[] { timeline });

            InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => WaveTimelineValidator.Validate(timeline, registry));

            StringAssert.Contains("enemy.test_unregistered", error.Message);
            StringAssert.Contains("wave 0, guaranteed entry 0", error.Message);
            StringAssert.Contains("not registered in the ContentDatabase", error.Message);
        }

        [Test]
        public void ImpostorWithARegisteredIdFails()
        {
            EnemyConfig registered = CreateEnemy("enemy.test");
            EnemyConfig impostor = CreateEnemy("enemy.test");
            WaveTimelineConfig timeline = CreateTimeline("waves.test", impostor, guaranteed: false);
            ContentRegistry registry = new ContentRegistry(new IContentEntry[] { registered, timeline });

            InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => WaveTimelineValidator.Validate(timeline, registry));

            StringAssert.Contains("enemy.test", error.Message);
            StringAssert.Contains("weighted entry 0", error.Message);
        }

        [Test]
        public void RegisteredEnemyPasses()
        {
            EnemyConfig enemy = CreateEnemy("enemy.test");
            WaveTimelineConfig timeline = CreateTimeline("waves.test", enemy, guaranteed: true);
            ContentRegistry registry = new ContentRegistry(new IContentEntry[] { enemy, timeline });

            Assert.DoesNotThrow(() => WaveTimelineValidator.Validate(timeline, registry));
        }

        [Test]
        public void CastleWavesContainOnlyKnightsArchersAndFinalBossKnight()
        {
            WaveTimelineConfig timeline = AssetDatabase.LoadAssetAtPath<WaveTimelineConfig>(CastlePath);

            Assert.NotNull(timeline);
            Assert.AreEqual(3, timeline.Waves.Count);
            Assert.AreEqual(30f, timeline.Waves[0].DurationSeconds);
            Assert.AreEqual(45f, timeline.Waves[1].DurationSeconds);
            Assert.AreEqual(600f, timeline.Waves[2].DurationSeconds);

            for (int waveIndex = 0; waveIndex < timeline.Waves.Count; waveIndex++)
            {
                Wave wave = timeline.Waves[waveIndex];

                foreach (WeightedEnemy entry in wave.Enemies)
                    Assert.IsTrue(entry.Enemy.Id == "enemy.knight_shield" || entry.Enemy.Id == "enemy.archer");

                foreach (GuaranteedSpawn entry in wave.Guaranteed)
                {
                    if (waveIndex == 2)
                        Assert.AreEqual("boss.knight", entry.Enemy.Boss.Id);
                    else
                        Assert.AreEqual("enemy.archer", entry.Enemy.Id);
                }
            }

            Assert.AreEqual(0, timeline.Waves[0].Guaranteed.Count);
            Assert.AreEqual(1, timeline.Waves[1].Guaranteed.Count);
            Assert.AreEqual(1, timeline.Waves[2].Guaranteed.Count);
            Assert.AreEqual(0f, timeline.Waves[2].Guaranteed[0].OffsetSeconds);
        }

        [Test]
        public void TrollLairWavesContainOnlyTrollsDevilsAndFinalDemonLord()
        {
            WaveTimelineConfig timeline = AssetDatabase.LoadAssetAtPath<WaveTimelineConfig>(LairPath);
            LevelConfig level = AssetDatabase.LoadAssetAtPath<LevelConfig>("Assets/_Project/Configs/Levels/Level_TrollLair.asset");

            Assert.NotNull(timeline);
            Assert.NotNull(level);
            Assert.AreSame(timeline, level.Waves);
            Assert.AreEqual(45f, level.ArenaRadius);
            Assert.AreEqual(30f, timeline.SpawnRadius);
            Assert.AreEqual(3, timeline.Waves.Count);
            Assert.AreEqual(30f, timeline.Waves[0].DurationSeconds);
            Assert.AreEqual(45f, timeline.Waves[1].DurationSeconds);
            Assert.AreEqual(600f, timeline.Waves[2].DurationSeconds);

            foreach (Wave wave in timeline.Waves)
            {
                foreach (WeightedEnemy entry in wave.Enemies)
                    Assert.IsTrue(entry.Enemy.Id == "enemy.troll" || entry.Enemy.Id == "enemy.devil");
            }

            Assert.AreEqual("enemy.troll", timeline.Waves[1].Guaranteed[0].Enemy.Id);
            Assert.AreEqual("boss.demon_lord", timeline.Waves[2].Guaranteed[0].Enemy.Boss.Id);
            Assert.AreEqual(0f, timeline.Waves[2].Guaranteed[0].OffsetSeconds);
        }

        [Test]
        public void TrollLairAndDemonLordAreRegisteredWithValidWaveReferences()
        {
            ContentDatabase database = AssetDatabase.LoadAssetAtPath<ContentDatabase>(DatabasePath);
            LevelConfig level = AssetDatabase.LoadAssetAtPath<LevelConfig>("Assets/_Project/Configs/Levels/Level_TrollLair.asset");
            EnemyConfig demonLord = AssetDatabase.LoadAssetAtPath<EnemyConfig>("Assets/_Project/Configs/Enemies/Enemy_DemonLord.asset");
            BossConfig boss = AssetDatabase.LoadAssetAtPath<BossConfig>("Assets/_Project/Configs/Enemies/Boss_DemonLord.asset");
            ContentRegistry registry = new ContentRegistry(database.Entries);

            Assert.NotNull(level.EnvironmentPrefab);
            Assert.NotNull(level.CameraRigPrefab);
            Assert.NotNull(level.HudPrefab);
            Assert.NotNull(level.Feedback);
            Assert.AreSame(boss, demonLord.Boss);
            Assert.NotNull(demonLord.ViewPrefab);
            Assert.IsNotNull(demonLord.ViewPrefab.GetComponent("ViewInterpolator"));
            Assert.AreSame(level, registry.Get<LevelConfig>(level.Id));
            Assert.AreSame(demonLord, registry.Get<EnemyConfig>(demonLord.Id));
            Assert.AreSame(boss, registry.Get<BossConfig>(boss.Id));
            Assert.AreSame(level.Waves, registry.Get<WaveTimelineConfig>(level.Waves.Id));
            Assert.DoesNotThrow(() => WaveTimelineValidator.Validate(level.Waves, registry));
        }

        private EnemyConfig CreateEnemy(string id)
        {
            GameObject view = new GameObject("View");
            _owned.Add(view);

            EnemyConfig enemy = ScriptableObject.CreateInstance<EnemyConfig>();
            enemy.name = id;
            SetField(enemy, "_id", id);
            SetField(enemy, "_viewPrefab", view);
            _owned.Add(enemy);

            return enemy;
        }

        private WaveTimelineConfig CreateTimeline(string id, EnemyConfig enemy, bool guaranteed)
        {
            Wave wave = new Wave();

            if (guaranteed)
            {
                GuaranteedSpawn spawn = new GuaranteedSpawn();
                SetField(spawn, "_enemy", enemy);
                SetField(wave, "_guaranteed", new[] { spawn });
                SetField(wave, "_enemies", Array.Empty<WeightedEnemy>());
            }
            else
            {
                WeightedEnemy weighted = new WeightedEnemy();
                SetField(weighted, "_enemy", enemy);
                SetField(wave, "_enemies", new[] { weighted });
                SetField(wave, "_guaranteed", Array.Empty<GuaranteedSpawn>());
            }

            WaveTimelineConfig timeline = ScriptableObject.CreateInstance<WaveTimelineConfig>();
            SetField(timeline, "_id", id);
            SetField(timeline, "_waves", new[] { wave });
            _owned.Add(timeline);

            return timeline;
        }

        private static void SetField(object target, string name, object value)
        {
            Type type = target.GetType();
            FieldInfo field = null;

            while (type != null && field == null)
            {
                field = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
                type = type.BaseType;
            }

            Assert.IsNotNull(field, $"{target.GetType().Name}.{name} was not found; the test fixture is out of date.");

            field.SetValue(target, value);
        }
    }
}

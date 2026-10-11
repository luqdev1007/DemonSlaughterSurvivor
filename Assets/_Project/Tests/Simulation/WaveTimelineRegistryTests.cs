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

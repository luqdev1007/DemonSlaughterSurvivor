using Game.Configs;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Simulation.Tests
{
    public sealed class ViewPrewarmPlanTests
    {
        private readonly List<Object> _owned = new List<Object>();
        private readonly Dictionary<GameObject, int> _plan = new Dictionary<GameObject, int>();

        [TearDown]
        public void TearDown()
        {
            for (int index = _owned.Count - 1; index >= 0; index--)
            {
                if (_owned[index] != null)
                    Object.DestroyImmediate(_owned[index]);
            }

            _owned.Clear();
        }

        [Test]
        public void AnEnemyGetsTheHighestLiveCapOfTheWavesItSpawnsIn()
        {
            EnemyConfig devil = Enemy("devil");

            WaveTimelineConfig timeline = Timeline(200, WeightedWave(50, devil), WeightedWave(100, devil), WeightedWave(12));

            ViewPrewarmPlan.Resolve(timeline, _plan);

            Assert.AreEqual(100, _plan[devil.ViewPrefab]);
        }

        [Test]
        public void TheCapLimitsEveryEnemy()
        {
            EnemyConfig devil = Enemy("devil");

            ViewPrewarmPlan.Resolve(Timeline(60, WeightedWave(600, devil)), _plan);

            Assert.AreEqual(60, _plan[devil.ViewPrefab]);
        }

        [Test]
        public void AGuaranteedEnemyGetsItsCountAndAZeroWeightEnemyNothing()
        {
            EnemyConfig boss = Enemy("boss");
            EnemyConfig rare = Enemy("rare");

            Wave wave = WeightedWave(12);
            WeightedEnemy zero = new WeightedEnemy();
            SetField(zero, "_enemy", rare);
            SetField(zero, "_weight", 0f);
            SetField(wave, "_enemies", new[] { zero });
            SetField(wave, "_guaranteed", new[] { Guaranteed(boss, 1), Guaranteed(boss, 2) });

            ViewPrewarmPlan.Resolve(Timeline(100, wave), _plan);

            Assert.AreEqual(3, _plan[boss.ViewPrefab]);
            Assert.IsFalse(_plan.ContainsKey(rare.ViewPrefab));
        }

        [Test]
        public void TheCorpseAndTheBoltFollowTheEnemy()
        {
            EnemyConfig archer = Enemy("archer");
            GameObject corpse = Own(new GameObject("corpse"));
            GameObject bolt = Own(new GameObject("bolt"));
            RangedAttackConfig attack = Own(ScriptableObject.CreateInstance<RangedAttackConfig>());
            SetField(attack, "_boltPrefab", bolt);
            SetField(archer, "_dismemberedViewPrefab", corpse);
            SetField(archer, "_dismemberChance", 0.35f);
            SetField(archer, "_rangedAttack", attack);

            ViewPrewarmPlan.Resolve(Timeline(100, WeightedWave(20, archer)), _plan);

            Assert.AreEqual(20, _plan[archer.ViewPrefab]);
            Assert.AreEqual(7, _plan[corpse]);
            Assert.AreEqual(20, _plan[bolt]);
        }

        [Test]
        public void NoTimelineGivesAnEmptyPlan()
        {
            _plan.Add(Own(new GameObject("stale")), 5);

            ViewPrewarmPlan.Resolve(null, _plan);

            Assert.AreEqual(0, _plan.Count);
        }

        private EnemyConfig Enemy(string name)
        {
            EnemyConfig enemy = Own(ScriptableObject.CreateInstance<EnemyConfig>());
            SetField(enemy, "_id", "enemy." + name);
            SetField(enemy, "_viewPrefab", Own(new GameObject(name)));

            return enemy;
        }

        private static Wave WeightedWave(int liveCap, params EnemyConfig[] enemies)
        {
            Wave wave = new Wave();
            WeightedEnemy[] weighted = new WeightedEnemy[enemies.Length];

            for (int index = 0; index < enemies.Length; index++)
            {
                weighted[index] = new WeightedEnemy();
                SetField(weighted[index], "_enemy", enemies[index]);
            }

            SetField(wave, "_liveCap", liveCap);
            SetField(wave, "_enemies", weighted);
            SetField(wave, "_guaranteed", Array.Empty<GuaranteedSpawn>());

            return wave;
        }

        private static GuaranteedSpawn Guaranteed(EnemyConfig enemy, int count)
        {
            GuaranteedSpawn spawn = new GuaranteedSpawn();
            SetField(spawn, "_enemy", enemy);
            SetField(spawn, "_count", count);

            return spawn;
        }

        private WaveTimelineConfig Timeline(int cap, params Wave[] waves)
        {
            WaveTimelineConfig timeline = Own(ScriptableObject.CreateInstance<WaveTimelineConfig>());
            SetField(timeline, "_waves", waves);
            SetField(timeline, "_prewarmCapPerEnemy", cap);

            return timeline;
        }

        private T Own<T>(T owned) where T : Object
        {
            _owned.Add(owned);

            return owned;
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo field = null;

            for (Type type = target.GetType(); type != null && field == null; type = type.BaseType)
                field = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.IsNotNull(field, $"{target.GetType().Name}.{name} was not found; the test fixture is out of date.");

            field.SetValue(target, value);
        }
    }
}

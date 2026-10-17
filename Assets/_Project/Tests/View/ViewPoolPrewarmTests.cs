using Game.Configs;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.View.Tests
{
    public sealed class ViewPoolPrewarmTests
    {
        private const string TrollConfigPath = "Assets/_Project/Configs/Enemies/Enemy_Troll.asset";
        private const int LiveCap = 3;

        private readonly List<Object> _owned = new List<Object>();

        private ViewPool _pool;
        private GameObject _prefab;

        [SetUp]
        public void SetUp()
        {
            FeedbackConfig feedback = Own(ScriptableObject.CreateInstance<FeedbackConfig>());
            SetField(feedback, "_dissolveMaterial", Own(new Material(Shader.Find("Hidden/Internal-Colored"))));

            EnemyConfig troll = AssetDatabase.LoadAssetAtPath<EnemyConfig>(TrollConfigPath);
            _prefab = troll.ViewPrefab;

            WeightedEnemy weighted = new WeightedEnemy();
            SetField(weighted, "_enemy", troll);

            Wave wave = new Wave();
            SetField(wave, "_liveCap", LiveCap);
            SetField(wave, "_enemies", new[] { weighted });
            SetField(wave, "_guaranteed", Array.Empty<GuaranteedSpawn>());

            WaveTimelineConfig timeline = Own(ScriptableObject.CreateInstance<WaveTimelineConfig>());
            SetField(timeline, "_waves", new[] { wave });

            LevelConfig level = Own(ScriptableObject.CreateInstance<LevelConfig>());
            SetField(level, "_feedback", feedback);
            SetField(level, "_waves", timeline);

            _pool = new ViewPool(level, null, null, null);
        }

        [TearDown]
        public void TearDown()
        {
            ViewPoolTicker ticker = (ViewPoolTicker)GetField(_pool, "_ticker");

            if (ticker != null)
                Object.DestroyImmediate(ticker.gameObject);

            foreach (object entry in (IEnumerable)GetField(_pool, "_all"))
            {
                ViewInterpolator view = (ViewInterpolator)entry.GetType().GetField("View").GetValue(entry);

                if (view != null)
                    Object.DestroyImmediate(view.gameObject);
            }

            for (int index = _owned.Count - 1; index >= 0; index--)
            {
                if (_owned[index] != null)
                    Object.DestroyImmediate(_owned[index]);
            }

            _owned.Clear();
        }

        [Test]
        public void PrewarmFillsTheFreeListWithInactiveViews()
        {
            _pool.Prewarm();

            Assert.AreEqual(LiveCap, FreeCount(_prefab));
            Assert.AreEqual(0, ActiveCount(), "a warmed view is not ticked until it is issued");

            foreach (object entry in (IEnumerable)GetField(_pool, "_all"))
            {
                ViewInterpolator view = (ViewInterpolator)entry.GetType().GetField("View").GetValue(entry);

                Assert.IsFalse(view.gameObject.activeSelf, $"'{view.name}' was left active after the prewarm");
            }
        }

        [Test]
        public void AfterThePrewarmTheLiveCapIsIssuedWithoutNewInstances()
        {
            _pool.Prewarm();

            int created = AllCount();

            for (int index = 0; index < LiveCap; index++)
                _pool.Create(_prefab, Vector3.zero);

            Assert.AreEqual(created, AllCount());
            Assert.AreEqual(0, FreeCount(_prefab));
        }

        [Test]
        public void ASecondPrewarmTopsUpInsteadOfDoubling()
        {
            _pool.Prewarm();

            int created = AllCount();

            _pool.Prewarm();

            Assert.AreEqual(created, AllCount());

            _pool.Create(_prefab, Vector3.zero);
            _pool.Prewarm();

            Assert.AreEqual(created + 1, AllCount(), "only the issued view is replaced");
            Assert.AreEqual(LiveCap, FreeCount(_prefab));
        }

        private int FreeCount(GameObject prefab)
        {
            IDictionary free = (IDictionary)GetField(_pool, "_free");

            return free.Contains(prefab) ? ((ICollection)free[prefab]).Count : 0;
        }

        private int AllCount()
        {
            return ((ICollection)GetField(_pool, "_all")).Count;
        }

        private int ActiveCount()
        {
            return ((List<ViewInterpolator>)GetField(_pool, "_active")).Count;
        }

        private T Own<T>(T asset) where T : Object
        {
            _owned.Add(asset);

            return asset;
        }

        private static object GetField(object target, string name)
        {
            return target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(target);
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

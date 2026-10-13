using Game.Configs;
using Game.Core;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.View.Tests
{
    public sealed class ViewPoolTickTests
    {
        private const string TrollConfigPath = "Assets/_Project/Configs/Enemies/Enemy_Troll.asset";

        private readonly List<Object> _owned = new List<Object>();

        private ViewPool _pool;
        private GameObject _prefab;

        [SetUp]
        public void SetUp()
        {
            FeedbackConfig feedback = Own(ScriptableObject.CreateInstance<FeedbackConfig>());
            SetField(feedback, "_dissolveMaterial", Own(new Material(Shader.Find("Hidden/Internal-Colored"))));

            LevelConfig level = Own(ScriptableObject.CreateInstance<LevelConfig>());
            SetField(level, "_feedback", feedback);

            _pool = new ViewPool(level, null, null, null);

            ScriptableObject troll = AssetDatabase.LoadAssetAtPath<ScriptableObject>(TrollConfigPath);
            _prefab = (GameObject)new SerializedObject(troll).FindProperty("_viewPrefab").objectReferenceValue;
        }

        [TearDown]
        public void TearDown()
        {
            ViewPoolTicker ticker = (ViewPoolTicker)GetField(_pool, "_ticker");

            if (ticker != null)
                Object.DestroyImmediate(ticker.gameObject);

            foreach (object entry in (System.Collections.IEnumerable)GetField(_pool, "_all"))
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
        public void TheTickPlacesEveryActiveViewAtItsSynchronisedPosition()
        {
            IView first = _pool.Create(_prefab, Vector3.zero);
            IView second = _pool.Create(_prefab, Vector3.zero);

            first.SetPosition(new Vector3(1f, 0f, 2f));
            second.SetPosition(new Vector3(-3f, 0f, 4f));

            ((ViewInterpolator)first).transform.position = Vector3.one * 99f;
            ((ViewInterpolator)second).transform.position = Vector3.one * 99f;

            _pool.Tick(Time.time, 0f);

            Assert.AreEqual(new Vector3(1f, 0f, 2f), ((ViewInterpolator)first).transform.position);
            Assert.AreEqual(new Vector3(-3f, 0f, 4f), ((ViewInterpolator)second).transform.position);
            Assert.AreEqual(2, ActiveCount());
            Assert.IsNotNull(GetField(_pool, "_ticker"), "the pool drives its views from one ticker");
        }

        [Test]
        public void ARetiredCorpseIsReturnedFromTheTickAndLeavesTheActiveList()
        {
            IView survivor = _pool.Create(_prefab, Vector3.zero);
            IView corpse = _pool.Create(_prefab, Vector3.zero);
            IView other = _pool.Create(_prefab, Vector3.zero);

            _pool.Retire(corpse, 1f, new DeathBlow(Vector3.zero, false, false));

            _pool.Tick(Time.time + 0.5f, 0f);
            Assert.AreEqual(3, ActiveCount(), "the corpse still lies there");

            _pool.Tick(Time.time + 1.5f, 0f);

            Assert.AreEqual(2, ActiveCount());
            Assert.IsFalse(((ViewInterpolator)corpse).gameObject.activeSelf, "back on the shelf");
            Assert.AreEqual(-1, ((ViewInterpolator)corpse).ActiveSlot);
            Assert.IsTrue(((ViewInterpolator)survivor).gameObject.activeSelf);
            Assert.IsTrue(((ViewInterpolator)other).gameObject.activeSelf);
        }

        [Test]
        public void AReleasedViewIsNoLongerTicked()
        {
            IView view = _pool.Create(_prefab, Vector3.zero);
            IView kept = _pool.Create(_prefab, Vector3.zero);

            view.SetPosition(new Vector3(5f, 0f, 5f));
            kept.SetPosition(new Vector3(2f, 0f, 2f));

            _pool.Release(view);

            Transform released = ((ViewInterpolator)view).transform;
            released.position = Vector3.one * 7f;

            _pool.Tick(Time.time, 0f);

            Assert.AreEqual(Vector3.one * 7f, released.position, "a returned view is left alone");
            Assert.AreEqual(1, ActiveCount());
            Assert.AreEqual(0, ((ViewInterpolator)kept).ActiveSlot, "the last view moved into the freed slot");
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

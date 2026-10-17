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
    public sealed class OptimizedRigPoolTests
    {
        private const string DevilPrefabPath = "Assets/_Project/Prefabs/Enemies/Enemy_Devil.prefab";
        private const string TrollConfigPath = "Assets/_Project/Configs/Enemies/Enemy_Troll.asset";

        private readonly List<Object> _owned = new List<Object>();

        private ViewPool _pool;

        [SetUp]
        public void SetUp()
        {
            FeedbackConfig feedback = Own(ScriptableObject.CreateInstance<FeedbackConfig>());
            SetField(feedback, "_dissolveMaterial", Own(new Material(Shader.Find("Hidden/Internal-Colored"))));

            LevelConfig level = Own(ScriptableObject.CreateInstance<LevelConfig>());
            SetField(level, "_feedback", feedback);

            _pool = new ViewPool(level, null, null, null, null);
        }

        [TearDown]
        public void TearDown()
        {
            GameObject templateRoot = (GameObject)GetField(_pool, "_templateRoot");

            if (templateRoot != null)
                Object.DestroyImmediate(templateRoot);

            ViewPoolTicker ticker = (ViewPoolTicker)GetField(_pool, "_ticker");

            if (ticker != null)
                Object.DestroyImmediate(ticker.gameObject);

            for (int index = _owned.Count - 1; index >= 0; index--)
            {
                if (_owned[index] != null)
                    Object.DestroyImmediate(_owned[index]);
            }

            _owned.Clear();
        }

        [Test]
        public void ViewOfAPrefabWithOptimizedRigHasNoBoneHierarchy()
        {
            GameObject devil = AssetDatabase.LoadAssetAtPath<GameObject>(DevilPrefabPath);

            Assert.IsNotNull(devil.GetComponent<OptimizedRig>(), "the devil prefab is the one the stress test measured; it must carry the marker");

            Animator first = Spawn(devil);
            Animator second = Spawn(devil);

            Assert.IsFalse(first.hasTransformHierarchy);
            Assert.IsFalse(second.hasTransformHierarchy);
            Assert.Less(first.GetComponentsInChildren<Transform>(true).Length, 10, "350 bones must collapse to the animator and its renderers");
            Assert.AreEqual(devil.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length, first.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length);
            Assert.AreEqual(1, ((Dictionary<GameObject, GameObject>)GetField(_pool, "_templates")).Count, "one template per prefab, built once");
        }

        [Test]
        public void ViewOfAPrefabWithoutTheMarkerKeepsItsBones()
        {
            ScriptableObject troll = AssetDatabase.LoadAssetAtPath<ScriptableObject>(TrollConfigPath);
            GameObject prefab = (GameObject)new SerializedObject(troll).FindProperty("_viewPrefab").objectReferenceValue;

            Assert.IsNull(prefab.GetComponent<OptimizedRig>());

            Animator animator = Spawn(prefab);

            Assert.IsTrue(animator.hasTransformHierarchy);
            Assert.AreEqual(prefab.GetComponentsInChildren<Transform>(true).Length, animator.transform.root.GetComponentsInChildren<Transform>(true).Length);
        }

        [Test]
        public void MisspelledExposedBoneFailsLoudly()
        {
            GameObject source = Own(Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(DevilPrefabPath)));
            SetField(source.GetComponent<OptimizedRig>(), "_exposedTransforms", new[] { "NoSuchBone" });

            InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => _pool.Create(source, Vector3.zero));

            StringAssert.Contains("NoSuchBone", error.Message);
        }

        private Animator Spawn(GameObject prefab)
        {
            IView view = _pool.Create(prefab, Vector3.zero);
            GameObject instance = ((ViewInterpolator)view).gameObject;

            _owned.Add(instance);

            return instance.GetComponentInChildren<Animator>(true);
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

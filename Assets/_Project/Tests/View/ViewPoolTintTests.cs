using Game.Configs;
using Game.Core;
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
    public sealed class ViewPoolTintTests
    {
        private const string TrollConfigPath = "Assets/_Project/Configs/Enemies/Enemy_Troll.asset";
        private const int Views = 30;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private readonly List<Object> _owned = new List<Object>();

        private ViewPool _pool;
        private EnemyConfig _troll;

        [SetUp]
        public void SetUp()
        {
            FeedbackConfig feedback = Own(ScriptableObject.CreateInstance<FeedbackConfig>());
            SetField(feedback, "_dissolveMaterial", Own(new Material(Shader.Find("Hidden/Internal-Colored"))));

            LevelConfig level = Own(ScriptableObject.CreateInstance<LevelConfig>());
            SetField(level, "_feedback", feedback);

            _troll = AssetDatabase.LoadAssetAtPath<EnemyConfig>(TrollConfigPath);

            _pool = new ViewPool(level, new ContentRegistry(new IContentEntry[] { _troll }), null, null);
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

            foreach (Material copy in ((IDictionary)GetField(_pool, "_tintCopies")).Values)
                Object.DestroyImmediate(copy);

            for (int index = _owned.Count - 1; index >= 0; index--)
            {
                if (_owned[index] != null)
                    Object.DestroyImmediate(_owned[index]);
            }

            _owned.Clear();
        }

        [Test]
        public void EveryIssuedViewWearsOneOfTheConfiguredTints()
        {
            Assert.Greater(_troll.Tints.Count, 1, "the troll config is expected to carry several tints");

            Renderer[] reference = _troll.ViewPrefab.GetComponentsInChildren<Renderer>(true);
            HashSet<Color> seen = new HashSet<Color>();

            for (int index = 0; index < Views; index++)
            {
                ViewInterpolator view = (ViewInterpolator)_pool.Create(_troll.ViewPrefab, Vector3.zero);
                Renderer[] renderers = view.GetComponentsInChildren<Renderer>(true);

                Color tint = TintOf(renderers, reference);

                Assert.IsTrue(IsConfigured(tint), $"view {index} wears {tint}, which is not one of the troll's tints");

                seen.Add(tint);
            }

            Assert.Greater(seen.Count, 1, $"{Views} views all wore the same tint");
        }

        [Test]
        public void ViewsOfOneTintShareTheMaterialCopies()
        {
            for (int index = 0; index < Views; index++)
                _pool.Create(_troll.ViewPrefab, Vector3.zero);

            int tintable = 0;
            HashSet<Material> sources = new HashSet<Material>();

            foreach (Renderer renderer in _troll.ViewPrefab.GetComponentsInChildren<Renderer>(true))
            {
                foreach (Material material in renderer.sharedMaterials)
                {
                    if (material != null && material.HasProperty(BaseColorId) && sources.Add(material))
                        tintable++;
                }
            }

            int nonWhite = 0;

            foreach (Color tint in _troll.Tints)
            {
                if (tint != Color.white)
                    nonWhite++;
            }

            Assert.AreEqual(tintable * nonWhite, ((IDictionary)GetField(_pool, "_tintCopies")).Count,
                "copies are made once per source material and tint, not per view");
        }

        private bool IsConfigured(Color tint)
        {
            foreach (Color configured in _troll.Tints)
            {
                if (Mathf.Abs(configured.r - tint.r) < 0.01f && Mathf.Abs(configured.g - tint.g) < 0.01f && Mathf.Abs(configured.b - tint.b) < 0.01f)
                    return true;
            }

            return false;
        }

        private static Color TintOf(Renderer[] renderers, Renderer[] reference)
        {
            for (int index = 0; index < renderers.Length; index++)
            {
                Material material = renderers[index].sharedMaterial;
                Material source = reference[index].sharedMaterial;

                if (material == null || source == null || source.HasProperty(BaseColorId) == false)
                    continue;

                Color now = material.GetColor(BaseColorId);
                Color was = source.GetColor(BaseColorId);

                return new Color(Ratio(now.r, was.r), Ratio(now.g, was.g), Ratio(now.b, was.b), 1f);
            }

            Assert.Fail("the troll has no tintable renderer");

            return default;
        }

        private static float Ratio(float now, float was)
        {
            return Mathf.Round(now / was * 100f) / 100f;
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

using NUnit.Framework;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Game.View.Tests
{
    public sealed class BerserkLightningFxTests
    {
        private const float FadeSeconds = 0.15f;

        private readonly List<Object> _owned = new List<Object>();

        private GameObject _root;
        private ViewInterpolator _view;
        private BerserkLightningFx _fx;
        private Renderer _body;
        private Material _lightningMaterial;

        [SetUp]
        public void SetUp()
        {
            _lightningMaterial = Own(new Material(Shader.Find("Hidden/Internal-Colored")));

            _root = Own(new GameObject("Hero"));
            _view = _root.AddComponent<ViewInterpolator>();

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.transform.SetParent(_root.transform, false);
            _body = body.GetComponent<Renderer>();

            GameObject lightning = new GameObject("BerserkLightning");
            lightning.transform.SetParent(_root.transform, false);
            _fx = lightning.AddComponent<BerserkLightningFx>();

            SerializedObject fx = new SerializedObject(_fx);
            fx.FindProperty("_material").objectReferenceValue = _lightningMaterial;
            fx.FindProperty("_fadeSeconds").floatValue = FadeSeconds;
            fx.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject view = new SerializedObject(_view);
            view.FindProperty("_berserkFx").objectReferenceValue = _fx;
            view.ApplyModifiedPropertiesWithoutUndo();
        }

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
        public void RepeatedTrueDoesNotRestartTheFade()
        {
            _view.SetBerserk(true);
            _fx.Tick(FadeSeconds * 0.5f);

            float halfway = _fx.Alpha;

            _view.SetBerserk(true);

            Assert.AreEqual(0.5f, halfway, 1e-5f, "fixture: half the fade must give half the alpha");
            Assert.AreEqual(halfway, _fx.Alpha, "a repeated true must not reset the fade");
            Assert.IsTrue(_fx.IsShowing);

            _fx.Tick(FadeSeconds * 0.5f);

            Assert.AreEqual(1f, _fx.Alpha, 1e-5f, "the fade must continue from where it was");
        }

        [Test]
        public void TwoModesGiveExactlyTwoActivations()
        {
            bool[] perTick = { false, false, true, true, true, false, false, true, true, false, false };
            int activations = 0;
            bool previous = _fx.IsShowing;

            foreach (bool state in perTick)
            {
                _view.SetBerserk(state);
                _fx.Tick(1f / 60f);

                if (_fx.IsShowing && previous == false)
                    activations++;

                previous = _fx.IsShowing;
            }

            Assert.AreEqual(2, activations);
        }

        [Test]
        public void FadeOutDisablesTheArcs()
        {
            _view.SetBerserk(true);
            _fx.Tick(FadeSeconds);

            Assert.IsTrue(AllArcsEnabled(true), "the arcs must be drawn while the effect is on");

            _view.SetBerserk(false);
            _fx.Tick(FadeSeconds * 0.5f);

            Assert.IsTrue(AllArcsEnabled(true), "the arcs must stay drawn while fading out");

            _fx.Tick(FadeSeconds * 0.5f);

            Assert.AreEqual(0f, _fx.Alpha);
            Assert.IsTrue(AllArcsEnabled(false), "the arcs must be switched off once faded out");
        }

        [Test]
        public void DissolveFadesTheEffectOutAndKeepsItOff()
        {
            _view.SetBerserk(true);
            _fx.Tick(FadeSeconds);

            _view.Dissolve();

            Assert.IsFalse(_fx.IsShowing, "dissolve must turn the effect off");
            Assert.AreEqual(1f, _fx.Alpha, "dissolve must fade, not cut");

            _view.SetBerserk(true);

            Assert.IsFalse(_fx.IsShowing, "the mode still running on the corpse must not bring the effect back");

            _fx.Tick(FadeSeconds);

            Assert.AreEqual(0f, _fx.Alpha);
            Assert.IsTrue(AllArcsEnabled(false));
        }

        [Test]
        public void ResetFeedbackClearsTheEffectAndTheDissolveLock()
        {
            _view.SetBerserk(true);
            _fx.Tick(FadeSeconds);
            _view.Dissolve();

            _view.ResetFeedback();

            Assert.IsFalse(_fx.IsShowing);
            Assert.AreEqual(0f, _fx.Alpha);
            Assert.IsTrue(AllArcsEnabled(false));

            _view.SetBerserk(true);

            Assert.IsTrue(_fx.IsShowing, "a view back from the pool must show the effect again");
        }

        [Test]
        public void ConfigureKeepsTheArcsOutOfDissolveAndBlink()
        {
            _fx.SetShowing(true);
            _fx.SetShowing(false);

            LineRenderer[] arcs = Arcs();
            Material dissolve = Own(new Material(Shader.Find("Hidden/Internal-Colored")));

            Assert.Greater(arcs.Length, 0, "fixture: the arcs must exist before Configure, as they do after Awake");

            _view.Configure(null, 0f, 0.1f, 0.8f, _ => dissolve, null);

            Renderer[] own = (Renderer[])typeof(ViewInterpolator)
                .GetField("_renderers", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(_view);

            CollectionAssert.Contains(own, _body);

            foreach (LineRenderer arc in arcs)
                CollectionAssert.DoesNotContain(own, arc, "a lightning arc must not be dissolved or blinked with the body");

            _view.Dissolve();

            Assert.AreSame(dissolve, _body.sharedMaterial, "fixture: the body must take the dissolve material");

            foreach (LineRenderer arc in arcs)
                Assert.AreSame(_lightningMaterial, arc.sharedMaterial);
        }

        [Test]
        public void ViewWithoutTheEffectIgnoresBerserk()
        {
            SerializedObject view = new SerializedObject(_view);
            view.FindProperty("_berserkFx").objectReferenceValue = null;
            view.ApplyModifiedPropertiesWithoutUndo();

            Assert.DoesNotThrow(() => _view.SetBerserk(true));
            Assert.IsFalse(_fx.IsShowing);
        }

        private LineRenderer[] Arcs()
        {
            return _fx.GetComponentsInChildren<LineRenderer>(true);
        }

        private bool AllArcsEnabled(bool expected)
        {
            LineRenderer[] arcs = Arcs();

            if (arcs.Length == 0)
                return false;

            foreach (LineRenderer arc in arcs)
            {
                if (arc.enabled != expected)
                    return false;
            }

            return true;
        }

        private T Own<T>(T owned) where T : Object
        {
            _owned.Add(owned);

            return owned;
        }
    }
}

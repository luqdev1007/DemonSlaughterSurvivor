using Game.Configs;
using Game.Configs.Editor;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Simulation.Tests
{
    public sealed class SwingBakeFreshnessTests
    {
        private const float PositionTolerance = 1e-4f;
        private const string AttackLayer = "Attack";
        private const string SpinLayer = "Spin";

        private sealed class BakedSwing
        {
            public BakedSwing(string label, WeaponConfig inputs, SwingVariant variant, string layer)
            {
                Label = label;
                Inputs = inputs;
                Variant = variant;
                Layer = layer;
            }

            public string Label { get; }

            public WeaponConfig Inputs { get; }

            public SwingVariant Variant { get; }

            public string Layer { get; }
        }

        private static IEnumerable<WeaponConfig> Weapons()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:" + nameof(WeaponConfig)))
                yield return AssetDatabase.LoadAssetAtPath<WeaponConfig>(AssetDatabase.GUIDToAssetPath(guid));
        }

        private static IEnumerable<CounterStrikeConfig> CounterStrikes()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:" + nameof(CounterStrikeConfig)))
                yield return AssetDatabase.LoadAssetAtPath<CounterStrikeConfig>(AssetDatabase.GUIDToAssetPath(guid));
        }

        private static IEnumerable<BakedSwing> Swings()
        {
            foreach (WeaponConfig weapon in Weapons())
            {
                for (int index = 0; index < weapon.VariantCount; index++)
                {
                    SwingVariant variant = weapon.Variant(index);

                    yield return new BakedSwing($"{weapon.Id} / {variant.AnimatorTrigger}", weapon, variant, AttackLayer);
                }
            }

            foreach (CounterStrikeConfig counter in CounterStrikes())
            {
                Assert.IsNotNull(counter.BakeSource, $"{counter.name} has no bake source weapon.");

                yield return new BakedSwing($"{counter.name} / {counter.Swing.AnimatorTrigger}", counter.BakeSource, counter.Swing, SpinLayer);
            }
        }

        [Test]
        public void ThereIsAtLeastOneWeaponAndOneCounterStrikeToCheck()
        {
            int weapons = 0;
            int counters = 0;

            foreach (WeaponConfig weapon in Weapons())
                weapons++;

            foreach (CounterStrikeConfig counter in CounterStrikes())
                counters++;

            Assert.Greater(weapons, 0);
            Assert.Greater(counters, 0);
        }

        [Test]
        public void EveryBakeMatchesTheClipItsStatePlays()
        {
            foreach (WeaponConfig weapon in Weapons())
            {
                Assert.Greater(weapon.VariantCount, 0, $"{weapon.Id} has no swing variants.");
                Assert.IsTrue(weapon.TriggerCoverage.IsBaked && weapon.TriggerCoverage.CoveredCount > 0, $"{weapon.Id}: trigger coverage is empty; the weapon would never swing.");
            }

            foreach (BakedSwing swing in Swings())
            {
                SwingVariant variant = swing.Variant;
                string label = swing.Label;

                Assert.IsNotNull(variant.Bake, $"{label}: no bake asset.");

                AnimationClip clip = SwingBaker.FindClip(swing.Inputs.BakeRig, variant.AnimatorTrigger);

                Assert.AreSame(clip, variant.Bake.Clip, $"{label}: the controller now plays another clip; rebake with Game/Bake Weapon Swings.");
                Assert.AreEqual(SwingClipHash.Compute(clip), variant.Bake.ClipHash, $"{label}: the clip curves changed after the bake; rebake.");
                Assert.AreEqual(clip.length, variant.Bake.ClipLength, $"{label}: the clip length changed after the bake; rebake.");
                Assert.AreEqual(swing.Inputs.BakeSampleRate, variant.Bake.SampleRate, $"{label}: the sample rate changed after the bake; rebake.");
                Assert.AreEqual(Mathf.FloorToInt(clip.length * variant.Bake.SampleRate + 1e-3f) + 1, variant.Bake.SampleCount, $"{label}: sample count does not match the clip length.");

                Assert.GreaterOrEqual(variant.WindowStart, 0f, $"{label}: window starts before the clip.");
                Assert.Greater(variant.WindowEnd, variant.WindowStart, $"{label}: empty window.");
                Assert.LessOrEqual(variant.WindowEnd, clip.length, $"{label}: window ends after the clip.");
                Assert.Greater(variant.PlaybackSpeed, 0f, $"{label}: playback speed must be positive.");
                Assert.IsTrue(variant.HalfAngleDegrees > 0f && (variant.HalfAngleDegrees <= 90f || variant.HalfAngleDegrees >= 180f), $"{label}: half angle must be in (0, 90] or 180 and above.");
                Assert.IsTrue(variant.Coverage.IsBaked && variant.Coverage.CoveredCount > 0, $"{label}: coverage is not baked.");
                Assert.Greater(variant.MaxReach, 0f, $"{label}: max reach is not baked.");
            }
        }

        [Test]
        public void EverySwingStatePlaysTheBakedClipAtTheSimulationSpeed()
        {
            foreach (BakedSwing swing in Swings())
            {
                Animator animator = swing.Inputs.BakeRig.GetComponentInChildren<Animator>(true);
                AnimatorController controller = animator.runtimeAnimatorController as AnimatorController;

                Assert.IsNotNull(controller, $"{swing.Label}: the bake rig has no AnimatorController.");
                Assert.IsFalse(animator.applyRootMotion, $"{swing.Label}: root motion is on; the simulation owns movement.");

                AnimatorControllerLayer layer = FindLayer(controller, swing.Layer);
                AnimatorState state = FindState(layer.stateMachine, swing.Variant.AnimatorTrigger);

                Assert.IsNotNull(state, $"{swing.Layer} layer has no state '{swing.Variant.AnimatorTrigger}'.");
                Assert.AreSame(swing.Variant.Bake.Clip, state.motion, $"'{swing.Variant.AnimatorTrigger}' plays another clip than the bake.");
                Assert.AreEqual(1f, state.speed, $"'{swing.Variant.AnimatorTrigger}' has its own speed; the playback speed lives in the config.");
                Assert.IsTrue(state.speedParameterActive && state.speedParameter == "AttackSpeed", $"'{swing.Variant.AnimatorTrigger}' does not follow the AttackSpeed parameter.");
            }
        }

        [Test]
        public void SpinLayerOverridesTheWholeBodyAndYieldsToTheDash()
        {
            foreach (CounterStrikeConfig counter in CounterStrikes())
            {
                AnimatorController controller = counter.BakeSource.BakeRig.GetComponentInChildren<Animator>(true).runtimeAnimatorController as AnimatorController;

                AnimatorControllerLayer spin = FindLayer(controller, SpinLayer);
                AnimatorControllerLayer attack = FindLayer(controller, AttackLayer);

                Assert.AreEqual(AnimatorLayerBlendingMode.Override, spin.blendingMode, "Spin must override the layers below.");
                Assert.AreEqual(1f, spin.defaultWeight, "Spin must play at full weight.");
                Assert.IsNull(spin.avatarMask, "Spin must drive the whole body, as the bake samples it.");
                Assert.AreEqual("Empty", spin.stateMachine.defaultState.name, "Spin must rest in an empty state.");
                Assert.IsNull(spin.stateMachine.defaultState.motion, "Spin's rest state must not play a clip.");

                Assert.IsTrue(LayerIndex(controller, SpinLayer) > LayerIndex(controller, AttackLayer), "Spin must sit above Attack.");
                Assert.IsTrue(HasAnyStateTransitionToEmpty(spin.stateMachine, "IsDashing"), "a dash must cut the Spin on screen as it does in the simulation.");
                Assert.IsTrue(HasAnyStateTransitionToEmpty(attack.stateMachine, "IsSpecial"), "a special attack must clear the weapon swing on the Attack layer.");
                Assert.IsNull(FindState(attack.stateMachine, counter.Swing.AnimatorTrigger), "the Spin state must live only on the Spin layer.");
            }
        }

        [Test]
        public void ResamplingTheRigReproducesTheBakedBlade()
        {
            foreach (BakedSwing swing in Swings())
            {
                Scene scene = EditorSceneManager.NewPreviewScene();

                try
                {
                    WeaponConfig inputs = swing.Inputs;
                    GameObject instance = Object.Instantiate(inputs.BakeRig);
                    SceneManager.MoveGameObjectToScene(instance, scene);
                    instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

                    Animator animator = instance.GetComponentInChildren<Animator>(true);
                    Transform hand = null;

                    foreach (Transform child in instance.GetComponentsInChildren<Transform>(true))
                    {
                        if (child.name == inputs.HandBoneName)
                            hand = child;
                    }

                    Assert.IsNotNull(hand, $"{swing.Label}: hand bone '{inputs.HandBoneName}' is gone from the rig.");

                    MeshFilter blade = hand.GetComponentInChildren<MeshFilter>(true);
                    SwingBake bake = swing.Variant.Bake;

                    SwingBaker.SampleClip(bake.Clip, animator.gameObject, hand, blade, bake.SampleRate, out Vector3[] hands, out Vector3[] tips);

                    Assert.AreEqual(bake.SampleCount, hands.Length);

                    for (int sample = 0; sample < hands.Length; sample++)
                    {
                        bake.SampleAt(sample / bake.SampleRate, out Vector3 bakedHand, out Vector3 bakedTip);

                        Assert.Less((bakedHand - hands[sample]).magnitude, PositionTolerance, $"{swing.Label} sample {sample}: the hand moved; rebake.");
                        Assert.Less((bakedTip - tips[sample]).magnitude, PositionTolerance, $"{swing.Label} sample {sample}: the blade moved; rebake.");
                    }
                }
                finally
                {
                    EditorSceneManager.ClosePreviewScene(scene);
                }
            }
        }

        [Test]
        public void ClipHashIsStableAndSeesAChangedKey()
        {
            WeaponConfig weapon = null;

            foreach (WeaponConfig candidate in Weapons())
                weapon = candidate;

            AnimationClip clip = weapon.Variant(0).Bake.Clip;

            Assert.AreEqual(SwingClipHash.Compute(clip), SwingClipHash.Compute(clip));

            AnimationClip copy = Object.Instantiate(clip);

            try
            {
                Assert.AreEqual(SwingClipHash.Compute(clip), SwingClipHash.Compute(copy));

                EditorCurveBinding binding = AnimationUtility.GetCurveBindings(copy)[0];
                AnimationCurve curve = AnimationUtility.GetEditorCurve(copy, binding);
                Keyframe[] keys = curve.keys;
                keys[keys.Length / 2].value += 0.001f;
                curve.keys = keys;
                AnimationUtility.SetEditorCurve(copy, binding, curve);

                Assert.AreNotEqual(SwingClipHash.Compute(clip), SwingClipHash.Compute(copy));
            }
            finally
            {
                Object.DestroyImmediate(copy);
            }
        }

        private static AnimatorControllerLayer FindLayer(AnimatorController controller, string name)
        {
            foreach (AnimatorControllerLayer layer in controller.layers)
            {
                if (layer.name == name)
                    return layer;
            }

            Assert.Fail($"{controller.name} has no {name} layer.");

            return null;
        }

        private static int LayerIndex(AnimatorController controller, string name)
        {
            AnimatorControllerLayer[] layers = controller.layers;

            for (int index = 0; index < layers.Length; index++)
            {
                if (layers[index].name == name)
                    return index;
            }

            return -1;
        }

        private static AnimatorState FindState(AnimatorStateMachine machine, string name)
        {
            foreach (ChildAnimatorState child in machine.states)
            {
                if (child.state.name == name)
                    return child.state;
            }

            return null;
        }

        private static bool HasAnyStateTransitionToEmpty(AnimatorStateMachine machine, string boolParameter)
        {
            foreach (AnimatorStateTransition transition in machine.anyStateTransitions)
            {
                if (transition.destinationState == null || transition.destinationState.name != "Empty")
                    continue;

                foreach (AnimatorCondition condition in transition.conditions)
                {
                    if (condition.parameter == boolParameter && condition.mode == AnimatorConditionMode.If)
                        return true;
                }
            }

            return false;
        }
    }
}

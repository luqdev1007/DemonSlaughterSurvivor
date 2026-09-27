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

        private static IEnumerable<WeaponConfig> Weapons()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:" + nameof(WeaponConfig)))
                yield return AssetDatabase.LoadAssetAtPath<WeaponConfig>(AssetDatabase.GUIDToAssetPath(guid));
        }

        [Test]
        public void ThereIsAtLeastOneWeaponToCheck()
        {
            int count = 0;

            foreach (WeaponConfig weapon in Weapons())
                count++;

            Assert.Greater(count, 0);
        }

        [Test]
        public void EveryBakeMatchesTheClipItsStatePlays()
        {
            foreach (WeaponConfig weapon in Weapons())
            {
                Assert.Greater(weapon.VariantCount, 0, $"{weapon.Id} has no swing variants.");

                for (int index = 0; index < weapon.VariantCount; index++)
                {
                    SwingVariant variant = weapon.Variant(index);
                    string label = $"{weapon.Id} / {variant.AnimatorTrigger}";

                    Assert.IsNotNull(variant.Bake, $"{label}: no bake asset.");

                    AnimationClip clip = SwingBaker.FindClip(weapon.BakeRig, variant.AnimatorTrigger);

                    Assert.AreSame(clip, variant.Bake.Clip, $"{label}: the controller now plays another clip; rebake with Game/Bake Weapon Swings.");
                    Assert.AreEqual(SwingClipHash.Compute(clip), variant.Bake.ClipHash, $"{label}: the clip curves changed after the bake; rebake.");
                    Assert.AreEqual(clip.length, variant.Bake.ClipLength, $"{label}: the clip length changed after the bake; rebake.");
                    Assert.AreEqual(weapon.BakeSampleRate, variant.Bake.SampleRate, $"{label}: the sample rate changed after the bake; rebake.");
                    Assert.AreEqual(Mathf.FloorToInt(clip.length * variant.Bake.SampleRate + 1e-3f) + 1, variant.Bake.SampleCount, $"{label}: sample count does not match the clip length.");

                    Assert.GreaterOrEqual(variant.WindowStart, 0f, $"{label}: window starts before the clip.");
                    Assert.Greater(variant.WindowEnd, variant.WindowStart, $"{label}: empty window.");
                    Assert.LessOrEqual(variant.WindowEnd, clip.length, $"{label}: window ends after the clip.");
                    Assert.Greater(variant.PlaybackSpeed, 0f, $"{label}: playback speed must be positive.");
                    Assert.IsTrue(variant.HalfAngleDegrees > 0f && (variant.HalfAngleDegrees <= 90f || variant.HalfAngleDegrees >= 180f), $"{label}: half angle must be in (0, 90] or 180 and above.");
                    Assert.IsTrue(variant.Coverage.IsBaked && variant.Coverage.CoveredCount > 0, $"{label}: coverage is not baked.");
                    Assert.Greater(variant.MaxReach, 0f, $"{label}: max reach is not baked.");
                }

                Assert.IsTrue(weapon.TriggerCoverage.IsBaked && weapon.TriggerCoverage.CoveredCount > 0, $"{weapon.Id}: trigger coverage is empty; the weapon would never swing.");
            }
        }

        [Test]
        public void AttackLayerPlaysEveryBakedSwingAtTheSimulationSpeed()
        {
            foreach (WeaponConfig weapon in Weapons())
            {
                Animator animator = weapon.BakeRig.GetComponentInChildren<Animator>(true);
                AnimatorController controller = animator.runtimeAnimatorController as AnimatorController;

                Assert.IsNotNull(controller, $"{weapon.Id}: the bake rig has no AnimatorController.");
                Assert.IsFalse(animator.applyRootMotion, $"{weapon.Id}: root motion is on; the simulation owns movement.");

                AnimatorControllerLayer attack = null;

                foreach (AnimatorControllerLayer layer in controller.layers)
                {
                    if (layer.name == "Attack")
                        attack = layer;
                }

                Assert.IsNotNull(attack, $"{controller.name} has no Attack layer.");

                for (int index = 0; index < weapon.VariantCount; index++)
                {
                    SwingVariant variant = weapon.Variant(index);
                    AnimatorState state = null;

                    foreach (ChildAnimatorState child in attack.stateMachine.states)
                    {
                        if (child.state.name == variant.AnimatorTrigger)
                            state = child.state;
                    }

                    Assert.IsNotNull(state, $"Attack layer has no state '{variant.AnimatorTrigger}'.");
                    Assert.AreSame(variant.Bake.Clip, state.motion, $"'{variant.AnimatorTrigger}' plays another clip than the bake.");
                    Assert.AreEqual(1f, state.speed, $"'{variant.AnimatorTrigger}' has its own speed; the playback speed lives in the weapon config.");
                    Assert.IsTrue(state.speedParameterActive && state.speedParameter == "AttackSpeed", $"'{variant.AnimatorTrigger}' does not follow the AttackSpeed parameter.");
                }
            }
        }

        [Test]
        public void ResamplingTheRigReproducesTheBakedBlade()
        {
            foreach (WeaponConfig weapon in Weapons())
            {
                Scene scene = EditorSceneManager.NewPreviewScene();

                try
                {
                    GameObject instance = Object.Instantiate(weapon.BakeRig);
                    SceneManager.MoveGameObjectToScene(instance, scene);
                    instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

                    Animator animator = instance.GetComponentInChildren<Animator>(true);
                    Transform hand = null;

                    foreach (Transform child in instance.GetComponentsInChildren<Transform>(true))
                    {
                        if (child.name == weapon.HandBoneName)
                            hand = child;
                    }

                    Assert.IsNotNull(hand, $"{weapon.Id}: hand bone '{weapon.HandBoneName}' is gone from the rig.");

                    MeshFilter blade = hand.GetComponentInChildren<MeshFilter>(true);

                    for (int index = 0; index < weapon.VariantCount; index++)
                    {
                        SwingVariant variant = weapon.Variant(index);

                        SwingBaker.SampleClip(variant.Bake.Clip, animator.gameObject, hand, blade, variant.Bake.SampleRate, out Vector3[] hands, out Vector3[] tips);

                        Assert.AreEqual(variant.Bake.SampleCount, hands.Length);

                        for (int sample = 0; sample < hands.Length; sample++)
                        {
                            variant.Bake.SampleAt(sample / variant.Bake.SampleRate, out Vector3 bakedHand, out Vector3 bakedTip);

                            Assert.Less((bakedHand - hands[sample]).magnitude, PositionTolerance, $"{weapon.Id} / {variant.AnimatorTrigger} sample {sample}: the hand moved; rebake.");
                            Assert.Less((bakedTip - tips[sample]).magnitude, PositionTolerance, $"{weapon.Id} / {variant.AnimatorTrigger} sample {sample}: the blade moved; rebake.");
                        }
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
    }
}

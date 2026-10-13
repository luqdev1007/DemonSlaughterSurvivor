using Game.Editor;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.View.Tests
{
    public sealed class TrimmedSkinFreshnessTests
    {
        private const float VertexTolerance = 1e-4f;
        private const float PoseDegrees = 35f;
        private const int Poses = 3;

        private static IEnumerable<TrimmedSkinBaker.Spec> Specs()
        {
            return TrimmedSkinBaker.Specs;
        }

        [TestCaseSource(nameof(Specs))]
        public void EveryBoneOfTheTrimmedSkinCarriesWeight(TrimmedSkinBaker.Spec spec)
        {
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(spec.Model);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(spec.Prefab);

            foreach (SkinnedMeshRenderer trimmed in prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                SkinnedMeshRenderer source = FindRenderer(model, trimmed.name);
                Mesh mesh = trimmed.sharedMesh;

                Assert.AreNotEqual(source.sharedMesh, mesh, $"{trimmed.name} still uses the model's mesh; run Game/Bake Trimmed Skins.");
                Assert.AreEqual(mesh.bindposes.Length, trimmed.bones.Length, $"{trimmed.name}: bones and bind poses disagree.");
                Assert.Less(trimmed.bones.Length, source.bones.Length, $"{trimmed.name} was not trimmed.");

                bool[] weighted = new bool[trimmed.bones.Length];

                foreach (BoneWeight1 weight in mesh.GetAllBoneWeights())
                {
                    if (weight.weight > 0f)
                        weighted[weight.boneIndex] = true;
                }

                for (int bone = 0; bone < weighted.Length; bone++)
                    Assert.IsTrue(weighted[bone], $"{trimmed.name}: bone '{trimmed.bones[bone].name}' carries no weight.");
            }
        }

        [TestCaseSource(nameof(Specs))]
        public void BoneNameHashesMatchThePathsTheOptimizedRigLooksUp(TrimmedSkinBaker.Spec spec)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(spec.Prefab);
            Transform animatorRoot = prefab.GetComponentInChildren<Animator>(true).transform;

            foreach (SkinnedMeshRenderer trimmed in prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                SerializedProperty hashes = new SerializedObject(trimmed.sharedMesh).FindProperty("m_BoneNameHashes");

                Assert.AreEqual(trimmed.bones.Length, hashes.arraySize, $"{trimmed.name}: one hash per bone.");

                for (int bone = 0; bone < trimmed.bones.Length; bone++)
                {
                    uint expected = (uint)Animator.StringToHash(AnimationUtility.CalculateTransformPath(trimmed.bones[bone], animatorRoot));

                    Assert.AreEqual(expected, (uint)hashes.GetArrayElementAtIndex(bone).longValue, $"{trimmed.name}: hash of '{trimmed.bones[bone].name}'.");
                }
            }
        }

        [TestCaseSource(nameof(Specs))]
        public void TheTrimmedSkinDeformsLikeTheModelInAnyPose(TrimmedSkinBaker.Spec spec)
        {
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(spec.Model);
            GameObject instance = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(spec.Prefab));
            List<Object> owned = new List<Object> { instance };

            try
            {
                Transform animatorRoot = instance.GetComponentInChildren<Animator>(true).transform;
                List<KeyValuePair<SkinnedMeshRenderer, SkinnedMeshRenderer>> pairs = new List<KeyValuePair<SkinnedMeshRenderer, SkinnedMeshRenderer>>();

                foreach (SkinnedMeshRenderer trimmed in instance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                    pairs.Add(new KeyValuePair<SkinnedMeshRenderer, SkinnedMeshRenderer>(trimmed, BuildReference(trimmed, FindRenderer(model, trimmed.name), model.transform, animatorRoot)));

                HashSet<Transform> skeleton = new HashSet<Transform>();

                foreach (KeyValuePair<SkinnedMeshRenderer, SkinnedMeshRenderer> pair in pairs)
                    skeleton.UnionWith(pair.Value.bones);

                Transform[] joints = new Transform[skeleton.Count];
                skeleton.CopyTo(joints);
                Quaternion[] rest = new Quaternion[joints.Length];

                for (int joint = 0; joint < joints.Length; joint++)
                    rest[joint] = joints[joint].localRotation;

                for (int pose = 0; pose < Poses; pose++)
                {
                    System.Random random = new System.Random(pose + 1);

                    for (int joint = 0; joint < joints.Length; joint++)
                    {
                        Vector3 euler = new Vector3(Spread(random), Spread(random), Spread(random));
                        joints[joint].localRotation = rest[joint] * Quaternion.Euler(euler);
                    }

                    foreach (KeyValuePair<SkinnedMeshRenderer, SkinnedMeshRenderer> pair in pairs)
                    {
                        float worst = MaxVertexGap(pair.Key, pair.Value, owned);

                        Assert.Less(worst, VertexTolerance, $"{pair.Key.name}, pose {pose}: the trimmed skin is {worst:F5} m off the model's; run Game/Bake Trimmed Skins.");
                    }
                }
            }
            finally
            {
                for (int index = owned.Count - 1; index >= 0; index--)
                    Object.DestroyImmediate(owned[index]);
            }
        }

        private static SkinnedMeshRenderer BuildReference(SkinnedMeshRenderer trimmed, SkinnedMeshRenderer source, Transform modelRoot, Transform animatorRoot)
        {
            GameObject reference = new GameObject(trimmed.name + "_Reference");
            reference.transform.SetParent(trimmed.transform.parent, false);
            reference.transform.localPosition = trimmed.transform.localPosition;
            reference.transform.localRotation = trimmed.transform.localRotation;
            reference.transform.localScale = trimmed.transform.localScale;

            Transform[] bones = new Transform[source.bones.Length];

            for (int bone = 0; bone < bones.Length; bone++)
                bones[bone] = animatorRoot.Find(AnimationUtility.CalculateTransformPath(source.bones[bone], modelRoot));

            SkinnedMeshRenderer renderer = reference.AddComponent<SkinnedMeshRenderer>();
            renderer.sharedMesh = source.sharedMesh;
            renderer.bones = bones;
            renderer.rootBone = trimmed.rootBone;

            return renderer;
        }

        private static float MaxVertexGap(SkinnedMeshRenderer trimmed, SkinnedMeshRenderer reference, List<Object> owned)
        {
            Mesh trimmedBake = new Mesh();
            Mesh referenceBake = new Mesh();
            owned.Add(trimmedBake);
            owned.Add(referenceBake);

            trimmed.BakeMesh(trimmedBake);
            reference.BakeMesh(referenceBake);

            Vector3[] a = trimmedBake.vertices;
            Vector3[] b = referenceBake.vertices;
            float worst = 0f;

            for (int vertex = 0; vertex < a.Length; vertex++)
                worst = Mathf.Max(worst, (a[vertex] - b[vertex]).magnitude);

            return worst;
        }

        private static float Spread(System.Random random)
        {
            return ((float)random.NextDouble() * 2f - 1f) * PoseDegrees;
        }

        private static SkinnedMeshRenderer FindRenderer(GameObject model, string name)
        {
            foreach (SkinnedMeshRenderer renderer in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (renderer.name == name)
                    return renderer;
            }

            Assert.Fail($"'{model.name}' has no skinned renderer '{name}'.");

            return null;
        }
    }
}

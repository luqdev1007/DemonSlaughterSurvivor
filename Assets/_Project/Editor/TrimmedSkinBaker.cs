using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Unity.Collections;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class TrimmedSkinBaker
    {
        private const string MenuPath = "Game/Bake Trimmed Skins";
        private const string BoneNameHashes = "m_BoneNameHashes";

        public sealed class Spec
        {
            public string Model;
            public string Prefab;
            public string Folder;
        }

        public static readonly Spec[] Specs =
        {
            new Spec
            {
                Model = "Assets/SimpleAssets/Demons/Devil/FBX/Devil_Rig.fbx",
                Prefab = "Assets/_Project/Prefabs/Enemies/Enemy_Devil.prefab",
                Folder = "Assets/_Project/Art/Models/Enemies/Demon/TrimmedSkins"
            }
        };

        [MenuItem(MenuPath, false, 121)]
        public static void BakeAllFromMenu()
        {
            Debug.Log(BakeAll());
        }

        public static string BakeAll()
        {
            StringBuilder report = new StringBuilder();

            for (int index = 0; index < Specs.Length; index++)
                Bake(Specs[index], report);

            AssetDatabase.SaveAssets();

            return report.ToString();
        }

        private static void Bake(Spec spec, StringBuilder report)
        {
            GameObject model = Load<GameObject>(spec.Model);
            GameObject contents = PrefabUtility.LoadPrefabContents(spec.Prefab);

            try
            {
                report.AppendLine($"== {spec.Prefab}");

                Transform animatorRoot = contents.GetComponentInChildren<Animator>(true).transform;

                if (AssetDatabase.IsValidFolder(spec.Folder) == false)
                    AssetDatabase.CreateFolder(Path.GetDirectoryName(spec.Folder).Replace('\\', '/'), Path.GetFileName(spec.Folder));

                foreach (SkinnedMeshRenderer target in contents.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    SkinnedMeshRenderer source = FindRenderer(model, target.name);
                    Mesh trimmed = Trim(source, model.transform, animatorRoot, out Transform[] bones);
                    string path = $"{spec.Folder}/{model.name}_{target.name}.asset";

                    target.sharedMesh = Save(trimmed, path);
                    target.bones = bones;

                    report.AppendLine($"{target.name}: {source.bones.Length} -> {bones.Length} bones, {path}");
                }

                PrefabUtility.SaveAsPrefabAsset(contents, spec.Prefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        private static Mesh Trim(SkinnedMeshRenderer source, Transform modelRoot, Transform animatorRoot, out Transform[] bones)
        {
            Mesh mesh = UnityEngine.Object.Instantiate(source.sharedMesh);
            mesh.name = source.sharedMesh.name;

            Transform[] sourceBones = source.bones;
            Matrix4x4[] sourceBinds = mesh.bindposes;
            Dictionary<int, int> remap = new Dictionary<int, int>();
            List<Transform> kept = new List<Transform>();
            List<Matrix4x4> binds = new List<Matrix4x4>();
            List<string> paths = new List<string>();

            NativeArray<byte> counts = new NativeArray<byte>(mesh.GetBonesPerVertex(), Allocator.Temp);
            NativeArray<BoneWeight1> weights = new NativeArray<BoneWeight1>(mesh.GetAllBoneWeights(), Allocator.Temp);

            try
            {
                for (int index = 0; index < weights.Length; index++)
                {
                    BoneWeight1 weight = weights[index];
                    int bone = 0;

                    if (weight.weight > 0f && remap.TryGetValue(weight.boneIndex, out bone) == false)
                    {
                        bone = kept.Count;
                        remap[weight.boneIndex] = bone;

                        string bonePath = AnimationUtility.CalculateTransformPath(sourceBones[weight.boneIndex], modelRoot);
                        Transform mapped = animatorRoot.Find(bonePath);

                        if (mapped == null)
                            throw new InvalidOperationException($"'{source.name}' is skinned to '{bonePath}', which the prefab does not have under '{animatorRoot.name}'.");

                        kept.Add(mapped);
                        binds.Add(sourceBinds[weight.boneIndex]);
                        paths.Add(bonePath);
                    }

                    weight.boneIndex = bone;
                    weights[index] = weight;
                }

                if (kept.Count == 0)
                    throw new InvalidOperationException($"'{source.name}' has no weighted bones.");

                mesh.bindposes = binds.ToArray();
                mesh.SetBoneWeights(counts, weights);
            }
            finally
            {
                counts.Dispose();
                weights.Dispose();
            }

            SerializedObject serialized = new SerializedObject(mesh);
            SerializedProperty hashes = serialized.FindProperty(BoneNameHashes);
            hashes.arraySize = paths.Count;

            for (int index = 0; index < paths.Count; index++)
                hashes.GetArrayElementAtIndex(index).longValue = (uint)Animator.StringToHash(paths[index]);

            serialized.ApplyModifiedPropertiesWithoutUndo();

            bones = kept.ToArray();

            return mesh;
        }

        private static Mesh Save(Mesh mesh, string path)
        {
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);

            if (existing == null)
            {
                AssetDatabase.CreateAsset(mesh, path);

                return mesh;
            }

            EditorUtility.CopySerialized(mesh, existing);
            UnityEngine.Object.DestroyImmediate(mesh);
            EditorUtility.SetDirty(existing);

            return existing;
        }

        private static SkinnedMeshRenderer FindRenderer(GameObject model, string name)
        {
            foreach (SkinnedMeshRenderer renderer in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (renderer.name == name)
                    return renderer;
            }

            throw new InvalidOperationException($"'{model.name}' has no skinned renderer '{name}'.");
        }

        private static T Load<T>(string path) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);

            if (asset == null)
                throw new InvalidOperationException($"Missing {typeof(T).Name} at '{path}'.");

            return asset;
        }
    }
}

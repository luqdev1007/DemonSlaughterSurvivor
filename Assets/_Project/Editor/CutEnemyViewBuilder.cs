using Game.Configs;
using Game.View;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class CutEnemyViewBuilder
    {
        private const string MenuPath = "Game/Build Cut Enemy Views";
        private const string GoreMaterialPath = "Assets/_Project/Art/Models/Enemies/Demon/Materials/Gore.mat";
        private const string HiddenMaterialPath = "Assets/_Project/Art/Materials/M_GoreHidden.mat";
        private const string TorsoName = "Body_torso";
        private const string ArmatureName = "Armature";
        private const float StumpMinWeightShare = 0.02f;
        private const float FloorMargin = 0.3f;
        private const float FallReachPerHeight = 1.1f;
        private const float DefaultChance = 0.35f;

        private static readonly string[] PartNames = { "Body_head", "Body_arm_L", "Body_arm_R" };
        private static readonly DismemberPart[] PartKinds = { DismemberPart.Head, DismemberPart.ArmLeft, DismemberPart.ArmRight };

        private sealed class Spec
        {
            public string Model;
            public string Original;
            public string Output;
            public string Config;
            public bool LiftHipsOutOfArmature;
            public string CopyFromOriginal;
        }

        private static readonly Spec[] Specs =
        {
            new Spec
            {
                Model = "Assets/_Project/Art/Models/Enemies/Demon/Devil_Rig_Cut.fbx",
                Original = "Assets/_Project/Prefabs/Enemies/Enemy_Devil.prefab",
                Output = "Assets/_Project/Prefabs/Enemies/Enemy_Devil_Cut.prefab",
                Config = "Assets/_Project/Configs/Enemies/Enemy_Devil.asset"
            },
            new Spec
            {
                Model = "Assets/Troll/Fbx/Troll_cannibal_Cut.fbx",
                Original = "Assets/_Project/Prefabs/Enemies/Troll.prefab",
                Output = "Assets/_Project/Prefabs/Enemies/Troll_Cut.prefab",
                Config = "Assets/_Project/Configs/Enemies/Enemy_Troll.asset"
            },
            new Spec
            {
                Model = "Assets/_Project/Art/Models/Enemies/KnightShield/Paladin WProp J Nordstrom_Cut.fbx",
                Original = "Assets/_Project/Prefabs/Enemies/KnightShield_Enemy.prefab",
                Output = "Assets/_Project/Prefabs/Enemies/KnightShield_Enemy_Cut.prefab",
                Config = "Assets/_Project/Configs/Enemies/Enemy_KnightShield.asset",
                LiftHipsOutOfArmature = true
            },
            new Spec
            {
                Model = "Assets/_Project/Art/Models/Enemies/Archer/Mannik_Cut.fbx",
                Original = "Assets/_Project/Prefabs/Enemies/Enemy_Archer.prefab",
                Output = "Assets/_Project/Prefabs/Enemies/Enemy_Archer_Cut.prefab",
                Config = "Assets/_Project/Configs/Enemies/Enemy_Archer.asset",
                LiftHipsOutOfArmature = true,
                CopyFromOriginal = "WeaponPoint"
            }
        };

        [MenuItem(MenuPath, false, 120)]
        public static void BuildAll()
        {
            StringBuilder report = new StringBuilder();

            for (int index = 0; index < Specs.Length; index++)
                Build(Specs[index], report);

            Debug.Log(report.ToString());
        }

        public static string Build(string output)
        {
            StringBuilder report = new StringBuilder();

            for (int index = 0; index < Specs.Length; index++)
            {
                if (Specs[index].Output == output)
                    Build(Specs[index], report);
            }

            return report.ToString();
        }

        private static void Build(Spec spec, StringBuilder report)
        {
            Material gore = Load<Material>(GoreMaterialPath);
            Material hidden = Load<Material>(HiddenMaterialPath);

            GameObject original = (GameObject)UnityEngine.Object.Instantiate(Load<GameObject>(spec.Original));
            original.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            GameObject root = new GameObject(System.IO.Path.GetFileNameWithoutExtension(spec.Output));

            try
            {
                report.AppendLine($"== {root.name}");

                root.AddComponent<ViewInterpolator>();

                Transform originalView = original.transform.Find("View");
                GameObject view = CreateView(spec, root.transform, originalView);

                if (string.IsNullOrEmpty(spec.CopyFromOriginal) == false)
                    CopyFromOriginal(spec.CopyFromOriginal, originalView, view.transform, report);

                CopyAttachmentMaterials(view.transform, originalView);

                SkinnedMeshRenderer torso = view.transform.Find(TorsoName).GetComponent<SkinnedMeshRenderer>();
                List<PartBuild> parts = CollectParts(view.transform, gore, report);

                MatchTorsoSlots(torso, parts, report);
                ResolveStumps(parts, report);
                AssignAttachments(view.transform, parts, report);
                HideGore(torso, parts, hidden);
                WidenBounds(root.transform, view.transform, report);
                WriteDismemberable(root, torso, gore, parts);

                GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, spec.Output);

                EnemyConfig config = Load<EnemyConfig>(spec.Config);
                SerializedObject serialized = new SerializedObject(config);
                serialized.FindProperty("_dismemberedViewPrefab").objectReferenceValue = saved;

                if (serialized.FindProperty("_dismemberChance").floatValue <= 0f)
                    serialized.FindProperty("_dismemberChance").floatValue = DefaultChance;

                serialized.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.SaveAssets();

                report.AppendLine($"saved {spec.Output}, wired into {spec.Config}");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(original);
            }
        }

        private sealed class PartBuild
        {
            public DismemberPart Kind;
            public SkinnedMeshRenderer Renderer;
            public int GoreSlot = -1;
            public int TorsoGoreSlot = -1;
            public Vector3 Cap;
            public Transform Stump;
            public readonly List<Renderer> Attachments = new List<Renderer>();
        }

        private static GameObject CreateView(Spec spec, Transform root, Transform originalView)
        {
            GameObject view = (GameObject)PrefabUtility.InstantiatePrefab(Load<GameObject>(spec.Model), root);
            view.name = "View";

            if (spec.LiftHipsOutOfArmature)
            {
                PrefabUtility.UnpackPrefabInstance(view, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

                Transform armature = view.transform.Find(ArmatureName);
                List<Transform> children = new List<Transform>();

                foreach (Transform child in armature)
                    children.Add(child);

                for (int index = 0; index < children.Count; index++)
                    children[index].SetParent(view.transform, false);

                UnityEngine.Object.DestroyImmediate(armature.gameObject);
            }

            view.transform.localPosition = originalView.localPosition;
            view.transform.localRotation = originalView.localRotation;
            view.transform.localScale = originalView.localScale;

            Animator source = originalView.GetComponent<Animator>();
            Animator animator = view.GetComponent<Animator>();

            if (animator == null)
                animator = view.AddComponent<Animator>();

            animator.runtimeAnimatorController = source.runtimeAnimatorController;
            animator.avatar = source.avatar;
            animator.applyRootMotion = source.applyRootMotion;
            animator.cullingMode = source.cullingMode;
            animator.updateMode = source.updateMode;

            return view;
        }

        private static void CopyFromOriginal(string nodeName, Transform originalView, Transform view, StringBuilder report)
        {
            Transform source = FindDeep(originalView, nodeName);

            if (source == null)
                throw new InvalidOperationException($"The original view has no '{nodeName}' to copy.");

            Transform parent = FindDeep(view, source.parent.name);

            if (parent == null)
                throw new InvalidOperationException($"The cut view has no '{source.parent.name}' to hang '{nodeName}' on.");

            GameObject copy = UnityEngine.Object.Instantiate(source.gameObject, parent);
            copy.name = source.name;
            copy.transform.localPosition = source.localPosition;
            copy.transform.localRotation = source.localRotation;
            copy.transform.localScale = source.localScale;

            foreach (SkinnedMeshRenderer skinned in copy.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                foreach (Transform bone in skinned.bones)
                {
                    if (bone != null && bone.IsChildOf(copy.transform) == false)
                        throw new InvalidOperationException($"'{skinned.name}' in the copied '{nodeName}' is skinned to '{bone.name}' outside the copy.");
                }
            }

            report.AppendLine($"copied '{nodeName}' from the original under '{parent.name}'");
        }

        private static void CopyAttachmentMaterials(Transform view, Transform originalView)
        {
            foreach (Renderer renderer in view.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer.name.StartsWith("Body_"))
                    continue;

                Transform match = FindDeep(originalView, renderer.name);

                if (match == null || match.TryGetComponent(out Renderer source) == false)
                    continue;

                if (source.sharedMaterials.Length == renderer.sharedMaterials.Length)
                    renderer.sharedMaterials = source.sharedMaterials;
            }
        }

        private static List<PartBuild> CollectParts(Transform view, Material gore, StringBuilder report)
        {
            List<PartBuild> parts = new List<PartBuild>();

            for (int index = 0; index < PartNames.Length; index++)
            {
                Transform node = view.Find(PartNames[index]);

                if (node == null)
                {
                    report.AppendLine($"no {PartNames[index]}: that part cannot come off");

                    continue;
                }

                PartBuild part = new PartBuild { Kind = PartKinds[index], Renderer = node.GetComponent<SkinnedMeshRenderer>() };
                Material[] materials = part.Renderer.sharedMaterials;

                for (int slot = 0; slot < materials.Length; slot++)
                {
                    if (materials[slot] == gore)
                        part.GoreSlot = slot;
                }

                if (part.GoreSlot < 0)
                    throw new InvalidOperationException($"{part.Renderer.name} has no slot with the gore material; check the import remap.");

                part.Cap = SubmeshCentroid(part.Renderer, part.GoreSlot);
                parts.Add(part);
            }

            return parts;
        }

        private static void MatchTorsoSlots(SkinnedMeshRenderer torso, List<PartBuild> parts, StringBuilder report)
        {
            for (int slot = 1; slot < torso.sharedMesh.subMeshCount; slot++)
            {
                Vector3 centroid = SubmeshCentroid(torso, slot);
                PartBuild best = null;
                float bestDistance = float.MaxValue;

                for (int index = 0; index < parts.Count; index++)
                {
                    float distance = (parts[index].Cap - centroid).magnitude;

                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        best = parts[index];
                    }
                }

                best.TorsoGoreSlot = slot;
                report.AppendLine($"torso slot {slot} -> {best.Renderer.name} (caps {bestDistance:F3} m apart)");
            }
        }

        private static void ResolveStumps(List<PartBuild> parts, StringBuilder report)
        {
            for (int index = 0; index < parts.Count; index++)
            {
                PartBuild part = parts[index];
                float[] shares = WeightShares(part.Renderer);
                float bestDistance = float.MaxValue;

                for (int bone = 0; bone < shares.Length; bone++)
                {
                    Transform candidate = part.Renderer.bones[bone];

                    if (candidate == null || shares[bone] < StumpMinWeightShare)
                        continue;

                    float distance = (candidate.position - part.Cap).magnitude;

                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        part.Stump = candidate;
                    }
                }

                if (part.Stump == null)
                    throw new InvalidOperationException($"{part.Renderer.name} has no bone to use as its stump.");

                report.AppendLine($"{part.Renderer.name}: gore slot {part.GoreSlot}, torso slot {part.TorsoGoreSlot}, stump {part.Stump.name} ({bestDistance:F3} m from the cap)");
            }
        }

        private static void AssignAttachments(Transform view, List<PartBuild> parts, StringBuilder report)
        {
            foreach (Renderer renderer in view.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer.name.StartsWith("Body_"))
                    continue;

                Transform anchor = renderer is SkinnedMeshRenderer skinned ? DominantBone(skinned) : renderer.transform.parent;
                PartBuild owner = null;

                for (int index = 0; index < parts.Count; index++)
                {
                    if (anchor != null && anchor.IsChildOf(parts[index].Stump))
                        owner = parts[index];
                }

                if (owner != null)
                    owner.Attachments.Add(renderer);

                report.AppendLine($"{renderer.name} (anchor {(anchor == null ? "-" : anchor.name)}) -> {(owner == null ? "stays on the body" : owner.Renderer.name)}");
            }
        }

        private static void HideGore(SkinnedMeshRenderer torso, List<PartBuild> parts, Material hidden)
        {
            Material[] torsoMaterials = torso.sharedMaterials;

            for (int slot = 1; slot < torsoMaterials.Length; slot++)
                torsoMaterials[slot] = hidden;

            torso.sharedMaterials = torsoMaterials;

            for (int index = 0; index < parts.Count; index++)
            {
                Material[] materials = parts[index].Renderer.sharedMaterials;
                materials[parts[index].GoreSlot] = hidden;
                parts[index].Renderer.sharedMaterials = materials;
            }
        }

        private static void WidenBounds(Transform root, Transform view, StringBuilder report)
        {
            float top = 0f;
            float radius = 0f;
            Mesh baked = new Mesh();

            try
            {
                foreach (SkinnedMeshRenderer renderer in view.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    renderer.BakeMesh(baked, false);
                    Matrix4x4 matrix = Matrix4x4.TRS(renderer.transform.position, renderer.transform.rotation, Vector3.one);

                    foreach (Vector3 vertex in baked.vertices)
                    {
                        Vector3 world = matrix.MultiplyPoint3x4(vertex);
                        top = Mathf.Max(top, world.y);
                        radius = Mathf.Max(radius, new Vector2(world.x, world.z).magnitude);
                    }
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(baked);
            }

            float half = radius + top * FallReachPerHeight;
            Vector3 min = new Vector3(-half, -FloorMargin, -half);
            Vector3 max = new Vector3(half, top + FloorMargin, half);

            foreach (SkinnedMeshRenderer renderer in view.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                Transform space = renderer.rootBone != null ? renderer.rootBone : renderer.transform;
                Matrix4x4 toLocal = space.worldToLocalMatrix * root.localToWorldMatrix;
                Bounds bounds = new Bounds(toLocal.MultiplyPoint3x4(min), Vector3.zero);

                for (int corner = 0; corner < 8; corner++)
                {
                    Vector3 point = new Vector3(
                        (corner & 1) == 0 ? min.x : max.x,
                        (corner & 2) == 0 ? min.y : max.y,
                        (corner & 4) == 0 ? min.z : max.z);

                    bounds.Encapsulate(toLocal.MultiplyPoint3x4(point));
                }

                renderer.localBounds = bounds;
                renderer.updateWhenOffscreen = false;
            }

            report.AppendLine($"body {top:F2} m tall, {radius:F2} m wide; skinned bounds widened to ±{half:F2} m");
        }

        private static void WriteDismemberable(GameObject root, SkinnedMeshRenderer torso, Material gore, List<PartBuild> parts)
        {
            DismemberableView dismemberable = root.AddComponent<DismemberableView>();
            SerializedObject serialized = new SerializedObject(dismemberable);

            serialized.FindProperty("_torso").objectReferenceValue = torso;
            serialized.FindProperty("_goreMaterial").objectReferenceValue = gore;

            SerializedProperty array = serialized.FindProperty("_parts");
            array.arraySize = parts.Count;

            for (int index = 0; index < parts.Count; index++)
            {
                PartBuild part = parts[index];
                SerializedProperty element = array.GetArrayElementAtIndex(index);

                element.FindPropertyRelative("_kind").enumValueIndex = (int)part.Kind;
                element.FindPropertyRelative("_renderer").objectReferenceValue = part.Renderer;
                element.FindPropertyRelative("_stump").objectReferenceValue = part.Stump;
                element.FindPropertyRelative("_goreSlot").intValue = part.GoreSlot;
                element.FindPropertyRelative("_torsoGoreSlot").intValue = part.TorsoGoreSlot;

                SerializedProperty attachments = element.FindPropertyRelative("_attachments");
                attachments.arraySize = part.Attachments.Count;

                for (int attachment = 0; attachment < part.Attachments.Count; attachment++)
                    attachments.GetArrayElementAtIndex(attachment).objectReferenceValue = part.Attachments[attachment];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Vector3 SubmeshCentroid(SkinnedMeshRenderer renderer, int submesh)
        {
            Mesh baked = new Mesh();

            try
            {
                renderer.BakeMesh(baked, false);
                Matrix4x4 matrix = Matrix4x4.TRS(renderer.transform.position, renderer.transform.rotation, Vector3.one);
                Vector3[] vertices = baked.vertices;
                HashSet<int> used = new HashSet<int>(baked.GetTriangles(submesh));
                Vector3 sum = Vector3.zero;

                foreach (int vertex in used)
                    sum += matrix.MultiplyPoint3x4(vertices[vertex]);

                return used.Count > 0 ? sum / used.Count : Vector3.zero;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(baked);
            }
        }

        private static float[] WeightShares(SkinnedMeshRenderer renderer)
        {
            BoneWeight[] weights = renderer.sharedMesh.boneWeights;
            float[] shares = new float[renderer.bones.Length];

            foreach (BoneWeight weight in weights)
            {
                shares[weight.boneIndex0] += weight.weight0;
                shares[weight.boneIndex1] += weight.weight1;
                shares[weight.boneIndex2] += weight.weight2;
                shares[weight.boneIndex3] += weight.weight3;
            }

            for (int index = 0; index < shares.Length; index++)
                shares[index] /= Mathf.Max(1, weights.Length);

            return shares;
        }

        private static Transform DominantBone(SkinnedMeshRenderer renderer)
        {
            float[] shares = WeightShares(renderer);
            int best = 0;

            for (int index = 1; index < shares.Length; index++)
            {
                if (shares[index] > shares[best])
                    best = index;
            }

            return shares.Length == 0 ? null : renderer.bones[best];
        }

        private static Transform FindDeep(Transform parent, string name)
        {
            if (parent.name == name)
                return parent;

            foreach (Transform child in parent)
            {
                Transform found = FindDeep(child, name);

                if (found != null)
                    return found;
            }

            return null;
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

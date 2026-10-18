using Game.View;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Game.Editor
{
    public static class HuntressAnimatorBuilder
    {
        private const string MenuPath = "Game/Build Huntress Animator";
        private const string ControllerPath = "Assets/_Project/Art/Animations/Huntress/Huntress.controller";
        private const string MaskPath = "Assets/_Project/Art/Animations/Huntress/Huntress_UpperBody.mask";
        private const string BakedFolder = "Assets/_Project/Art/Animations/Huntress/Baked/";
        private const string PrefabPath = "Assets/_Project/Prefabs/Heroes/Huntress.prefab";
        private const string SpineToken = "mixamorig:Spine";
        private const float TurnBlend = 0.12f;
        private const float BowBlend = 0.1f;

        private static readonly string[] SpineBones = { "mixamorig:Spine", "mixamorig:Spine1", "mixamorig:Spine2" };
        private static readonly float[] SpineWeights = { 0.3f, 0.3f, 0.4f };

        private static readonly (string Clip, Vector2 Position)[] Locomotion =
        {
            ("Huntress_Idle", new Vector2(0f, 0f)),
            ("Huntress_WalkForward", new Vector2(0f, 1.17f)),
            ("Huntress_WalkBack", new Vector2(0f, -0.95f)),
            ("Huntress_WalkLeft", new Vector2(-1.5f, 0f)),
            ("Huntress_WalkRight", new Vector2(1.58f, 0f)),
            ("Huntress_RunForward", new Vector2(0f, 3.56f)),
            ("Huntress_RunBack", new Vector2(0f, -2.85f)),
            ("Huntress_RunLeft", new Vector2(-3.13f, 0f)),
            ("Huntress_RunRight", new Vector2(3.25f, 0f)),
        };

        [MenuItem(MenuPath, false, 124)]
        public static void Build()
        {
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(PrefabPath);

            try
            {
                Animator animator = prefabRoot.GetComponentInChildren<Animator>(true);

                if (animator == null)
                    throw new InvalidOperationException($"{PrefabPath} has no Animator.");

                AvatarMask mask = BuildMask(animator.transform);
                AnimatorController controller = BuildController(mask);

                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;

                WireRig(prefabRoot, animator);

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"{nameof(HuntressAnimatorBuilder)}: built {ControllerPath} and wired {PrefabPath}.");
        }

        private static AnimatorController BuildController(AvatarMask mask)
        {
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) != null)
                AssetDatabase.DeleteAsset(ControllerPath);

            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

            controller.AddParameter("MoveX", AnimatorControllerParameterType.Float);
            controller.AddParameter("MoveZ", AnimatorControllerParameterType.Float);
            controller.AddParameter("LocomotionSpeed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Turn", AnimatorControllerParameterType.Int);
            controller.AddParameter("AttackSpeed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Draw", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Release", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("BowCancel", AnimatorControllerParameterType.Trigger);

            SetDefault(controller, "LocomotionSpeed", 1f);
            SetDefault(controller, "AttackSpeed", 1f);

            BuildBaseLayer(controller);
            BuildBowLayer(controller, mask);

            EditorUtility.SetDirty(controller);

            return controller;
        }

        private static void BuildBaseLayer(AnimatorController controller)
        {
            AnimatorStateMachine machine = controller.layers[0].stateMachine;

            AnimatorState locomotion = controller.CreateBlendTreeInController("Locomotion", out BlendTree tree, 0);
            tree.blendType = BlendTreeType.FreeformDirectional2D;
            tree.blendParameter = "MoveX";
            tree.blendParameterY = "MoveZ";

            foreach ((string clip, Vector2 position) in Locomotion)
                tree.AddChild(Clip(clip), position);

            locomotion.speedParameterActive = true;
            locomotion.speedParameter = "LocomotionSpeed";
            machine.defaultState = locomotion;

            AnimatorState left = machine.AddState("TurnLeft", new Vector3(300f, 120f, 0f));
            left.motion = Clip("Huntress_TurnLeft");

            AnimatorState right = machine.AddState("TurnRight", new Vector3(300f, -120f, 0f));
            right.motion = Clip("Huntress_TurnRight");

            Connect(locomotion, left, "Turn", AnimatorConditionMode.Equals, -1f);
            Connect(locomotion, right, "Turn", AnimatorConditionMode.Equals, 1f);
            Connect(left, locomotion, "Turn", AnimatorConditionMode.NotEqual, -1f);
            Connect(right, locomotion, "Turn", AnimatorConditionMode.NotEqual, 1f);
        }

        private static void BuildBowLayer(AnimatorController controller, AvatarMask mask)
        {
            AnimatorStateMachine machine = new AnimatorStateMachine { name = "Bow", hideFlags = HideFlags.HideInHierarchy };
            AssetDatabase.AddObjectToAsset(machine, controller);

            AnimatorControllerLayer layer = new AnimatorControllerLayer
            {
                name = "Bow",
                defaultWeight = 1f,
                avatarMask = mask,
                blendingMode = AnimatorLayerBlendingMode.Override,
                stateMachine = machine
            };

            controller.AddLayer(layer);

            AnimatorState empty = machine.AddState("Empty", new Vector3(300f, 0f, 0f));
            AnimatorState draw = machine.AddState("Draw", new Vector3(550f, -80f, 0f));
            AnimatorState hold = machine.AddState("Hold", new Vector3(800f, -80f, 0f));
            AnimatorState release = machine.AddState("Release", new Vector3(550f, 80f, 0f));

            machine.defaultState = empty;

            draw.motion = Clip("Huntress_DrawArrow");
            draw.speedParameterActive = true;
            draw.speedParameter = "AttackSpeed";
            hold.motion = Clip("Huntress_AimHold");
            release.motion = Clip("Huntress_Release");
            release.speedParameterActive = true;
            release.speedParameter = "AttackSpeed";

            AnimatorStateTransition toDraw = machine.AddAnyStateTransition(draw);
            toDraw.AddCondition(AnimatorConditionMode.If, 0f, "Draw");
            toDraw.duration = BowBlend;
            toDraw.hasExitTime = false;
            toDraw.canTransitionToSelf = true;

            AnimatorStateTransition drawToHold = draw.AddTransition(hold);
            drawToHold.hasExitTime = true;
            drawToHold.exitTime = 1f;
            drawToHold.duration = 0.05f;

            AnimatorStateTransition toRelease = machine.AddAnyStateTransition(release);
            toRelease.AddCondition(AnimatorConditionMode.If, 0f, "Release");
            toRelease.duration = 0.04f;
            toRelease.hasExitTime = false;
            toRelease.canTransitionToSelf = true;

            AnimatorStateTransition releaseToEmpty = release.AddTransition(empty);
            releaseToEmpty.hasExitTime = true;
            releaseToEmpty.exitTime = 1f;
            releaseToEmpty.duration = 0.15f;

            AnimatorStateTransition cancel = machine.AddAnyStateTransition(empty);
            cancel.AddCondition(AnimatorConditionMode.If, 0f, "BowCancel");
            cancel.duration = BowBlend;
            cancel.hasExitTime = false;
        }

        private static AvatarMask BuildMask(Transform root)
        {
            AvatarMask mask = AssetDatabase.LoadAssetAtPath<AvatarMask>(MaskPath);

            if (mask == null)
            {
                mask = new AvatarMask();
                AssetDatabase.CreateAsset(mask, MaskPath);
            }

            List<string> paths = new List<string>();

            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child == root)
                    continue;

                paths.Add(AnimationUtility.CalculateTransformPath(child, root));
            }

            mask.transformCount = paths.Count;

            for (int index = 0; index < paths.Count; index++)
            {
                mask.SetTransformPath(index, paths[index]);
                mask.SetTransformActive(index, paths[index].Contains(SpineToken));
            }

            for (int part = 0; part < (int)AvatarMaskBodyPart.LastBodyPart; part++)
                mask.SetHumanoidBodyPartActive((AvatarMaskBodyPart)part, false);

            EditorUtility.SetDirty(mask);

            return mask;
        }

        private static void WireRig(GameObject prefabRoot, Animator animator)
        {
            AimRig rig = prefabRoot.GetComponent<AimRig>();

            if (rig == null)
                rig = prefabRoot.AddComponent<AimRig>();

            Transform[] spine = new Transform[SpineBones.Length];

            for (int index = 0; index < SpineBones.Length; index++)
                spine[index] = FindDeep(animator.transform, SpineBones[index]);

            SerializedObject serializedRig = new SerializedObject(rig);
            serializedRig.FindProperty("_animator").objectReferenceValue = animator;

            SerializedProperty bones = serializedRig.FindProperty("_spine");
            SerializedProperty weights = serializedRig.FindProperty("_spineWeights");
            bones.arraySize = spine.Length;
            weights.arraySize = spine.Length;

            for (int index = 0; index < spine.Length; index++)
            {
                bones.GetArrayElementAtIndex(index).objectReferenceValue = spine[index];
                weights.GetArrayElementAtIndex(index).floatValue = SpineWeights[index];
            }

            serializedRig.ApplyModifiedPropertiesWithoutUndo();

            ViewInterpolator view = prefabRoot.GetComponent<ViewInterpolator>();
            SerializedObject serializedView = new SerializedObject(view);
            serializedView.FindProperty("_aimRig").objectReferenceValue = rig;
            serializedView.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Transform FindDeep(Transform root, string name)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == name)
                    return child;
            }

            throw new InvalidOperationException($"{PrefabPath} has no bone {name}.");
        }

        private static AnimationClip Clip(string name)
        {
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(BakedFolder + name + ".anim");

            if (clip == null)
                throw new InvalidOperationException($"{BakedFolder}{name}.anim is missing. Run Game/Bake Huntress Clips first.");

            return clip;
        }

        private static void Connect(AnimatorState from, AnimatorState to, string parameter, AnimatorConditionMode mode, float threshold)
        {
            AnimatorStateTransition transition = from.AddTransition(to);
            transition.AddCondition(mode, threshold, parameter);
            transition.hasExitTime = false;
            transition.duration = TurnBlend;
        }

        private static void SetDefault(AnimatorController controller, string name, float value)
        {
            AnimatorControllerParameter[] parameters = controller.parameters;

            for (int index = 0; index < parameters.Length; index++)
            {
                if (parameters[index].name == name)
                    parameters[index].defaultFloat = value;
            }

            controller.parameters = parameters;
        }
    }
}

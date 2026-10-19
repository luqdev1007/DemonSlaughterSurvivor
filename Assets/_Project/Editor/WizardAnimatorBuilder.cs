using Game.View;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Game.Editor
{
    public static class WizardAnimatorBuilder
    {
        private const string MenuPath = "Game/Build Wizard Animator";
        private const string Folder = "Assets/_Project/Art/Animations/Wizard/";
        private const string ControllerPath = Folder + "Wizard.controller";
        private const string MaskPath = Folder + "Wizard_UpperBody.mask";
        private const string HoverClipPath = Folder + "Wizard_Hover.anim";
        private const string PrefabPath = "Assets/_Project/Prefabs/Heroes/Wizard.prefab";
        private const string PoseSource = "Standing 1H Magic Attack 01";
        private const string BookName = "Wizard_Book";
        private const string BookMountName = "BookMount";
        private const string HeadBone = "head";
        private static readonly Vector3 BookMountPosition = new Vector3(0f, 1.08f, 0.55f);
        private static readonly Vector3 BookMountEuler = new Vector3(12f, 0f, 0f);
        private const float BookScale = 0.95f;

        private static readonly (string State, string Clip, bool Mirror, string Trigger)[] FireballCasts =
        {
            ("FireballRight1", "Standing 1H Magic Attack 01", false, "CastFireballR1"),
            ("FireballLeft1", "Standing 1H Magic Attack 01", true, "CastFireballL1"),
            ("FireballRight2", "Standing 1H Magic Attack 02", false, "CastFireballR2"),
            ("FireballLeft2", "Standing 1H Magic Attack 02", true, "CastFireballL2"),
        };
        private const string NovaClip = "Standing 2H Magic Area Attack 02";
        private const string RayClip = "Standing 2H Magic Attack 02";
        private const string RayHoldClipPath = Folder + "Wizard_RayHold.anim";
        private const float RayHoldSeconds = 1.6f;
        private const float RayTremble = 0.012f;

        private static readonly (string Muscle, float Value)[] RayArms =
        {
            ("Arm Front-Back", -0.8f),
            ("Arm Down-Up", 0.15f),
            ("Arm Twist In-Out", 0f),
            ("Forearm Stretch", 1f),
            ("Forearm Twist In-Out", 0f),
            ("Hand Down-Up", -0.8f),
            ("Hand In-Out", 0f),
            ("Shoulder Front-Back", 0.2f),
        };
        private const string ViewName = "View";
        private const float CastBlend = 0.12f;
        private const float ViewScale = 0.82f;
        private const float HoverPivot = 0.9f;

        private const float HoverSeconds = 2.4f;
        private const float HoverHeight = 1.15f;
        private const float HoverBob = 0.04f;
        private const int HoverKeys = 24;

        private static readonly (string Muscle, float Left, float Right)[] HangingLegs =
        {
            ("Upper Leg Front-Back", 0.05f, -0.05f),
            ("Upper Leg In-Out", -0.08f, -0.08f),
            ("Upper Leg Twist In-Out", 0f, 0f),
            ("Lower Leg Stretch", 0.95f, 1f),
            ("Lower Leg Twist In-Out", 0f, 0f),
            ("Foot Up-Down", 0.9f, 0.9f),
            ("Foot Twist In-Out", 0f, 0f),
            ("Toes Up-Down", 0.6f, 0.6f),
        };

        private static readonly (string Muscle, float Amplitude, float Phase)[] Sway =
        {
            ("Left Upper Leg Front-Back", 0.05f, 0f),
            ("Right Upper Leg Front-Back", 0.05f, 0.5f),
            ("Left Lower Leg Stretch", 0.04f, 0.25f),
            ("Right Lower Leg Stretch", 0.03f, 0.75f),
            ("Chest Front-Back", 0.03f, 0.1f),
            ("Left Arm Down-Up", 0.03f, 0.2f),
            ("Right Arm Down-Up", 0.03f, 0.7f),
        };

        [MenuItem(MenuPath, false, 125)]
        public static void Build()
        {
            AnimationClip hover = BuildHoverClip();
            AvatarMask mask = BuildMask();
            AnimatorController controller = BuildController(hover, mask);

            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(PrefabPath);

            try
            {
                Animator animator = EnsureViewChild(prefabRoot);

                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
                animator.transform.localScale = Vector3.one * ViewScale;

                WireView(prefabRoot, animator);
                WireBook(prefabRoot, animator);

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"{nameof(WizardAnimatorBuilder)}: built {ControllerPath}, {HoverClipPath} and wired {PrefabPath}.");
        }

        private static AnimationClip BuildHoverClip()
        {
            AnimationClip source = SourceClip(PoseSource);
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(HoverClipPath);

            if (clip == null)
            {
                clip = new AnimationClip();
                AssetDatabase.CreateAsset(clip, HoverClipPath);
            }

            clip.ClearCurves();

            Dictionary<string, float> pose = new Dictionary<string, float>();

            foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(source))
            {
                string name = binding.propertyName;

                if (IsMuscle(name) == false)
                    continue;

                pose[name] = AnimationUtility.GetEditorCurve(source, binding).Evaluate(0f);
            }

            foreach ((string muscle, float left, float right) in HangingLegs)
            {
                pose["Left " + muscle] = left;
                pose["Right " + muscle] = right;
            }

            foreach (KeyValuePair<string, float> entry in pose)
                SetCurve(clip, entry.Key, Wave(entry.Value, SwayOf(entry.Key, out float phase), phase));

            SetCurve(clip, "RootT.x", Wave(0f, 0f, 0f));
            SetCurve(clip, "RootT.y", Wave(HoverHeight, HoverBob, 0f));
            SetCurve(clip, "RootT.z", Wave(0f, 0f, 0f));
            SetCurve(clip, "RootQ.x", Wave(0f, 0f, 0f));
            SetCurve(clip, "RootQ.y", Wave(0f, 0f, 0f));
            SetCurve(clip, "RootQ.z", Wave(0f, 0f, 0f));
            SetCurve(clip, "RootQ.w", Wave(1f, 0f, 0f));

            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.loopBlend = true;
            settings.keepOriginalOrientation = true;
            settings.keepOriginalPositionY = true;
            settings.keepOriginalPositionXZ = true;
            settings.loopBlendOrientation = true;
            settings.loopBlendPositionY = true;
            settings.loopBlendPositionXZ = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            EditorUtility.SetDirty(clip);

            return clip;
        }

        private static bool IsMuscle(string name)
        {
            if (name.StartsWith("RootT") || name.StartsWith("RootQ") || name.StartsWith("Motion"))
                return false;

            return name.Contains("FootT") == false && name.Contains("FootQ") == false && name.Contains("HandT") == false && name.Contains("HandQ") == false;
        }

        private static float SwayOf(string muscle, out float phase)
        {
            foreach ((string name, float amplitude, float offset) in Sway)
            {
                if (name == muscle)
                {
                    phase = offset;

                    return amplitude;
                }
            }

            phase = 0f;

            return 0f;
        }

        private static AnimationCurve Wave(float center, float amplitude, float phase)
        {
            Keyframe[] keys = new Keyframe[HoverKeys + 1];

            for (int index = 0; index <= HoverKeys; index++)
            {
                float time = HoverSeconds * index / HoverKeys;
                float angle = (index / (float)HoverKeys + phase) * Mathf.PI * 2f;
                keys[index] = new Keyframe(time, center + amplitude * Mathf.Sin(angle));
            }

            AnimationCurve curve = new AnimationCurve(keys);

            for (int index = 0; index < keys.Length; index++)
                AnimationUtility.SetKeyLeftTangentMode(curve, index, AnimationUtility.TangentMode.ClampedAuto);

            return curve;
        }

        private static void SetCurve(AnimationClip clip, string property, AnimationCurve curve)
        {
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(string.Empty, typeof(Animator), property), curve);
        }

        private static AvatarMask BuildMask()
        {
            AvatarMask mask = AssetDatabase.LoadAssetAtPath<AvatarMask>(MaskPath);

            if (mask == null)
            {
                mask = new AvatarMask();
                AssetDatabase.CreateAsset(mask, MaskPath);
            }

            for (int part = 0; part < (int)AvatarMaskBodyPart.LastBodyPart; part++)
            {
                AvatarMaskBodyPart bodyPart = (AvatarMaskBodyPart)part;
                bool upper = bodyPart != AvatarMaskBodyPart.Root && bodyPart != AvatarMaskBodyPart.LeftLeg && bodyPart != AvatarMaskBodyPart.RightLeg
                    && bodyPart != AvatarMaskBodyPart.LeftFootIK && bodyPart != AvatarMaskBodyPart.RightFootIK;

                mask.SetHumanoidBodyPartActive(bodyPart, upper);
            }

            EditorUtility.SetDirty(mask);

            return mask;
        }

        private static AnimatorController BuildController(AnimationClip hover, AvatarMask mask)
        {
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) != null)
                AssetDatabase.DeleteAsset(ControllerPath);

            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

            controller.AddParameter("AttackSpeed", AnimatorControllerParameterType.Float);
            foreach ((string _, string _, bool _, string trigger) in FireballCasts)
                controller.AddParameter(trigger, AnimatorControllerParameterType.Trigger);

            controller.AddParameter("CastNova", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("CastRay", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("CastCancel", AnimatorControllerParameterType.Trigger);

            AnimatorControllerParameter[] parameters = controller.parameters;
            parameters[0].defaultFloat = 1f;
            controller.parameters = parameters;

            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            AnimatorState hoverState = machine.AddState("Hover", new Vector3(300f, 0f, 0f));
            hoverState.motion = hover;
            machine.defaultState = hoverState;

            BuildCastLayer(controller, mask);

            EditorUtility.SetDirty(controller);

            return controller;
        }

        private static void BuildCastLayer(AnimatorController controller, AvatarMask mask)
        {
            AnimatorStateMachine machine = new AnimatorStateMachine { name = "Cast", hideFlags = HideFlags.HideInHierarchy };
            AssetDatabase.AddObjectToAsset(machine, controller);

            controller.AddLayer(new AnimatorControllerLayer
            {
                name = "Cast",
                defaultWeight = 1f,
                avatarMask = mask,
                blendingMode = AnimatorLayerBlendingMode.Override,
                stateMachine = machine
            });

            AnimatorState empty = machine.AddState("Empty", new Vector3(300f, 0f, 0f));
            machine.defaultState = empty;

            for (int index = 0; index < FireballCasts.Length; index++)
            {
                (string state, string clip, bool mirror, string trigger) = FireballCasts[index];
                CastState(machine, empty, state, clip, trigger, mirror, new Vector3(550f, -240f + index * 80f, 0f));
            }

            CastState(machine, empty, "Nova", NovaClip, "CastNova", false, new Vector3(550f, 160f, 0f));
            RayStates(machine, empty);

            AnimatorStateTransition cancel = machine.AddAnyStateTransition(empty);
            cancel.AddCondition(AnimatorConditionMode.If, 0f, "CastCancel");
            cancel.duration = CastBlend;
            cancel.hasExitTime = false;
        }

        private static void RayStates(AnimatorStateMachine machine, AnimatorState empty)
        {
            AnimationClip source = SourceClip(RayClip);

            AnimatorState charge = machine.AddState("RayCharge", new Vector3(800f, 160f, 0f));
            charge.motion = source;
            charge.speedParameterActive = true;
            charge.speedParameter = "AttackSpeed";

            AnimatorState hold = machine.AddState("RayHold", new Vector3(1050f, 160f, 0f));
            hold.motion = BuildRayHoldClip();

            AnimatorStateTransition enter = machine.AddAnyStateTransition(charge);
            enter.AddCondition(AnimatorConditionMode.If, 0f, "CastRay");
            enter.duration = CastBlend;
            enter.hasExitTime = false;
            enter.canTransitionToSelf = false;

            AnimatorStateTransition toHold = charge.AddTransition(hold);
            toHold.hasExitTime = true;
            toHold.exitTime = RayHoldSeconds / source.length;
            toHold.duration = 0.2f;
        }

        private static AnimationClip BuildRayHoldClip()
        {
            AnimationClip hover = AssetDatabase.LoadAssetAtPath<AnimationClip>(HoverClipPath);

            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(RayHoldClipPath);

            if (clip == null)
            {
                clip = new AnimationClip();
                AssetDatabase.CreateAsset(clip, RayHoldClipPath);
            }

            clip.ClearCurves();

            foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(hover))
            {
                if (IsMuscle(binding.propertyName) == false)
                    continue;

                float value = AnimationUtility.GetEditorCurve(hover, binding).Evaluate(0f);
                bool arm = false;

                foreach ((string muscle, float pose) in RayArms)
                {
                    if (binding.propertyName == "Left " + muscle || binding.propertyName == "Right " + muscle)
                    {
                        value = pose;
                        arm = true;
                    }
                }

                float phase = (binding.propertyName.Length * 37 % 100) / 100f;

                SetCurve(clip, binding.propertyName, Tremble(value, arm ? RayTremble : 0f, phase));
            }

            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);

            return clip;
        }

        private static AnimationCurve Tremble(float center, float amplitude, float phase)
        {
            const int keys = 12;
            const float seconds = 0.6f;
            Keyframe[] frames = new Keyframe[keys + 1];

            for (int index = 0; index <= keys; index++)
            {
                float angle = (index / (float)keys * 4f + phase) * Mathf.PI * 2f;
                frames[index] = new Keyframe(seconds * index / keys, center + amplitude * Mathf.Sin(angle));
            }

            return new AnimationCurve(frames);
        }

        private static void CastState(AnimatorStateMachine machine, AnimatorState empty, string name, string clip, string trigger, bool mirror, Vector3 position)
        {
            AnimatorState state = machine.AddState(name, position);
            state.motion = SourceClip(clip);
            state.mirror = mirror;
            state.speedParameterActive = true;
            state.speedParameter = "AttackSpeed";

            AnimatorStateTransition enter = machine.AddAnyStateTransition(state);
            enter.AddCondition(AnimatorConditionMode.If, 0f, trigger);
            enter.duration = CastBlend;
            enter.hasExitTime = false;
            enter.canTransitionToSelf = true;

            AnimatorStateTransition exit = state.AddTransition(empty);
            exit.hasExitTime = true;
            exit.exitTime = 0.92f;
            exit.duration = 0.25f;
        }

        private static Animator EnsureViewChild(GameObject prefabRoot)
        {
            Transform view = prefabRoot.transform.Find(ViewName);

            if (view != null)
                return view.GetComponent<Animator>();

            Animator rootAnimator = prefabRoot.GetComponent<Animator>();

            if (rootAnimator == null)
                throw new InvalidOperationException($"{PrefabPath} has neither a {ViewName} child nor an Animator on its root.");

            Avatar avatar = rootAnimator.avatar;

            List<Transform> children = new List<Transform>();

            foreach (Transform child in prefabRoot.transform)
                children.Add(child);

            GameObject viewObject = new GameObject(ViewName);
            viewObject.transform.SetParent(prefabRoot.transform, false);

            foreach (Transform child in children)
                child.SetParent(viewObject.transform, false);

            UnityEngine.Object.DestroyImmediate(rootAnimator);

            Animator animator = viewObject.AddComponent<Animator>();
            animator.avatar = avatar;

            return animator;
        }

        private static void WireView(GameObject prefabRoot, Animator animator)
        {
            if (prefabRoot.GetComponent<ViewInterpolator>() == null)
                prefabRoot.AddComponent<ViewInterpolator>();

            HoverRig hoverRig = prefabRoot.GetComponent<HoverRig>();

            if (hoverRig == null)
                hoverRig = prefabRoot.AddComponent<HoverRig>();

            SerializedObject serializedRig = new SerializedObject(hoverRig);
            serializedRig.FindProperty("_body").objectReferenceValue = animator.transform;
            serializedRig.FindProperty("_pivotHeight").floatValue = HoverPivot;
            serializedRig.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject serializedView = new SerializedObject(prefabRoot.GetComponent<ViewInterpolator>());
            serializedView.FindProperty("_hoverRig").objectReferenceValue = hoverRig;
            serializedView.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WireBook(GameObject prefabRoot, Animator animator)
        {
            Transform view = animator.transform;
            Transform mount = prefabRoot.transform.Find(BookMountName);

            if (mount == null)
                mount = view.Find(BookMountName);

            if (mount == null)
                mount = new GameObject(BookMountName).transform;

            mount.SetParent(prefabRoot.transform, false);

            mount.localPosition = BookMountPosition;
            mount.localRotation = Quaternion.Euler(BookMountEuler);
            mount.localScale = Vector3.one;

            Transform book = view.Find(BookName);

            if (book == null)
                book = mount.Find(BookName);

            if (book == null)
                throw new InvalidOperationException($"{PrefabPath} has no {BookName} under {ViewName}.");

            book.SetParent(mount, false);
            book.gameObject.SetActive(true);
            book.localRotation = Quaternion.identity;
            book.localScale = Vector3.one * BookScale;

            book.localPosition = Vector3.zero;

            Renderer renderer = book.GetComponentInChildren<Renderer>(true);
            Vector3 center = mount.InverseTransformPoint(renderer.bounds.center);
            book.localPosition = -center;

            BookFloat bookFloat = prefabRoot.GetComponent<BookFloat>();

            if (bookFloat == null)
                bookFloat = prefabRoot.AddComponent<BookFloat>();

            SerializedObject serialized = new SerializedObject(bookFloat);
            serialized.FindProperty("_book").objectReferenceValue = mount;
            serialized.FindProperty("_head").objectReferenceValue = FindBone(view, HeadBone);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Transform FindBone(Transform root, string name)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == name)
                    return child;
            }

            throw new InvalidOperationException($"{PrefabPath} has no bone {name}.");
        }

        private static AnimationClip SourceClip(string name)
        {
            string path = Folder + "Wizard@" + name + ".fbx";

            foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (asset is AnimationClip clip && clip.name.StartsWith("__") == false)
                    return clip;
            }

            throw new InvalidOperationException($"{path} has no animation clip.");
        }
    }
}

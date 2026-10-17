using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class BerserkClipBaker
    {
        public const string SourceFolder = "Assets/_Project/Art/Animations/Berserk/";
        public const string OutputFolder = "Assets/_Project/Art/Animations/Berserk/Baked/";

        private const string MenuPath = "Game/Bake Berserk Clips";
        private const string HipsPath = "mixamorig:Hips";
        private const string LeftToePath = "mixamorig:LeftToeBase";
        private const string RightToePath = "mixamorig:RightToeBase";
        private const string HeroPrefabPath = "Assets/_Project/Prefabs/Heroes/Berserk.prefab";
        private const float GroundToeHeight = 0.002f;
        private const string LeftToken = "Left";
        private const string RightToken = "Right";

        [Flags]
        public enum Options
        {
            None = 0,
            InPlace = 1,
            RemoveYaw = 2,
            Mirror = 4,
            Loop = 8
        }

        public readonly struct Entry
        {
            public readonly string Output;
            public readonly string Source;
            public readonly int FirstFrame;
            public readonly int LastFrame;
            public readonly Options Options;

            public Entry(string output, string source, int firstFrame, int lastFrame, Options options)
            {
                Output = output;
                Source = source;
                FirstFrame = firstFrame;
                LastFrame = lastFrame;
                Options = options;
            }
        }

        public static readonly Entry[] Entries =
        {
            new Entry("Berserk_Charge", "Charge_FinalSlash_Blend.fbx", 0, 18, Options.InPlace),
            new Entry("Berserk_DashMove", "Charge_FinalSlash_Blend.fbx", 18, 27, Options.InPlace),
            new Entry("Berserk_FinalSlash", "Charge_FinalSlash_Blend.fbx", 71, 91, Options.InPlace),
            new Entry("Berserk_ChargeRun", "ChareRun_Blend.fbx", 0, 18, Options.InPlace | Options.Loop),
            new Entry("Berserk_DashFinalAttack", "Dash_FinalAttack_Archers_PERK.fbx", 26, 56, Options.InPlace),
            new Entry("Berserk_ExecutionExtra", "ExecutionExtraAnim.fbx", 0, 65, Options.InPlace),
            new Entry("Berserk_HitReact", "Hit_React_v1.fbx", 0, 23, Options.None),
            new Entry("Berserk_SlashDownUp", "SlashFromDownToUp.fbx", 0, 35, Options.InPlace),
            new Entry("Berserk_SpinLoop", "SpinAttackLoop_Need_Cut.fbx", 3, 51, Options.InPlace | Options.Loop),
            new Entry("Berserk_SpinDownUpSlash", "SpinDownToUpSlashAttack.fbx", 0, 54, Options.InPlace),
            new Entry("Berserk_SpinPowerSlashFlame", "SpinPowerSlashRightAttack_Flame.fbx", 0, 54, Options.InPlace),
            new Entry("Berserk_StrafeLeft", "Strafe_new.fbx", 0, 33, Options.InPlace | Options.Loop),
            new Entry("Berserk_StrafeRight", "Strafe_new.fbx", 0, 33, Options.InPlace | Options.Loop | Options.Mirror),
            new Entry("Berserk_TurnRight", "Turn.fbx", 0, 22, Options.InPlace | Options.RemoveYaw),
            new Entry("Berserk_TurnLeft", "Turn.fbx", 0, 22, Options.InPlace | Options.RemoveYaw | Options.Mirror),
        };

        [MenuItem(MenuPath, false, 122)]
        public static void BakeAll()
        {
            Directory.CreateDirectory(OutputFolder);

            foreach (Entry entry in Entries)
                Bake(entry);

            AssetDatabase.SaveAssets();
            Debug.Log($"{nameof(BerserkClipBaker)}: baked {Entries.Length} clips into {OutputFolder}");
        }

        public static AnimationClip LoadSource(string source)
        {
            foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(SourceFolder + source))
            {
                if (asset is AnimationClip clip && clip.name.StartsWith("__preview") == false)
                    return clip;
            }

            throw new InvalidOperationException($"{SourceFolder + source} has no animation clip.");
        }

        public static AnimationClip Bake(Entry entry)
        {
            AnimationClip source = LoadSource(entry.Source);
            float frameRate = source.frameRate;
            int frameCount = entry.LastFrame - entry.FirstFrame;

            if (frameCount <= 0)
                throw new InvalidOperationException($"{entry.Output}: frame range {entry.FirstFrame}-{entry.LastFrame} is empty.");

            bool mirror = (entry.Options & Options.Mirror) != 0;

            Dictionary<string, Dictionary<string, float[]>> tracks = SampleTracks(source, entry.FirstFrame, frameCount, frameRate);

            if ((entry.Options & Options.InPlace) != 0)
                RemovePlanarDrift(tracks);

            if ((entry.Options & Options.RemoveYaw) != 0)
                RemoveYawDrift(tracks, frameCount);

            if (mirror)
                tracks = MirrorTracks(tracks);

            AnimationClip clip = BuildClip(tracks, frameRate);

            AlignToGround(clip, tracks, frameRate);

            UnityEngine.Object.DestroyImmediate(clip);
            clip = BuildClip(tracks, frameRate);

            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = (entry.Options & Options.Loop) != 0;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            return Save(entry.Output, clip);
        }

        private static AnimationClip BuildClip(Dictionary<string, Dictionary<string, float[]>> tracks, float frameRate)
        {
            AnimationClip clip = new AnimationClip { frameRate = frameRate };

            foreach (KeyValuePair<string, Dictionary<string, float[]>> track in tracks)
            {
                foreach (KeyValuePair<string, float[]> property in track.Value)
                {
                    EditorCurveBinding binding = EditorCurveBinding.FloatCurve(track.Key, typeof(Transform), property.Key);
                    AnimationUtility.SetEditorCurve(clip, binding, BuildCurve(property.Value, frameRate));
                }
            }

            clip.EnsureQuaternionContinuity();

            return clip;
        }

        private static void AlignToGround(AnimationClip clip, Dictionary<string, Dictionary<string, float[]>> tracks, float frameRate)
        {
            GameObject hero = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(HeroPrefabPath));
            hero.hideFlags = HideFlags.HideAndDontSave;

            try
            {
                Animator animator = hero.GetComponentInChildren<Animator>(true);
                Transform root = animator.transform;
                Transform hips = root.Find(HipsPath);
                Transform leftToe = FindDeep(root, LeftToePath);
                Transform rightToe = FindDeep(root, RightToePath);

                float lowest = float.MaxValue;
                int frameCount = tracks[HipsPath]["m_LocalPosition.y"].Length - 1;

                for (int frame = 0; frame <= frameCount; frame++)
                {
                    clip.SampleAnimation(animator.gameObject, frame / frameRate);
                    lowest = Mathf.Min(lowest, root.InverseTransformPoint(leftToe.position).y, root.InverseTransformPoint(rightToe.position).y);
                }

                float lift = (GroundToeHeight - lowest) / hips.parent.lossyScale.y * root.lossyScale.y;
                float[] height = tracks[HipsPath]["m_LocalPosition.y"];

                for (int frame = 0; frame < height.Length; frame++)
                    height[frame] += lift;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(hero);
            }
        }

        private static Transform FindDeep(Transform root, string name)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == name)
                    return child;
            }

            throw new InvalidOperationException($"{HeroPrefabPath} has no bone {name}.");
        }

        private static Dictionary<string, Dictionary<string, float[]>> SampleTracks(AnimationClip source, int firstFrame, int frameCount, float frameRate)
        {
            Dictionary<string, Dictionary<string, float[]>> tracks = new Dictionary<string, Dictionary<string, float[]>>();

            foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(source))
            {
                if (binding.type != typeof(Transform))
                    continue;

                AnimationCurve curve = AnimationUtility.GetEditorCurve(source, binding);
                float[] values = new float[frameCount + 1];

                for (int frame = 0; frame <= frameCount; frame++)
                    values[frame] = curve.Evaluate((firstFrame + frame) / frameRate);

                if (tracks.TryGetValue(binding.path, out Dictionary<string, float[]> properties) == false)
                {
                    properties = new Dictionary<string, float[]>();
                    tracks.Add(binding.path, properties);
                }

                properties[binding.propertyName] = values;
            }

            return tracks;
        }

        private static void RemovePlanarDrift(Dictionary<string, Dictionary<string, float[]>> tracks)
        {
            if (tracks.TryGetValue(HipsPath, out Dictionary<string, float[]> hips) == false)
                throw new InvalidOperationException($"Clip has no {HipsPath} track to keep in place.");

            Detrend(hips, "m_LocalPosition.x");
            Detrend(hips, "m_LocalPosition.z");
        }

        private static void Detrend(Dictionary<string, float[]> properties, string property)
        {
            if (properties.TryGetValue(property, out float[] values) == false)
                return;

            int last = values.Length - 1;
            float start = values[0];
            float end = values[last];

            for (int frame = 0; frame <= last; frame++)
                values[frame] -= (end - start) * frame / last;
        }

        private static void RemoveYawDrift(Dictionary<string, Dictionary<string, float[]>> tracks, int frameCount)
        {
            Dictionary<string, float[]> hips = tracks[HipsPath];
            float[] x = hips["m_LocalRotation.x"];
            float[] y = hips["m_LocalRotation.y"];
            float[] z = hips["m_LocalRotation.z"];
            float[] w = hips["m_LocalRotation.w"];

            float startYaw = Yaw(new Quaternion(x[0], y[0], z[0], w[0]));
            float endYaw = startYaw + Mathf.DeltaAngle(startYaw, Yaw(new Quaternion(x[frameCount], y[frameCount], z[frameCount], w[frameCount])));

            for (int frame = 0; frame <= frameCount; frame++)
            {
                float drift = (endYaw - startYaw) * frame / frameCount;
                Quaternion rotation = Quaternion.AngleAxis(-drift, Vector3.up) * new Quaternion(x[frame], y[frame], z[frame], w[frame]);

                x[frame] = rotation.x;
                y[frame] = rotation.y;
                z[frame] = rotation.z;
                w[frame] = rotation.w;
            }
        }

        private static float Yaw(Quaternion rotation)
        {
            Vector3 forward = rotation * Vector3.forward;

            return Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
        }

        private static Dictionary<string, Dictionary<string, float[]>> MirrorTracks(Dictionary<string, Dictionary<string, float[]>> tracks)
        {
            Dictionary<string, Dictionary<string, float[]>> mirrored = new Dictionary<string, Dictionary<string, float[]>>();

            foreach (KeyValuePair<string, Dictionary<string, float[]>> track in tracks)
            {
                Dictionary<string, float[]> properties = new Dictionary<string, float[]>();

                foreach (KeyValuePair<string, float[]> property in track.Value)
                {
                    float[] values = (float[])property.Value.Clone();

                    if (property.Key == "m_LocalPosition.x" || property.Key == "m_LocalRotation.y" || property.Key == "m_LocalRotation.z")
                    {
                        for (int frame = 0; frame < values.Length; frame++)
                            values[frame] = -values[frame];
                    }

                    properties[property.Key] = values;
                }

                mirrored[SwapSide(track.Key)] = properties;
            }

            return mirrored;
        }

        public static string SwapSide(string path)
        {
            string[] parts = path.Split('/');

            for (int index = 0; index < parts.Length; index++)
            {
                if (parts[index].Contains(LeftToken))
                    parts[index] = parts[index].Replace(LeftToken, RightToken);
                else if (parts[index].Contains(RightToken))
                    parts[index] = parts[index].Replace(RightToken, LeftToken);
            }

            return string.Join("/", parts);
        }

        private static AnimationCurve BuildCurve(float[] values, float frameRate)
        {
            Keyframe[] keys = new Keyframe[values.Length];
            float step = 1f / frameRate;

            for (int frame = 0; frame < values.Length; frame++)
            {
                float previous = values[Mathf.Max(0, frame - 1)];
                float next = values[Mathf.Min(values.Length - 1, frame + 1)];
                int span = Mathf.Min(values.Length - 1, frame + 1) - Mathf.Max(0, frame - 1);
                float tangent = span > 0 ? (next - previous) / (span * step) : 0f;

                keys[frame] = new Keyframe(frame * step, values[frame], tangent, tangent);
            }

            return new AnimationCurve(keys);
        }

        private static AnimationClip Save(string name, AnimationClip clip)
        {
            string path = OutputFolder + name + ".anim";
            AnimationClip existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            clip.name = name;

            if (existing == null)
            {
                AssetDatabase.CreateAsset(clip, path);

                return clip;
            }

            EditorUtility.CopySerialized(clip, existing);
            existing.name = name;
            EditorUtility.SetDirty(existing);
            UnityEngine.Object.DestroyImmediate(clip);

            return existing;
        }
    }
}

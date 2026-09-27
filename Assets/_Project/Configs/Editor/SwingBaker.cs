using Game.Simulation.Services;
using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Game.Configs.Editor
{
    public static class SwingBaker
    {
        public const float TickSeconds = 1f / 60f;

        private const string BakeFolder = "Assets/_Project/Configs/Weapons/Bakes";
        private const int SegmentBuffer = 64;

        [MenuItem("Game/Bake Weapon Swings")]
        public static void BakeAllFromMenu()
        {
            Debug.Log(BakeAll());
        }

        public static string BakeAll()
        {
            StringBuilder report = new StringBuilder();

            foreach (string guid in AssetDatabase.FindAssets("t:" + nameof(WeaponConfig)))
            {
                WeaponConfig weapon = AssetDatabase.LoadAssetAtPath<WeaponConfig>(AssetDatabase.GUIDToAssetPath(guid));

                report.Append(Bake(weapon));
            }

            AssetDatabase.SaveAssets();

            return report.ToString();
        }

        public static AnimationClip FindClip(GameObject rig, string stateName)
        {
            Animator animator = rig.GetComponentInChildren<Animator>(true);

            if (animator == null)
                throw new InvalidOperationException($"Bake rig '{rig.name}' has no Animator.");

            AnimatorController controller = animator.runtimeAnimatorController as AnimatorController;

            if (controller == null)
                throw new InvalidOperationException($"Bake rig '{rig.name}' has no AnimatorController.");

            AnimationClip found = null;

            foreach (AnimatorControllerLayer layer in controller.layers)
            {
                foreach (ChildAnimatorState child in layer.stateMachine.states)
                {
                    if (child.state.name != stateName)
                        continue;

                    AnimationClip clip = child.state.motion as AnimationClip;

                    if (clip == null)
                        continue;

                    if (found != null && found != clip)
                        throw new InvalidOperationException(
                            $"Controller '{controller.name}' has two states named '{stateName}' with different clips; the bake cannot tell which one plays.");

                    found = clip;
                }
            }

            if (found == null)
                throw new InvalidOperationException($"Controller '{controller.name}' has no state '{stateName}' with a clip.");

            return found;
        }

        public static string Bake(WeaponConfig weapon)
        {
            if (weapon.BakeRig == null)
                throw new InvalidOperationException($"{nameof(WeaponConfig)} '{weapon.Id}' has no bake rig.");

            if (string.IsNullOrEmpty(weapon.HandBoneName))
                throw new InvalidOperationException($"{nameof(WeaponConfig)} '{weapon.Id}' has no hand bone name.");

            if (weapon.BakeSampleRate <= 0f)
                throw new InvalidOperationException($"{nameof(WeaponConfig)} '{weapon.Id}' has a non-positive bake sample rate.");

            StringBuilder report = new StringBuilder();
            report.Append($"{weapon.Id}:\n");

            Scene scene = EditorSceneManager.NewPreviewScene();

            try
            {
                GameObject instance = (GameObject)Object.Instantiate(weapon.BakeRig);
                SceneManager.MoveGameObjectToScene(instance, scene);
                instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

                Animator animator = instance.GetComponentInChildren<Animator>(true);
                Transform hand = FindDescendant(instance.transform, weapon.HandBoneName);

                if (hand == null)
                    throw new InvalidOperationException($"Bake rig '{weapon.BakeRig.name}' has no bone '{weapon.HandBoneName}'.");

                MeshFilter blade = hand.GetComponentInChildren<MeshFilter>(true);

                if (blade == null || blade.sharedMesh == null)
                    throw new InvalidOperationException($"Bone '{weapon.HandBoneName}' has no mesh under it to take the blade from.");

                SwingCoverage trigger = null;
                bool[] intersection = new bool[SwingCoverage.AngleBins * SwingCoverage.DistanceBins];

                for (int cell = 0; cell < intersection.Length; cell++)
                    intersection[cell] = true;

                for (int index = 0; index < weapon.VariantCount; index++)
                {
                    SwingVariant variant = weapon.Variant(index);
                    AnimationClip clip = FindClip(weapon.BakeRig, variant.AnimatorTrigger);

                    SwingBake bake = variant.Bake;

                    if (bake == null)
                        throw new InvalidOperationException(
                            $"{nameof(WeaponConfig)} '{weapon.Id}' variant '{variant.AnimatorTrigger}' has no {nameof(SwingBake)} asset assigned.");

                    SampleClip(clip, animator.gameObject, hand, blade, weapon.BakeSampleRate, out Vector3[] hands, out Vector3[] tips);

                    bake.Overwrite(clip, weapon.BakeSampleRate, clip.length, SwingClipHash.Compute(clip), hands, tips);
                    EditorUtility.SetDirty(bake);

                    float windowStart = variant.WindowStart;
                    float windowEnd = variant.WindowEnd;

                    if (windowEnd <= windowStart)
                        SuggestWindow(hands, tips, weapon.BakeSampleRate, weapon.WindowReachThreshold, weapon.CoverageBodyHeight, variant.HalfAngleDegrees, out windowStart, out windowEnd);

                    float maxReach = ResolveMaxReach(bake, windowStart, windowEnd);

                    SwingCoverage coverage = ComputeCoverage(bake, variant.PlaybackSpeed, windowStart, windowEnd, variant.HalfAngleDegrees, weapon.CoverageBodyHeight, weapon.CoverageBodyRadius);

                    variant.OverwriteBakeResults(windowStart, windowEnd, maxReach, coverage);

                    for (int angle = 0; angle < SwingCoverage.AngleBins; angle++)
                    {
                        for (int distance = 0; distance < SwingCoverage.DistanceBins; distance++)
                            intersection[angle * SwingCoverage.DistanceBins + distance] &= coverage.Cell(angle, distance);
                    }

                    report.Append(string.Format(
                        CultureInfo.InvariantCulture,
                        "  {0}: clip {1:F4}s, {2} samples, hash {3}, window {4:F4}-{5:F4}s (ticks {6:F1}-{7:F1} at speed {8}), max reach {9:F2} m, covered cells {10}\n",
                        variant.AnimatorTrigger,
                        clip.length,
                        hands.Length,
                        bake.ClipHash,
                        windowStart,
                        windowEnd,
                        windowStart / variant.PlaybackSpeed / TickSeconds,
                        windowEnd / variant.PlaybackSpeed / TickSeconds,
                        variant.PlaybackSpeed,
                        maxReach,
                        coverage.CoveredCount));
                }

                trigger = new SwingCoverage();
                trigger.Overwrite(intersection);
                weapon.OverwriteTriggerCoverage(trigger);
                EditorUtility.SetDirty(weapon);

                report.Append($"  trigger (intersection) covered cells {trigger.CoveredCount}\n");
                report.Append(DescribeCoverage(trigger));
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }

            return report.ToString();
        }

        public static void SampleClip(AnimationClip clip, GameObject animatorObject, Transform hand, MeshFilter blade, float sampleRate, out Vector3[] hands, out Vector3[] tips)
        {
            int count = Mathf.FloorToInt(clip.length * sampleRate + 1e-3f) + 1;

            hands = new Vector3[count];
            tips = new Vector3[count];

            clip.SampleAnimation(animatorObject, 0f);

            Vector3 tipLocal = ResolveTipLocal(blade, hand.position);

            for (int index = 0; index < count; index++)
            {
                float time = Mathf.Min(index / sampleRate, clip.length);

                clip.SampleAnimation(animatorObject, time);

                hands[index] = hand.position;
                tips[index] = blade.transform.TransformPoint(tipLocal);
            }
        }

        public static Vector3 ResolveTipLocal(MeshFilter blade, Vector3 handWorld)
        {
            Bounds bounds = blade.sharedMesh.bounds;
            Vector3 extents = bounds.extents;

            Vector3 axis = Vector3.right * extents.x;

            if (extents.y > extents.x && extents.y >= extents.z)
                axis = Vector3.up * extents.y;

            if (extents.z > extents.x && extents.z > extents.y)
                axis = Vector3.forward * extents.z;

            Vector3 first = bounds.center + axis;
            Vector3 second = bounds.center - axis;

            float firstDistance = (blade.transform.TransformPoint(first) - handWorld).sqrMagnitude;
            float secondDistance = (blade.transform.TransformPoint(second) - handWorld).sqrMagnitude;

            return firstDistance >= secondDistance ? first : second;
        }

        public static void SuggestWindow(Vector3[] hands, Vector3[] tips, float sampleRate, float reachThreshold, float height, float halfAngleDegrees, out float start, out float end)
        {
            int first = -1;
            int last = -1;

            for (int index = 0; index < tips.Length; index++)
            {
                Vector3 tip = tips[index];
                float reach = new Vector2(tip.x, tip.z).magnitude;
                float angle = Mathf.Atan2(tip.x, tip.z) * Mathf.Rad2Deg;

                if (reach < reachThreshold)
                    continue;

                if (tip.y > height)
                    continue;

                if (halfAngleDegrees < SwingGeometry.FullCircleHalfAngle && Mathf.Abs(angle) > halfAngleDegrees)
                    continue;

                if (first < 0)
                    first = index;

                last = index;
            }

            if (first < 0)
                throw new InvalidOperationException("No sample of the swing reaches the threshold in front of the rig below the coverage height; set the window by hand.");

            start = first / sampleRate;
            end = last / sampleRate;
        }

        public static float ResolveMaxReach(SwingBake bake, float windowStart, float windowEnd)
        {
            float max = 0f;

            for (int index = 0; index < bake.SampleCount; index++)
            {
                float time = index / bake.SampleRate;

                if (time < windowStart - 1e-4f || time > windowEnd + 1e-4f)
                    continue;

                bake.SampleAt(time, out Vector3 hand, out Vector3 tip);

                max = Mathf.Max(max, new Vector2(hand.x, hand.z).magnitude, new Vector2(tip.x, tip.z).magnitude);
            }

            return max;
        }

        public static SwingCoverage ComputeCoverage(SwingBake bake, float playbackSpeed, float windowStart, float windowEnd, float halfAngleDegrees, float bodyHeight, float bodyRadius)
        {
            bool[] cells = new bool[SwingCoverage.AngleBins * SwingCoverage.DistanceBins];

            Vector3[] hands = new Vector3[SegmentBuffer];
            Vector3[] tips = new Vector3[SegmentBuffer];

            SwingPose pose = new SwingPose(Vector3.zero, Vector3.forward);

            bake.SampleAt(0f, out Vector3 previousHand, out Vector3 previousTip);

            float time = 0f;

            while (time < bake.ClipLength)
            {
                float next = Mathf.Min(time + TickSeconds * playbackSpeed, bake.ClipLength);

                int count = SwingSweep.BuildSegments(bake, time, next, windowStart, windowEnd, pose.ToWorld(previousHand), pose.ToWorld(previousTip), pose, hands, tips);

                if (count > 0)
                {
                    for (int angle = 0; angle < SwingCoverage.AngleBins; angle++)
                    {
                        for (int distance = 0; distance < SwingCoverage.DistanceBins; distance++)
                        {
                            int cell = angle * SwingCoverage.DistanceBins + distance;

                            if (cells[cell])
                                continue;

                            float radians = SwingCoverage.CellAngle(angle) * Mathf.Deg2Rad;
                            float reach = SwingCoverage.CellDistance(distance);
                            Vector3 target = new Vector3(Mathf.Sin(radians) * reach, 0f, Mathf.Cos(radians) * reach);

                            cells[cell] = SwingSweep.Hits(hands, tips, count, pose, halfAngleDegrees, target, bodyRadius, bodyHeight);
                        }
                    }
                }

                bake.SampleAt(next, out previousHand, out previousTip);

                time = next;
            }

            SwingCoverage coverage = new SwingCoverage();
            coverage.Overwrite(cells);

            return coverage;
        }

        public static string DescribeCoverage(SwingCoverage coverage)
        {
            StringBuilder text = new StringBuilder();

            for (int distance = 0; distance < SwingCoverage.DistanceBins; distance += 2)
            {
                text.Append(string.Format(CultureInfo.InvariantCulture, "    {0:F1} m:", SwingCoverage.CellDistance(distance)));

                int runStart = -1;

                for (int angle = 0; angle <= SwingCoverage.AngleBins; angle++)
                {
                    bool covered = angle < SwingCoverage.AngleBins && coverage.Cell(angle, distance);

                    if (covered && runStart < 0)
                        runStart = angle;

                    if (covered == false && runStart >= 0)
                    {
                        text.Append(string.Format(CultureInfo.InvariantCulture, " [{0:F0}..{1:F0}]", SwingCoverage.CellAngle(runStart), SwingCoverage.CellAngle(angle - 1)));
                        runStart = -1;
                    }
                }

                text.Append('\n');
            }

            return text.ToString();
        }

        public static SwingBake CreateBakeAsset(string weaponId, string trigger)
        {
            Directory.CreateDirectory(BakeFolder);

            string path = $"{BakeFolder}/Swing_{trigger}.asset";

            SwingBake existing = AssetDatabase.LoadAssetAtPath<SwingBake>(path);

            if (existing != null)
                return existing;

            SwingBake bake = ScriptableObject.CreateInstance<SwingBake>();
            AssetDatabase.CreateAsset(bake, path);

            return bake;
        }

        private static Transform FindDescendant(Transform root, string name)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == name)
                    return child;
            }

            return null;
        }
    }
}

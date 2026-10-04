using System;
using System.Globalization;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Game.Configs.Editor
{
    public static class LeapBaker
    {
        public static string Bake(HeroicLeapConfig leap)
        {
            WeaponConfig source = leap.BakeSource;

            if (source == null || source.BakeRig == null)
                throw new InvalidOperationException($"{nameof(HeroicLeapConfig)} '{leap.name}' has no bake source weapon with a rig.");

            AnimationClip clip = SwingBaker.FindClip(source.BakeRig, leap.AnimatorTrigger);
            float stateSpeed = FindStateSpeed(source.BakeRig, leap.AnimatorTrigger, out bool speedParameterActive);

            if (speedParameterActive)
                throw new InvalidOperationException(
                    $"State '{leap.AnimatorTrigger}' follows a speed parameter; the leap needs a fixed state speed so its landing tick is fixed.");

            float landing = FindLanding(clip, source.BakeRig, leap.ToeBoneNames, leap.ContactHeight, source.BakeSampleRate);
            int landingTick = LandingTick(landing, stateSpeed);
            uint hash = SwingClipHash.Compute(clip);

            leap.OverwriteBakeResults(clip, hash, stateSpeed, landing, landingTick);
            EditorUtility.SetDirty(leap);

            return string.Format(
                CultureInfo.InvariantCulture,
                "{0}: clip {1} {2:F4}s, hash {3}, state speed {4}, toes back under {5} m at {6:F4}s of the clip, landing tick {7}, blocks walking {8} ticks\n",
                leap.name,
                clip.name,
                clip.length,
                hash,
                stateSpeed,
                leap.ContactHeight,
                landing,
                landingTick,
                leap.TotalTicks);
        }

        public static int LandingTick(float landingClipTime, float stateSpeed)
        {
            return Mathf.Max(1, Mathf.CeilToInt(landingClipTime / stateSpeed / SwingBaker.TickSeconds - 1e-4f));
        }

        public static float FindStateSpeed(GameObject rig, string stateName, out bool speedParameterActive)
        {
            AnimatorController controller = rig.GetComponentInChildren<Animator>(true).runtimeAnimatorController as AnimatorController;

            if (controller == null)
                throw new InvalidOperationException($"Bake rig '{rig.name}' has no AnimatorController.");

            foreach (AnimatorControllerLayer layer in controller.layers)
            {
                foreach (ChildAnimatorState child in layer.stateMachine.states)
                {
                    if (child.state.name != stateName)
                        continue;

                    speedParameterActive = child.state.speedParameterActive;

                    return child.state.speed;
                }
            }

            throw new InvalidOperationException($"Controller '{controller.name}' has no state '{stateName}'.");
        }

        public static float FindLanding(AnimationClip clip, GameObject rig, string[] toeBoneNames, float contactHeight, float sampleRate)
        {
            if (toeBoneNames == null || toeBoneNames.Length == 0)
                throw new InvalidOperationException("The leap names no toe bones to find the landing by.");

            Scene scene = EditorSceneManager.NewPreviewScene();

            try
            {
                GameObject instance = Object.Instantiate(rig);
                SceneManager.MoveGameObjectToScene(instance, scene);
                instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

                Animator animator = instance.GetComponentInChildren<Animator>(true);
                Transform[] toes = new Transform[toeBoneNames.Length];

                for (int index = 0; index < toes.Length; index++)
                {
                    toes[index] = FindDescendant(instance.transform, toeBoneNames[index]);

                    if (toes[index] == null)
                        throw new InvalidOperationException($"Bake rig '{rig.name}' has no bone '{toeBoneNames[index]}'.");
                }

                int count = Mathf.FloorToInt(clip.length * sampleRate + 1e-3f) + 1;
                bool airborne = false;
                float previousTime = 0f;
                float previousHeight = 0f;

                for (int sample = 0; sample < count; sample++)
                {
                    float time = Mathf.Min(sample / sampleRate, clip.length);

                    clip.SampleAnimation(animator.gameObject, time);

                    float height = LowestToe(toes);

                    if (airborne == false)
                    {
                        if (height > contactHeight)
                            airborne = true;
                    }
                    else if (height <= contactHeight)
                    {
                        float t = (previousHeight - contactHeight) / (previousHeight - height);

                        return previousTime + (time - previousTime) * t;
                    }

                    previousTime = time;
                    previousHeight = height;
                }

                throw new InvalidOperationException($"Clip '{clip.name}' never lifts the toes above {contactHeight} m and brings them back; no landing to bake.");
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        private static float LowestToe(Transform[] toes)
        {
            float lowest = float.MaxValue;

            for (int index = 0; index < toes.Length; index++)
                lowest = Mathf.Min(lowest, toes[index].position.y);

            return lowest;
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

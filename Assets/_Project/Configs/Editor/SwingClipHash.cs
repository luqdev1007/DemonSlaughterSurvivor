using Game.Core;
using System;
using UnityEditor;
using UnityEngine;

namespace Game.Configs.Editor
{
    public static class SwingClipHash
    {
        private const uint OffsetBasis = 2166136261u;
        private const uint Prime = 16777619u;

        public static uint Compute(AnimationClip clip)
        {
            if (clip == null)
                throw new ArgumentNullException(nameof(clip));

            uint hash = OffsetBasis;

            hash = MixFloat(hash, clip.length);
            hash = MixFloat(hash, clip.frameRate);

            EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(clip);

            Array.Sort(bindings, CompareBindings);

            hash = MixInt(hash, bindings.Length);

            for (int index = 0; index < bindings.Length; index++)
            {
                EditorCurveBinding binding = bindings[index];

                hash = MixInt(hash, (int)StableHash.Fnv1a32(binding.path));
                hash = MixInt(hash, (int)StableHash.Fnv1a32(binding.propertyName));
                hash = MixInt(hash, (int)StableHash.Fnv1a32(binding.type == null ? string.Empty : binding.type.FullName));

                AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding);
                Keyframe[] keys = curve == null ? Array.Empty<Keyframe>() : curve.keys;

                hash = MixInt(hash, keys.Length);

                for (int key = 0; key < keys.Length; key++)
                {
                    hash = MixFloat(hash, keys[key].time);
                    hash = MixFloat(hash, keys[key].value);
                    hash = MixFloat(hash, keys[key].inTangent);
                    hash = MixFloat(hash, keys[key].outTangent);
                    hash = MixFloat(hash, keys[key].inWeight);
                    hash = MixFloat(hash, keys[key].outWeight);
                    hash = MixInt(hash, (int)keys[key].weightedMode);
                }
            }

            hash = MixInt(hash, AnimationUtility.GetObjectReferenceCurveBindings(clip).Length);

            return hash;
        }

        private static int CompareBindings(EditorCurveBinding left, EditorCurveBinding right)
        {
            int byPath = string.CompareOrdinal(left.path, right.path);

            if (byPath != 0)
                return byPath;

            int byProperty = string.CompareOrdinal(left.propertyName, right.propertyName);

            if (byProperty != 0)
                return byProperty;

            string leftType = left.type == null ? string.Empty : left.type.FullName;
            string rightType = right.type == null ? string.Empty : right.type.FullName;

            return string.CompareOrdinal(leftType, rightType);
        }

        private static uint MixFloat(uint hash, float value)
        {
            return MixInt(hash, BitConverter.SingleToInt32Bits(value));
        }

        private static uint MixInt(uint hash, int value)
        {
            for (int shift = 0; shift < 32; shift += 8)
            {
                hash ^= (uint)((value >> shift) & 0xFF);
                hash = unchecked(hash * Prime);
            }

            return hash;
        }
    }
}

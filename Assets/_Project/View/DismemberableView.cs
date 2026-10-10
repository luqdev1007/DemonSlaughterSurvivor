using System;
using UnityEngine;

namespace Game.View
{
    public sealed class DismemberableView : MonoBehaviour
    {
        [Serializable]
        public sealed class Part
        {
            [SerializeField] private DismemberPart _kind;
            [SerializeField] private SkinnedMeshRenderer _renderer;
            [SerializeField] private Transform _stump;

            public DismemberPart Kind => _kind;

            public SkinnedMeshRenderer Renderer => _renderer;

            public Transform Stump => _stump;
        }

        [SerializeField] private Part[] _parts = Array.Empty<Part>();

        public int PartCount => _parts.Length;

        public Part GetPart(int index)
        {
            return _parts[index];
        }

        public void Validate(string owner)
        {
            if (_parts.Length == 0)
                throw new InvalidOperationException($"{owner}: {nameof(DismemberableView)} on '{name}' has no parts to cut off.");

            for (int index = 0; index < _parts.Length; index++)
            {
                Part part = _parts[index];

                if (part == null || part.Renderer == null || part.Stump == null)
                    throw new InvalidOperationException(
                        $"{owner}: part {index} of {nameof(DismemberableView)} on '{name}' needs both a skinned renderer and a stump bone.");
            }
        }

        public void Restore()
        {
            for (int index = 0; index < _parts.Length; index++)
            {
                SkinnedMeshRenderer target = _parts[index].Renderer;

                if (target != null)
                    target.enabled = true;
            }
        }
    }
}

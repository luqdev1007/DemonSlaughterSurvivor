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
            [SerializeField] private int _goreSlot = 1;
            [SerializeField] private int _torsoGoreSlot = -1;
            [SerializeField] private Renderer[] _attachments = Array.Empty<Renderer>();

            public DismemberPart Kind => _kind;

            public SkinnedMeshRenderer Renderer => _renderer;

            public Transform Stump => _stump;

            public int GoreSlot => _goreSlot;

            public int TorsoGoreSlot => _torsoGoreSlot;

            public Renderer[] Attachments => _attachments;

            public bool IsAttached => _renderer != null && _renderer.enabled;
        }

        [SerializeField] private SkinnedMeshRenderer _torso;
        [SerializeField] private Material _goreMaterial;
        [SerializeField] private Part[] _parts = Array.Empty<Part>();

        private Material[] _torsoMaterials;
        private bool _torsoChanged;

        public int PartCount => _parts.Length;

        public Part GetPart(int index)
        {
            return _parts[index];
        }

        public void Validate(string owner)
        {
            if (_parts.Length == 0)
                throw new InvalidOperationException($"{owner}: {nameof(DismemberableView)} on '{name}' has no parts to cut off.");

            if (_torso == null || _goreMaterial == null)
                throw new InvalidOperationException(
                    $"{owner}: {nameof(DismemberableView)} on '{name}' needs the torso renderer and the gore material.");

            int torsoSlots = _torso.sharedMaterials.Length;

            for (int index = 0; index < _parts.Length; index++)
            {
                Part part = _parts[index];

                if (part == null || part.Renderer == null || part.Stump == null)
                    throw new InvalidOperationException(
                        $"{owner}: part {index} of {nameof(DismemberableView)} on '{name}' needs both a skinned renderer and a stump bone.");

                if (part.GoreSlot < 0 || part.GoreSlot >= part.Renderer.sharedMaterials.Length)
                    throw new InvalidOperationException(
                        $"{owner}: part {index} ({part.Kind}) on '{name}' has gore slot {part.GoreSlot}, " +
                        $"but its renderer has {part.Renderer.sharedMaterials.Length} material slots.");

                if (part.TorsoGoreSlot >= torsoSlots)
                    throw new InvalidOperationException(
                        $"{owner}: part {index} ({part.Kind}) on '{name}' opens torso slot {part.TorsoGoreSlot}, " +
                        $"but the torso has {torsoSlots} material slots.");

                for (int attachment = 0; attachment < part.Attachments.Length; attachment++)
                {
                    if (part.Attachments[attachment] == null)
                        throw new InvalidOperationException(
                            $"{owner}: part {index} ({part.Kind}) on '{name}' has an empty attachment slot {attachment}.");
                }
            }
        }

        public void Detach(Part part)
        {
            part.Renderer.enabled = false;

            for (int index = 0; index < part.Attachments.Length; index++)
                part.Attachments[index].enabled = false;

            if (part.TorsoGoreSlot < 0)
                return;

            if (_torsoMaterials == null)
                _torsoMaterials = _torso.sharedMaterials;

            Material[] opened = _torso.sharedMaterials;
            opened[part.TorsoGoreSlot] = _goreMaterial;
            _torso.sharedMaterials = opened;
            _torsoChanged = true;
        }

        public Material[] ResolveGibMaterials(Part part)
        {
            Material[] materials = part.Renderer.sharedMaterials;
            materials[part.GoreSlot] = _goreMaterial;

            return materials;
        }

        public void Restore()
        {
            for (int index = 0; index < _parts.Length; index++)
            {
                Part part = _parts[index];

                if (part.Renderer != null)
                    part.Renderer.enabled = true;

                for (int attachment = 0; attachment < part.Attachments.Length; attachment++)
                {
                    if (part.Attachments[attachment] != null)
                        part.Attachments[attachment].enabled = true;
                }
            }

            if (_torsoChanged == false)
                return;

            _torso.sharedMaterials = _torsoMaterials;
            _torsoChanged = false;
        }
    }
}

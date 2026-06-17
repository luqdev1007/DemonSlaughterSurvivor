using System.Collections.Generic;
using UnityEngine;

namespace DemonSlaughter.Gameplay.Camera
{
    public sealed class CameraOcclusionHandler : MonoBehaviour
    {
        [SerializeField] private LayerMask _occlusionLayers;
        [SerializeField] private float _fadeAlpha = 0.25f;
        [SerializeField] private float _sphereCastRadius = 0.5f;
        [SerializeField] private float _fadeDuration = 0.2f;

        private Transform _target;
        private readonly Dictionary<Renderer, RendererData> _fadedRenderers = new();
        private readonly HashSet<Renderer> _currentFrameHits = new();

        public void SetTarget(Transform target)
        {
            _target = target;
        }

        private void Update()
        {
            if (_target == null) return;

            _currentFrameHits.Clear();

            CastAndFade();
            RestoreNotHit();
        }

        private void CastAndFade()
        {
            var camTransform = transform;
            var direction = _target.position - camTransform.position;
            var distance = direction.magnitude;

            var hits = Physics.SphereCastAll(
                camTransform.position,
                _sphereCastRadius,
                direction.normalized,
                distance,
                _occlusionLayers);

            foreach (var hit in hits)
            {
                var renderers = hit.collider
                    .GetComponentsInChildren<Renderer>();

                foreach (var rend in renderers)
                {
                    _currentFrameHits.Add(rend);

                    if (_fadedRenderers.ContainsKey(rend)) continue;

                    var data = new RendererData(rend);

                    for (int i = 0; i < data.FadedMaterials.Length; i++)
                        URPMaterialFader.SetTransparent(data.FadedMaterials[i], _fadeAlpha);

                    rend.materials = data.FadedMaterials;
                    _fadedRenderers[rend] = data;
                }
            }
        }

        private void RestoreNotHit()
        {
            var toRestore = new List<Renderer>();

            foreach (var kvp in _fadedRenderers)
            {
                if (_currentFrameHits.Contains(kvp.Key)) continue;
                toRestore.Add(kvp.Key);
            }

            foreach (var rend in toRestore)
            {
                if (rend == null)
                {
                    _fadedRenderers.Remove(rend);
                    continue;
                }

                var data = _fadedRenderers[rend];

                for (int i = 0; i < data.OriginalMaterials.Length; i++)
                    URPMaterialFader.SetOpaque(data.FadedMaterials[i], data.OriginalColors[i]);

                rend.materials = data.OriginalMaterials;
                _fadedRenderers.Remove(rend);
            }
        }

        private void OnDestroy()
        {
            foreach (var kvp in _fadedRenderers)
            {
                if (kvp.Key == null) continue;
                kvp.Key.materials = kvp.Value.OriginalMaterials;
            }

            _fadedRenderers.Clear();
        }
    }
}
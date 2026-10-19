using UnityEngine;

namespace Game.View
{
    public sealed class EnergyShieldFx : MonoBehaviour
    {
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        [SerializeField] private GameObject _bubble;
        [SerializeField] private GameObject _hit;
        [SerializeField, Min(0.05f)] private float _holdSeconds = 0.6f;
        [SerializeField, Range(0f, 1f)] private float _hiddenBelow = 0.001f;
        [SerializeField, Range(0.001f, 0.2f)] private float _step = 0.01f;

        private Material[] _materials;
        private Color[] _baseColors;
        private float _strength = -1f;
        private float _hitRemaining;

        public void SetStrength(float strength)
        {
            if (_bubble == null || Mathf.Abs(strength - _strength) < _step)
                return;

            _strength = strength;

            bool visible = strength > _hiddenBelow;

            if (_bubble.activeSelf != visible)
                _bubble.SetActive(visible);

            if (visible == false)
                return;

            CacheMaterials();

            for (int index = 0; index < _materials.Length; index++)
                _materials[index].SetColor(ColorId, _baseColors[index] * strength);
        }

        public void Flash()
        {
            if (_hit == null)
                return;

            if (_hit.activeSelf)
                _hit.SetActive(false);

            _hit.SetActive(true);
            _hitRemaining = _holdSeconds;
        }

        public void HideImmediate()
        {
            _strength = -1f;
            _hitRemaining = 0f;

            if (_bubble != null && _bubble.activeSelf)
                _bubble.SetActive(false);

            if (_hit != null && _hit.activeSelf)
                _hit.SetActive(false);
        }

        private void CacheMaterials()
        {
            if (_materials != null)
                return;

            ParticleSystemRenderer[] renderers = _bubble.GetComponentsInChildren<ParticleSystemRenderer>(true);
            _materials = new Material[renderers.Length];
            _baseColors = new Color[renderers.Length];

            for (int index = 0; index < renderers.Length; index++)
            {
                _materials[index] = renderers[index].material;
                _baseColors[index] = _materials[index].GetColor(ColorId);
            }
        }

        private void Update()
        {
            if (_hitRemaining <= 0f)
                return;

            _hitRemaining -= Time.deltaTime;

            if (_hitRemaining <= 0f && _hit != null)
                _hit.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_materials == null)
                return;

            for (int index = 0; index < _materials.Length; index++)
                Destroy(_materials[index]);
        }
    }
}

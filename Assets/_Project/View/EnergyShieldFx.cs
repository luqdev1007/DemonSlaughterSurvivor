using UnityEngine;

namespace Game.View
{
    public sealed class EnergyShieldFx : MonoBehaviour
    {
        [SerializeField] private GameObject _bubble;
        [SerializeField, Min(0.05f)] private float _holdSeconds = 0.5f;
        [SerializeField, Min(0f)] private float _minScale = 0.8f;
        [SerializeField, Min(0f)] private float _maxScale = 1.15f;
        [SerializeField, Min(0.1f)] private float _fullStrengthDamage = 20f;

        private float _remaining;
        private Vector3 _baseScale = Vector3.one;
        private bool _hasBaseScale;

        public void Flash(float absorbed)
        {
            if (_bubble == null)
                return;

            if (_hasBaseScale == false)
            {
                _baseScale = _bubble.transform.localScale;
                _hasBaseScale = true;
            }

            float strength = Mathf.Clamp01(absorbed / _fullStrengthDamage);
            _bubble.transform.localScale = _baseScale * Mathf.Lerp(_minScale, _maxScale, strength);
            _remaining = _holdSeconds;

            if (_bubble.activeSelf == false)
                _bubble.SetActive(true);
        }

        public void HideImmediate()
        {
            _remaining = 0f;

            if (_bubble != null && _bubble.activeSelf)
                _bubble.SetActive(false);
        }

        private void Update()
        {
            if (_remaining <= 0f)
                return;

            _remaining -= Time.deltaTime;

            if (_remaining <= 0f)
                HideImmediate();
        }
    }
}

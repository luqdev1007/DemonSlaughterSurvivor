using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public sealed class ConcentrationOrbView : MonoBehaviour
    {
        private static readonly int LevelId = Shader.PropertyToID("_Level");
        private static readonly int ClockId = Shader.PropertyToID("_Clock");

        [SerializeField] private Image _fill;
        [SerializeField] private Material _fillMaterial;

        [Header("Motion")]
        [SerializeField] private float _levelSharpness = 6f;

        private Material _material;
        private float _target = 1f;
        private float _level = 1f;
        private float _time;

        public void Show(float fraction)
        {
            _target = Mathf.Clamp01(fraction);
        }

        private void Awake()
        {
            if (_fill == null || _fillMaterial == null)
                return;

            _material = new Material(_fillMaterial);
            _fill.material = _material;
        }

        private void Update()
        {
            float delta = Time.unscaledDeltaTime;

            _time += delta;
            _level = Mathf.Lerp(_level, _target, 1f - Mathf.Exp(-_levelSharpness * delta));

            if (_material == null)
                return;

            _material.SetFloat(LevelId, _level);
            _material.SetFloat(ClockId, _time);
        }

        private void OnDestroy()
        {
            if (_material != null)
                Destroy(_material);
        }
    }
}

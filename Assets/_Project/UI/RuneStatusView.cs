using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public sealed class RuneStatusView : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private TMP_Text _label;
        [SerializeField] private Image _fill;
        [SerializeField] private float _pulseHz = 1.5f;

        private string _shownName;
        private string _format;

        public bool IsWired => _root != null && _label != null && _fill != null;

        public void Show(bool active, string name, Color color, float remaining, float total)
        {
            if (_root.activeSelf != active)
                _root.SetActive(active);

            if (active == false)
            {
                _shownName = null;

                return;
            }

            if (_shownName != name)
            {
                _shownName = name;
                _format = name + "  {0:1}s";
                _label.color = color.linear;
                _fill.color = color.linear;
            }

            _label.SetText(_format, remaining);
            _fill.fillAmount = total > 0f ? Mathf.Clamp01(remaining / total) : 0f;

            float pulse = 0.85f + 0.15f * Mathf.Sin(Time.unscaledTime * _pulseHz * Mathf.PI * 2f);
            Color fill = color.linear;
            fill.a = pulse;
            _fill.color = fill;
        }
    }
}

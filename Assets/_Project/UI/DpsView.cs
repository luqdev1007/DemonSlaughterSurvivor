using System.Globalization;
using TMPro;
using UnityEngine;

namespace Game.UI
{
    public sealed class DpsView : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private TMP_Text _value;
        [SerializeField] private float _windowSeconds = 15f;

        private int _shown = -1;

        public bool IsWired => _root != null && _value != null && _windowSeconds > 0f;

        public float WindowSeconds => _windowSeconds;

        public void SetVisible(bool visible)
        {
            if (_root.activeSelf != visible)
                _root.SetActive(visible);
        }

        public void SetValue(float dps)
        {
            int rounded = Mathf.RoundToInt(dps);

            if (rounded == _shown)
                return;

            _shown = rounded;
            _value.SetText(rounded.ToString(CultureInfo.InvariantCulture));
        }
    }
}

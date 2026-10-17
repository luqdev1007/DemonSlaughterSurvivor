using TMPro;
using UnityEngine;

namespace Game.UI
{
    public sealed class BossMarkerView : MonoBehaviour
    {
        [SerializeField] private RectTransform _root;
        [SerializeField] private RectTransform _arrow;
        [SerializeField] private TMP_Text _label;
        [SerializeField, Range(0f, 0.4f)] private float _edgeMargin = 0.07f;
        [SerializeField] private string _labelFormat = "{0:0} m";
        [SerializeField] private float _labelOffset = 64f;

        private int _shownMeters = -1;

        public bool IsWired => _root != null && _arrow != null && _label != null && string.IsNullOrEmpty(_labelFormat) == false;

        public void Show(bool bossAlive, Vector3 viewport, float distance)
        {
            if (bossAlive == false || IsOnScreen(viewport))
            {
                SetShown(false);

                return;
            }

            Vector2 direction = new Vector2(viewport.x - 0.5f, viewport.y - 0.5f);

            if (viewport.z < 0f)
                direction = -direction;

            if (direction.sqrMagnitude < 1e-8f)
                direction = Vector2.down;

            float half = 0.5f - _edgeMargin;
            float scale = Mathf.Min(half / Mathf.Max(Mathf.Abs(direction.x), 1e-6f), half / Mathf.Max(Mathf.Abs(direction.y), 1e-6f));
            Vector2 anchor = new Vector2(0.5f, 0.5f) + direction * scale;

            _root.anchorMin = anchor;
            _root.anchorMax = anchor;
            _root.anchoredPosition = Vector2.zero;

            _arrow.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            _label.rectTransform.anchoredPosition = -direction.normalized * _labelOffset;

            int meters = Mathf.RoundToInt(distance);

            if (meters != _shownMeters)
            {
                _shownMeters = meters;
                _label.SetText(_labelFormat, meters);
            }

            SetShown(true);
        }

        private static bool IsOnScreen(Vector3 viewport)
        {
            return viewport.z > 0f && viewport.x >= 0f && viewport.x <= 1f && viewport.y >= 0f && viewport.y <= 1f;
        }

        private void SetShown(bool shown)
        {
            if (_root.gameObject.activeSelf == shown)
                return;

            _root.gameObject.SetActive(shown);
        }
    }
}

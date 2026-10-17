using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public sealed class PerkTooltipView : MonoBehaviour
    {
        [SerializeField] private RectTransform _panel;
        [SerializeField] private CanvasGroup _group;
        [SerializeField] private Image _border;
        [SerializeField] private TMP_Text _title;
        [SerializeField] private TMP_Text _subtitle;
        [SerializeField] private TMP_Text _description;
        [SerializeField] private TMP_Text _stats;
        [SerializeField] private TMP_Text _requires;
        [SerializeField] private float _gap = 16f;
        [SerializeField] private float _slideDistance = 40f;
        [SerializeField] private float _slideSeconds = 0.18f;

        private Sequence _tween;

        public bool IsWired => _panel != null && _group != null && _border != null && _title != null && _subtitle != null
            && _description != null && _stats != null && _requires != null;

        public void Show(PerkCardContent content, RarityPalette palette, RectTransform card, bool toTheLeft)
        {
            Color color = palette.Color(content.Rarity);

            _border.color = color;
            _title.SetText(content.Title);
            _title.color = color;
            _subtitle.SetText(content.NextLevel > 1
                ? $"{palette.Name(content.Rarity)} · уровень {content.NextLevel - 1} → {content.NextLevel} из {content.MaxLevel}"
                : $"{palette.Name(content.Rarity)} · уровень 1 из {content.MaxLevel}");
            _description.SetText(content.Description);
            SetOptional(_stats, content.Stats);
            SetOptional(_requires, content.Requires);

            RectTransform parent = (RectTransform)_panel.parent;
            Rect rect = card.rect;
            Vector3 world = card.TransformPoint(new Vector3(toTheLeft ? rect.xMin : rect.xMax, rect.center.y, 0f));
            Vector2 local = parent.InverseTransformPoint(world);
            float direction = toTheLeft ? -1f : 1f;
            Vector2 target = local + new Vector2(_gap * direction, 0f);

            _panel.pivot = new Vector2(toTheLeft ? 1f : 0f, 0.5f);

            _tween?.Kill();
            gameObject.SetActive(true);
            _panel.anchoredPosition = target - new Vector2(_slideDistance * direction, 0f);
            _group.alpha = 0f;

            _tween = DOTween.Sequence()
                .Join(DOTween.To(() => _panel.anchoredPosition, position => _panel.anchoredPosition = position, target, _slideSeconds).SetEase(Ease.OutCubic))
                .Join(DOTween.To(() => _group.alpha, alpha => _group.alpha = alpha, 1f, _slideSeconds))
                .SetUpdate(true);
        }

        public void Hide()
        {
            _tween?.Kill();
            _tween = null;
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            _tween?.Kill();
        }

        private static void SetOptional(TMP_Text text, string value)
        {
            bool has = string.IsNullOrEmpty(value) == false;

            text.gameObject.SetActive(has);

            if (has)
                text.SetText(value);
        }
    }
}

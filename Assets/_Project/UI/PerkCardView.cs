using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.UI
{
    public sealed class PerkCardView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Button _button;
        [SerializeField] private Image _border;
        [SerializeField] private Image _glow;
        [SerializeField] private Image _icon;
        [SerializeField] private Image _iconBorder;
        [SerializeField] private TMP_Text _hotkey;
        [SerializeField] private TMP_Text _title;
        [SerializeField] private TMP_Text _rarity;
        [SerializeField] private TMP_Text _rank;
        [SerializeField] private Image[] _pips;
        [SerializeField] private GameObject _combo;
        [SerializeField] private Image[] _comboIcons;
        [SerializeField] private Color _emptyPip = new Color(0.2f, 0.2f, 0.22f, 0.9f);
        [SerializeField] private float _glowAlpha = 0.18f;

        private Action<int> _onHover;
        private Action<int> _onLeave;
        private int _slot;

        public bool IsWired => _button != null && _border != null && _glow != null && _icon != null && _iconBorder != null
            && _hotkey != null && _title != null && _rarity != null && _rank != null
            && _pips != null && _pips.Length > 0 && _combo != null && _comboIcons != null && _comboIcons.Length > 0;

        public RectTransform Rect => (RectTransform)transform;

        public void Bind(int slot, Action<int> onPick, Action<int> onHover, Action<int> onLeave)
        {
            _slot = slot;
            _onHover = onHover;
            _onLeave = onLeave;

            _button.onClick.AddListener(() => onPick(_slot));
        }

        public void Unbind()
        {
            if (_button != null)
                _button.onClick.RemoveAllListeners();

            _onHover = null;
            _onLeave = null;
        }

        public void Show(PerkCardContent content, RarityPalette palette)
        {
            gameObject.SetActive(true);

            Color color = palette.Color(content.Rarity);

            _border.color = color;
            _iconBorder.color = color;
            _glow.color = new Color(color.r, color.g, color.b, 0f);
            _icon.sprite = content.Icon;
            _icon.enabled = content.Icon != null;
            _hotkey.SetText(content.Hotkey);
            _title.SetText(content.Title);
            _rarity.SetText(palette.Name(content.Rarity));
            _rarity.color = color;
            _rank.SetText(content.NextLevel.ToString());

            for (int index = 0; index < _pips.Length; index++)
            {
                _pips[index].gameObject.SetActive(index < content.MaxLevel);
                _pips[index].color = index < content.NextLevel ? color : _emptyPip;
            }

            _combo.SetActive(content.IsCombo);

            for (int index = 0; index < _comboIcons.Length; index++)
            {
                bool used = index < content.ComboIcons.Count;

                _comboIcons[index].gameObject.SetActive(used);

                if (used)
                    _comboIcons[index].sprite = content.ComboIcons[index];
            }
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void SetHighlighted(bool highlighted)
        {
            Color color = _glow.color;
            color.a = highlighted ? _glowAlpha : 0f;
            _glow.color = color;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _onHover?.Invoke(_slot);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _onLeave?.Invoke(_slot);
        }
    }
}

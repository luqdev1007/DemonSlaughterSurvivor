using System;
using UnityEngine;

namespace Game.UI
{
    public sealed class UpgradeChoiceView : MonoBehaviour
    {
        public const int SlotCount = 3;

        [SerializeField] private GameObject _panel;
        [SerializeField] private PerkCardView[] _cards;
        [SerializeField] private PerkTooltipView _tooltip;
        [SerializeField] private RarityPalette _palette = new RarityPalette();

        private readonly PerkCardContent[] _contents = new PerkCardContent[SlotCount];

        private int _hovered = -1;

        public bool IsWired
        {
            get
            {
                if (_panel == null || _cards == null || _cards.Length != SlotCount || _tooltip == null || _tooltip.IsWired == false)
                    return false;

                for (int slot = 0; slot < SlotCount; slot++)
                {
                    if (_cards[slot] == null || _cards[slot].IsWired == false)
                        return false;
                }

                return true;
            }
        }

        public void Bind(Action<int> onPick)
        {
            for (int slot = 0; slot < SlotCount; slot++)
                _cards[slot].Bind(slot, onPick, OnHover, OnLeave);
        }

        public void Unbind()
        {
            for (int slot = 0; slot < SlotCount; slot++)
            {
                if (_cards[slot] != null)
                    _cards[slot].Unbind();
            }
        }

        public void ShowSlot(int slot, PerkCardContent content)
        {
            _contents[slot] = content;
            _cards[slot].Show(content, _palette);
            _cards[slot].SetHighlighted(false);
        }

        public void HideSlot(int slot)
        {
            _contents[slot] = null;
            _cards[slot].Hide();

            if (_hovered == slot)
                OnLeave(slot);
        }

        public void SetOpen(bool open)
        {
            _panel.SetActive(open);

            if (open == false && _hovered >= 0)
                OnLeave(_hovered);
        }

        private void OnHover(int slot)
        {
            if (_contents[slot] == null)
                return;

            if (_hovered >= 0)
                _cards[_hovered].SetHighlighted(false);

            _hovered = slot;
            _cards[slot].SetHighlighted(true);
            _tooltip.Show(_contents[slot], _palette, _cards[slot].Rect, slot == SlotCount - 1);
        }

        private void OnLeave(int slot)
        {
            if (_hovered != slot)
                return;

            _cards[slot].SetHighlighted(false);
            _hovered = -1;
            _tooltip.Hide();
        }
    }
}

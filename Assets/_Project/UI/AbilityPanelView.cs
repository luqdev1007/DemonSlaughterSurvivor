using System.Collections.Generic;
using UnityEngine;

namespace Game.UI
{
    public sealed class AbilityPanelView : MonoBehaviour
    {
        [SerializeField] private RectTransform _container;
        [SerializeField] private AbilitySlotView _template;
        [SerializeField] private Sprite _dashIcon;

        private readonly List<AbilitySlotView> _slots = new List<AbilitySlotView>();

        public bool IsWired => _container != null && _template != null && _template.IsWired;

        public int SlotCount => _slots.Count;

        public Sprite DashIcon => _dashIcon;

        public void AddSlot(Sprite icon)
        {
            AbilitySlotView slot = Instantiate(_template, _container);
            slot.gameObject.SetActive(true);
            slot.SetIcon(icon);
            _slots.Add(slot);
        }

        public void SetSlot(int index, float remaining, float fill)
        {
            _slots[index].SetCooldown(remaining, fill);
        }
    }
}

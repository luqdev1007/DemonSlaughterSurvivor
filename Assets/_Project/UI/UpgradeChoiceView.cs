using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public sealed class UpgradeChoiceView : MonoBehaviour
    {
        public const int SlotCount = 3;

        [SerializeField] private GameObject _panel;
        [SerializeField] private Button[] _buttons;
        [SerializeField] private TMP_Text[] _titles;
        [SerializeField] private TMP_Text[] _descriptions;

        public bool IsWired
        {
            get
            {
                if (_panel == null || _buttons == null || _titles == null || _descriptions == null)
                    return false;

                if (_buttons.Length != SlotCount || _titles.Length != SlotCount || _descriptions.Length != SlotCount)
                    return false;

                for (int slot = 0; slot < SlotCount; slot++)
                {
                    if (_buttons[slot] == null || _titles[slot] == null || _descriptions[slot] == null)
                        return false;
                }

                return true;
            }
        }

        public void Bind(Action<int> onPick)
        {
            for (int slot = 0; slot < SlotCount; slot++)
            {
                int captured = slot;

                _buttons[slot].onClick.AddListener(() => onPick(captured));
            }
        }

        public void Unbind()
        {
            for (int slot = 0; slot < SlotCount; slot++)
            {
                if (_buttons[slot] != null)
                    _buttons[slot].onClick.RemoveAllListeners();
            }
        }

        public void ShowSlot(int slot, string title, string description)
        {
            _buttons[slot].gameObject.SetActive(true);
            _titles[slot].SetText(title);
            _descriptions[slot].SetText(description);
        }

        public void HideSlot(int slot)
        {
            _buttons[slot].gameObject.SetActive(false);
        }

        public void SetOpen(bool open)
        {
            _panel.SetActive(open);
        }
    }
}

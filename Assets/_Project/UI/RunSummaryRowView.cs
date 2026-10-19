using TMPro;
using UnityEngine;

namespace Game.UI
{
    public sealed class RunSummaryRowView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _label;
        [SerializeField] private TMP_Text _value;

        public bool IsWired => _label != null && _value != null;

        public void Show(string label, string value)
        {
            _label.SetText(label);
            _value.SetText(value);
        }
    }
}

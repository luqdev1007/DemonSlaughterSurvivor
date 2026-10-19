using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public sealed class RunSummaryView : MonoBehaviour
    {
        [SerializeField] private UiFade _fade;
        [SerializeField] private ScrollRect _scroll;
        [SerializeField] private RectTransform _rows;
        [SerializeField] private RunSummaryRowView _rowTemplate;
        [SerializeField] private Button _exit;

        private readonly List<RunSummaryRowView> _shown = new List<RunSummaryRowView>();

        public bool IsWired => _fade != null && _fade.IsWired && _scroll != null && _rows != null
            && _rowTemplate != null && _rowTemplate.IsWired && _exit != null;

        public void Bind(Action onExit)
        {
            _exit.onClick.AddListener(() => onExit());
        }

        public void Unbind()
        {
            if (_exit != null)
                _exit.onClick.RemoveAllListeners();
        }

        public void ClearRows()
        {
            for (int index = 0; index < _shown.Count; index++)
                Destroy(_shown[index].gameObject);

            _shown.Clear();
        }

        public void AddRow(string label, string value)
        {
            RunSummaryRowView row = Instantiate(_rowTemplate, _rows);
            row.gameObject.SetActive(true);
            row.Show(label, value);

            _shown.Add(row);
        }

        public void Open()
        {
            _rowTemplate.gameObject.SetActive(false);

            _fade.Show();

            LayoutRebuilder.ForceRebuildLayoutImmediate(_rows);
            _scroll.verticalNormalizedPosition = 1f;
        }

        public void HideImmediate()
        {
            _rowTemplate.gameObject.SetActive(false);

            _fade.HideImmediate();
        }
    }
}

using Game.Core;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Game.UI
{
    public sealed class DamageNumberLayerView : MonoBehaviour
    {
        [SerializeField] private RectTransform _layer;
        [SerializeField] private TMP_Text _template;
        [SerializeField] private int _capacity = 40;

        [Header("Motion")]
        [SerializeField] private float _lifeSeconds = 0.9f;
        [SerializeField] private float _fadeSeconds = 0.3f;
        [SerializeField] private float _risePixels = 70f;
        [SerializeField] private float _jitterPixels = 22f;
        [SerializeField] private float _mergeSeconds = 0.15f;
        [SerializeField] private float _popScale = 1.5f;
        [SerializeField] private float _popSeconds = 0.12f;

        [Header("Styles: normal, crit, magic, magic crit")]
        [SerializeField] private Color[] _colors =
        {
            new Color(1f, 1f, 1f),
            new Color(1f, 0.22f, 0.18f),
            new Color(0.35f, 0.62f, 1f),
            new Color(0.78f, 0.35f, 1f)
        };

        [SerializeField] private float[] _scales = { 1f, 1.35f, 1f, 1.35f };

        private readonly List<DamageNumberItem> _items = new List<DamageNumberItem>();

        public bool IsWired => _layer != null && _template != null && _capacity > 0;

        public RectTransform Layer => _layer;

        public float LifeSeconds => _lifeSeconds;

        public float FadeSeconds => _fadeSeconds;

        public float RisePixels => _risePixels;

        public float JitterPixels => _jitterPixels;

        public float MergeSeconds => _mergeSeconds;

        public float PopScale => _popScale;

        public float PopSeconds => _popSeconds;

        public IReadOnlyList<DamageNumberItem> Items => _items;

        public Color StyleColor(DamageNumberStyle style)
        {
            int index = (int)style;

            return index < _colors.Length ? _colors[index] : Color.white;
        }

        public float StyleScale(DamageNumberStyle style)
        {
            int index = (int)style;

            return index < _scales.Length ? _scales[index] : 1f;
        }

        public DamageNumberItem Acquire()
        {
            DamageNumberItem oldest = null;

            for (int index = 0; index < _items.Count; index++)
            {
                DamageNumberItem item = _items[index];

                if (item.Active == false)
                    return item;

                if (oldest == null || item.Age > oldest.Age)
                    oldest = item;
            }

            if (_items.Count < _capacity)
            {
                TMP_Text text = Instantiate(_template, _layer);
                DamageNumberItem created = new DamageNumberItem(text);
                _items.Add(created);

                return created;
            }

            return oldest;
        }
    }

    public sealed class DamageNumberItem
    {
        public DamageNumberItem(TMP_Text text)
        {
            Text = text;
            Rect = text.rectTransform;
            text.gameObject.SetActive(false);
        }

        public TMP_Text Text { get; }

        public RectTransform Rect { get; }

        public bool Active;
        public int Key;
        public Vector3 World;
        public float Amount;
        public DamageNumberStyle Style;
        public float Age;
        public float PopAge;
        public float Jitter;

        public void Deactivate()
        {
            Active = false;
            Text.gameObject.SetActive(false);
        }
    }
}

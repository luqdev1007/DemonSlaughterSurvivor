using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public sealed class MetaHubView : MonoBehaviour
    {
        private static readonly string[] TabNames = { "Оружие и реликвии", "Древо героя", "Общая сила" };

        [SerializeField] private MetaUiStyle _style;
        [SerializeField] private UiFade _fade;

        private RectTransform _root;
        private TMP_Text _memory;
        private TMP_Text _magic;
        private TMP_Text _relics;
        private RectTransform _heroRow;
        private RectTransform _devRow;
        private readonly Button[] _tabs = new Button[3];
        private readonly RectTransform[] _pages = new RectTransform[3];
        private TMP_Text _toast;
        private float _toastHideAt;

        public event Action<int> TabSelected;
        public event Action Closed;

        public bool IsWired => _style != null && _style.IsWired && _fade != null && _fade.IsWired;

        public MetaUiStyle Style => _style;

        public RectTransform HeroRow
        {
            get
            {
                Build();

                return _heroRow;
            }
        }

        public RectTransform DevRow
        {
            get
            {
                Build();

                return _devRow;
            }
        }

        public RectTransform Page(int index)
        {
            Build();

            return _pages[index];
        }

        public void Open()
        {
            Build();
            _fade.Show();
        }

        public void Close()
        {
            _fade.Hide();
        }

        public void HideImmediate()
        {
            _fade.HideImmediate();
        }

        public void SetWallet(int memory, int magic, int relics)
        {
            Build();

            _memory.text = UiKit.Number(memory);
            _magic.text = UiKit.Number(magic);
            _relics.text = UiKit.Number(relics);
        }

        public void ShowTab(int index)
        {
            Build();

            for (int page = 0; page < _pages.Length; page++)
            {
                _pages[page].gameObject.SetActive(page == index);

                TMP_Text label = _tabs[page].GetComponentInChildren<TMP_Text>();
                label.color = page == index ? MetaPalette.Bone : MetaPalette.Dim;
                _tabs[page].GetComponent<Image>().sprite = page == index ? _style.Button : _style.ButtonOff;
            }
        }

        public void Toast(string message)
        {
            Build();

            _toast.text = message;
            _toast.transform.parent.gameObject.SetActive(true);
            _toastHideAt = Time.unscaledTime + 2.4f;
        }

        private void Update()
        {
            if (_toast == null || _toast.transform.parent.gameObject.activeSelf == false)
                return;

            if (Time.unscaledTime >= _toastHideAt)
                _toast.transform.parent.gameObject.SetActive(false);
        }

        private void Build()
        {
            if (_root != null)
                return;

            _root = UiKit.Stretch("Content", transform);
            UiKit.Fill(_root, "Background", null, MetaPalette.Void, 0f, true);

            Image glow = UiKit.Image(_root, "Glow", _style.SoftGlow, MetaPalette.WithAlpha(MetaPalette.Blood, 0.12f));
            UiKit.Place(glow.rectTransform, new Vector2(0.2f, 0.5f), Vector2.zero, new Vector2(1400f, 1200f));

            TMP_Text logo = UiKit.Text(_root, "Logo", _style.TitleFont, 44f, MetaPalette.Bone, TextAlignmentOptions.Left,
                $"DEMON <color={MetaPalette.Tag(MetaPalette.Blood)}>SLAUGHTER</color>");
            logo.characterSpacing = 6f;
            UiKit.Place(logo.rectTransform, new Vector2(0f, 1f), new Vector2(330f, -52f), new Vector2(600f, 60f));

            TMP_Text caption = UiKit.Text(_root, "Caption", _style.BodyFont, 20f, MetaPalette.Dim, TextAlignmentOptions.Left, "ПРОКАЧКА · МЕТА-ПРОГРЕССИЯ");
            caption.characterSpacing = 8f;
            UiKit.Place(caption.rectTransform, new Vector2(0f, 1f), new Vector2(330f, -92f), new Vector2(600f, 30f));

            _memory = WalletChip("Memory", 0, _style.MemoryIcon, "Осколки воспоминаний", MetaPalette.Memory);
            _magic = WalletChip("Magic", 1, _style.MagicIcon, "Магические осколки", MetaPalette.Magic);
            _relics = WalletChip("Relics", 2, _style.RelicIcon, "Реликвии", MetaPalette.Gold);

            Image rule = UiKit.Image(_root, "Rule", null, MetaPalette.Line);
            rule.rectTransform.anchorMin = new Vector2(0.015f, 1f);
            rule.rectTransform.anchorMax = new Vector2(0.985f, 1f);
            rule.rectTransform.offsetMin = new Vector2(0f, -128f);
            rule.rectTransform.offsetMax = new Vector2(0f, -126f);

            _heroRow = UiKit.Rect("Heroes", _root);
            UiKit.Place(_heroRow, new Vector2(0f, 1f), new Vector2(330f, -168f), new Vector2(600f, 56f));
            Row(_heroRow, 10f);

            _devRow = UiKit.Rect("Dev", _root);
            UiKit.Place(_devRow, new Vector2(1f, 1f), new Vector2(-690f, -168f), new Vector2(1000f, 44f));
            Row(_devRow, 8f);

            Button close = UiKit.Button(_root, "Close", _style, "Назад", () => Closed?.Invoke(), 24f);
            UiKit.Place((RectTransform)close.transform, new Vector2(1f, 1f), new Vector2(-95f, -168f), new Vector2(150f, 56f));

            for (int index = 0; index < _tabs.Length; index++)
            {
                int captured = index;
                _tabs[index] = UiKit.Button(_root, "Tab" + index, _style, TabNames[index], () => TabSelected?.Invoke(captured), 28f);
                UiKit.Place((RectTransform)_tabs[index].transform, new Vector2(0f, 1f), new Vector2(200f + index * 400f, -236f), new Vector2(380f, 64f));

                _pages[index] = UiKit.Rect("Page" + index, _root);
                _pages[index].anchorMin = Vector2.zero;
                _pages[index].anchorMax = Vector2.one;
                _pages[index].offsetMin = new Vector2(30f, 24f);
                _pages[index].offsetMax = new Vector2(-30f, -280f);
            }

            Image toastBack = UiKit.Image(_root, "Toast", _style.Bar, Color.white);
            UiKit.Place(toastBack.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 70f), new Vector2(820f, 70f));
            _toast = UiKit.Text(toastBack.transform, "Text", _style.BodyFont, 26f, MetaPalette.Bone, TextAlignmentOptions.Center);
            UiKit.Fill(_toast.rectTransform, 10f);
            toastBack.gameObject.SetActive(false);
        }

        private TMP_Text WalletChip(string name, int slot, Sprite icon, string caption, Color accent)
        {
            Image chip = UiKit.Card(_root, name, _style);
            UiKit.Place(chip.rectTransform, new Vector2(1f, 1f), new Vector2(-180f - (2 - slot) * 330f, -62f), new Vector2(310f, 92f));

            Image iconImage = UiKit.Image(chip.transform, "Icon", icon, Color.white);
            iconImage.preserveAspect = true;
            UiKit.Place(iconImage.rectTransform, new Vector2(0f, 0.5f), new Vector2(46f, 0f), new Vector2(56f, 56f));

            TMP_Text label = UiKit.Text(chip.transform, "Caption", _style.BodyFont, 18f, MetaPalette.Dim, TextAlignmentOptions.Left, caption);
            UiKit.Place(label.rectTransform, new Vector2(0f, 0.5f), new Vector2(190f, 18f), new Vector2(220f, 26f));

            TMP_Text value = UiKit.Text(chip.transform, "Value", _style.BodyFont, 32f, accent, TextAlignmentOptions.Left, "0");
            value.fontStyle = FontStyles.Bold;
            UiKit.Place(value.rectTransform, new Vector2(0f, 0.5f), new Vector2(190f, -14f), new Vector2(220f, 40f));

            return value;
        }

        private static void Row(RectTransform rect, float spacing)
        {
            HorizontalLayoutGroup row = rect.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = spacing;
            row.childAlignment = TextAnchor.MiddleLeft;
            row.childControlWidth = false;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = true;
        }
    }
}

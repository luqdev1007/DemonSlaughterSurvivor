using DG.Tweening;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public sealed class RunEndView : MonoBehaviour
    {
        private const float CountSeconds = 1.1f;
        private const float CardWidth = 520f;
        private const float CardHeight = 400f;

        [SerializeField] private MetaUiStyle _style;
        [SerializeField] private UiFade _fade;

        private RectTransform _root;
        private TMP_Text _title;
        private TMP_Text _subtitle;
        private CardWidgets _memory;
        private CardWidgets _magic;
        private CanvasGroup _relicGroup;
        private Image _relicIcon;
        private TMP_Text _relicEyebrow;
        private TMP_Text _relicName;
        private TMP_Text _relicText;
        private Button _claim;
        private TMP_Text _wallet;
        private GameObject _statsPopup;
        private RectTransform _statsRows;
        private readonly List<Tween> _tweens = new List<Tween>();

        private Action _onClaim;
        private Action _onAgain;
        private Action _onMenu;
        private Action _onUpgrade;

        public bool IsWired => _style != null && _style.IsWired && _fade != null && _fade.IsWired;

        public void Bind(Action onClaim, Action onAgain, Action onMenu, Action onUpgrade)
        {
            _onClaim = onClaim;
            _onAgain = onAgain;
            _onMenu = onMenu;
            _onUpgrade = onUpgrade;
        }

        public void Unbind()
        {
            _onClaim = null;
            _onAgain = null;
            _onMenu = null;
            _onUpgrade = null;
        }

        public void HideImmediate()
        {
            _fade.HideImmediate();
        }

        public void Show(RunEndModel model)
        {
            Build();

            _title.text = model.Title;
            _title.color = model.Victory ? MetaPalette.Blood : MetaPalette.Dim;
            _subtitle.text = model.Subtitle;

            FillCard(_memory, model.MemoryShards, model.MemoryLines, model.MemoryNote);
            FillCard(_magic, model.MagicShards, model.MagicLines, model.MagicNote);

            _relicGroup.alpha = model.RelicActive ? 1f : 0.45f;
            _relicIcon.sprite = model.RelicActive ? _style.RelicIcon : _style.RelicOffIcon;
            _relicEyebrow.text = model.RelicEyebrow;
            _relicName.text = model.RelicName;
            _relicName.color = model.RelicActive ? MetaPalette.Gold : MetaPalette.Dim;
            _relicText.text = model.RelicText;

            UiKit.Clear(_statsRows);

            foreach ((string label, string value) in model.Stats)
                AddStatRow(label, value);

            _statsPopup.SetActive(false);
            _fade.Show();

            KillTweens();
            CountUp(_memory.Total, model.MemoryShards);
            CountUp(_magic.Total, model.MagicShards);
        }

        public void SetClaimed(bool claimed, string wallet)
        {
            Build();

            _claim.interactable = claimed == false;
            UiKit.SetLabel(_claim, claimed ? "Награды получены" : "Забрать награды");
            _wallet.text = wallet;
        }

        private void OnDestroy()
        {
            KillTweens();
        }

        private void Build()
        {
            if (_root != null)
                return;

            _root = UiKit.Stretch("Content", transform);
            UiKit.Fill(_root, "Dim", null, MetaPalette.Void, 0f, true);

            Image glow = UiKit.Image(_root, "Glow", _style.SoftGlow, MetaPalette.WithAlpha(MetaPalette.Blood, 0.25f));
            UiKit.Place(glow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 360f), new Vector2(1400f, 500f));

            _title = UiKit.Text(_root, "Title", _style.TitleFont, 132f, MetaPalette.Blood, TextAlignmentOptions.Center);
            _title.characterSpacing = 12f;
            UiKit.Place(_title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 390f), new Vector2(1400f, 160f));

            _subtitle = UiKit.Text(_root, "Subtitle", _style.BodyFont, 30f, MetaPalette.Dim, TextAlignmentOptions.Center);
            UiKit.Place(_subtitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 300f), new Vector2(1600f, 50f));

            _memory = BuildCard("Memory", -570f, _style.MemoryIcon, MetaPalette.Memory, "Осколки воспоминаний");
            _magic = BuildCard("Magic", 0f, _style.MagicIcon, MetaPalette.Magic, "Магические осколки");
            BuildRelicCard();

            RectTransform buttons = UiKit.Rect("Buttons", _root);
            UiKit.Place(buttons, new Vector2(0.5f, 0.5f), new Vector2(0f, -270f), new Vector2(1760f, 84f));
            HorizontalLayoutGroup row = buttons.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 18f;
            row.childAlignment = TextAnchor.MiddleCenter;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = true;
            row.childForceExpandHeight = true;

            UiKit.Button(buttons, "Stats", _style, "Статистика забега", () => _statsPopup.SetActive(true));
            _claim = UiKit.Button(buttons, "Claim", _style, "Забрать награды", () => _onClaim?.Invoke());
            UiKit.Button(buttons, "Again", _style, "Сыграть ещё раз", () => _onAgain?.Invoke());
            UiKit.Button(buttons, "Menu", _style, "В меню", () => _onMenu?.Invoke());
            UiKit.Button(buttons, "Upgrade", _style, "Прокачка", () => _onUpgrade?.Invoke());

            _wallet = UiKit.Text(_root, "Wallet", _style.BodyFont, 26f, MetaPalette.Dim, TextAlignmentOptions.Center);
            UiKit.Place(_wallet.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -350f), new Vector2(1600f, 44f));

            BuildStatsPopup();
        }

        private CardWidgets BuildCard(string name, float x, Sprite icon, Color accent, string caption)
        {
            Image card = UiKit.Card(_root, name, _style);
            UiKit.Place(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(x, 20f), new Vector2(CardWidth, CardHeight));

            Image stripe = UiKit.Image(card.transform, "Stripe", null, accent);
            UiKit.Area(stripe.rectTransform, new Vector2(0.04f, 0.985f), new Vector2(0.96f, 0.995f));

            Image iconImage = UiKit.Image(card.transform, "Icon", icon, Color.white);
            iconImage.preserveAspect = true;
            UiKit.Place(iconImage.rectTransform, new Vector2(0f, 1f), new Vector2(76f, -76f), new Vector2(84f, 84f));

            TMP_Text total = UiKit.Text(card.transform, "Total", _style.BodyFont, 64f, accent, TextAlignmentOptions.Left, "0");
            total.fontStyle = FontStyles.Bold;
            UiKit.Place(total.rectTransform, new Vector2(0f, 1f), new Vector2(300f, -62f), new Vector2(300f, 70f));

            TMP_Text label = UiKit.Text(card.transform, "Caption", _style.BodyFont, 24f, MetaPalette.Dim, TextAlignmentOptions.Left, caption);
            UiKit.Place(label.rectTransform, new Vector2(0f, 1f), new Vector2(300f, -112f), new Vector2(300f, 34f));

            RectTransform lines = UiKit.Rect("Lines", card.transform);
            UiKit.Area(lines, new Vector2(0.07f, 0.08f), new Vector2(0.93f, 0.66f));
            VerticalLayoutGroup column = lines.gameObject.AddComponent<VerticalLayoutGroup>();
            column.spacing = 6f;
            column.childControlHeight = false;
            column.childControlWidth = true;
            column.childForceExpandHeight = false;
            column.childAlignment = TextAnchor.UpperLeft;

            return new CardWidgets { Total = total, Lines = lines };
        }

        private void BuildRelicCard()
        {
            Image card = UiKit.Card(_root, "Relic", _style);
            UiKit.Place(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(570f, 20f), new Vector2(CardWidth, CardHeight));
            _relicGroup = card.gameObject.AddComponent<CanvasGroup>();

            Image stripe = UiKit.Image(card.transform, "Stripe", null, MetaPalette.Gold);
            UiKit.Area(stripe.rectTransform, new Vector2(0.04f, 0.985f), new Vector2(0.96f, 0.995f));

            _relicIcon = UiKit.Image(card.transform, "Icon", _style.RelicIcon, Color.white);
            _relicIcon.preserveAspect = true;
            UiKit.Place(_relicIcon.rectTransform, new Vector2(0f, 1f), new Vector2(84f, -84f), new Vector2(104f, 104f));

            _relicEyebrow = UiKit.Text(card.transform, "Eyebrow", _style.BodyFont, 20f, MetaPalette.Faint, TextAlignmentOptions.Left);
            _relicEyebrow.characterSpacing = 6f;
            UiKit.Place(_relicEyebrow.rectTransform, new Vector2(0f, 1f), new Vector2(320f, -54f), new Vector2(330f, 30f));

            _relicName = UiKit.Text(card.transform, "Name", _style.TitleFont, 34f, MetaPalette.Gold, TextAlignmentOptions.TopLeft);
            UiKit.Place(_relicName.rectTransform, new Vector2(0f, 1f), new Vector2(320f, -112f), new Vector2(330f, 84f));

            _relicText = UiKit.Text(card.transform, "Text", _style.BodyFont, 22f, MetaPalette.Dim, TextAlignmentOptions.TopLeft);
            UiKit.Area(_relicText.rectTransform, new Vector2(0.07f, 0.06f), new Vector2(0.93f, 0.6f));
        }

        private void BuildStatsPopup()
        {
            RectTransform popup = UiKit.Stretch("StatsPopup", _root);
            _statsPopup = popup.gameObject;
            UiKit.Fill(popup, "Dim", null, MetaPalette.WithAlpha(MetaPalette.Void, 0.97f), 0f, true);

            Image panel = UiKit.Panel(popup, "Panel", _style);
            UiKit.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 700f));

            TMP_Text header = UiKit.Text(panel.transform, "Header", _style.TitleFont, 46f, MetaPalette.Bone, TextAlignmentOptions.Left, "Статистика забега");
            UiKit.Place(header.rectTransform, new Vector2(0.5f, 1f), new Vector2(-80f, -70f), new Vector2(520f, 60f));

            Button close = UiKit.Button(panel.transform, "Close", _style, "Закрыть", () => _statsPopup.SetActive(false), 22f);
            UiKit.Place((RectTransform)close.transform, new Vector2(1f, 1f), new Vector2(-130f, -70f), new Vector2(170f, 60f));

            _statsRows = UiKit.Rect("Rows", panel.transform);
            UiKit.Area(_statsRows, new Vector2(0.08f, 0.07f), new Vector2(0.92f, 0.82f));
            VerticalLayoutGroup column = _statsRows.gameObject.AddComponent<VerticalLayoutGroup>();
            column.spacing = 4f;
            column.childControlHeight = false;
            column.childControlWidth = true;
            column.childForceExpandHeight = false;

            _statsPopup.SetActive(false);
        }

        private void FillCard(CardWidgets card, int total, List<(string Label, string Value)> lines, string note)
        {
            card.Total.text = "0";
            UiKit.Clear(card.Lines);

            foreach ((string label, string value) in lines)
                AddLine(card.Lines, label, value, MetaPalette.Dim, MetaPalette.Bone, 24f);

            if (string.IsNullOrEmpty(note) == false)
                AddLine(card.Lines, note, string.Empty, MetaPalette.Faint, MetaPalette.Faint, 20f);
        }

        private void AddLine(RectTransform parent, string label, string value, Color labelColor, Color valueColor, float size)
        {
            RectTransform line = UiKit.Rect("Line", parent);
            line.sizeDelta = new Vector2(0f, size + 8f);

            TMP_Text left = UiKit.Text(line, "Label", _style.BodyFont, size, labelColor, TextAlignmentOptions.Left, label);
            UiKit.Area(left.rectTransform, Vector2.zero, new Vector2(0.75f, 1f));

            TMP_Text right = UiKit.Text(line, "Value", _style.BodyFont, size, valueColor, TextAlignmentOptions.Right, value);
            UiKit.Area(right.rectTransform, new Vector2(0.55f, 0f), Vector2.one);
        }

        private void AddStatRow(string label, string value)
        {
            RectTransform row = UiKit.Rect("Row", _statsRows);
            row.sizeDelta = new Vector2(0f, 52f);

            Image rule = UiKit.Image(row, "Rule", null, MetaPalette.Line);
            UiKit.Area(rule.rectTransform, Vector2.zero, new Vector2(1f, 0.03f));

            TMP_Text left = UiKit.Text(row, "Label", _style.BodyFont, 28f, MetaPalette.Bone, TextAlignmentOptions.Left, label);
            UiKit.Area(left.rectTransform, Vector2.zero, new Vector2(0.7f, 1f));

            TMP_Text right = UiKit.Text(row, "Value", _style.BodyFont, 28f, MetaPalette.Bone, TextAlignmentOptions.Right, value);
            UiKit.Area(right.rectTransform, new Vector2(0.5f, 0f), Vector2.one);
        }

        private void CountUp(TMP_Text text, int target)
        {
            float shown = 0f;

            Tween tween = DOTween.To(() => shown, value =>
                {
                    shown = value;
                    text.text = UiKit.Number(value);
                }, target, CountSeconds)
                .SetEase(Ease.OutCubic)
                .SetUpdate(true);

            _tweens.Add(tween);
        }

        private void KillTweens()
        {
            foreach (Tween tween in _tweens)
            {
                if (tween != null && tween.IsActive())
                    tween.Kill();
            }

            _tweens.Clear();
        }

        private struct CardWidgets
        {
            public TMP_Text Total;
            public RectTransform Lines;
        }
    }
}

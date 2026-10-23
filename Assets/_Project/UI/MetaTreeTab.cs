using Game.Configs;
using Game.Core;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public sealed class MetaTreeTab
    {
        private const float Scale = 1.12f;
        private static readonly Vector2 Origin = new Vector2(20f, -20f);

        private readonly RectTransform _page;
        private readonly MetaHubView _hub;
        private readonly MetaUiStyle _style;
        private readonly IMetaProgress _meta;
        private readonly IContentRegistry _content;

        private RectTransform _body;
        private string _characterId;
        private string _selected;

        public MetaTreeTab(RectTransform page, MetaHubView hub, IMetaProgress meta, IContentRegistry content)
        {
            _page = page;
            _hub = hub;
            _style = hub.Style;
            _meta = meta;
            _content = content;
        }

        public void SetCharacter(string characterId)
        {
            if (_characterId == characterId)
                return;

            _characterId = characterId;
            _selected = null;
        }

        public void Refresh()
        {
            if (_body != null)
                Object.Destroy(_body.gameObject);

            _body = UiKit.Stretch("Tree", _page);

            HeroTreeConfig tree = FindTree();

            if (tree == null)
            {
                TMP_Text empty = UiKit.Text(_body, "Empty", _style.BodyFont, 30f, MetaPalette.Dim, TextAlignmentOptions.Center, "У этого героя пока нет древа.");
                UiKit.Fill(empty.rectTransform);

                return;
            }

            if (_selected == null || tree.Find(_selected) == null)
                _selected = tree.Nodes[0].Id;

            TMP_Text eyebrow = UiKit.Text(_body, "Eyebrow", _style.BodyFont, 20f, MetaPalette.Faint, TextAlignmentOptions.Left, "ДРЕВО ГЕРОЯ · ЗА ОСКОЛКИ ВОСПОМИНАНИЙ");
            eyebrow.characterSpacing = 4f;
            UiKit.Place(eyebrow.rectTransform, new Vector2(0f, 1f), new Vector2(330f, -14f), new Vector2(620f, 28f));

            TMP_Text title = UiKit.Text(_body, "Title", _style.TitleFont, 40f, MetaPalette.Bone, TextAlignmentOptions.Left, tree.DisplayName);
            UiKit.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(330f, -50f), new Vector2(620f, 48f));

            Button reset = UiKit.Button(_body, "Reset", _style, "Сбросить древо (вернуть осколки)", Reset, 22f);
            UiKit.Place((RectTransform)reset.transform, new Vector2(1f, 1f), new Vector2(-250f, -38f), new Vector2(460f, 60f));

            RectTransform canvas = UiKit.Rect("Canvas", _body);
            canvas.anchorMin = new Vector2(0f, 0f);
            canvas.anchorMax = new Vector2(0f, 1f);
            canvas.pivot = new Vector2(0f, 1f);
            canvas.offsetMin = new Vector2(0f, 40f);
            canvas.offsetMax = new Vector2(1340f, -84f);

            Image frame = UiKit.Fill(canvas, "Frame", _style.PanelBack, Color.white);
            UiKit.Fill(frame.transform, "Border", _style.PanelFrame, Color.white);

            foreach (HeroTreeBranchLabel label in tree.BranchLabels)
            {
                TMP_Text text = UiKit.Text(canvas, "Branch", _style.TitleFont, 24f, MetaPalette.Faint, TextAlignmentOptions.Left, label.Text);
                text.characterSpacing = 8f;
                text.rectTransform.pivot = new Vector2(0f, 0.5f);
                UiKit.Place(text.rectTransform, new Vector2(0f, 1f), Map(label.Position) + new Vector2(150f, 0f), new Vector2(400f, 34f));
            }

            foreach (HeroTreeNode node in tree.Nodes)
            {
                foreach (string parent in node.Parents)
                    Edge(canvas, tree.Find(parent), node);

                foreach (HeroTreeGate gate in node.Gates)
                {
                    foreach (string member in gate.AnyOf)
                        Edge(canvas, tree.Find(member), node);
                }
            }

            foreach (HeroTreeNode node in tree.Nodes)
                Node(canvas, node);

            Legend();
            Panel(tree, tree.Find(_selected));
        }

        private HeroTreeConfig FindTree()
        {
            foreach (HeroTreeConfig tree in _content.All<HeroTreeConfig>())
            {
                if (tree.Character != null && tree.Character.Id == _characterId)
                    return tree;
            }

            return null;
        }

        private static Vector2 Map(Vector2 position)
        {
            return Origin + new Vector2(position.x * Scale, -position.y * Scale);
        }

        private bool IsOpen(HeroTreeNode node)
        {
            MetaStatus status = _meta.NodeOffer(_characterId, node.Id).Status;

            return status == MetaStatus.Available || status == MetaStatus.NotEnoughCurrency;
        }

        private void Edge(RectTransform canvas, HeroTreeNode from, HeroTreeNode to)
        {
            if (from == null || to == null)
                return;

            bool lit = _meta.NodeRank(_characterId, from.Id) > 0 && _meta.NodeRank(_characterId, to.Id) > 0;
            bool half = _meta.NodeRank(_characterId, from.Id) > 0 && IsOpen(to);

            Vector2 a = Map(from.Position);
            Vector2 b = Map(to.Position);
            Vector2 delta = b - a;

            Image line = UiKit.Image(canvas, "Edge", null, lit ? MetaPalette.Blood : half ? new Color(0.48f, 0.29f, 0.17f) : new Color(0.18f, 0.15f, 0.15f));
            RectTransform rect = line.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = a;
            rect.sizeDelta = new Vector2(delta.magnitude, lit ? 6f : 3f);
            rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        }

        private void Node(RectTransform canvas, HeroTreeNode node)
        {
            int rank = _meta.NodeRank(_characterId, node.Id);
            bool open = IsOpen(node);
            bool selected = node.Id == _selected;
            Color ring = RingColor(node.Type);
            float radius = Radius(node.Type) * Scale;
            Vector2 center = Map(node.Position);

            RectTransform root = UiKit.Rect(node.Id, canvas);
            UiKit.Place(root, new Vector2(0f, 1f), center, new Vector2(radius * 2f, radius * 2f));
            CanvasGroup group = root.gameObject.AddComponent<CanvasGroup>();
            group.alpha = rank > 0 || open ? 1f : 0.4f;

            if (selected || (rank > 0 && rank >= node.MaxRank))
            {
                Image halo = UiKit.Image(root, "Halo", _style.SoftGlow, MetaPalette.WithAlpha(selected ? MetaPalette.Ember : ring, selected ? 0.55f : 0.3f));
                UiKit.Place(halo.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(radius * 4f, radius * 4f));
            }

            if (node.Type == HeroTreeNodeType.Golden)
            {
                Image outer = UiKit.Image(root, "Outer", _style.CircleRing, MetaPalette.WithAlpha(ring, 0.45f));
                UiKit.Place(outer.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(radius * 2.6f, radius * 2.6f));
            }

            Image fill = UiKit.Image(root, "Fill", Shape(node.Type, false), rank > 0 ? FillColor(node.Type) : new Color(0.08f, 0.06f, 0.07f), true);
            UiKit.Fill(fill.rectTransform);
            Image border = UiKit.Image(root, "Ring", Shape(node.Type, true), ring);
            UiKit.Fill(border.rectTransform, selected ? -4f : 0f);

            Button button = fill.gameObject.AddComponent<Button>();
            button.targetGraphic = fill;
            string id = node.Id;
            button.onClick.AddListener(() => Select(id));

            string glyph = node.Type == HeroTreeNodeType.Class || node.MaxRank > 1 ? $"{rank}/{node.MaxRank}" : Glyph(node.Type);
            TMP_Text inner = UiKit.Text(root, "Glyph", _style.BodyFont, node.MaxRank > 1 ? 22f : 26f, rank > 0 ? MetaPalette.Ember : MetaPalette.Faint, TextAlignmentOptions.Center, glyph);
            inner.fontStyle = FontStyles.Bold;
            UiKit.Fill(inner.rectTransform);

            string name = node.Effect.NotImplemented ? node.DisplayName + $" <color={MetaPalette.Tag(MetaPalette.Ember)}><size=75%>· скоро</size></color>" : node.DisplayName;
            TMP_Text label = UiKit.Text(root, "Label", _style.BodyFont, 19f, rank > 0 ? MetaPalette.Bone : open ? MetaPalette.Dim : MetaPalette.Faint, TextAlignmentOptions.Top, name);
            label.lineSpacing = -20f;
            UiKit.Place(label.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, -32f), new Vector2(150f, 56f));
        }

        private void Legend()
        {
            TMP_Text legend = UiKit.Text(_body, "Legend", _style.BodyFont, 20f, MetaPalette.Dim, TextAlignmentOptions.Left,
                "Круг — свойство класса (ранги)   ■ модификатор перка из забега   ◆ ключевой выбор (одно из двух)   ▲ золотой узел");
            legend.textWrappingMode = TextWrappingModes.NoWrap;
            UiKit.Place(legend.rectTransform, new Vector2(0f, 0f), new Vector2(650f, 18f), new Vector2(1260f, 30f));
        }

        private void Panel(HeroTreeConfig tree, HeroTreeNode node)
        {
            Image panel = UiKit.Panel(_body, "Panel", _style);
            panel.rectTransform.anchorMin = new Vector2(0f, 0f);
            panel.rectTransform.anchorMax = new Vector2(1f, 1f);
            panel.rectTransform.offsetMin = new Vector2(1370f, 40f);
            panel.rectTransform.offsetMax = new Vector2(0f, -84f);

            RectTransform content = UiKit.Stretch("Content", panel.transform, 34f);
            VerticalLayoutGroup column = content.gameObject.AddComponent<VerticalLayoutGroup>();
            column.spacing = 14f;
            column.childControlHeight = true;
            column.childControlWidth = true;
            column.childForceExpandHeight = false;

            int rank = _meta.NodeRank(_characterId, node.Id);
            MetaOffer offer = _meta.NodeOffer(_characterId, node.Id);

            Line(content, $"<color={MetaPalette.Tag(MetaPalette.Faint)}>{TypeName(node.Type).ToUpperInvariant()}</color>", 20f);
            Line(content, node.DisplayName, 40f, _style.TitleFont, MetaPalette.Bone);

            if (node.Perk != null)
                Line(content, $"<color={MetaPalette.Tag(MetaPalette.Ember)}>Меняет перк забега: «{node.Perk.DisplayName}»</color>", 22f);

            string description = MetaText.Describe(node.Description, node.DisplayPerRank, rank);

            if (string.IsNullOrEmpty(node.RankNote) == false)
                description += $" <color={MetaPalette.Tag(MetaPalette.Faint)}>({node.RankNote})</color>";

            Line(content, description, 26f);

            if (node.MaxRank > 1)
                Line(content, MetaText.Pips(rank, node.MaxRank) + $"  <color={MetaPalette.Tag(MetaPalette.Dim)}>{rank}/{node.MaxRank}</color>", 28f);

            if (string.IsNullOrEmpty(node.Excludes) == false && tree.Find(node.Excludes) is HeroTreeNode excluded)
                Line(content, $"<color={MetaPalette.Tag(MetaPalette.Dim)}>Исключает «{excluded.DisplayName}»: {excluded.Description}</color>", 22f);

            if (node.Effect.NotImplemented)
                Line(content, $"<color={MetaPalette.Tag(MetaPalette.Ember)}>Скоро: эффект ещё не работает в бою. Ранг сохранится и заработает, когда эффект появится.</color>", 22f);

            string reason = Reason(tree, node, offer.Status);

            if (string.IsNullOrEmpty(reason) == false)
                Line(content, $"<color={MetaPalette.Tag(MetaPalette.Ember)}>{reason}</color>", 22f);

            if (node.Type == HeroTreeNodeType.Start)
            {
                Line(content, $"<color={MetaPalette.Tag(MetaPalette.Dim)}>Всегда открыт.</color>", 22f);
            }
            else
            {
                string label = offer.Status == MetaStatus.Maxed ? "Изучено" : "Изучить  " + MetaText.Cost(offer.Cost, true);
                Button buy = UiKit.Button(content, "Buy", _style, label, () => Buy(node.Id), 26f);
                buy.gameObject.AddComponent<LayoutElement>().preferredHeight = 70f;
                buy.interactable = offer.CanBuy;
            }

            Line(content, $"<color={MetaPalette.Tag(MetaPalette.Dim)}>Потрачено на древо: {UiKit.Number(_meta.TreeSpent(_characterId))} осколков воспоминаний</color>", 20f);
        }

        private string Reason(HeroTreeConfig tree, HeroTreeNode node, MetaStatus status)
        {
            switch (status)
            {
                case MetaStatus.Excluded:
                    return $"Закрыто: выбран «{tree.Find(node.Excludes).DisplayName}». Сбрось древо, чтобы поменять.";
                case MetaStatus.GateUnmet:
                    return $"Нужно дойти до конца {node.GatesRequired} веток из {node.Gates.Count} (сейчас: {OpenGates(node)}).";
                case MetaStatus.NeedsParents:
                    List<string> names = new List<string>();

                    foreach (string parent in node.Parents)
                        names.Add(tree.Find(parent).DisplayName);

                    return $"Сначала изучи «{string.Join("», «", names)}».";
                case MetaStatus.NotEnoughCurrency:
                    return "Не хватает осколков воспоминаний.";
                default:
                    return null;
            }
        }

        private int OpenGates(HeroTreeNode node)
        {
            int open = 0;

            foreach (HeroTreeGate gate in node.Gates)
            {
                foreach (string member in gate.AnyOf)
                {
                    if (_meta.NodeRank(_characterId, member) > 0)
                    {
                        open++;

                        break;
                    }
                }
            }

            return open;
        }

        private void Line(RectTransform parent, string text, float size, TMP_FontAsset font = null, Color? color = null)
        {
            TMP_Text line = UiKit.Text(parent, "Line", font ?? _style.BodyFont, size, color ?? MetaPalette.Bone, TextAlignmentOptions.TopLeft, text);
            line.overflowMode = TextOverflowModes.Overflow;
        }

        private void Select(string nodeId)
        {
            _selected = nodeId;
            Refresh();
        }

        private void Buy(string nodeId)
        {
            MetaStatus status = _meta.BuyNode(_characterId, nodeId);

            if (status != MetaStatus.Available)
                _hub.Toast(MetaText.Reason(status, true));
        }

        private void Reset()
        {
            int refund = _meta.ResetTree(_characterId);
            _hub.Toast($"Древо сброшено, возвращено {UiKit.Number(refund)} осколков");
            Refresh();
        }

        private Sprite Shape(HeroTreeNodeType type, bool ring)
        {
            switch (type)
            {
                case HeroTreeNodeType.PerkModifier:
                    return ring ? _style.SquareRing : _style.Square;
                case HeroTreeNodeType.Keystone:
                    return ring ? _style.DiamondRing : _style.Diamond;
                default:
                    return ring ? _style.CircleRing : _style.Circle;
            }
        }

        private static float Radius(HeroTreeNodeType type)
        {
            switch (type)
            {
                case HeroTreeNodeType.Golden:
                    return 34f;
                case HeroTreeNodeType.Start:
                    return 32f;
                case HeroTreeNodeType.Keystone:
                    return 30f;
                default:
                    return 26f;
            }
        }

        private static Color RingColor(HeroTreeNodeType type)
        {
            switch (type)
            {
                case HeroTreeNodeType.Class:
                    return MetaPalette.Bone;
                case HeroTreeNodeType.PerkModifier:
                    return MetaPalette.Ember;
                case HeroTreeNodeType.Golden:
                    return MetaPalette.Gold;
                default:
                    return MetaPalette.Blood;
            }
        }

        private static Color FillColor(HeroTreeNodeType type)
        {
            switch (type)
            {
                case HeroTreeNodeType.Golden:
                    return new Color(0.23f, 0.16f, 0.06f);
                case HeroTreeNodeType.PerkModifier:
                    return new Color(0.23f, 0.13f, 0.05f);
                default:
                    return new Color(0.16f, 0.08f, 0.09f);
            }
        }

        private static string Glyph(HeroTreeNodeType type)
        {
            switch (type)
            {
                case HeroTreeNodeType.PerkModifier:
                    return "■";
                case HeroTreeNodeType.Keystone:
                    return "◆";
                case HeroTreeNodeType.Golden:
                    return "▲";
                default:
                    return "+";
            }
        }

        private static string TypeName(HeroTreeNodeType type)
        {
            switch (type)
            {
                case HeroTreeNodeType.Start:
                    return "Начало";
                case HeroTreeNodeType.Class:
                    return "Свойство класса";
                case HeroTreeNodeType.PerkModifier:
                    return "Модификатор перка";
                case HeroTreeNodeType.Keystone:
                    return "Ключевой выбор";
                default:
                    return "Золотой узел";
            }
        }
    }
}

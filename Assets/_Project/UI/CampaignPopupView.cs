using Game.Configs;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public sealed class CampaignPopupView : MonoBehaviour
    {
        private MetaUiStyle _style;
        private RectTransform _content;

        public event Action<string> LevelSelected;
        public event Action Closed;

        public void Initialize(MetaUiStyle style)
        {
            _style = style;
            RectTransform root = (RectTransform)transform;
            UiKit.Fill(root);

            Image dim = UiKit.Fill(root, "Dim", null, MetaPalette.WithAlpha(MetaPalette.Void, 0.92f), 0f, true);
            dim.transform.SetAsFirstSibling();

            Image panel = UiKit.Panel(root, "Panel", style);
            UiKit.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1020f, 690f));

            TMP_Text title = UiKit.Text(panel.transform, "Title", style.TitleFont, 38f, MetaPalette.Bone,
                TextAlignmentOptions.Center, "КАМПАНИЯ");
            UiKit.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -52f), new Vector2(700f, 55f));

            Button close = UiKit.Button(panel.transform, "CloseButton", style, "НАЗАД", () => Closed?.Invoke(), 18f);
            UiKit.Place((RectTransform)close.transform, new Vector2(1f, 1f), new Vector2(-105f, -52f), new Vector2(150f, 50f));

            RectTransform viewport = UiKit.Rect("Viewport", panel.transform);
            UiKit.Place(viewport, new Vector2(0.5f, 0.5f), new Vector2(0f, -28f), new Vector2(950f, 530f));
            viewport.gameObject.AddComponent<RectMask2D>();

            _content = UiKit.Rect("Levels", viewport);
            _content.anchorMin = new Vector2(0f, 1f);
            _content.anchorMax = new Vector2(1f, 1f);
            _content.pivot = new Vector2(0.5f, 1f);
            _content.anchoredPosition = Vector2.zero;

            GridLayoutGroup grid = _content.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(430f, 440f);
            grid.spacing = new Vector2(30f, 20f);
            grid.padding = new RectOffset(30, 30, 10, 10);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;

            ContentSizeFitter fitter = _content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.content = _content;
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.scrollSensitivity = 30f;

            gameObject.SetActive(false);
        }

        public void SetLevels(IReadOnlyList<CampaignLevel> levels, IReadOnlyList<CampaignTileState> states)
        {
            for (int index = 0; index < levels.Count; index++)
            {
                CampaignLevel entry = levels[index];
                Image card = UiKit.Card(_content, "Level_" + entry.Level.Id, _style);
                CampaignLevelTileView tile = card.gameObject.AddComponent<CampaignLevelTileView>();
                tile.Build(entry, states[index], _style, () => LevelSelected?.Invoke(entry.Level.Id));
            }
        }

        public void Show() => gameObject.SetActive(true);

        public void Hide() => gameObject.SetActive(false);
    }
}

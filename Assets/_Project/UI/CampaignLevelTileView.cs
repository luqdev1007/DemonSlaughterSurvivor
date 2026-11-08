using Game.Configs;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public enum CampaignTileState
    {
        Locked,
        ComingSoon,
        Ready
    }

    public sealed class CampaignLevelTileView : MonoBehaviour
    {
        public void Build(CampaignLevel entry, CampaignTileState state, MetaUiStyle style, Action onSelected)
        {
            RectTransform root = (RectTransform)transform;
            Image card = gameObject.GetComponent<Image>();
            card.color = state == CampaignTileState.Locked ? new Color(0.55f, 0.55f, 0.55f, 1f) : Color.white;

            Image preview = UiKit.Image(root, "Preview", entry.Preview, entry.Preview == null ? MetaPalette.Ash : Color.white);
            UiKit.Place(preview.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -120f), new Vector2(380f, 190f));
            preview.preserveAspect = entry.Preview != null;

            if (entry.Preview == null)
            {
                TMP_Text placeholder = UiKit.Text(preview.transform, "PreviewFallback", style.BodyFont, 20f,
                    MetaPalette.Dim, TextAlignmentOptions.Center, "ИЗОБРАЖЕНИЕ ПОЗЖЕ");
                UiKit.Fill(placeholder.rectTransform);
            }

            TMP_Text title = UiKit.Text(root, "LevelName", style.TitleFont, 26f, MetaPalette.Bone,
                TextAlignmentOptions.Center, entry.Level.DisplayName);
            UiKit.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -242f), new Vector2(390f, 45f));

            string status = state == CampaignTileState.Ready ? "ДОСТУПЕН"
                : state == CampaignTileState.ComingSoon ? "В РАЗРАБОТКЕ" : "ЗАКРЫТО";
            Color statusColor = state == CampaignTileState.Ready ? MetaPalette.Gold : MetaPalette.Dim;
            TMP_Text stateText = UiKit.Text(root, "Status", style.BodyFont, 20f, statusColor, TextAlignmentOptions.Center, status);
            UiKit.Place(stateText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -285f), new Vector2(390f, 35f));

            if (state == CampaignTileState.Locked && entry.Prerequisites.Count > 0)
            {
                string requirement = "Пройдите: " + entry.Prerequisites[0].DisplayName;
                TMP_Text hint = UiKit.Text(root, "Requirement", style.BodyFont, 17f, MetaPalette.Dim,
                    TextAlignmentOptions.Center, requirement);
                UiKit.Place(hint.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -323f), new Vector2(390f, 32f));
            }

            Button select = UiKit.Button(root, "SelectButton", style,
                state == CampaignTileState.Ready ? "ВЫБРАТЬ" : status, onSelected, 19f);
            UiKit.Place((RectTransform)select.transform, new Vector2(0.5f, 0f), new Vector2(0f, 36f), new Vector2(250f, 52f));
            select.interactable = state == CampaignTileState.Ready;
        }
    }
}

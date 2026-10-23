using Game.Configs;
using Game.Core;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public sealed class MetaGlobalTab
    {
        private const float CardWidth = 580f;
        private const float CardHeight = 170f;

        private readonly RectTransform _page;
        private readonly MetaUiStyle _style;
        private readonly IMetaProgress _meta;
        private readonly IContentRegistry _content;
        private readonly MetaHubView _hub;

        private RectTransform _body;

        public MetaGlobalTab(RectTransform page, MetaHubView hub, IMetaProgress meta, IContentRegistry content)
        {
            _page = page;
            _hub = hub;
            _style = hub.Style;
            _meta = meta;
            _content = content;
        }

        public void Refresh()
        {
            if (_body != null)
                Object.Destroy(_body.gameObject);

            _body = UiKit.Stretch("Global", _page);

            IReadOnlyList<GlobalPowerConfig> configs = _content.All<GlobalPowerConfig>();

            if (configs.Count == 0)
                return;

            GlobalPowerConfig global = configs[0];

            TMP_Text header = UiKit.Text(_body, "Header", _style.TitleFont, 40f, MetaPalette.Bone, TextAlignmentOptions.Left, "Пассивы игрока");
            UiKit.Place(header.rectTransform, new Vector2(0f, 1f), new Vector2(330f, -26f), new Vector2(620f, 52f));

            TMP_Text eyebrow = UiKit.Text(_body, "Eyebrow", _style.BodyFont, 20f, MetaPalette.Faint, TextAlignmentOptions.Left, "ОБЩАЯ СИЛА · ВСЕ ГЕРОИ · ЗА ОСКОЛКИ ВОСПОМИНАНИЙ");
            eyebrow.characterSpacing = 4f;
            UiKit.Place(eyebrow.rectTransform, new Vector2(0f, 1f), new Vector2(330f, -64f), new Vector2(620f, 28f));

            int total = _meta.TotalGlobalRanks;
            int max = global.Passives.Count * global.MaxRank;

            TMP_Text progress = UiKit.Text(_body, "Progress", _style.BodyFont, 22f, MetaPalette.Dim, TextAlignmentOptions.Right,
                $"Изучено рангов: {total} / {max} · Парагон с {global.ParagonUnlockRanks}");
            UiKit.Place(progress.rectTransform, new Vector2(1f, 1f), new Vector2(-280f, -30f), new Vector2(540f, 30f));

            Image bar = UiKit.Image(_body, "Bar", null, MetaPalette.Ash);
            UiKit.Place(bar.rectTransform, new Vector2(1f, 1f), new Vector2(-280f, -60f), new Vector2(540f, 10f));
            Image fill = UiKit.Image(bar.transform, "Fill", null, MetaPalette.Ember);
            float fraction = global.ParagonUnlockRanks <= 0 ? 1f : Mathf.Clamp01(total / (float)global.ParagonUnlockRanks);
            UiKit.Area(fill.rectTransform, Vector2.zero, new Vector2(fraction, 1f));

            for (int index = 0; index < global.Passives.Count; index++)
            {
                MetaPassive passive = global.Passives[index];
                float x = 300f + (index % 3) * (CardWidth + 30f);
                float y = -195f - (index / 3) * (CardHeight + 20f);

                BuildPassive(passive, x, y, global.MaxRank);
            }

            BuildParagon(global);
        }

        private void BuildPassive(MetaPassive passive, float x, float y, int maxRank)
        {
            int rank = _meta.GlobalRank(passive.Id);
            MetaOffer offer = _meta.GlobalRankOffer(passive.Id);

            Image card = UiKit.Card(_body, passive.Id, _style);
            UiKit.Place(card.rectTransform, new Vector2(0f, 1f), new Vector2(x, y), new Vector2(CardWidth, CardHeight));

            TMP_Text name = UiKit.Text(card.transform, "Name", _style.TitleFont, 32f, MetaPalette.Bone, TextAlignmentOptions.Left, passive.DisplayName);
            UiKit.Place(name.rectTransform, new Vector2(0f, 1f), new Vector2(200f, -38f), new Vector2(360f, 44f));

            TMP_Text rankText = UiKit.Text(card.transform, "Rank", _style.BodyFont, 24f, MetaPalette.Dim, TextAlignmentOptions.Right, $"{rank}/{maxRank}  {MetaText.Pips(rank, maxRank)}");
            UiKit.Place(rankText.rectTransform, new Vector2(1f, 1f), new Vector2(-130f, -38f), new Vector2(230f, 40f));

            string now = $"Сейчас <color={MetaPalette.Tag(MetaPalette.Memory)}>+{MetaText.Value(rank * passive.DisplayPerRank)}{passive.DisplayUnit}</color>";

            if (rank < maxRank)
                now += $" → +{MetaText.Value((rank + 1) * passive.DisplayPerRank)}{passive.DisplayUnit}";

            string text = now + "\n<size=80%>" + passive.Description + "</size>";

            if (passive.Effect.NotImplemented)
                text += $"\n<size=80%><color={MetaPalette.Tag(MetaPalette.Ember)}>{MetaText.Soon}</color></size>";

            TMP_Text effect = UiKit.Text(card.transform, "Effect", _style.BodyFont, 24f, MetaPalette.Dim, TextAlignmentOptions.TopLeft, text);
            UiKit.Area(effect.rectTransform, new Vector2(0.05f, 0.08f), new Vector2(0.62f, 0.72f));

            string label = offer.Status == MetaStatus.Maxed ? "Максимум" : "Улучшить  " + MetaText.Cost(offer.Cost, true);
            Button buy = UiKit.Button(card.transform, "Buy", _style, label, () => Buy(passive.Id), 22f);
            UiKit.Place((RectTransform)buy.transform, new Vector2(1f, 0f), new Vector2(-120f, 42f), new Vector2(210f, 60f));
            buy.interactable = offer.CanBuy;
        }

        private void BuildParagon(GlobalPowerConfig global)
        {
            bool open = _meta.IsParagonUnlocked;

            RectTransform block = UiKit.Rect("Paragon", _body);
            UiKit.Place(block, new Vector2(0.5f, 0f), new Vector2(0f, 150f), new Vector2(1830f, 290f));

            Image frame = UiKit.Image(block, "Frame", _style.PanelFrame, Color.white);
            UiKit.Fill(frame.rectTransform);

            TMP_Text title = UiKit.Text(block, "Title", _style.TitleFont, 36f, MetaPalette.Ember, TextAlignmentOptions.Left,
                $"Парагон <size=55%><color={MetaPalette.Tag(MetaPalette.Dim)}>бесконечные ранги, цена растёт на 6% за ранг</color></size>");
            UiKit.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(520f, -40f), new Vector2(980f, 50f));

            for (int index = 0; index < global.Paragon.Count; index++)
            {
                MetaPassive passive = global.Paragon[index];
                int rank = _meta.ParagonRank(passive.Id);
                MetaOffer offer = _meta.ParagonRankOffer(passive.Id);

                Image card = UiKit.Card(block, passive.Id, _style);
                UiKit.Place(card.rectTransform, new Vector2(0f, 0f), new Vector2(250f + index * 445f, 105f), new Vector2(420f, 180f));

                TMP_Text rankText = UiKit.Text(card.transform, "Rank", _style.BodyFont, 46f, MetaPalette.Ember, TextAlignmentOptions.Left, rank.ToString());
                rankText.fontStyle = FontStyles.Bold;
                UiKit.Place(rankText.rectTransform, new Vector2(0f, 1f), new Vector2(70f, -44f), new Vector2(110f, 56f));

                string description = passive.Description + (passive.Effect.NotImplemented ? $"\n<color={MetaPalette.Tag(MetaPalette.Ember)}>{MetaText.Soon}</color>" : string.Empty);
                TMP_Text name = UiKit.Text(card.transform, "Name", _style.BodyFont, 22f, MetaPalette.Bone, TextAlignmentOptions.TopLeft,
                    $"<b>{passive.DisplayName}</b>\n<size=85%><color={MetaPalette.Tag(MetaPalette.Dim)}>{description} · сейчас +{rank}%</color></size>");
                UiKit.Place(name.rectTransform, new Vector2(0f, 1f), new Vector2(265f, -62f), new Vector2(290f, 100f));

                Button buy = UiKit.Button(card.transform, "Buy", _style, $"Ранг {rank + 1}  " + MetaText.Cost(offer.Cost, true), () => BuyParagon(passive.Id), 22f);
                UiKit.Place((RectTransform)buy.transform, new Vector2(0.5f, 0f), new Vector2(0f, 38f), new Vector2(360f, 56f));
                buy.interactable = offer.CanBuy;
            }

            if (open)
                return;

            Image veil = UiKit.Fill(block, "Veil", null, MetaPalette.WithAlpha(MetaPalette.Void, 0.96f), 2f, true);

            Image message = UiKit.Card(veil.transform, "Lock", _style);
            UiKit.Place(message.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620f, 150f));

            int left = Mathf.Max(0, global.ParagonUnlockRanks - _meta.TotalGlobalRanks);
            TMP_Text text = UiKit.Text(message.transform, "Text", _style.BodyFont, 26f, MetaPalette.Dim, TextAlignmentOptions.Center,
                $"<size=130%><color={MetaPalette.Tag(MetaPalette.Bone)}>Парагон закрыт</color></size>\nИзучи ещё {left} рангов общей силы.");
            UiKit.Fill(text.rectTransform, 16f);
        }

        private void Buy(string passiveId)
        {
            int before = _meta.TotalGlobalRanks;
            MetaStatus status = _meta.BuyGlobalRank(passiveId);

            if (status != MetaStatus.Available)
            {
                _hub.Toast(MetaText.Reason(status, true));

                return;
            }

            if (_meta.IsParagonUnlocked && before < _meta.TotalGlobalRanks && _content.All<GlobalPowerConfig>()[0].ParagonUnlockRanks == _meta.TotalGlobalRanks)
                _hub.Toast("Парагон открыт");
        }

        private void BuyParagon(string passiveId)
        {
            MetaStatus status = _meta.BuyParagonRank(passiveId);

            if (status != MetaStatus.Available)
                _hub.Toast(MetaText.Reason(status, true));
        }
    }
}

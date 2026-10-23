using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public static class UiKit
    {
        public static RectTransform Rect(string name, Transform parent)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.layer = parent.gameObject.layer;

            RectTransform rect = (RectTransform)gameObject.transform;
            rect.SetParent(parent, false);

            return rect;
        }

        public static RectTransform Stretch(string name, Transform parent, float inset = 0f)
        {
            RectTransform rect = Rect(name, parent);
            Fill(rect, inset);

            return rect;
        }

        public static void Fill(RectTransform rect, float inset = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }

        public static RectTransform Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            return rect;
        }

        public static RectTransform Area(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            return rect;
        }

        public static Image Image(Transform parent, string name, Sprite sprite, Color color, bool raycast = false)
        {
            RectTransform rect = Rect(name, parent);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = raycast;
            image.preserveAspect = false;

            return image;
        }

        public static Image Fill(Transform parent, string name, Sprite sprite, Color color, float inset = 0f, bool raycast = false)
        {
            Image image = Image(parent, name, sprite, color, raycast);
            Fill(image.rectTransform, inset);

            return image;
        }

        public static TMP_Text Text(Transform parent, string name, TMP_FontAsset font, float size, Color color, TextAlignmentOptions alignment, string value = "")
        {
            RectTransform rect = Rect(name, parent);
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();

            if (font != null)
                text.font = font;

            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.richText = true;
            text.text = value;

            return text;
        }

        public static Button Button(Transform parent, string name, MetaUiStyle style, string label, Action onClick, float fontSize = 26f)
        {
            Image back = Image(parent, name, style.Button, Color.white, true);
            Button button = back.gameObject.AddComponent<Button>();
            button.targetGraphic = back;

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.15f, 1.1f, 1.1f, 1f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.6f);
            colors.colorMultiplier = 1.2f;
            button.colors = colors;

            TMP_Text text = Text(back.transform, "Label", style.BodyFont, fontSize, MetaPalette.Bone, TextAlignmentOptions.Center, label);
            text.fontStyle = FontStyles.Bold;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            Fill(text.rectTransform, 6f);

            if (onClick != null)
                button.onClick.AddListener(() => onClick());

            return button;
        }

        public static void SetLabel(Button button, string label)
        {
            TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);

            if (text != null)
                text.text = label;
        }

        public static Image Panel(Transform parent, string name, MetaUiStyle style, float alpha = 1f)
        {
            Image back = Image(parent, name, style.PanelBack, MetaPalette.WithAlpha(Color.white, alpha), true);
            Image frame = Fill(back.transform, "Frame", style.PanelFrame, Color.white);
            frame.raycastTarget = false;

            return back;
        }

        public static Image Card(Transform parent, string name, MetaUiStyle style)
        {
            Image back = Image(parent, name, style.CardBack, Color.white, true);
            Fill(back.transform, "Frame", style.CardFrame, Color.white);

            return back;
        }

        public static void Clear(Transform parent)
        {
            for (int index = parent.childCount - 1; index >= 0; index--)
                UnityEngine.Object.Destroy(parent.GetChild(index).gameObject);
        }

        public static string Number(float value)
        {
            return Mathf.RoundToInt(value).ToString("#,0", System.Globalization.CultureInfo.InvariantCulture).Replace(',', ' ');
        }

        public static string Time(float seconds)
        {
            int total = Mathf.FloorToInt(seconds);

            return string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}:{1:00}", total / 60, total % 60);
        }
    }
}

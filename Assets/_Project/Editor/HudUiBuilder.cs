using Game.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    public static class HudUiBuilder
    {
        private const string HudPath = "Assets/_Project/Prefabs/UI/Hud.prefab";
        private const string FontPath = "Assets/_Project/Art/Fonts/CormorantSC-Bold SDF.asset";
        private const string Sprites = "Assets/GUI Pro-FantasyRPG/ResourcesData/Sprites/Component/";
        private const string DashIconPath = "Assets/Blink/Art/Icons/Classes/Assassin/Hunter/Hunter1.png";
        private const int PipCount = 5;
        private const int ComboIconCount = 2;

        private static readonly Color CardBack = new Color(0.09f, 0.08f, 0.11f, 0.97f).linear;
        private static readonly Color Gold = new Color(0.95f, 0.80f, 0.45f).linear;
        private static readonly Color Muted = new Color(0.72f, 0.70f, 0.68f).linear;

        private static TMP_FontAsset _titleFont;
        private static TMP_FontAsset _bodyFont;

        [MenuItem("Game/Build Upgrade Choice UI")]
        public static void Build()
        {
            _titleFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            _bodyFont = TMP_Settings.defaultFontAsset;

            GameObject root = PrefabUtility.LoadPrefabContents(HudPath);

            try
            {
                Transform choice = root.transform.Find("UpgradeChoice");
                Transform panel = choice.Find("Panel");
                RectTransform cards = (RectTransform)panel.Find("Cards");

                for (int index = cards.childCount - 1; index >= 0; index--)
                    Object.DestroyImmediate(cards.GetChild(index).gameObject);

                Transform oldTooltip = panel.Find("Tooltip");

                if (oldTooltip != null)
                    Object.DestroyImmediate(oldTooltip.gameObject);

                cards.sizeDelta = new Vector2(1240f, 480f);
                cards.anchoredPosition = new Vector2(0f, -30f);

                HorizontalLayoutGroup layout = cards.GetComponent<HorizontalLayoutGroup>();
                layout.spacing = 40f;
                layout.childAlignment = TextAnchor.MiddleCenter;
                layout.childControlWidth = false;
                layout.childControlHeight = false;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = false;

                PerkCardView[] built = new PerkCardView[UpgradeChoiceView.SlotCount];

                for (int slot = 0; slot < built.Length; slot++)
                    built[slot] = BuildCard(cards, slot);

                PerkTooltipView tooltip = BuildTooltip(panel);

                SerializedObject view = new SerializedObject(choice.GetComponent<UpgradeChoiceView>());
                SerializedProperty cardsProperty = view.FindProperty("_cards");
                cardsProperty.arraySize = built.Length;

                for (int slot = 0; slot < built.Length; slot++)
                    cardsProperty.GetArrayElementAtIndex(slot).objectReferenceValue = built[slot];

                view.FindProperty("_tooltip").objectReferenceValue = tooltip;
                view.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, HudPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            Debug.Log("Upgrade choice UI rebuilt in " + HudPath);
        }

        [MenuItem("Game/Build Ability Panel")]
        public static void BuildAbilityPanel()
        {
            _titleFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            _bodyFont = TMP_Settings.defaultFontAsset;

            GameObject root = PrefabUtility.LoadPrefabContents(HudPath);

            try
            {
                Transform old = root.transform.Find("AbilityPanel");

                if (old != null)
                    Object.DestroyImmediate(old.gameObject);

                RectTransform panel = Rect("AbilityPanel", root.transform, new Vector2(640f, 84f));
                panel.anchorMin = new Vector2(0.5f, 0f);
                panel.anchorMax = new Vector2(0.5f, 0f);
                panel.pivot = new Vector2(0.5f, 0f);
                panel.anchoredPosition = new Vector2(0f, 54f);

                HorizontalLayoutGroup layout = panel.gameObject.AddComponent<HorizontalLayoutGroup>();
                layout.spacing = 10f;
                layout.childAlignment = TextAnchor.LowerCenter;
                layout.childControlWidth = false;
                layout.childControlHeight = false;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = false;

                AbilitySlotView template = BuildAbilitySlot(panel);
                template.gameObject.SetActive(false);

                AbilityPanelView view = panel.gameObject.AddComponent<AbilityPanelView>();
                SerializedObject serialized = new SerializedObject(view);
                serialized.FindProperty("_container").objectReferenceValue = panel;
                serialized.FindProperty("_template").objectReferenceValue = template;
                serialized.FindProperty("_dashIcon").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(DashIconPath);
                serialized.ApplyModifiedPropertiesWithoutUndo();

                SerializedObject hud = new SerializedObject(root.GetComponent<HudView>());
                hud.FindProperty("_abilityPanel").objectReferenceValue = view;
                hud.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, HudPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            Debug.Log("Ability panel rebuilt in " + HudPath);
        }

        [MenuItem("Game/Build Damage Numbers")]
        public static void BuildDamageNumbers()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(HudPath);

            try
            {
                Transform old = root.transform.Find("DamageNumbers");

                if (old != null)
                    Object.DestroyImmediate(old.gameObject);

                RectTransform layer = Stretch("DamageNumbers", root.transform, 0f);
                layer.SetSiblingIndex(0);

                TMP_Text template = Text("Template", layer, TMP_Settings.defaultFontAsset, 34f, Color.white, TextAlignmentOptions.Center);
                template.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                template.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                template.rectTransform.sizeDelta = new Vector2(220f, 60f);
                template.fontStyle = FontStyles.Bold;
                template.textWrappingMode = TextWrappingModes.NoWrap;
                template.outlineWidth = 0.22f;
                template.outlineColor = new Color32(0, 0, 0, 230);
                template.gameObject.SetActive(false);

                DamageNumberLayerView view = layer.gameObject.AddComponent<DamageNumberLayerView>();
                SerializedObject serialized = new SerializedObject(view);
                serialized.FindProperty("_layer").objectReferenceValue = layer;
                serialized.FindProperty("_template").objectReferenceValue = template;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                SerializedObject hud = new SerializedObject(root.GetComponent<HudView>());
                hud.FindProperty("_damageNumbers").objectReferenceValue = view;
                hud.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, HudPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            BuildSettingsRows();

            Debug.Log("Damage numbers layer and settings rows rebuilt.");
        }

        [MenuItem("Game/Build Dps Meter")]
        public static void BuildDpsMeter()
        {
            _titleFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            _bodyFont = TMP_Settings.defaultFontAsset;

            GameObject root = PrefabUtility.LoadPrefabContents(HudPath);

            try
            {
                Transform old = root.transform.Find("Dps");

                if (old != null)
                    Object.DestroyImmediate(old.gameObject);

                RectTransform holder = Rect("Dps", root.transform, new Vector2(220f, 46f));
                holder.anchorMin = Vector2.zero;
                holder.anchorMax = Vector2.zero;
                holder.pivot = Vector2.zero;
                holder.anchoredPosition = new Vector2(40f, 272f);

                RectTransform panel = Stretch("Panel", holder, 0f);
                AddImage(panel.gameObject, "Popup/Popup_02_White_Bg", new Color(0.07f, 0.06f, 0.09f, 0.8f).linear, false);

                TMP_Text label = Text("Label", panel, _titleFont, 24f, Gold, TextAlignmentOptions.MidlineLeft);
                Place(label.rectTransform, new Vector2(0f, 0.5f), new Vector2(54f, 0f), new Vector2(80f, 40f));
                label.SetText("DPS");

                TMP_Text value = Text("Value", panel, _bodyFont, 28f, Color.white, TextAlignmentOptions.MidlineRight);
                Place(value.rectTransform, new Vector2(1f, 0.5f), new Vector2(-70f, 0f), new Vector2(120f, 40f));
                value.fontStyle = FontStyles.Bold;
                value.textWrappingMode = TextWrappingModes.NoWrap;
                value.SetText("0");

                DpsView view = holder.gameObject.AddComponent<DpsView>();
                SerializedObject serialized = new SerializedObject(view);
                serialized.FindProperty("_root").objectReferenceValue = panel.gameObject;
                serialized.FindProperty("_value").objectReferenceValue = value;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                SerializedObject hud = new SerializedObject(root.GetComponent<HudView>());
                hud.FindProperty("_dps").objectReferenceValue = view;
                hud.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, HudPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            Debug.Log("Dps meter rebuilt in " + HudPath);
        }

        private static void BuildSettingsRows()
        {
            const string popupPath = "Assets/_Project/Prefabs/UI/SettingsPopup.prefab";

            GameObject root = PrefabUtility.LoadPrefabContents(popupPath);

            try
            {
                RectTransform panel = (RectTransform)root.transform.Find("Panel");

                foreach (string name in new[] { "DamageNumbersLabel", "DamageNumbersToggle", "DamageNumberSizeLabel", "DamageNumberSizeSlider" })
                {
                    Transform old = panel.Find(name);

                    if (old != null)
                        Object.DestroyImmediate(old.gameObject);
                }

                panel.sizeDelta = new Vector2(900f, 640f);
                Move(panel, "Title", 245f);
                Move(panel, "CloseButton", 270f);
                Move(panel, "VolumeLabel", 125f);
                Move(panel, "VolumeSlider", 125f);
                Move(panel, "MuteLabel", 35f);
                Move(panel, "MuteToggle", 35f);

                Toggle toggle = Clone<Toggle>(panel, "MuteToggle", "DamageNumbersToggle", -55f);
                Clone<TMP_Text>(panel, "MuteLabel", "DamageNumbersLabel", -55f).SetText("Цифры урона");
                Slider slider = Clone<Slider>(panel, "VolumeSlider", "DamageNumberSizeSlider", -145f);
                Clone<TMP_Text>(panel, "VolumeLabel", "DamageNumberSizeLabel", -145f).SetText("Размер цифр");

                SerializedObject view = new SerializedObject(root.GetComponent<SettingsPopupView>());
                view.FindProperty("_damageNumbers").objectReferenceValue = toggle;
                view.FindProperty("_damageNumberSize").objectReferenceValue = slider;
                view.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, popupPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void Move(Transform panel, string name, float y)
        {
            RectTransform rect = (RectTransform)panel.Find(name);
            rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, y);
        }

        private static T Clone<T>(Transform panel, string source, string name, float y) where T : Component
        {
            GameObject copy = Object.Instantiate(panel.Find(source).gameObject, panel);
            copy.name = name;

            RectTransform rect = (RectTransform)copy.transform;
            rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, y);

            return copy.GetComponent<T>();
        }

        private static AbilitySlotView BuildAbilitySlot(Transform parent)
        {
            RectTransform slot = Rect("Slot", parent, new Vector2(76f, 76f));
            AddImage(slot.gameObject, "Frame/ItemFrame_01_Bg", new Color(0.12f, 0.11f, 0.14f, 0.95f).linear, false);

            RectTransform iconRect = Rect("Icon", slot, new Vector2(62f, 62f));
            Image icon = iconRect.gameObject.AddComponent<Image>();
            icon.raycastTarget = false;
            icon.preserveAspect = true;

            RectTransform coolRect = Rect("Cooldown", slot, new Vector2(62f, 62f));
            Image cooldown = coolRect.gameObject.AddComponent<Image>();
            cooldown.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            cooldown.color = new Color(0.02f, 0.02f, 0.04f, 0.85f).linear;
            cooldown.raycastTarget = false;
            cooldown.type = Image.Type.Filled;
            cooldown.fillMethod = Image.FillMethod.Radial360;
            cooldown.fillOrigin = (int)Image.Origin360.Top;
            cooldown.fillClockwise = false;
            cooldown.fillAmount = 0f;

            Image frame = AddImage(Stretch("Frame", slot, 0f).gameObject, "Frame/ItemFrame_01_Border_White", Color.white, false);

            TMP_Text timer = Text("Timer", slot, _bodyFont, 22f, Color.white, TextAlignmentOptions.Center);
            StretchRect(timer.rectTransform, 0f);
            timer.fontStyle = FontStyles.Bold;
            timer.outlineWidth = 0.25f;
            timer.outlineColor = new Color32(0, 0, 0, 255);

            AbilitySlotView view = slot.gameObject.AddComponent<AbilitySlotView>();
            SerializedObject serialized = new SerializedObject(view);
            serialized.FindProperty("_icon").objectReferenceValue = icon;
            serialized.FindProperty("_cooldown").objectReferenceValue = cooldown;
            serialized.FindProperty("_frame").objectReferenceValue = frame;
            serialized.FindProperty("_timer").objectReferenceValue = timer;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return view;
        }

        private static PerkCardView BuildCard(Transform parent, int slot)
        {
            RectTransform card = Rect("Card" + (slot + 1), parent, new Vector2(340f, 460f));
            Image back = AddImage(card.gameObject, "Frame/CardFrame_02_Bg", CardBack, true);

            Button button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = back;
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f).linear;
            button.colors = colors;

            Image glow = AddImage(Stretch("Glow", card, -10f).gameObject, "Frame/BorderFrame_Square02_BgGlow", new Color(1f, 1f, 1f, 0f), false);
            Image border = AddImage(Stretch("Border", card, 0f).gameObject, "Frame/CardFrame_02_Border", Color.white, false);

            TMP_Text hotkey = Text("Hotkey", card, _titleFont, 28f, Gold, TextAlignmentOptions.Center);
            Place(hotkey.rectTransform, new Vector2(0f, 1f), new Vector2(26f, -26f), new Vector2(44f, 44f));

            RectTransform iconSlot = Rect("IconSlot", card, new Vector2(164f, 164f));
            Place(iconSlot, new Vector2(0.5f, 1f), new Vector2(0f, -112f), iconSlot.sizeDelta);
            AddImage(iconSlot.gameObject, "Frame/ItemFrame_01_Bg", new Color(0.16f, 0.15f, 0.18f).linear, false);

            RectTransform iconRect = Rect("Icon", iconSlot, new Vector2(132f, 132f));
            Image icon = iconRect.gameObject.AddComponent<Image>();
            icon.raycastTarget = false;
            icon.preserveAspect = true;

            Image iconBorder = AddImage(Stretch("IconBorder", iconSlot, 0f).gameObject, "Frame/ItemFrame_01_Border_White", Color.white, false);

            RectTransform badge = Rect("RankBadge", iconSlot, new Vector2(50f, 50f));
            Place(badge, new Vector2(1f, 0f), new Vector2(-6f, 6f), badge.sizeDelta);
            AddImage(badge.gameObject, "Frame/BorderFrame_Circle", new Color(0.12f, 0.11f, 0.14f).linear, false);
            TMP_Text rank = Text("Rank", badge, _titleFont, 26f, Gold, TextAlignmentOptions.Center);
            StretchRect(rank.rectTransform, 0f);

            RectTransform pipRow = Rect("Pips", card, new Vector2(240f, 24f));
            Place(pipRow, new Vector2(0.5f, 1f), new Vector2(0f, -220f), pipRow.sizeDelta);
            HorizontalLayoutGroup pipLayout = pipRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            pipLayout.spacing = 8f;
            pipLayout.childAlignment = TextAnchor.MiddleCenter;
            pipLayout.childControlWidth = false;
            pipLayout.childControlHeight = false;
            pipLayout.childForceExpandWidth = false;
            pipLayout.childForceExpandHeight = false;

            Image[] pips = new Image[PipCount];

            for (int index = 0; index < PipCount; index++)
            {
                RectTransform pip = Rect("Pip" + (index + 1), pipRow, new Vector2(22f, 22f));
                pips[index] = AddImage(pip.gameObject, "Frame/BasicFrame_Diamond_80_Bg", Color.white, false);
            }

            TMP_Text title = Text("Title", card, _titleFont, 32f, Color.white, TextAlignmentOptions.Center);
            Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -278f), new Vector2(300f, 76f));

            TMP_Text rarity = Text("Rarity", card, _bodyFont, 21f, Color.white, TextAlignmentOptions.Center);
            Place(rarity.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -334f), new Vector2(300f, 30f));

            RectTransform combo = Rect("Combo", card, new Vector2(300f, 70f));
            Place(combo, new Vector2(0.5f, 0f), new Vector2(0f, 56f), combo.sizeDelta);
            TMP_Text comboLabel = Text("Label", combo, _titleFont, 22f, Gold, TextAlignmentOptions.MidlineLeft);
            Place(comboLabel.rectTransform, new Vector2(0f, 0.5f), new Vector2(60f, 0f), new Vector2(120f, 40f));
            comboLabel.SetText("КОМБО");

            Image[] comboIcons = new Image[ComboIconCount];

            for (int index = 0; index < ComboIconCount; index++)
            {
                RectTransform frame = Rect("Parent" + (index + 1), combo, new Vector2(56f, 56f));
                Place(frame, new Vector2(1f, 0.5f), new Vector2(-38f - index * 64f, 0f), frame.sizeDelta);
                AddImage(frame.gameObject, "Frame/ItemFrame_01_Bg", new Color(0.16f, 0.15f, 0.18f).linear, false);
                RectTransform inner = Rect("Icon", frame, new Vector2(44f, 44f));
                comboIcons[index] = inner.gameObject.AddComponent<Image>();
                comboIcons[index].raycastTarget = false;
                comboIcons[index].preserveAspect = true;
            }

            PerkCardView view = card.gameObject.AddComponent<PerkCardView>();
            SerializedObject serialized = new SerializedObject(view);
            serialized.FindProperty("_button").objectReferenceValue = button;
            serialized.FindProperty("_border").objectReferenceValue = border;
            serialized.FindProperty("_glow").objectReferenceValue = glow;
            serialized.FindProperty("_icon").objectReferenceValue = icon;
            serialized.FindProperty("_iconBorder").objectReferenceValue = iconBorder;
            serialized.FindProperty("_hotkey").objectReferenceValue = hotkey;
            serialized.FindProperty("_title").objectReferenceValue = title;
            serialized.FindProperty("_rarity").objectReferenceValue = rarity;
            serialized.FindProperty("_rank").objectReferenceValue = rank;
            serialized.FindProperty("_combo").objectReferenceValue = combo.gameObject;
            AssignArray(serialized.FindProperty("_pips"), pips);
            AssignArray(serialized.FindProperty("_comboIcons"), comboIcons);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return view;
        }

        private static PerkTooltipView BuildTooltip(Transform panel)
        {
            RectTransform tooltip = Rect("Tooltip", panel, new Vector2(440f, 200f));
            tooltip.anchorMin = new Vector2(0.5f, 0.5f);
            tooltip.anchorMax = new Vector2(0.5f, 0.5f);
            AddImage(tooltip.gameObject, "Popup/Popup_02_White_Bg", new Color(0.07f, 0.06f, 0.09f, 0.97f).linear, false);

            CanvasGroup group = tooltip.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;

            VerticalLayoutGroup layout = tooltip.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(30, 30, 26, 28);
            layout.spacing = 10f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = tooltip.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            RectTransform borderRect = Stretch("Border", tooltip, 0f);
            borderRect.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            Image border = AddImage(borderRect.gameObject, "Popup/Popup_02_White_Border", Color.white, false);

            TMP_Text title = Text("Title", tooltip, _titleFont, 34f, Color.white, TextAlignmentOptions.Left);
            TMP_Text subtitle = Text("Subtitle", tooltip, _bodyFont, 19f, Muted, TextAlignmentOptions.Left);
            TMP_Text description = Text("Description", tooltip, _bodyFont, 22f, Color.white, TextAlignmentOptions.Left);
            TMP_Text stats = Text("Stats", tooltip, _bodyFont, 22f, new Color(0.55f, 0.92f, 0.55f).linear, TextAlignmentOptions.Left);
            TMP_Text requires = Text("Requires", tooltip, _bodyFont, 20f, Gold, TextAlignmentOptions.Left);

            PerkTooltipView view = tooltip.gameObject.AddComponent<PerkTooltipView>();
            SerializedObject serialized = new SerializedObject(view);
            serialized.FindProperty("_panel").objectReferenceValue = tooltip;
            serialized.FindProperty("_group").objectReferenceValue = group;
            serialized.FindProperty("_border").objectReferenceValue = border;
            serialized.FindProperty("_title").objectReferenceValue = title;
            serialized.FindProperty("_subtitle").objectReferenceValue = subtitle;
            serialized.FindProperty("_description").objectReferenceValue = description;
            serialized.FindProperty("_stats").objectReferenceValue = stats;
            serialized.FindProperty("_requires").objectReferenceValue = requires;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            tooltip.gameObject.SetActive(false);

            return view;
        }

        private static RectTransform Rect(string name, Transform parent, Vector2 size)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.layer = parent.gameObject.layer;

            RectTransform rect = (RectTransform)gameObject.transform;
            rect.SetParent(parent, false);
            rect.sizeDelta = size;

            return rect;
        }

        private static RectTransform Stretch(string name, Transform parent, float inset)
        {
            RectTransform rect = Rect(name, parent, Vector2.zero);
            StretchRect(rect, inset);

            return rect;
        }

        private static void StretchRect(RectTransform rect, float inset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }

        private static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static Image AddImage(GameObject target, string sprite, Color color, bool raycast)
        {
            Image image = target.AddComponent<Image>();
            image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Sprites + sprite + ".png");
            image.color = color;
            image.raycastTarget = raycast;

            if (image.sprite != null && image.sprite.border != Vector4.zero)
                image.type = Image.Type.Sliced;

            if (image.sprite == null)
                Debug.LogWarning("Upgrade choice UI: missing sprite " + sprite);

            return image;
        }

        private static TMP_Text Text(string name, Transform parent, TMP_FontAsset font, float size, Color color, TextAlignmentOptions alignment)
        {
            RectTransform rect = Rect(name, parent, new Vector2(300f, 40f));
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();

            if (font != null)
                text.font = font;

            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.Normal;

            return text;
        }

        private static void AssignArray(SerializedProperty property, Object[] values)
        {
            property.arraySize = values.Length;

            for (int index = 0; index < values.Length; index++)
                property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
        }
    }
}

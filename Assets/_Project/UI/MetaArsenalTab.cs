using Game.Configs;
using Game.Core;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public sealed class MetaArsenalTab
    {
        private const float StageWidth = 560f;
        private const float StageHeight = 690f;
        private const float ArtScale = 1.12f;
        private const int CollectionSlots = 4;

        private readonly RectTransform _page;
        private readonly MetaHubView _hub;
        private readonly MetaUiStyle _style;
        private readonly IMetaProgress _meta;
        private readonly IContentRegistry _content;

        private RectTransform _body;
        private string _characterId;
        private string _selectedSocket;

        public MetaArsenalTab(RectTransform page, MetaHubView hub, IMetaProgress meta, IContentRegistry content)
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
            _selectedSocket = null;
        }

        public void Refresh()
        {
            if (_body != null)
                Object.Destroy(_body.gameObject);

            _body = UiKit.Stretch("Arsenal", _page);

            List<UniqueWeaponConfig> weapons = Weapons();

            if (weapons.Count == 0)
            {
                TMP_Text empty = UiKit.Text(_body, "Empty", _style.BodyFont, 30f, MetaPalette.Dim, TextAlignmentOptions.Center,
                    "У этого героя пока нет уникального оружия.");
                UiKit.Fill(empty.rectTransform);

                return;
            }

            string equipped = _meta.EquippedWeapon(_characterId);
            UniqueWeaponConfig weapon = weapons.Find(candidate => candidate.Id == equipped) ?? weapons[0];

            if (_selectedSocket == null || weapon.FindSocket(_selectedSocket) == null)
                _selectedSocket = weapon.Sockets.Count > 0 ? weapon.Sockets[0].Id : null;

            BuildPicker(weapons, weapon);
            BuildStage(weapon);
            BuildPanel(weapon);
        }

        private List<UniqueWeaponConfig> Weapons()
        {
            List<UniqueWeaponConfig> weapons = new List<UniqueWeaponConfig>();

            foreach (UniqueWeaponConfig weapon in _content.All<UniqueWeaponConfig>())
            {
                if (weapon.Character != null && weapon.Character.Id == _characterId)
                    weapons.Add(weapon);
            }

            weapons.Sort((left, right) => left.MenuOrder.CompareTo(right.MenuOrder));

            return weapons;
        }

        private void BuildPicker(List<UniqueWeaponConfig> weapons, UniqueWeaponConfig current)
        {
            for (int index = 0; index < weapons.Count; index++)
            {
                UniqueWeaponConfig weapon = weapons[index];
                bool forged = _meta.IsForged(weapon.Id);
                string subtitle = weapon.ComingLater ? weapon.Subtitle : forged ? weapon.Subtitle : "Выковать за " + MetaText.Cost(weapon.ForgeCost, false);
                string label = $"<size=120%>{weapon.DisplayName}</size>\n<size=80%><color={MetaPalette.Tag(MetaPalette.Dim)}>{subtitle}</color></size>";

                string id = weapon.Id;
                Button button = UiKit.Button(_body, id, _style, label, () => Pick(id), 19f);
                UiKit.Place((RectTransform)button.transform, new Vector2(0f, 1f), new Vector2(100f + index * 188f, -50f), new Vector2(180f, 92f));
                button.GetComponent<Image>().sprite = weapon.Id == current.Id ? _style.Button : _style.ButtonOff;
                button.interactable = weapon.ComingLater == false;
                button.GetComponentInChildren<TMP_Text>().textWrappingMode = TextWrappingModes.Normal;
            }
        }

        private void BuildStage(UniqueWeaponConfig weapon)
        {
            int level = _meta.WeaponLevel(weapon.Id);

            Image stage = UiKit.Panel(_body, "Stage", _style);
            UiKit.Place(stage.rectTransform, new Vector2(0f, 1f), new Vector2(StageWidth * 0.5f + 10f, -110f - StageHeight * 0.5f), new Vector2(StageWidth, StageHeight));

            Image glow = UiKit.Image(stage.transform, "Glow", _style.SoftGlow, MetaPalette.WithAlpha(MetaPalette.Blood, 0.25f));
            UiKit.Place(glow.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520f, 620f));

            TMP_Text levelText = UiKit.Text(stage.transform, "Level", _style.BodyFont, 22f, MetaPalette.Dim, TextAlignmentOptions.TopLeft,
                $"Уровень оружия\n<size=160%><color={MetaPalette.Tag(MetaPalette.Bone)}>{Mathf.Max(level, 1)} / {weapon.MaxLevel}</color></size>");
            UiKit.Place(levelText.rectTransform, new Vector2(0f, 1f), new Vector2(130f, -70f), new Vector2(220f, 90f));

            RectTransform art = UiKit.Rect("Art", stage.transform);
            UiKit.Place(art, new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(200f * ArtScale, 560f * ArtScale));

            Sprite sprite = weapon.Art != null ? weapon.Art : weapon.Id.Contains("axe") ? _style.AxeArt : _style.BladeArt;
            Image artImage = UiKit.Image(art, "Weapon", sprite, Color.white);
            artImage.preserveAspect = true;
            UiKit.Fill(artImage.rectTransform);

            foreach (WeaponSocket socket in weapon.Sockets)
                BuildSocket(art, weapon, socket, level);
        }

        private void BuildSocket(RectTransform art, UniqueWeaponConfig weapon, WeaponSocket socket, int level)
        {
            bool locked = level < socket.UnlockLevel;
            bool selected = socket.Id == _selectedSocket;
            float size = (socket.Kind == SocketKind.Relic ? 54f : 40f) * ArtScale;
            Vector2 position = new Vector2((socket.Position.x - 100f) * ArtScale, (280f - socket.Position.y) * ArtScale);

            RectTransform root = UiKit.Rect("Socket_" + socket.Id, art);
            UiKit.Place(root, new Vector2(0.5f, 0.5f), position, new Vector2(size, size));

            if (selected)
            {
                Image halo = UiKit.Image(root, "Halo", _style.SoftGlow, MetaPalette.WithAlpha(MetaPalette.Ember, 0.7f));
                UiKit.Place(halo.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size * 2.2f, size * 2.2f));
            }

            Image back = UiKit.Image(root, "Back", _style.Circle, MetaPalette.WithAlpha(MetaPalette.Void, locked ? 0.6f : 1f), true);
            UiKit.Fill(back.rectTransform);

            Image ring = UiKit.Image(root, "Ring", _style.CircleRing, MetaPalette.WithAlpha(SocketColor(socket.Kind), locked ? 0.35f : 1f));
            UiKit.Fill(ring.rectTransform, selected ? -4f : 0f);

            Button button = back.gameObject.AddComponent<Button>();
            button.targetGraphic = back;
            string id = socket.Id;
            button.onClick.AddListener(() => SelectSocket(id));

            if (locked)
            {
                TMP_Text text = UiKit.Text(root, "Lock", _style.BodyFont, 16f, MetaPalette.Dim, TextAlignmentOptions.Center, "ур." + socket.UnlockLevel);
                UiKit.Fill(text.rectTransform);

                return;
            }

            string content = _meta.SocketContent(weapon.Id, socket.Id);
            Sprite icon = ContentIcon(content);

            if (icon == null)
                return;

            Image inner = UiKit.Image(root, "Content", icon, Color.white);
            inner.preserveAspect = true;
            UiKit.Fill(inner.rectTransform, size * 0.14f);
        }

        private void BuildPanel(UniqueWeaponConfig weapon)
        {
            Image panel = UiKit.Panel(_body, "Panel", _style);
            panel.rectTransform.anchorMin = new Vector2(0f, 0f);
            panel.rectTransform.anchorMax = new Vector2(1f, 1f);
            panel.rectTransform.offsetMin = new Vector2(StageWidth + 40f, 0f);
            panel.rectTransform.offsetMax = Vector2.zero;

            RectTransform left = UiKit.Rect("Left", panel.transform);
            UiKit.Area(left, new Vector2(0.03f, 0.04f), new Vector2(0.49f, 0.96f));
            RectTransform right = UiKit.Rect("Right", panel.transform);
            UiKit.Area(right, new Vector2(0.52f, 0.04f), new Vector2(0.97f, 0.96f));

            Column(left);
            Column(right);

            BuildWeaponInfo(left, weapon);
            BuildSlot(right, weapon);
            BuildRelic(right, weapon);
            BuildCollection(right);
        }

        private void BuildWeaponInfo(RectTransform column, UniqueWeaponConfig weapon)
        {
            int level = Mathf.Max(1, _meta.WeaponLevel(weapon.Id));
            bool forged = _meta.IsForged(weapon.Id);

            Line(column, $"<color={MetaPalette.Tag(MetaPalette.Faint)}>УНИКАЛЬНОЕ ОРУЖИЕ</color>", 20f);
            Line(column, weapon.DisplayName, 40f, _style.TitleFont, MetaPalette.Bone);

            CharacterConfig character = weapon.Character;
            float swordDamage = character != null && character.StartingWeapon != null ? character.StartingWeapon.Damage : 0f;
            float levelBonus = weapon.LevelDamageBonus(level);
            float treeBonus = TreeWeaponDamageBonus();
            float damage = swordDamage * weapon.DamageScale * (1f + levelBonus + treeBonus);

            Line(column, Stat("Урон оружия", damage.ToString("0.#", CultureInfo.InvariantCulture)), 24f);
            Line(column, $"<color={MetaPalette.Tag(MetaPalette.Faint)}><size=85%>{swordDamage:0.#} (меч героя) × {weapon.DamageScale:0.##} × (1 + {levelBonus:0.##} уровень + {treeBonus:0.##} древо)</size></color>", 20f);
            Line(column, Stat("Скорость атаки", "×" + weapon.AttackSpeedScale.ToString("0.##", CultureInfo.InvariantCulture)), 24f);
            Line(column, Stat("Бонус уровня", $"+{Mathf.RoundToInt(levelBonus * 100f)}%"), 24f);
            Line(column, Stat("Урон оружия из древа", $"+{Mathf.RoundToInt(treeBonus * 100f)}%"), 24f);

            if (forged)
            {
                MetaOffer offer = _meta.WeaponUpgradeOffer(weapon.Id);
                string label = offer.Status == MetaStatus.Maxed ? "Максимальный уровень" : $"Улучшить до ур. {level + 1}  " + MetaText.Cost(offer.Cost, false);
                Button upgrade = UiKit.Button(column, "Upgrade", _style, label, () => Upgrade(weapon), 24f);
                upgrade.gameObject.AddComponent<LayoutElement>().preferredHeight = 68f;
                upgrade.interactable = offer.CanBuy;
            }

            foreach (WeaponTrait trait in weapon.Traits)
            {
                bool on = level >= trait.UnlockLevel;
                string soon = trait.Effect.NotImplemented ? $" <color={MetaPalette.Tag(MetaPalette.Ember)}><size=80%>(скоро)</size></color>" : string.Empty;
                string badge = $"<color={MetaPalette.Tag(on ? MetaPalette.Ember : MetaPalette.Faint)}>ур.{trait.UnlockLevel}</color>";
                Color color = on ? MetaPalette.Bone : MetaPalette.Faint;
                Line(column, $"{badge}  <b>{trait.DisplayName}.</b>{soon} <size=90%>{trait.Description}</size>", 22f, null, color);
            }
        }

        private void BuildSlot(RectTransform column, UniqueWeaponConfig weapon)
        {
            WeaponSocket socket = _selectedSocket == null ? null : weapon.FindSocket(_selectedSocket);

            if (socket == null)
                return;

            Line(column, $"<color={MetaPalette.Tag(MetaPalette.Faint)}>ВЫБРАНО: {SocketName(socket.Kind).ToUpperInvariant()}</color>", 20f);

            if (_meta.IsForged(weapon.Id) == false)
            {
                Line(column, "Сначала выкуй оружие.", 22f, null, MetaPalette.Dim);

                return;
            }

            if (_meta.WeaponLevel(weapon.Id) < socket.UnlockLevel)
            {
                Line(column, $"Откроется на {socket.UnlockLevel} уровне оружия.", 22f, null, MetaPalette.Dim);

                return;
            }

            string current = _meta.SocketContent(weapon.Id, socket.Id);

            if (socket.Kind == SocketKind.Relic)
            {
                BuildRelicChoices(column, weapon, socket, current);

                return;
            }

            if (string.IsNullOrEmpty(current) == false && _content.TryGet(current, out SocketGemConfig inserted))
            {
                Button remove = UiKit.Button(column, "Remove", _style, $"Извлечь «{inserted.DisplayName}»", () => Clear(weapon, socket), 22f);
                remove.gameObject.AddComponent<LayoutElement>().preferredHeight = 56f;
            }

            List<SocketGemConfig> gems = new List<SocketGemConfig>(_content.All<SocketGemConfig>());
            gems.Sort((left, right) => left.MenuOrder.CompareTo(right.MenuOrder));

            foreach (SocketGemConfig gem in gems)
            {
                if (socket.Accepts(gem.Color) == false)
                    continue;

                int count = _meta.GemCount(gem.Id);
                string label = $"<b>{gem.DisplayName}</b>  <color={MetaPalette.Tag(MetaPalette.Dim)}>×{count}</color>\n<size=80%><color={MetaPalette.Tag(MetaPalette.Dim)}>{gem.Description}</color></size>";

                if (gem.Effect.NotImplemented)
                    label += $" <size=75%><color={MetaPalette.Tag(MetaPalette.Ember)}>(скоро)</color></size>";

                string id = gem.Id;
                Button pick = Pick(column, label, GemIcon(gem.Color), () => Insert(weapon, socket, id));
                pick.interactable = count > 0;
            }

            if (socket.Kind != SocketKind.Prismatic)
                Line(column, $"Сюда подходят только {ColorName(socket.Kind)} самоцветы; остальные встают в призматическое гнездо.", 20f, null, MetaPalette.Faint);
        }

        private void BuildRelicChoices(RectTransform column, UniqueWeaponConfig weapon, WeaponSocket socket, string current)
        {
            bool any = false;

            foreach (RelicConfig relic in _content.All<RelicConfig>())
            {
                if (_meta.OwnsRelic(relic.Id) == false)
                    continue;

                any = true;
                bool here = current == relic.Id;
                string where = string.Empty;

                if (here == false && _meta.TryGetRelicPlacement(relic.Id, out string otherWeapon, out string _) && _content.TryGet(otherWeapon, out UniqueWeaponConfig other))
                    where = $"\n<size=80%><color={MetaPalette.Tag(MetaPalette.Ember)}>сейчас в «{other.DisplayName}» — вставка перенесёт</color></size>";

                string label = $"<b>{relic.DisplayName}</b>  <color={MetaPalette.Tag(MetaPalette.Dim)}>{(here ? "Извлечь" : "Вставить")}</color>\n<size=80%><color={MetaPalette.Tag(MetaPalette.Dim)}>{relic.BossName} · {relic.Echo}</color></size>{where}";
                string id = relic.Id;

                Pick(column, label, _style.RelicIcon, () =>
                {
                    if (here)
                        Clear(weapon, socket);
                    else
                        InsertRelic(weapon, socket, id);
                });
            }

            if (any == false)
                Line(column, "Коллекция пуста. Победи босса уровня и забери реликвию на экране итогов.", 22f, null, MetaPalette.Dim);
        }

        private void BuildRelic(RectTransform column, UniqueWeaponConfig weapon)
        {
            RelicConfig shown = null;
            string inSocket = null;

            foreach (WeaponSocket socket in weapon.Sockets)
            {
                if (socket.Kind == SocketKind.Relic)
                    inSocket = _meta.SocketContent(weapon.Id, socket.Id);
            }

            foreach (RelicConfig relic in _content.All<RelicConfig>())
            {
                if (_meta.OwnsRelic(relic.Id) && (shown == null || inSocket == relic.Id))
                    shown = relic;
            }

            if (shown == null)
                return;

            int level = _meta.RelicLevel(shown.Id);
            bool inserted = _meta.TryGetRelicPlacement(shown.Id, out string placedWeapon, out string _) && placedWeapon == weapon.Id;

            Line(column, $"<color={MetaPalette.Tag(MetaPalette.Gold)}><size=120%>{shown.DisplayName}</size></color>   <color={MetaPalette.Tag(MetaPalette.Gold)}>ур. {level}/{shown.MaxLevel}</color>", 24f, _style.TitleFont);
            Line(column, shown.Description, 20f, null, MetaPalette.Dim);

            List<string> tags = new List<string>();

            foreach (string tag in shown.Tags)
            {
                bool hot = HasTagBoost(weapon, tag);
                tags.Add($"<color={MetaPalette.Tag(hot ? MetaPalette.Ember : MetaPalette.Dim)}>[{TagName(tag)}{(hot ? " +" : string.Empty)}]</color>");
            }

            Line(column, string.Join("  ", tags) + (inserted ? "   Вставлена в это оружие." : "   Не вставлена: эффект не работает."), 20f, null, MetaPalette.Dim);

            if (shown.Effect.NotImplemented)
                Line(column, $"<color={MetaPalette.Tag(MetaPalette.Ember)}>Скоро: боевой эффект реликвии ещё в разработке.</color>", 20f);

            MetaOffer offer = _meta.RelicUpgradeOffer(shown.Id);
            string label = offer.Status == MetaStatus.Maxed ? "Реликвия максимальна" : "Улучшить реликвию  " + MetaText.Cost(offer.Cost, false);
            string id = shown.Id;
            Button upgrade = UiKit.Button(column, "RelicUp", _style, label, () => UpgradeRelic(id), 22f);
            upgrade.gameObject.AddComponent<LayoutElement>().preferredHeight = 56f;
            upgrade.interactable = offer.CanBuy;
        }

        private void BuildCollection(RectTransform column)
        {
            Line(column, $"<color={MetaPalette.Tag(MetaPalette.Faint)}>КОЛЛЕКЦИЯ РЕЛИКВИЙ</color>", 20f);

            RectTransform row = UiKit.Rect("Collection", column);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 110f;
            HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 12f;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childAlignment = TextAnchor.MiddleLeft;

            List<RelicConfig> relics = new List<RelicConfig>(_content.All<RelicConfig>());
            relics.Sort((left, right) => left.MenuOrder.CompareTo(right.MenuOrder));

            for (int index = 0; index < Mathf.Max(CollectionSlots, relics.Count); index++)
            {
                RelicConfig relic = index < relics.Count ? relics[index] : null;
                bool owned = relic != null && _meta.OwnsRelic(relic.Id);

                Image slot = UiKit.Card(row, "Slot", _style);
                slot.rectTransform.sizeDelta = new Vector2(104f, 104f);

                Image icon = UiKit.Image(slot.transform, "Icon", owned ? _style.RelicIcon : _style.RelicOffIcon, Color.white);
                icon.preserveAspect = true;
                UiKit.Area(icon.rectTransform, new Vector2(0.2f, 0.28f), new Vector2(0.8f, 0.9f));

                string caption = owned ? relic.BossName : "?";
                TMP_Text label = UiKit.Text(slot.transform, "Caption", _style.BodyFont, 15f, owned ? MetaPalette.Gold : MetaPalette.Faint, TextAlignmentOptions.Center, caption);
                UiKit.Area(label.rectTransform, new Vector2(0f, 0.02f), new Vector2(1f, 0.28f));
            }
        }

        private bool HasTagBoost(UniqueWeaponConfig weapon, string tag)
        {
            string key = TuningEntry.TagPrefix + tag;

            foreach (WeaponSocket socket in weapon.Sockets)
            {
                string content = _meta.SocketContent(weapon.Id, socket.Id);

                if (string.IsNullOrEmpty(content) || _content.TryGet(content, out SocketGemConfig gem) == false)
                    continue;

                foreach (TuningEntry entry in gem.Effect.Tuning)
                {
                    if (entry.Key == key)
                        return true;
                }
            }

            return false;
        }

        private float TreeWeaponDamageBonus()
        {
            float bonus = 0f;

            foreach (HeroTreeConfig tree in _content.All<HeroTreeConfig>())
            {
                if (tree.Character == null || tree.Character.Id != _characterId)
                    continue;

                foreach (HeroTreeNode node in tree.Nodes)
                {
                    int rank = _meta.NodeRank(_characterId, node.Id);

                    if (rank <= 0 || node.Effect.NotImplemented)
                        continue;

                    foreach (StatModifierEntry entry in node.Effect.Modifiers)
                    {
                        if (entry != null && entry.Stat == StatId.WeaponDamage && entry.Op == StatOp.Increased)
                            bonus += entry.Value * rank;
                    }
                }
            }

            return bonus;
        }

        private Button Pick(RectTransform column, string label, Sprite icon, System.Action onClick)
        {
            Image back = UiKit.Image(column, "Pick", _style.CardBack, Color.white, true);
            back.gameObject.AddComponent<LayoutElement>().preferredHeight = 78f;

            Button button = back.gameObject.AddComponent<Button>();
            button.targetGraphic = back;
            button.onClick.AddListener(() => onClick());

            Image iconImage = UiKit.Image(back.transform, "Icon", icon, Color.white);
            iconImage.preserveAspect = true;
            UiKit.Place(iconImage.rectTransform, new Vector2(0f, 0.5f), new Vector2(38f, 0f), new Vector2(52f, 52f));

            TMP_Text text = UiKit.Text(back.transform, "Text", _style.BodyFont, 22f, MetaPalette.Bone, TextAlignmentOptions.Left, label);
            UiKit.Area(text.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f));
            text.rectTransform.offsetMin = new Vector2(76f, 4f);
            text.rectTransform.offsetMax = new Vector2(-10f, -4f);

            return button;
        }

        private void Line(RectTransform parent, string text, float size, TMP_FontAsset font = null, Color? color = null)
        {
            TMP_Text line = UiKit.Text(parent, "Line", font ?? _style.BodyFont, size, color ?? MetaPalette.Bone, TextAlignmentOptions.TopLeft, text);
            line.overflowMode = TextOverflowModes.Overflow;
        }

        private static string Stat(string label, string value)
        {
            return $"<color={MetaPalette.Tag(MetaPalette.Dim)}>{label}</color>  <b>{value}</b>";
        }

        private static void Column(RectTransform rect)
        {
            VerticalLayoutGroup column = rect.gameObject.AddComponent<VerticalLayoutGroup>();
            column.spacing = 10f;
            column.childControlHeight = true;
            column.childControlWidth = true;
            column.childForceExpandHeight = false;
            column.childAlignment = TextAnchor.UpperLeft;
        }

        private Sprite ContentIcon(string content)
        {
            if (string.IsNullOrEmpty(content))
                return null;

            if (_content.TryGet(content, out SocketGemConfig gem))
                return gem.Icon != null ? gem.Icon : GemIcon(gem.Color);

            if (_content.TryGet(content, out RelicConfig relic))
                return relic.Icon != null ? relic.Icon : _style.RelicIcon;

            return null;
        }

        private Sprite GemIcon(GemColor color)
        {
            switch (color)
            {
                case GemColor.Red:
                    return _style.RedGem;
                case GemColor.Blue:
                    return _style.BlueGem;
                default:
                    return _style.GreenGem;
            }
        }

        private static Color SocketColor(SocketKind kind)
        {
            switch (kind)
            {
                case SocketKind.Relic:
                    return MetaPalette.Gold;
                case SocketKind.Red:
                    return MetaPalette.GemRed;
                case SocketKind.Blue:
                    return MetaPalette.GemBlue;
                case SocketKind.Green:
                    return MetaPalette.GemGreen;
                default:
                    return MetaPalette.Bone;
            }
        }

        private static string SocketName(SocketKind kind)
        {
            switch (kind)
            {
                case SocketKind.Relic:
                    return "Гнездо реликвии";
                case SocketKind.Red:
                    return "Красное гнездо";
                case SocketKind.Blue:
                    return "Синее гнездо";
                case SocketKind.Green:
                    return "Зелёное гнездо";
                default:
                    return "Призматическое гнездо";
            }
        }

        private static string ColorName(SocketKind kind)
        {
            switch (kind)
            {
                case SocketKind.Red:
                    return "красные";
                case SocketKind.Blue:
                    return "синие";
                default:
                    return "зелёные";
            }
        }

        private static string TagName(string tag)
        {
            switch (tag)
            {
                case "Charge":
                    return "Натиск";
                case "Physical":
                    return "Физика";
                default:
                    return tag;
            }
        }

        private void Pick(string weaponId)
        {
            bool forged = _meta.IsForged(weaponId);
            MetaStatus status = _meta.ForgeOrEquip(_characterId, weaponId);

            if (status != MetaStatus.Available)
            {
                _hub.Toast(MetaText.Reason(status, false));

                return;
            }

            _selectedSocket = null;

            if (forged == false && _content.TryGet(weaponId, out UniqueWeaponConfig weapon))
                _hub.Toast($"«{weapon.DisplayName}» выкован");
        }

        private void Upgrade(UniqueWeaponConfig weapon)
        {
            MetaStatus status = _meta.UpgradeWeapon(weapon.Id);

            if (status != MetaStatus.Available)
            {
                _hub.Toast(MetaText.Reason(status, false));

                return;
            }

            int level = _meta.WeaponLevel(weapon.Id);

            foreach (WeaponTrait trait in weapon.Traits)
            {
                if (trait.UnlockLevel == level)
                    _hub.Toast($"Открыто свойство «{trait.DisplayName}»");
            }

            foreach (WeaponSocket socket in weapon.Sockets)
            {
                if (socket.UnlockLevel == level)
                    _hub.Toast("Открыто новое гнездо");
            }
        }

        private void SelectSocket(string socketId)
        {
            _selectedSocket = socketId;
            Refresh();
        }

        private void Insert(UniqueWeaponConfig weapon, WeaponSocket socket, string gemId)
        {
            MetaStatus status = _meta.InsertGem(weapon.Id, socket.Id, gemId);

            if (status != MetaStatus.Available)
                _hub.Toast(MetaText.Reason(status, false));
        }

        private void InsertRelic(UniqueWeaponConfig weapon, WeaponSocket socket, string relicId)
        {
            string previous = _meta.TryGetRelicPlacement(relicId, out string otherWeapon, out string _) ? otherWeapon : null;
            MetaStatus status = _meta.InsertRelic(weapon.Id, socket.Id, relicId);

            if (status != MetaStatus.Available)
            {
                _hub.Toast(MetaText.Reason(status, false));

                return;
            }

            if (previous != null && previous != weapon.Id && _content.TryGet(previous, out UniqueWeaponConfig other))
                _hub.Toast($"Реликвия перенесена из «{other.DisplayName}»");
        }

        private void Clear(UniqueWeaponConfig weapon, WeaponSocket socket)
        {
            _meta.ClearSocket(weapon.Id, socket.Id);
        }

        private void UpgradeRelic(string relicId)
        {
            MetaStatus status = _meta.UpgradeRelic(relicId);

            if (status != MetaStatus.Available)
                _hub.Toast(MetaText.Reason(status, false));
        }
    }
}

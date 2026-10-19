using DG.Tweening;
using Game.Configs;
using Game.Core;
using R3;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using VContainer.Unity;

namespace Game.UI
{
    public sealed class HudPresenter : IInitializable, ILateTickable, IDisposable
    {
        private const float MarkerHeight = 2f;

        private readonly LevelConfig _level;
        private readonly PlayerVitals _vitals;
        private readonly AbilityPanel _abilityPanel;
        private readonly UpgradeChoice _choice;
        private readonly IUpgradeChoiceSubmit _submit;
        private readonly IContentRegistry _content;
        private readonly PlayerExperience _experience;
        private readonly BossVitals _bossVitals;
        private readonly RunResult _result;
        private readonly RunStats _stats;
        private readonly PauseMenu _pauseMenu;
        private readonly ISoundSettings _soundSettings;
        private readonly ICombatTextSettings _combatText;
        private readonly DamageNumbers _damageNumbers;
        private readonly Announcements _announcements;
        private readonly RuneStatus _runeStatus;
        private readonly DpsMeter _dpsMeter;
        private readonly ItemChoice _itemChoice;
        private readonly IDpsToggleInput _dpsToggle;
        private readonly ICameraService _camera;

        private GameObject _instance;
        private HudView _view;
        private IDisposable _subscription;
        private Tween _experienceTween;
        private int _shownLevel;
        private SettingsPopupPresenter _settingsPopup;

        private readonly PerkCardContent[] _cardContents = { new PerkCardContent(), new PerkCardContent(), new PerkCardContent() };
        private readonly PerkCardContent[] _itemContents = { new PerkCardContent(), new PerkCardContent(), new PerkCardContent(), new PerkCardContent() };
        private readonly StringBuilder _text = new StringBuilder();

        public HudPresenter(LevelConfig level, PlayerVitals vitals, AbilityPanel abilityPanel, UpgradeChoice choice, IUpgradeChoiceSubmit submit, IContentRegistry content, PlayerExperience experience, BossVitals bossVitals, RunResult result, RunStats stats, PauseMenu pauseMenu, ISoundSettings soundSettings, ICombatTextSettings combatText, DamageNumbers damageNumbers, DpsMeter dpsMeter, IDpsToggleInput dpsToggle, ItemChoice itemChoice, ICameraService camera, Announcements announcements, RuneStatus runeStatus)
        {
            _runeStatus = runeStatus;
            _announcements = announcements;
            _camera = camera;
            _pauseMenu = pauseMenu;
            _soundSettings = soundSettings;
            _abilityPanel = abilityPanel;
            _combatText = combatText;
            _damageNumbers = damageNumbers;
            _dpsMeter = dpsMeter;
            _itemChoice = itemChoice;
            _dpsToggle = dpsToggle;
            _level = level;
            _vitals = vitals;
            _choice = choice;
            _submit = submit;
            _content = content;
            _experience = experience;
            _bossVitals = bossVitals;
            _result = result;
            _stats = stats;
        }

        public void Initialize()
        {
            if (_level.HudPrefab == null)
                throw new InvalidOperationException(
                    $"{nameof(LevelConfig)} '{_level.Id}' has no HUD prefab assigned.");

            _instance = UnityEngine.Object.Instantiate(_level.HudPrefab);
            _instance.name = _level.HudPrefab.name;

            if (_instance.TryGetComponent(out _view) == false || _view.IsWired == false)
                throw new InvalidOperationException(
                    $"HUD prefab '{_level.HudPrefab.name}' needs a {nameof(HudView)} on its root with the health fill, health text, ultimate charge fill, vignette, " +
                    $"an {nameof(UpgradeChoiceView)} with its panel and {_view.UpgradeChoice.SlotCount} wired perk cards and a tooltip," +
                    $"the experience fill and level text, the boss bar with its fill and name, a wired {nameof(BossMarkerView)}, the victory banner, a wired {nameof(RunSummaryView)}, " +
                    $"and a {nameof(PauseMenuView)} with its fade, resume and settings buttons and a wired {nameof(SettingsPopupView)} assigned.");

            _view.UpgradeChoice.Bind(_submit.Submit);
            _view.ItemChoice.Bind(_submit.Submit);
            RefreshChoice();
            RefreshItemChoice();

            _settingsPopup = new SettingsPopupPresenter(_view.PauseMenu.SettingsPopup, _soundSettings, _combatText);
            _view.PauseMenu.Bind(_pauseMenu.RequestResume, _settingsPopup.Open);
            _view.PauseMenu.HideImmediate();

            _view.RunSummary.Bind(_result.RequestExit);
            _view.RunSummary.HideImmediate();

            _subscription = Disposable.Combine(
                _vitals.Health.Subscribe(this, (_, presenter) => presenter.Refresh()),
                _vitals.MaxHealth.Subscribe(this, (_, presenter) => presenter.Refresh()),
                _vitals.UltimateCharge.Subscribe(this, (_, presenter) => presenter.Refresh()),
                _vitals.UltimateActive.Subscribe(this, (_, presenter) => presenter.Refresh()),
                _vitals.HasUltimate.Subscribe(this, (_, presenter) => presenter.Refresh()),
                _vitals.HasDuality.Subscribe(this, (_, presenter) => presenter.Refresh()),
                _vitals.Demonic.Subscribe(this, (_, presenter) => presenter.Refresh()),
                _vitals.HasConcentration.Subscribe(this, (_, presenter) => presenter.Refresh()),
                _vitals.Concentration.Subscribe(this, (_, presenter) => presenter.Refresh()),
                _choice.Changed.Subscribe(this, (_, presenter) => presenter.RefreshChoice()),
                _itemChoice.Changed.Subscribe(this, (_, presenter) => presenter.RefreshItemChoice()),
                _pauseMenu.Changed.Subscribe(this, (_, presenter) => presenter.RefreshPause()),
                _experience.Changed.Subscribe(this, (_, presenter) => presenter.RefreshExperience()),
                _bossVitals.Visible.Subscribe(this, (_, presenter) => presenter.RefreshBoss()),
                _bossVitals.Health.Subscribe(this, (_, presenter) => presenter.RefreshBoss()),
                _bossVitals.MaxHealth.Subscribe(this, (_, presenter) => presenter.RefreshBoss()),
                _bossVitals.DisplayName.Subscribe(this, (_, presenter) => presenter.RefreshBoss()),
                _result.Victory.Subscribe(this, (victory, presenter) => presenter._view.ShowVictory(victory)),
                _result.Summary.Subscribe(this, (summary, presenter) => presenter.RefreshSummary(summary)));
        }

        public void LateTick()
        {
            RefreshAbilityPanel();

            if (_view != null)
            {
                _damageNumbers.Tick(_view.DamageNumbers, _camera, _combatText, Time.deltaTime);

                if (_view.Announcement != null && _announcements.TryTake(out string announcement))
                    _view.Announcement.Show(announcement);

                if (_view.RuneStatus != null && _view.RuneStatus.IsWired)
                    _view.RuneStatus.Show(_runeStatus.Active, _runeStatus.Name, _runeStatus.Color, _runeStatus.Remaining, _runeStatus.Total);

                if (_view.RuneBeacon != null && _view.RuneBeacon.IsWired)
                {
                    bool hasViewport = _camera.TryWorldToViewport(_runeStatus.RunePosition + Vector3.up, out Vector3 runeViewport);
                    _view.RuneBeacon.Show(_runeStatus.RunePresent, hasViewport, runeViewport, _runeStatus.RuneColor);
                }
                RefreshDps();
            }

            if (_view == null)
                return;

            bool alive = _bossVitals.Visible.CurrentValue;
            Vector3 viewport = default;

            if (alive)
                alive = _camera.TryWorldToViewport(_bossVitals.Position + Vector3.up * MarkerHeight, out viewport);

            _view.BossMarker.Show(alive, viewport, _bossVitals.DistanceToHero);
        }

        public void Dispose()
        {
            _subscription?.Dispose();
            _subscription = null;

            KillExperienceTween();

            _settingsPopup?.Dispose();
            _settingsPopup = null;

            if (_view != null)
            {
                _view.UpgradeChoice.Unbind();
                _view.ItemChoice.Unbind();
                _view.PauseMenu.Unbind();
                _view.RunSummary.Unbind();
            }

            if (_instance != null)
                UnityEngine.Object.Destroy(_instance);

            _instance = null;
            _view = null;
        }

        private void Refresh()
        {
            float maxHealth = _vitals.MaxHealth.CurrentValue;

            _instance.SetActive(maxHealth > 0f);

            if (maxHealth <= 0f)
                return;

            _view.ShowHealth(_vitals.Health.CurrentValue, maxHealth);
            _view.ShowUltimateAvailable(_vitals.HasUltimate.CurrentValue);
            _view.ShowUltimateCharge(_vitals.UltimateCharge.CurrentValue);
            _view.ShowBerserk(_vitals.UltimateActive.CurrentValue);
            _view.ShowDuality(_vitals.HasDuality.CurrentValue, _vitals.Demonic.CurrentValue, _vitals.FormThreshold.CurrentValue);
            _view.ShowConcentration(_vitals.HasConcentration.CurrentValue, _vitals.Concentration.CurrentValue);
        }

        private void RefreshBoss()
        {
            _view.ShowBoss(_bossVitals.Visible.CurrentValue, _bossVitals.DisplayName.CurrentValue, _bossVitals.Health.CurrentValue, _bossVitals.MaxHealth.CurrentValue);
        }

        private void RefreshExperience()
        {
            int level = _experience.Level;
            float target = _experience.Fraction;

            KillExperienceTween();

            if (_shownLevel == 0)
            {
                _view.SetExperienceFill(target);
                _view.ShowLevel(level);
                _shownLevel = level;

                return;
            }

            float seconds = _view.ExperienceFillSeconds;

            if (level > _shownLevel)
            {
                Sequence sequence = DOTween.Sequence();
                sequence.Append(FillTo(1f, seconds));
                sequence.AppendCallback(() => WrapLevel(level));
                sequence.Append(FillTo(target, seconds));
                sequence.SetUpdate(true);

                _experienceTween = sequence;
            }
            else
            {
                _experienceTween = FillTo(target, seconds).SetUpdate(true);
            }

            _shownLevel = level;
        }

        private Tween FillTo(float target, float seconds)
        {
            return DOTween.To(() => _view.ExperienceFill, _view.SetExperienceFill, target, seconds);
        }

        private void WrapLevel(int level)
        {
            _view.SetExperienceFill(0f);
            _view.ShowLevel(level);
        }

        private void KillExperienceTween()
        {
            if (_experienceTween != null && _experienceTween.IsActive())
                _experienceTween.Kill();

            _experienceTween = null;
        }

        private void RefreshSummary(bool open)
        {
            if (open == false)
                return;

            RunSummaryView view = _view.RunSummary;

            view.ClearRows();
            view.AddRow("Лучший DPS", FormatNumber(_dpsMeter.BestDps));
            view.AddRow("Самый сильный удар", FormatNumber(_dpsMeter.Biggest));
            view.AddRow("Нанесено урона", FormatNumber(_dpsMeter.Total));
            view.AddRow("Получено урона", FormatNumber(_stats.DamageTaken));
            view.AddRow("Время прохождения", FormatTime(_result.RunSeconds));
            view.AddRow("Убито врагов", _stats.EnemiesKilled.ToString(CultureInfo.InvariantCulture));
            view.AddRow("Собрано сфер", _stats.GemsCollected.ToString(CultureInfo.InvariantCulture));
            view.AddRow("Найдено предметов", _stats.ItemsFound.ToString(CultureInfo.InvariantCulture));

            _view.ShowVictory(false);
            view.Open();
        }

        private static string FormatNumber(float value)
        {
            return Mathf.RoundToInt(value).ToString("#,0", CultureInfo.InvariantCulture).Replace(',', ' ');
        }

        private static string FormatTime(float seconds)
        {
            int total = Mathf.FloorToInt(seconds);

            return string.Format(CultureInfo.InvariantCulture, "{0}:{1:00}", total / 60, total % 60);
        }

        private void RefreshPause()
        {
            _view.PauseMenu.SetOpen(_pauseMenu.IsOpen);

            if (_pauseMenu.IsOpen == false)
                _settingsPopup.Close();
        }

        private void RefreshChoice()
        {
            UpgradeChoiceView view = _view.UpgradeChoice;

            view.SetOpen(_choice.IsOpen);

            for (int slot = 0; slot < view.SlotCount; slot++)
            {
                if (slot >= _choice.Count)
                {
                    view.HideSlot(slot);

                    continue;
                }

                UpgradeOffer offer = _choice.Offer(slot);
                PerkConfig perk = _content.Get<PerkConfig>(offer.PerkId);

                view.ShowSlot(slot, Describe(_cardContents[slot], slot, perk, offer.NextLevel));
            }
        }

        private void RefreshDps()
        {
            if (_dpsToggle.ConsumePressed())
                _combatText.SetDpsVisible(_combatText.DpsVisible == false);

            DpsView view = _view.Dps;

            view.SetVisible(_combatText.DpsVisible);

            if (_combatText.DpsVisible == false)
                return;

            _dpsMeter.SetWindow(view.WindowSeconds);
            view.SetValue(_dpsMeter.Dps());
        }

        private void RefreshAbilityPanel()
        {
            if (_view == null)
                return;

            AbilityPanelView view = _view.AbilityPanel;

            for (int index = 0; index < _abilityPanel.Count; index++)
            {
                AbilityPanelEntry entry = _abilityPanel.Entry(index);

                if (index >= view.SlotCount)
                    view.AddSlot(ResolveAbilityIcon(entry.Id, view));

                view.SetSlot(index, entry.Remaining, entry.Fill);
            }
        }

        private Sprite ResolveAbilityIcon(string abilityId, AbilityPanelView view)
        {
            if (_content.TryGet(abilityId, out IAbilityIcon ability) && ability.Icon != null)
                return ability.Icon;

            return view.DashIcon;
        }

        private void RefreshItemChoice()
        {
            UpgradeChoiceView view = _view.ItemChoice;
            IReadOnlyList<ItemOfferData> offers = _itemChoice.Offers;

            view.SetOpen(_itemChoice.IsOpen);

            for (int slot = 0; slot < view.SlotCount; slot++)
            {
                if (slot >= offers.Count || slot >= _itemContents.Length)
                {
                    view.HideSlot(slot);

                    continue;
                }

                view.ShowSlot(slot, DescribeItem(_itemContents[slot], slot, offers[slot]));
            }
        }

        private PerkCardContent DescribeItem(PerkCardContent content, int slot, ItemOfferData offer)
        {
            ItemConfig item = _content.Get<ItemConfig>(offer.ItemId);

            content.Hotkey = (slot + 1).ToString();
            content.Title = item.DisplayName;
            content.Icon = item.Icon;
            content.Rarity = (Rarity)offer.Rarity;
            content.NextLevel = offer.Rank;
            content.MaxLevel = ItemConfig.RankCount;
            content.RankLabel = $"ранг {offer.Rank} из {ItemConfig.RankCount}";
            content.ComboIcons.Clear();

            content.Description = DescribeRoll(offer);

            if (offer.Equipped == null)
            {
                content.Stats = "Слот пуст";
            }
            else
            {
                ItemConfig equipped = _content.Get<ItemConfig>(offer.Equipped.ItemId);
                content.Stats = $"<color=#A89F96>Заменит: {equipped.DisplayName}, ранг {offer.Equipped.Rank}\n{DescribeRoll(offer.Equipped)}</color>";
            }

            content.Requires = ItemSlotText.Name(item.Slot);

            return content;
        }

        private string DescribeRoll(ItemOfferData roll)
        {
            _text.Clear();
            StatText.AppendLine(_text, roll.Main.Stat, roll.Main.Op, roll.Main.Value);

            for (int index = 0; index < roll.Minors.Count; index++)
                StatText.AppendLine(_text, roll.Minors[index].Stat, roll.Minors[index].Op, roll.Minors[index].Value);

            return _text.ToString();
        }

        private PerkCardContent Describe(PerkCardContent content, int slot, PerkConfig perk, int nextLevel)
        {
            content.Hotkey = (slot + 1).ToString();
            content.Title = perk.DisplayName;
            content.Icon = perk.Icon;
            content.Rarity = perk.Rarity;
            content.NextLevel = nextLevel;
            content.MaxLevel = perk.MaxLevel;
            content.Description = perk.Description;
            content.RankLabel = null;

            _text.Clear();

            IReadOnlyList<StatModifierEntry> modifiers = perk.Level(nextLevel).Modifiers;

            if (modifiers != null)
            {
                for (int index = 0; index < modifiers.Count; index++)
                    StatText.AppendLine(_text, modifiers[index].Stat, modifiers[index].Op, modifiers[index].Value);
            }

            content.Stats = _text.ToString();

            content.ComboIcons.Clear();
            _text.Clear();

            IReadOnlyList<PerkConfig> requires = perk.Requires;

            for (int index = 0; index < requires.Count; index++)
            {
                if (requires[index] == null)
                    continue;

                content.ComboIcons.Add(requires[index].Icon);
                _text.Append(_text.Length == 0 ? "Комбо: " : " + ").Append(requires[index].DisplayName);
            }

            content.Requires = _text.ToString();

            return content;
        }
    }
}

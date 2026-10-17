using DG.Tweening;
using Game.Configs;
using Game.Core;
using R3;
using System;
using System.Collections.Generic;
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
        private readonly UpgradeChoice _choice;
        private readonly IUpgradeChoiceSubmit _submit;
        private readonly IContentRegistry _content;
        private readonly PlayerExperience _experience;
        private readonly BossVitals _bossVitals;
        private readonly RunResult _result;
        private readonly PauseMenu _pauseMenu;
        private readonly ISoundSettings _soundSettings;
        private readonly ICameraService _camera;

        private GameObject _instance;
        private HudView _view;
        private IDisposable _subscription;
        private Tween _experienceTween;
        private int _shownLevel;
        private SettingsPopupPresenter _settingsPopup;

        private readonly PerkCardContent[] _cardContents = { new PerkCardContent(), new PerkCardContent(), new PerkCardContent() };
        private readonly StringBuilder _text = new StringBuilder();

        public HudPresenter(LevelConfig level, PlayerVitals vitals, UpgradeChoice choice, IUpgradeChoiceSubmit submit, IContentRegistry content, PlayerExperience experience, BossVitals bossVitals, RunResult result, PauseMenu pauseMenu, ISoundSettings soundSettings, ICameraService camera)
        {
            _camera = camera;
            _pauseMenu = pauseMenu;
            _soundSettings = soundSettings;
            _level = level;
            _vitals = vitals;
            _choice = choice;
            _submit = submit;
            _content = content;
            _experience = experience;
            _bossVitals = bossVitals;
            _result = result;
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
                    $"an {nameof(UpgradeChoiceView)} with its panel and {UpgradeChoiceView.SlotCount} wired perk cards and a tooltip," +
                    $"the experience fill and level text, the boss bar with its fill and name, a wired {nameof(BossMarkerView)}, the victory banner, " +
                    $"and a {nameof(PauseMenuView)} with its fade, resume and settings buttons and a wired {nameof(SettingsPopupView)} assigned.");

            _view.UpgradeChoice.Bind(_submit.Submit);
            RefreshChoice();

            _settingsPopup = new SettingsPopupPresenter(_view.PauseMenu.SettingsPopup, _soundSettings);
            _view.PauseMenu.Bind(_pauseMenu.RequestResume, _settingsPopup.Open);
            _view.PauseMenu.HideImmediate();

            _subscription = Disposable.Combine(
                _vitals.Health.Subscribe(this, (_, presenter) => presenter.Refresh()),
                _vitals.MaxHealth.Subscribe(this, (_, presenter) => presenter.Refresh()),
                _vitals.UltimateCharge.Subscribe(this, (_, presenter) => presenter.Refresh()),
                _vitals.UltimateActive.Subscribe(this, (_, presenter) => presenter.Refresh()),
                _choice.Changed.Subscribe(this, (_, presenter) => presenter.RefreshChoice()),
                _pauseMenu.Changed.Subscribe(this, (_, presenter) => presenter.RefreshPause()),
                _experience.Changed.Subscribe(this, (_, presenter) => presenter.RefreshExperience()),
                _bossVitals.Visible.Subscribe(this, (_, presenter) => presenter.RefreshBoss()),
                _bossVitals.Health.Subscribe(this, (_, presenter) => presenter.RefreshBoss()),
                _bossVitals.MaxHealth.Subscribe(this, (_, presenter) => presenter.RefreshBoss()),
                _bossVitals.DisplayName.Subscribe(this, (_, presenter) => presenter.RefreshBoss()),
                _result.Victory.Subscribe(this, (victory, presenter) => presenter._view.ShowVictory(victory)));
        }

        public void LateTick()
        {
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
                _view.PauseMenu.Unbind();
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
            _view.ShowUltimateCharge(_vitals.UltimateCharge.CurrentValue);
            _view.ShowBerserk(_vitals.UltimateActive.CurrentValue);
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

            for (int slot = 0; slot < UpgradeChoiceView.SlotCount; slot++)
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

        private PerkCardContent Describe(PerkCardContent content, int slot, PerkConfig perk, int nextLevel)
        {
            content.Hotkey = (slot + 1).ToString();
            content.Title = perk.DisplayName;
            content.Icon = perk.Icon;
            content.Rarity = perk.Rarity;
            content.NextLevel = nextLevel;
            content.MaxLevel = perk.MaxLevel;
            content.Description = perk.Description;

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

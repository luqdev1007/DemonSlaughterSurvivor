using DG.Tweening;
using Game.Configs;
using Game.Core;
using R3;
using System;
using UnityEngine;
using VContainer.Unity;

namespace Game.UI
{
    public sealed class HudPresenter : IInitializable, IDisposable
    {
        private readonly LevelConfig _level;
        private readonly PlayerVitals _vitals;
        private readonly UpgradeChoice _choice;
        private readonly IUpgradeChoiceSubmit _submit;
        private readonly IContentRegistry _content;
        private readonly PlayerExperience _experience;

        private GameObject _instance;
        private HudView _view;
        private IDisposable _subscription;
        private Tween _experienceTween;
        private int _shownLevel;

        public HudPresenter(LevelConfig level, PlayerVitals vitals, UpgradeChoice choice, IUpgradeChoiceSubmit submit, IContentRegistry content, PlayerExperience experience)
        {
            _level = level;
            _vitals = vitals;
            _choice = choice;
            _submit = submit;
            _content = content;
            _experience = experience;
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
                    $"an {nameof(UpgradeChoiceView)} with its panel and {UpgradeChoiceView.SlotCount} buttons, titles and descriptions, " +
                    "and the experience fill and level text assigned.");

            _view.UpgradeChoice.Bind(_submit.Submit);
            RefreshChoice();

            _subscription = Disposable.Combine(
                _vitals.Health.Subscribe(this, (_, presenter) => presenter.Refresh()),
                _vitals.MaxHealth.Subscribe(this, (_, presenter) => presenter.Refresh()),
                _vitals.UltimateCharge.Subscribe(this, (_, presenter) => presenter.Refresh()),
                _vitals.UltimateActive.Subscribe(this, (_, presenter) => presenter.Refresh()),
                _choice.Changed.Subscribe(this, (_, presenter) => presenter.RefreshChoice()),
                _experience.Changed.Subscribe(this, (_, presenter) => presenter.RefreshExperience()));
        }

        public void Dispose()
        {
            _subscription?.Dispose();
            _subscription = null;

            KillExperienceTween();

            if (_view != null)
                _view.UpgradeChoice.Unbind();

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

                view.ShowSlot(slot, $"[{slot + 1}] {perk.DisplayName}", $"{perk.Description}\n{offer.NextLevel} / {perk.MaxLevel}");
            }
        }
    }
}

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

        private GameObject _instance;
        private HudView _view;
        private IDisposable _subscription;

        public HudPresenter(LevelConfig level, PlayerVitals vitals, UpgradeChoice choice, IUpgradeChoiceSubmit submit, IContentRegistry content)
        {
            _level = level;
            _vitals = vitals;
            _choice = choice;
            _submit = submit;
            _content = content;
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
                    $"HUD prefab '{_level.HudPrefab.name}' needs a {nameof(HudView)} on its root with the health fill, health text, ultimate charge fill, vignette " +
                    $"and an {nameof(UpgradeChoiceView)} with its panel and {UpgradeChoiceView.SlotCount} buttons, titles and descriptions assigned.");

            _view.UpgradeChoice.Bind(_submit.Submit);
            RefreshChoice();

            _subscription = Disposable.Combine(
                _vitals.Health.Subscribe(this, (_, presenter) => presenter.Refresh()),
                _vitals.MaxHealth.Subscribe(this, (_, presenter) => presenter.Refresh()),
                _vitals.UltimateCharge.Subscribe(this, (_, presenter) => presenter.Refresh()),
                _vitals.UltimateActive.Subscribe(this, (_, presenter) => presenter.Refresh()),
                _choice.Changed.Subscribe(this, (_, presenter) => presenter.RefreshChoice()));
        }

        public void Dispose()
        {
            _subscription?.Dispose();
            _subscription = null;

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

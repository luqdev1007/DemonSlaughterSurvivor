using Game.Configs;
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

        private GameObject _instance;
        private HudView _view;
        private IDisposable _subscription;

        public HudPresenter(LevelConfig level, PlayerVitals vitals)
        {
            _level = level;
            _vitals = vitals;
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
                    $"HUD prefab '{_level.HudPrefab.name}' needs a {nameof(HudView)} on its root with the health fill and text assigned.");

            _subscription = Disposable.Combine(
                _vitals.Health.Subscribe(this, (_, presenter) => presenter.Refresh()),
                _vitals.MaxHealth.Subscribe(this, (_, presenter) => presenter.Refresh()));
        }

        public void Dispose()
        {
            _subscription?.Dispose();
            _subscription = null;

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
        }
    }
}

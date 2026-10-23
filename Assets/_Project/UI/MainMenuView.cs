using Cysharp.Threading.Tasks;
using Game.Configs;
using Game.Core;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using VContainer;

namespace Game.UI
{
    public sealed class MainMenuView : MonoBehaviour
    {
        [SerializeField] private Button _playButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _quitButton;
        [SerializeField] private Button _upgradeButton;
        [SerializeField] private MetaHubView _hub;
        [SerializeField] private SettingsPopupView _settingsPopup;

        [SerializeField] private string _levelId;
        [SerializeField, FormerlySerializedAs("_characterId")] private string _defaultCharacterId;

        [Header("Hero Choice")]
        [SerializeField] private MenuHeroLineup _lineup;

        private CancellationToken _lifeTime;
        private bool _isStarting;
        private IRunLauncher _launcher;
        private ISoundSettings _soundSettings;
        private ICombatTextSettings _combatText;
        private SettingsPopupPresenter _settings;
        private IContentRegistry _content;
        private IHeroSelection _heroSelection;
        private string _characterId;
        private IMetaProgress _meta;
        private IMenuRoute _menuRoute;
        private MetaHubPresenter _hubPresenter;

        [Inject]
        public void Construct(IRunLauncher runLauncher, ISoundSettings soundSettings, ICombatTextSettings combatText, IContentRegistry content, IHeroSelection heroSelection, IMetaProgress meta, IMenuRoute menuRoute)
        {
            _meta = meta;
            _menuRoute = menuRoute;
            _launcher = runLauncher;
            _soundSettings = soundSettings;
            _combatText = combatText;
            _content = content;
            _heroSelection = heroSelection;
        }

        private void Start()
        {
            if (_playButton == null || _settingsButton == null || _quitButton == null || _upgradeButton == null || _settingsPopup == null || _settingsPopup.IsWired == false
                || _hub == null || _hub.IsWired == false)
                throw new InvalidOperationException(
                    $"{nameof(MainMenuView)} needs the play, upgrade, settings and quit buttons, a wired {nameof(SettingsPopupView)} and a wired {nameof(MetaHubView)} assigned.");

            _lifeTime = this.GetCancellationTokenOnDestroy();

            _settings = new SettingsPopupPresenter(_settingsPopup, _soundSettings, _combatText);

            _playButton.onClick.AddListener(OnPlayClicked);
            _settingsButton.onClick.AddListener(_settings.Open);
            _quitButton.onClick.AddListener(OnQuitClicked);

            BuildHeroChoice();

            _hubPresenter = new MetaHubPresenter(_hub, _meta, _content, _heroSelection);
            _hubPresenter.Closed += OnHubClosed;
            _upgradeButton.onClick.AddListener(OpenHub);

            if (_menuRoute.ConsumeUpgradeRequest())
                OpenHub();
        }

        private void OnDestroy()
        {
            if (_playButton != null)
                _playButton.onClick.RemoveListener(OnPlayClicked);

            if (_settingsButton != null && _settings != null)
                _settingsButton.onClick.RemoveListener(_settings.Open);

            if (_quitButton != null)
                _quitButton.onClick.RemoveListener(OnQuitClicked);

            if (_upgradeButton != null)
                _upgradeButton.onClick.RemoveListener(OpenHub);

            if (_hubPresenter != null)
            {
                _hubPresenter.Closed -= OnHubClosed;
                _hubPresenter.Dispose();
                _hubPresenter = null;
            }

            _settings?.Dispose();
            _settings = null;

            if (_lineup != null)
                _lineup.Selected -= SelectHero;
        }

        private void BuildHeroChoice()
        {
            List<CharacterConfig> heroes = new List<CharacterConfig>(_content.All<CharacterConfig>());
            heroes.Sort(CompareHeroes);

            _characterId = ResolveInitialHero(heroes);

            if (_lineup == null)
                return;

            _lineup.Build(heroes, _characterId);
            _lineup.Selected += SelectHero;
        }

        private string ResolveInitialHero(List<CharacterConfig> heroes)
        {
            string saved = _heroSelection.SelectedCharacterId;

            if (string.IsNullOrEmpty(saved) == false && _content.TryGet(saved, out CharacterConfig _))
                return saved;

            if (string.IsNullOrEmpty(_defaultCharacterId) == false)
                return _defaultCharacterId;

            return heroes.Count > 0 ? heroes[0].Id : null;
        }

        private void SelectHero(string characterId)
        {
            if (_isStarting)
                return;

            _characterId = characterId;
            _heroSelection.SelectCharacter(characterId);
        }

        private static int CompareHeroes(CharacterConfig left, CharacterConfig right)
        {
            int byOrder = left.MenuOrder.CompareTo(right.MenuOrder);

            return byOrder != 0 ? byOrder : string.CompareOrdinal(left.Id, right.Id);
        }

        private void OnPlayClicked()
        {
            if (_isStarting)
                return;

            StartRunAsync().Forget();
        }

        private void OpenHub()
        {
            if (_isStarting)
                return;

            _hubPresenter.Open(_characterId);
        }

        private void OnHubClosed()
        {
            string selected = _heroSelection.SelectedCharacterId;

            if (string.IsNullOrEmpty(selected) || _content.TryGet(selected, out CharacterConfig _) == false)
                return;

            _characterId = selected;

            if (_lineup != null)
                _lineup.Show(selected);
        }

        private void OnQuitClicked()
        {
            Application.Quit();
        }

        private async UniTaskVoid StartRunAsync()
        {
            _isStarting = true;
            SetButtonsInteractable(false);

            try
            {
                RunRequest runRequest = new RunRequest(_levelId, _characterId, RunMode.Story);
                await _launcher.StartAsync(runRequest, _lifeTime);
            }
            catch (OperationCanceledException)
            {
            }
            catch (ContentNotFoundException e)
            {
                Debug.LogException(e);
            }
            finally
            {
                _isStarting = false;

                if (this != null)
                    SetButtonsInteractable(true);
            }
        }

        private void SetButtonsInteractable(bool interactable)
        {
            _playButton.interactable = interactable;
            _settingsButton.interactable = interactable;
            _quitButton.interactable = interactable;
            _upgradeButton.interactable = interactable;

            if (_lineup != null)
                _lineup.SetInteractable(interactable);
        }
    }
}

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

        [Inject]
        public void Construct(IRunLauncher runLauncher, ISoundSettings soundSettings, ICombatTextSettings combatText, IContentRegistry content, IHeroSelection heroSelection)
        {
            _launcher = runLauncher;
            _soundSettings = soundSettings;
            _combatText = combatText;
            _content = content;
            _heroSelection = heroSelection;
        }

        private void Start()
        {
            if (_playButton == null || _settingsButton == null || _quitButton == null || _settingsPopup == null || _settingsPopup.IsWired == false)
                throw new InvalidOperationException(
                    $"{nameof(MainMenuView)} needs the play, settings and quit buttons and a wired {nameof(SettingsPopupView)} assigned.");

            _lifeTime = this.GetCancellationTokenOnDestroy();

            _settings = new SettingsPopupPresenter(_settingsPopup, _soundSettings, _combatText);

            _playButton.onClick.AddListener(OnPlayClicked);
            _settingsButton.onClick.AddListener(_settings.Open);
            _quitButton.onClick.AddListener(OnQuitClicked);

            BuildHeroChoice();
        }

        private void OnDestroy()
        {
            if (_playButton != null)
                _playButton.onClick.RemoveListener(OnPlayClicked);

            if (_settingsButton != null && _settings != null)
                _settingsButton.onClick.RemoveListener(_settings.Open);

            if (_quitButton != null)
                _quitButton.onClick.RemoveListener(OnQuitClicked);

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

            if (_lineup != null)
                _lineup.SetInteractable(interactable);
        }
    }
}

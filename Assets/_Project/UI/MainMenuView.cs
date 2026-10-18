using Cysharp.Threading.Tasks;
using Game.Configs;
using Game.Core;
using System;
using System.Collections.Generic;
using System.Threading;
using TMPro;
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
        [SerializeField] private Button _heroButtonTemplate;
        [SerializeField] private Color _heroSelectedColor = new Color(1f, 0.78f, 0.35f, 1f);
        [SerializeField] private Color _heroIdleColor = new Color(0.75f, 0.75f, 0.75f, 1f);

        private CancellationToken _lifeTime;
        private bool _isStarting;
        private IRunLauncher _launcher;
        private ISoundSettings _soundSettings;
        private ICombatTextSettings _combatText;
        private SettingsPopupPresenter _settings;
        private IContentRegistry _content;
        private IHeroSelection _heroSelection;
        private readonly List<HeroButton> _heroButtons = new List<HeroButton>();
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

            foreach (HeroButton hero in _heroButtons)
            {
                if (hero.Button != null)
                    hero.Button.onClick.RemoveAllListeners();
            }

            _heroButtons.Clear();
        }

        private void BuildHeroChoice()
        {
            List<CharacterConfig> heroes = new List<CharacterConfig>(_content.All<CharacterConfig>());
            heroes.Sort(CompareHeroes);

            _characterId = ResolveInitialHero(heroes);

            if (_heroButtonTemplate == null)
                return;

            _heroButtonTemplate.gameObject.SetActive(false);

            if (heroes.Count < 2)
                return;

            Transform list = _heroButtonTemplate.transform.parent;

            foreach (CharacterConfig hero in heroes)
            {
                Button button = Instantiate(_heroButtonTemplate, list);
                button.name = "Hero_" + hero.Id;
                button.gameObject.SetActive(true);

                TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);

                if (label != null)
                    label.text = hero.DisplayName;

                string id = hero.Id;
                button.onClick.AddListener(() => SelectHero(id));

                _heroButtons.Add(new HeroButton(id, button, label));
            }

            RefreshHeroChoice();
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

            RefreshHeroChoice();
        }

        private void RefreshHeroChoice()
        {
            foreach (HeroButton hero in _heroButtons)
            {
                if (hero.Label != null)
                    hero.Label.color = hero.Id == _characterId ? _heroSelectedColor : _heroIdleColor;
            }
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

            foreach (HeroButton hero in _heroButtons)
            {
                if (hero.Button != null)
                    hero.Button.interactable = interactable;
            }
        }

        private readonly struct HeroButton
        {
            public HeroButton(string id, Button button, TMP_Text label)
            {
                Id = id;
                Button = button;
                Label = label;
            }

            public string Id { get; }

            public Button Button { get; }

            public TMP_Text Label { get; }
        }
    }
}

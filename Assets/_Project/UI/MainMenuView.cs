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
        private enum Screen
        {
            Main,
            Campaign,
            Character,
            Settings,
            Hub,
            Endless,
            Quit
        }

        [SerializeField] private Button _playButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _quitButton;
        [SerializeField] private Button _upgradeButton;
        [SerializeField] private MetaHubView _hub;
        [SerializeField] private SettingsPopupView _settingsPopup;
        [SerializeField, FormerlySerializedAs("_characterId")] private string _defaultCharacterId;

        [Header("Hero Choice")]
        [SerializeField] private MenuHeroLineup _lineup;

        private CancellationToken _lifeTime;
        private bool _isStarting;
        private Screen _screen;
        private string _selectedLevelId;
        private string _characterId;
        private IRunLauncher _launcher;
        private ISoundSettings _soundSettings;
        private ICombatTextSettings _combatText;
        private IContentRegistry _content;
        private IHeroSelection _heroSelection;
        private IMetaProgress _meta;
        private ICampaignProgress _campaignProgress;
        private IMenuRoute _menuRoute;
        private IPauseInput _pauseInput;
        private CampaignConfig _campaign;
        private SettingsPopupPresenter _settings;
        private MetaHubPresenter _hubPresenter;
        private CampaignPopupPresenter _campaignPresenter;
        private RectTransform _mainActions;
        private RectTransform _characterActions;
        private CampaignPopupView _campaignView;
        private RectTransform _messageRoot;
        private TMP_Text _messageTitle;
        private TMP_Text _messageBody;
        private Button _messageConfirm;
        private Button _messageCancel;
        private Button _endlessButton;
        private Button _characterPlayButton;
        private Button _characterBackButton;

        [Inject]
        public void Construct(IRunLauncher runLauncher, ISoundSettings soundSettings, ICombatTextSettings combatText,
            IContentRegistry content, IHeroSelection heroSelection, IMetaProgress meta, ICampaignProgress campaignProgress,
            IMenuRoute menuRoute, IPauseInput pauseInput)
        {
            _launcher = runLauncher;
            _soundSettings = soundSettings;
            _combatText = combatText;
            _content = content;
            _heroSelection = heroSelection;
            _meta = meta;
            _campaignProgress = campaignProgress;
            _menuRoute = menuRoute;
            _pauseInput = pauseInput;
        }

        private void Start()
        {
            if (_playButton == null || _settingsButton == null || _quitButton == null || _upgradeButton == null ||
                _lineup == null || _settingsPopup == null || _settingsPopup.IsWired == false || _hub == null || _hub.IsWired == false)
                throw new InvalidOperationException($"{nameof(MainMenuView)} needs its existing buttons, lineup, settings popup and meta hub wired.");

            IReadOnlyList<CampaignConfig> campaigns = _content.All<CampaignConfig>();
            _campaign = campaigns.Count > 0 ? campaigns[0] : throw new InvalidOperationException("CampaignConfig is not registered.");
            _campaign.Validate();
            _lifeTime = this.GetCancellationTokenOnDestroy();

            BuildHeroChoice();
            BuildNavigation();

            _settings = new SettingsPopupPresenter(_settingsPopup, _soundSettings, _combatText);
            _settings.Closed += OnSettingsClosed;
            _hubPresenter = new MetaHubPresenter(_hub, _meta, _content, _heroSelection);
            _hubPresenter.Closed += OnHubClosed;

            _campaignView = UiKit.Rect("CampaignPopup", transform).gameObject.AddComponent<CampaignPopupView>();
            _campaignView.Initialize(_hub.Style);
            _campaignPresenter = new CampaignPopupPresenter(_campaignView, _campaign, _campaignProgress, _content);
            _campaignPresenter.LevelSelected += SelectLevel;
            _campaignPresenter.Closed += OnCampaignClosed;

            BuildMessagePopup();
            BindButtons();
            Show(Screen.Main);

            if (_menuRoute.ConsumeUpgradeRequest())
                OpenHub();
        }

        private void Update()
        {
            if (_isStarting || _pauseInput.ConsumePausePressed() == false)
                return;

            switch (_screen)
            {
                case Screen.Campaign:
                case Screen.Endless:
                case Screen.Quit:
                    Show(Screen.Main);
                    break;
                case Screen.Character:
                    Show(Screen.Campaign);
                    break;
                case Screen.Settings:
                    _settings.Close();
                    break;
                case Screen.Hub:
                    _hubPresenter.Close();
                    break;
            }
        }

        private void BuildHeroChoice()
        {
            List<CharacterConfig> heroes = new List<CharacterConfig>(_content.All<CharacterConfig>());
            heroes.Sort((left, right) =>
            {
                int byOrder = left.MenuOrder.CompareTo(right.MenuOrder);
                return byOrder != 0 ? byOrder : string.CompareOrdinal(left.Id, right.Id);
            });

            string saved = _heroSelection.SelectedCharacterId;
            _characterId = string.IsNullOrEmpty(saved) == false && _content.TryGet(saved, out CharacterConfig _)
                ? saved
                : string.IsNullOrEmpty(_defaultCharacterId) == false && _content.TryGet(_defaultCharacterId, out CharacterConfig _)
                    ? _defaultCharacterId
                    : heroes.Count > 0 ? heroes[0].Id : null;

            _lineup.Build(heroes, _characterId);
            _lineup.Selected += SelectHero;
            _lineup.SetVisible(false);
        }

        private void BuildNavigation()
        {
            _mainActions = UiKit.Rect("MainActions", transform);
            UiKit.Place(_mainActions, new Vector2(0.5f, 0.5f), new Vector2(0f, -115f), new Vector2(410f, 420f));

            _endlessButton = Instantiate(_playButton, _mainActions);
            _endlessButton.name = "EndlessButton";
            UiKit.SetLabel(_endlessButton, "БЕСКОНЕЧНЫЙ ЗАБЕГ");

            PlaceAction(_playButton, _mainActions, 0, "КАМПАНИЯ");
            PlaceAction(_endlessButton, _mainActions, 1, "БЕСКОНЕЧНЫЙ ЗАБЕГ");
            PlaceAction(_upgradeButton, _mainActions, 2, "ПРОКАЧКА");
            PlaceAction(_settingsButton, _mainActions, 3, "НАСТРОЙКИ");
            PlaceAction(_quitButton, _mainActions, 4, "ВЫХОД");

            _characterActions = UiKit.Rect("CharacterActions", transform);
            UiKit.Place(_characterActions, new Vector2(0.5f, 0f), new Vector2(0f, 84f), new Vector2(500f, 70f));
            _characterPlayButton = Instantiate(_playButton, _characterActions);
            _characterPlayButton.name = "CharacterPlayButton";
            _characterBackButton = Instantiate(_quitButton, _characterActions);
            _characterBackButton.name = "CharacterBackButton";
            PlaceChoice(_characterPlayButton, -140f, "ИГРАТЬ");
            PlaceChoice(_characterBackButton, 140f, "НАЗАД");
        }

        private static void PlaceAction(Button button, RectTransform parent, int index, string label)
        {
            button.transform.SetParent(parent, false);
            UiKit.Place((RectTransform)button.transform, new Vector2(0.5f, 1f), new Vector2(0f, -42f - 78f * index), new Vector2(390f, 66f));
            UiKit.SetLabel(button, label);
        }

        private static void PlaceChoice(Button button, float x, string label)
        {
            UiKit.Place((RectTransform)button.transform, new Vector2(0.5f, 0.5f), new Vector2(x, 0f), new Vector2(230f, 66f));
            UiKit.SetLabel(button, label);
        }

        private void BuildMessagePopup()
        {
            _messageRoot = UiKit.Rect("MessagePopup", transform);
            UiKit.Fill(_messageRoot);
            UiKit.Fill(_messageRoot, "Dim", null, MetaPalette.WithAlpha(MetaPalette.Void, 0.92f), 0f, true);

            Image panel = UiKit.Panel(_messageRoot, "Panel", _hub.Style);
            UiKit.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(680f, 370f));

            _messageTitle = UiKit.Text(panel.transform, "Title", _hub.Style.TitleFont, 34f, MetaPalette.Bone, TextAlignmentOptions.Center);
            UiKit.Place(_messageTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(590f, 60f));
            _messageBody = UiKit.Text(panel.transform, "Body", _hub.Style.BodyFont, 23f, MetaPalette.Dim, TextAlignmentOptions.Center);
            UiKit.Place(_messageBody.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(580f, 100f));

            _messageConfirm = UiKit.Button(panel.transform, "ConfirmButton", _hub.Style, "ПОНЯТНО", OnMessageConfirm, 21f);
            _messageCancel = UiKit.Button(panel.transform, "CancelButton", _hub.Style, "ОТМЕНА", () => Show(Screen.Main), 21f);
            UiKit.Place((RectTransform)_messageConfirm.transform, new Vector2(0.5f, 0f), new Vector2(-135f, 55f), new Vector2(230f, 58f));
            UiKit.Place((RectTransform)_messageCancel.transform, new Vector2(0.5f, 0f), new Vector2(135f, 55f), new Vector2(230f, 58f));
            _messageRoot.gameObject.SetActive(false);
        }

        private void BindButtons()
        {
            _playButton.onClick.AddListener(OpenCampaign);
            _endlessButton.onClick.AddListener(OpenEndless);
            _upgradeButton.onClick.AddListener(OpenHub);
            _settingsButton.onClick.AddListener(OpenSettings);
            _quitButton.onClick.AddListener(OpenQuit);
            _characterPlayButton.onClick.AddListener(OnPlayClicked);
            _characterBackButton.onClick.AddListener(() => Show(Screen.Campaign));
        }

        private void Show(Screen screen)
        {
            _screen = screen;
            _mainActions.gameObject.SetActive(screen == Screen.Main);
            _characterActions.gameObject.SetActive(screen == Screen.Character);
            _campaignPresenter.Close();
            _messageRoot.gameObject.SetActive(false);
            _lineup.SetVisible(screen == Screen.Character);

            if (screen == Screen.Campaign)
                _campaignPresenter.Open();
            else if (screen == Screen.Endless || screen == Screen.Quit)
                ShowMessage(screen);
        }

        private void ShowMessage(Screen screen)
        {
            bool quit = screen == Screen.Quit;
            _messageTitle.text = quit ? "ВЫХОД ИЗ ИГРЫ" : "БЕСКОНЕЧНЫЙ ЗАБЕГ";
            _messageBody.text = quit ? "Вы уверены, что хотите выйти?" : "Режим находится в разработке и появится позже.";
            UiKit.SetLabel(_messageConfirm, quit ? "ВЫЙТИ" : "ПОНЯТНО");
            _messageCancel.gameObject.SetActive(quit);
            UiKit.Place((RectTransform)_messageConfirm.transform, new Vector2(0.5f, 0f),
                new Vector2(quit ? -135f : 0f, 55f), new Vector2(230f, 58f));
            _messageRoot.gameObject.SetActive(true);
        }

        private void OpenCampaign()
        {
            if (_screen == Screen.Main && _isStarting == false)
                Show(Screen.Campaign);
        }

        private void SelectLevel(string levelId)
        {
            if (_screen != Screen.Campaign || _isStarting)
                return;

            _selectedLevelId = levelId;
            Show(Screen.Character);
            _lineup.Show(_characterId);
        }

        private void SelectHero(string characterId)
        {
            if (_screen != Screen.Character || _isStarting)
                return;

            _characterId = characterId;
            _heroSelection.SelectCharacter(characterId);
        }

        private void OpenHub()
        {
            if (_screen != Screen.Main || _isStarting)
                return;

            Show(Screen.Hub);
            _hubPresenter.Open(_characterId);
        }

        private void OnHubClosed()
        {
            string selected = _heroSelection.SelectedCharacterId;

            if (string.IsNullOrEmpty(selected) == false && _content.TryGet(selected, out CharacterConfig _))
            {
                _characterId = selected;
                _lineup.Show(selected);
            }

            Show(Screen.Main);
        }

        private void OpenSettings()
        {
            if (_screen != Screen.Main || _isStarting)
                return;

            Show(Screen.Settings);
            _settings.Open();
        }

        private void OnSettingsClosed() => Show(Screen.Main);

        private void OnCampaignClosed() => Show(Screen.Main);

        private void OpenEndless()
        {
            if (_screen == Screen.Main && _isStarting == false)
                Show(Screen.Endless);
        }

        private void OpenQuit()
        {
            if (_screen == Screen.Main && _isStarting == false)
                Show(Screen.Quit);
        }

        private void OnMessageConfirm()
        {
            if (_screen == Screen.Quit)
                Application.Quit();
            else if (_screen == Screen.Endless)
                Show(Screen.Main);
        }

        private void OnPlayClicked()
        {
            if (_screen != Screen.Character || _isStarting || string.IsNullOrEmpty(_selectedLevelId) || string.IsNullOrEmpty(_characterId))
                return;

            StartRunAsync().Forget();
        }

        private async UniTaskVoid StartRunAsync()
        {
            _isStarting = true;
            _characterPlayButton.interactable = false;
            _characterBackButton.interactable = false;
            _lineup.SetInteractable(false);

            try
            {
                RunRequest request = new RunRequest(_selectedLevelId, _characterId, RunMode.Story);
                await _launcher.StartAsync(request, _lifeTime);
            }
            catch (OperationCanceledException)
            {
            }
            catch (ContentNotFoundException exception)
            {
                Debug.LogException(exception);
            }
            finally
            {
                _isStarting = false;

                if (this != null)
                {
                    _characterPlayButton.interactable = true;
                    _characterBackButton.interactable = true;
                    _lineup.SetInteractable(_screen == Screen.Character);
                }
            }
        }

        private void OnDestroy()
        {
            if (_playButton != null)
                _playButton.onClick.RemoveListener(OpenCampaign);
            if (_endlessButton != null)
                _endlessButton.onClick.RemoveListener(OpenEndless);
            if (_upgradeButton != null)
                _upgradeButton.onClick.RemoveListener(OpenHub);
            if (_settingsButton != null)
                _settingsButton.onClick.RemoveListener(OpenSettings);
            if (_quitButton != null)
                _quitButton.onClick.RemoveListener(OpenQuit);
            if (_characterPlayButton != null)
                _characterPlayButton.onClick.RemoveListener(OnPlayClicked);
            if (_lineup != null)
                _lineup.Selected -= SelectHero;

            if (_campaignPresenter != null)
            {
                _campaignPresenter.LevelSelected -= SelectLevel;
                _campaignPresenter.Closed -= OnCampaignClosed;
                _campaignPresenter.Dispose();
            }

            if (_hubPresenter != null)
            {
                _hubPresenter.Closed -= OnHubClosed;
                _hubPresenter.Dispose();
            }

            if (_settings != null)
            {
                _settings.Closed -= OnSettingsClosed;
                _settings.Dispose();
            }
        }
    }
}

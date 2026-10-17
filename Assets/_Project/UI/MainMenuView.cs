using Cysharp.Threading.Tasks;
using Game.Core;
using System;
using System.Threading;
using UnityEngine;
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
        [SerializeField] private string _characterId;

        private CancellationToken _lifeTime;
        private bool _isStarting;
        private IRunLauncher _launcher;
        private ISoundSettings _soundSettings;
        private SettingsPopupPresenter _settings;

        [Inject]
        public void Construct(IRunLauncher runLauncher, ISoundSettings soundSettings)
        {
            _launcher = runLauncher;
            _soundSettings = soundSettings;
        }

        private void Start()
        {
            if (_playButton == null || _settingsButton == null || _quitButton == null || _settingsPopup == null || _settingsPopup.IsWired == false)
                throw new InvalidOperationException(
                    $"{nameof(MainMenuView)} needs the play, settings and quit buttons and a wired {nameof(SettingsPopupView)} assigned.");

            _lifeTime = this.GetCancellationTokenOnDestroy();

            _settings = new SettingsPopupPresenter(_settingsPopup, _soundSettings);

            _playButton.onClick.AddListener(OnPlayClicked);
            _settingsButton.onClick.AddListener(_settings.Open);
            _quitButton.onClick.AddListener(OnQuitClicked);

            Debug.Log($"[RunEndTrace] MainMenuView started, frame {Time.frameCount}");
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
        }
    }
}

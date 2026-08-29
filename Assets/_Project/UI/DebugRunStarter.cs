using Cysharp.Threading.Tasks;
using Game.Core;
using System;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Game.UI
{
    public sealed class DebugRunStarter : MonoBehaviour
    {
        [SerializeField] private Button _playButton;

        [SerializeField] private string _levelId;
        [SerializeField] private string _characterId;

        private CancellationToken _lifeTime;
        private bool _isStarting;
        private IRunLauncher _launcher;

        [Inject]
        public void Construct(IRunLauncher runLauncher)
        {
            _launcher = runLauncher;
        }

        private void Awake()
        {
            _lifeTime = this.GetCancellationTokenOnDestroy();

            _playButton.onClick.AddListener(OnPlayClicked);
        }

        private void OnDestroy()
        {
            _playButton.onClick.RemoveListener(OnPlayClicked);
        }

        private void OnPlayClicked()
        {
            if (_isStarting)
                return;

            StartRunAsync().Forget();
        }

        private async UniTaskVoid StartRunAsync()
        {
            _isStarting = true;
            _playButton.interactable = false;

            try
            {
                RunRequest runRequest = new RunRequest(_levelId, _characterId, RunMode.Story);
                await _launcher.StartAsync(runRequest, _lifeTime);
            }
            catch (OperationCanceledException)
            {
                // Панель закрыта
            }
            catch (ContentNotFoundException e)
            {
                Debug.LogException(e);
            }
            finally
            {
                _isStarting = false;

                if (_playButton != null)
                    _playButton.interactable = true;
            }
        }
    }
}

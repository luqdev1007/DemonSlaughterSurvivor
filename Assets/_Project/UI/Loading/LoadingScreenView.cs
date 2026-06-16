using Cysharp.Threading.Tasks;
using DemonSlaughter.Core.Loading;
using UnityEngine;
using UnityEngine.UI;

namespace DemonSlaughter.UI.Loading
{
    public sealed class LoadingScreenView : MonoBehaviour, ILoadingScreen
    {
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Slider _progressBar;
        [SerializeField] private float _fadeDuration = 0.3f;

        private void Awake()
        {
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
            DontDestroyOnLoad(gameObject);
        }

        public void SetProgress(float progress)
        {
            if (_progressBar != null)
                _progressBar.value = progress;
        }

        public async UniTask ShowAsync()
        {
            _canvasGroup.blocksRaycasts = true;
            await FadeAsync(0f, 1f);
        }

        public async UniTask HideAsync()
        {
            await FadeAsync(1f, 0f);
            _canvasGroup.blocksRaycasts = false;
        }

        private async UniTask FadeAsync(float from, float to)
        {
            float elapsed = 0f;

            while (elapsed < _fadeDuration)
            {
                elapsed += Time.deltaTime;
                _canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / _fadeDuration);
                await UniTask.Yield();
            }

            _canvasGroup.alpha = to;
        }
    }
}
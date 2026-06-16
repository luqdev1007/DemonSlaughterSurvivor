using Cysharp.Threading.Tasks;
using DemonSlaughter.Core.Loading;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace DemonSlaughter.UI.Loading
{
    public sealed class LoadingScreenView : MonoBehaviour, ILoadingScreen
    {
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Slider _progressBar;

        [Header("Animation")]
        [SerializeField] private float _fadeDuration = 0.3f;
        [SerializeField] private Ease _fadeEase = Ease.InOutSine;
        [SerializeField] private float _progressDuration = 0.2f;
        [SerializeField] private Ease _progressEase = Ease.OutCubic;

        private void Awake()
        {
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            _canvasGroup.DOKill();

            if (_progressBar != null)
                _progressBar.DOKill();
        }

        public UniTask ShowAsync()
        {
            _canvasGroup.blocksRaycasts = true;
            return FadeAsync(1f);
        }

        public UniTask HideAsync()
        {
            return FadeAsync(0f).ContinueWith(() =>
                _canvasGroup.blocksRaycasts = false);
        }

        public void SetProgress(float progress)
        {
            if (_progressBar == null) return;

            _progressBar
                .DOValue(progress, _progressDuration)
                .SetEase(_progressEase)
                .SetUpdate(true);
        }

        private UniTask FadeAsync(float targetAlpha)
        {
            var utcs = new UniTaskCompletionSource();

            _canvasGroup
                .DOFade(targetAlpha, _fadeDuration)
                .SetEase(_fadeEase)
                .SetUpdate(true)
                .OnComplete(() => utcs.TrySetResult());

            return utcs.Task;
        }
    }
}
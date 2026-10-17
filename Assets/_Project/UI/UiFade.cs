using DG.Tweening;
using UnityEngine;

namespace Game.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class UiFade : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _group;
        [SerializeField] private float _seconds = 0.2f;

        private Tween _tween;

        public bool IsWired => _group != null;

        public void Show()
        {
            gameObject.SetActive(true);

            _group.interactable = true;
            _group.blocksRaycasts = true;

            KillTween();

            _tween = FadeTo(1f);
        }

        public void Hide()
        {
            if (gameObject.activeSelf == false)
                return;

            _group.interactable = false;
            _group.blocksRaycasts = false;

            KillTween();

            _tween = FadeTo(0f).OnComplete(Deactivate);
        }

        public void HideImmediate()
        {
            KillTween();

            _group.alpha = 0f;
            _group.interactable = false;
            _group.blocksRaycasts = false;

            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            KillTween();
        }

        private Tween FadeTo(float target)
        {
            return DOTween.To(() => _group.alpha, alpha => _group.alpha = alpha, target, _seconds).SetUpdate(true);
        }

        private void Deactivate()
        {
            _tween = null;

            gameObject.SetActive(false);
        }

        private void KillTween()
        {
            if (_tween != null && _tween.IsActive())
                _tween.Kill();

            _tween = null;
        }
    }
}

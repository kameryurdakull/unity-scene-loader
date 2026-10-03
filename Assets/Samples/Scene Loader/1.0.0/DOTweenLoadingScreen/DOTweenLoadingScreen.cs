using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace SceneLoader.Samples
{
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class DOTweenLoadingScreen : MonoBehaviour, ILoadingScreenController
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Slider progressBar;
        [SerializeField, Min(0f)] private float fadeDuration = 0.3f;
        private Tween _fade;

        private void Awake()
        {
            if (transform.parent != null)
                throw new InvalidOperationException("The loading screen must be on a root GameObject.");
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            DontDestroyOnLoad(gameObject);
            ResetScreen();
        }

        public async UniTask ShowAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            UpdateProgress(0f);
            canvasGroup.blocksRaycasts = true;
            await FadeAsync(1f, cancellationToken);
        }

        public async UniTask HideAsync()
        {
            if (this == null || canvasGroup == null) return;
            try { await FadeAsync(0f, this.GetCancellationTokenOnDestroy()); }
            finally
            {
                if (this != null && canvasGroup != null) ResetScreen();
            }
        }

        public void UpdateProgress(float progress)
        {
            if (progressBar != null) progressBar.normalizedValue = Mathf.Clamp01(progress);
        }

        private async UniTask FadeAsync(float target, CancellationToken cancellationToken)
        {
            _fade?.Kill();
            if (fadeDuration <= 0f)
            {
                canvasGroup.alpha = target;
                return;
            }

            var tween = DOTween.To(() => canvasGroup.alpha, value => canvasGroup.alpha = value,
                target, fadeDuration).SetEase(Ease.InOutQuad).SetUpdate(true);
            _fade = tween;
            try
            {
                await UniTask.WaitUntil(() => !tween.IsActive() || tween.IsComplete(),
                    cancellationToken: cancellationToken);
            }
            finally
            {
                tween.Kill();
                if (_fade == tween) _fade = null;
            }
        }

        private void ResetScreen()
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        private void OnDestroy() => _fade?.Kill();
        private void Reset() => canvasGroup = GetComponent<CanvasGroup>();
    }
}

using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace FeaturesCommon
{
    public class FadeManager : MonoBehaviour
    {
        public static FadeManager Instance { get; private set; }

        [Header("Fade Settings")]
        [SerializeField] private Image fadeImage;
        [SerializeField] private float defaultFadeDuration = 0.5f;
        [SerializeField] private Color fadeColor = Color.black;

        private Coroutine currentFadeCoroutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (fadeImage != null)
            {
                fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f);
                fadeImage.raycastTarget = false;
            }
        }

        public Coroutine FadeIn(float duration = -1f, System.Action onComplete = null)
        {
            return StartFade(0f, 1f, duration > 0 ? duration : defaultFadeDuration, onComplete);
        }

        public Coroutine FadeOut(float duration = -1f, System.Action onComplete = null)
        {
            return StartFade(1f, 0f, duration > 0 ? duration : defaultFadeDuration, onComplete);
        }

        public Coroutine FadeTo(float targetAlpha, float duration = -1f, System.Action onComplete = null)
        {
            if (fadeImage == null) return null;
            float startAlpha = fadeImage.color.a;
            return StartFade(startAlpha, targetAlpha, duration > 0 ? duration : defaultFadeDuration, onComplete);
        }

        private Coroutine StartFade(float fromAlpha, float toAlpha, float duration, System.Action onComplete)
        {
            if (fadeImage == null) return null;

            if (currentFadeCoroutine != null)
            {
                StopCoroutine(currentFadeCoroutine);
            }

            currentFadeCoroutine = StartCoroutine(FadeCoroutine(fromAlpha, toAlpha, duration, onComplete));
            return currentFadeCoroutine;
        }

        private IEnumerator FadeCoroutine(float fromAlpha, float toAlpha, float duration, System.Action onComplete)
        {
            if (fadeImage == null) yield break;

            float elapsed = 0f;
            Color color = fadeImage.color;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float alpha = Mathf.Lerp(fromAlpha, toAlpha, t);
                color.a = alpha;
                fadeImage.color = color;
                yield return null;
            }

            color.a = toAlpha;
            fadeImage.color = color;

            currentFadeCoroutine = null;
            onComplete?.Invoke();
        }

        public void SetFadeInstant(float alpha)
        {
            if (fadeImage != null)
            {
                Color c = fadeImage.color;
                c.a = alpha;
                fadeImage.color = c;
            }
        }

        public bool IsFading => currentFadeCoroutine != null;
    }
}
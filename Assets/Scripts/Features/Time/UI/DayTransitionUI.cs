using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Layar transisi pergantian hari (murni UI Listener).
// Tidak memodifikasi data waktu; hanya bereaksi terhadap event OnDayChanged
// dari TimeManager untuk menampilkan fade hitam "Day X".
public class DayTransitionUI : MonoBehaviour
{
    [SerializeField] private CanvasGroup transitionCanvasGroup;
    [SerializeField] private Text dayText;
    [SerializeField] private float fadeDuration = 1.5f;

    void Awake()
    {
        // Sembunyikan panel sejak awal agar tidak menghalangi UI lain.
        if (transitionCanvasGroup != null)
        {
            transitionCanvasGroup.alpha = 0f;
            transitionCanvasGroup.blocksRaycasts = false;
        }
    }

    void OnEnable()
    {
        if (TimeManager.Instance != null)
            TimeManager.Instance.OnDayChanged += OnDayChangedHandler;
    }

    void OnDisable()
    {
        if (TimeManager.Instance != null)
            TimeManager.Instance.OnDayChanged -= OnDayChangedHandler;
    }

    private void OnDayChangedHandler(int newDay)
    {
        if (transitionCanvasGroup == null)
            return;

        // Antre transisi; text diberi label hari baru ("Day 2", dst).
        StartCoroutine(TransitionRoutine(newDay));
    }

    private IEnumerator TransitionRoutine(int newDay)
    {
        if (dayText != null)
            dayText.text = "Day " + newDay;

        // Blokir semua klik selama transisi berlangsung.
        transitionCanvasGroup.blocksRaycasts = true;

        float half = Mathf.Max(fadeDuration / 2f, 0.05f);

        // Fade in: alpha 0 -> 1.
        for (float t = 0f; t < half; t += Time.deltaTime)
        {
            transitionCanvasGroup.alpha = Mathf.Clamp01(t / half);
            yield return null;
        }
        transitionCanvasGroup.alpha = 1f;

        // Tahan beberapa saat layar penuh.
        yield return new WaitForSeconds(1.5f);

        // Fade out: alpha 1 -> 0.
        for (float t = 0f; t < half; t += Time.deltaTime)
        {
            transitionCanvasGroup.alpha = Mathf.Clamp01(1f - (t / half));
            yield return null;
        }
        transitionCanvasGroup.alpha = 0f;

        // Buka kembali input setelah transisi selesai.
        transitionCanvasGroup.blocksRaycasts = false;
    }
}
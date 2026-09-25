using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Listener visual modern untuk indikator rasa haus (thirst) pemain.
/// Mendukung label persentase TextMeshPro, warna safir laut pekat,
/// serta peringatan bahaya dehidrasi (dehydration alert pulse saat memicu penalti damage).
/// </summary>
public class PlayerThirstUI : MonoBehaviour
{
    [Header("Core References")]
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private Slider thirstSlider;
    [SerializeField] private TMP_Text thirstText;
    [SerializeField] private Image fillImage;
    [SerializeField] private Image iconImage;
    [SerializeField] private GameObject warningIndicator;

    [Header("Color & Styling")]
    [SerializeField] private Color normalColor = new Color(0.05f, 0.55f, 0.88f, 1f); // Ocean Sapphire Blue
    [SerializeField] private Color warningColor = new Color(0.95f, 0.15f, 0.15f, 1f); // Dehydration Red
    [SerializeField] [Range(0.05f, 0.3f)] private float warningThresholdPercent = 0.15f;

    private Coroutine warningCoroutine;

    private void Awake()
    {
        if (thirstSlider == null)
            thirstSlider = GetComponent<Slider>() ?? GetComponentInChildren<Slider>();

        if (thirstText == null)
            thirstText = GetComponentInChildren<TMP_Text>(true);

        if (fillImage == null && thirstSlider != null && thirstSlider.fillRect != null)
            fillImage = thirstSlider.fillRect.GetComponent<Image>();

        if (warningIndicator == null)
        {
            var warnTf = transform.Find("WarningIndicator") ?? transform.Find("WarningIcon");
            if (warnTf != null) warningIndicator = warnTf.gameObject;
        }
    }

    private void Start()
    {
        EnsurePlayerStatsBound();

        if (playerStats != null)
        {
            UpdateThirstVisual(playerStats.currentThirst, playerStats.maxThirst);
        }
    }

    private void OnEnable()
    {
        EnsurePlayerStatsBound();

        if (playerStats != null)
        {
            playerStats.OnThirstChanged += UpdateThirstVisual;
            UpdateThirstVisual(playerStats.currentThirst, playerStats.maxThirst);
        }
    }

    private void OnDisable()
    {
        if (playerStats != null)
            playerStats.OnThirstChanged -= UpdateThirstVisual;

        if (warningCoroutine != null)
        {
            StopCoroutine(warningCoroutine);
            warningCoroutine = null;
        }
    }

    private void EnsurePlayerStatsBound()
    {
        if (playerStats == null)
            playerStats = FindFirstObjectByType<PlayerStats>();
    }

    private void UpdateThirstVisual(float current, float max)
    {
        if (thirstSlider != null)
        {
            thirstSlider.maxValue = max;
            thirstSlider.value = current;
        }

        float pct = max > 0 ? (current / max) : 0f;
        if (thirstText != null)
        {
            thirstText.text = $"{Mathf.RoundToInt(pct * 100f)}%";
        }

        bool isDehydrated = current <= 0.01f;
        bool isLow = pct <= warningThresholdPercent;

        if (isLow || isDehydrated)
        {
            if (warningIndicator != null)
                warningIndicator.SetActive(true);

            if (warningCoroutine == null && isActiveAndEnabled)
                warningCoroutine = StartCoroutine(WarningPulseRoutine());
        }
        else
        {
            if (warningIndicator != null)
                warningIndicator.SetActive(false);

            if (warningCoroutine != null)
            {
                StopCoroutine(warningCoroutine);
                warningCoroutine = null;
            }

            if (fillImage != null)
                fillImage.color = normalColor;
        }
    }

    private IEnumerator WarningPulseRoutine()
    {
        while (true)
        {
            float pingPong = Mathf.PingPong(Time.time * 4f, 1f);
            if (fillImage != null)
                fillImage.color = Color.Lerp(normalColor, warningColor, pingPong);

            if (warningIndicator != null)
            {
                var warnImg = warningIndicator.GetComponent<Graphic>();
                if (warnImg != null)
                    warnImg.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.3f, 1f, pingPong));
            }

            yield return null;
        }
    }
}

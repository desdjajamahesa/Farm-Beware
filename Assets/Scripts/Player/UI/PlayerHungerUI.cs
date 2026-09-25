using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Listener visual modern untuk indikator rasa lapar (hunger) pemain.
/// Mendukung label persentase TextMeshPro, warna oranye panggang hangat,
/// serta peringatan bahaya kelaparan (starvation alert pulse saat memicu penalti damage).
/// </summary>
public class PlayerHungerUI : MonoBehaviour
{
    [Header("Core References")]
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private Slider hungerSlider;
    [SerializeField] private TMP_Text hungerText;
    [SerializeField] private Image fillImage;
    [SerializeField] private Image iconImage;
    [SerializeField] private GameObject warningIndicator;

    [Header("Color & Styling")]
    [SerializeField] private Color normalColor = new Color(0.92f, 0.42f, 0.05f, 1f); // Warm Roast Orange
    [SerializeField] private Color warningColor = new Color(0.95f, 0.15f, 0.15f, 1f); // Starvation Red
    [SerializeField] [Range(0.05f, 0.3f)] private float warningThresholdPercent = 0.15f;

    private Coroutine warningCoroutine;

    private void Awake()
    {
        if (hungerSlider == null)
            hungerSlider = GetComponent<Slider>() ?? GetComponentInChildren<Slider>();

        if (hungerText == null)
            hungerText = GetComponentInChildren<TMP_Text>(true);

        if (fillImage == null && hungerSlider != null && hungerSlider.fillRect != null)
            fillImage = hungerSlider.fillRect.GetComponent<Image>();

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
            UpdateHungerVisual(playerStats.currentHunger, playerStats.maxHunger);
        }
    }

    private void OnEnable()
    {
        EnsurePlayerStatsBound();

        if (playerStats != null)
        {
            playerStats.OnHungerChanged += UpdateHungerVisual;
            UpdateHungerVisual(playerStats.currentHunger, playerStats.maxHunger);
        }
    }

    private void OnDisable()
    {
        if (playerStats != null)
            playerStats.OnHungerChanged -= UpdateHungerVisual;

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

    private void UpdateHungerVisual(float current, float max)
    {
        if (hungerSlider != null)
        {
            hungerSlider.maxValue = max;
            hungerSlider.value = current;
        }

        float pct = max > 0 ? (current / max) : 0f;
        if (hungerText != null)
        {
            hungerText.text = $"{Mathf.RoundToInt(pct * 100f)}%";
        }

        bool isStarving = current <= 0.01f;
        bool isLow = pct <= warningThresholdPercent;

        if (isLow || isStarving)
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

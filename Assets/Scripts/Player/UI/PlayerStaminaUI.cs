using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Listener visual modern untuk bar stamina pemain.
/// Mendukung label angka TextMeshPro, warna emas/amber hangat,
/// serta umpan balik kelelahan (exhaustion dimming + shake saat stamina habis).
/// </summary>
public class PlayerStaminaUI : MonoBehaviour
{
    [Header("Core References")]
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private Slider staminaSlider;
    [SerializeField] private TMP_Text staminaText;
    [SerializeField] private Image fillImage;
    [SerializeField] private Transform iconTransform;

    [Header("Color & Styling")]
    [SerializeField] private Color normalColor = new Color(0.96f, 0.62f, 0.04f, 1f); // Warm Amber Gold
    [SerializeField] private Color exhaustedColor = new Color(0.45f, 0.45f, 0.5f, 0.8f); // Slate Grey

    private bool wasExhausted = false;
    private Coroutine shakeCoroutine;
    private Vector3 originalPos;
    private RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
            originalPos = rectTransform.anchoredPosition;

        if (staminaSlider == null)
            staminaSlider = GetComponent<Slider>() ?? GetComponentInChildren<Slider>();

        if (staminaText == null)
            staminaText = GetComponentInChildren<TMP_Text>(true);

        if (fillImage == null && staminaSlider != null && staminaSlider.fillRect != null)
            fillImage = staminaSlider.fillRect.GetComponent<Image>();
    }

    private void Start()
    {
        EnsurePlayerStatsBound();

        if (playerStats != null)
        {
            UpdateStaminaVisual(playerStats.currentStamina, playerStats.maxStamina);
        }
    }

    private void OnEnable()
    {
        EnsurePlayerStatsBound();

        if (playerStats != null)
        {
            playerStats.OnStaminaChanged += UpdateStaminaVisual;
            UpdateStaminaVisual(playerStats.currentStamina, playerStats.maxStamina);
        }
    }

    private void OnDisable()
    {
        if (playerStats != null)
            playerStats.OnStaminaChanged -= UpdateStaminaVisual;

        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
            shakeCoroutine = null;
        }

        if (rectTransform != null)
            rectTransform.anchoredPosition = originalPos;
    }

    private void EnsurePlayerStatsBound()
    {
        if (playerStats == null)
            playerStats = FindFirstObjectByType<PlayerStats>();
    }

    private void UpdateStaminaVisual(float current, float max)
    {
        if (staminaSlider != null)
        {
            staminaSlider.maxValue = max;
            staminaSlider.value = current;
        }

        if (staminaText != null)
        {
            staminaText.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
        }

        bool isCurrentlyExhausted = current <= 0.1f;

        if (isCurrentlyExhausted && !wasExhausted)
        {
            wasExhausted = true;
            if (fillImage != null)
                fillImage.color = exhaustedColor;

            if (isActiveAndEnabled && shakeCoroutine == null)
                shakeCoroutine = StartCoroutine(ShakeRoutine());
        }
        else if (!isCurrentlyExhausted && wasExhausted)
        {
            wasExhausted = false;
            if (fillImage != null)
                fillImage.color = normalColor;
        }
        else if (!isCurrentlyExhausted && fillImage != null && fillImage.color != normalColor)
        {
            fillImage.color = normalColor;
        }
    }

    private IEnumerator ShakeRoutine()
    {
        if (rectTransform == null) yield break;

        float elapsed = 0f;
        float duration = 0.25f;
        float magnitude = 3f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float offsetX = Random.Range(-magnitude, magnitude);
            rectTransform.anchoredPosition = originalPos + new Vector3(offsetX, 0f, 0f);
            yield return null;
        }

        rectTransform.anchoredPosition = originalPos;
        shakeCoroutine = null;
    }
}

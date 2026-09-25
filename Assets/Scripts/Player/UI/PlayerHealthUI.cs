using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Listener visual modern & responsif untuk bar kesehatan pemain.
/// Mendukung Ghost Damage Bar (efek lag damage), label angka TextMeshPro,
/// kilatan saat sembuh (heal flash), dan denyut bahaya saat darah kritis.
/// </summary>
public class PlayerHealthUI : MonoBehaviour
{
    [Header("Core References")]
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Slider ghostSlider;
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private Image fillImage;
    [SerializeField] private Transform iconTransform;

    [Header("Ghost Bar Settings")]
    [SerializeField] private float ghostDelay = 0.35f;
    [SerializeField] private float ghostShrinkDuration = 0.45f;

    [Header("Color & Styling")]
    [SerializeField] private Color normalColor = new Color(0.86f, 0.15f, 0.15f, 1f); // Ruby Red
    [SerializeField] private Color healColor = new Color(0.2f, 0.95f, 0.35f, 1f);
    [SerializeField] private Color criticalColor = new Color(1f, 0.1f, 0.1f, 1f);
    [SerializeField] [Range(0.05f, 0.5f)] private float criticalThresholdPercent = 0.25f;

    private Coroutine ghostCoroutine;
    private Coroutine healFlashCoroutine;
    private Coroutine criticalPulseCoroutine;
    private Vector3 originalIconScale = Vector3.one;

    private void Awake()
    {
        if (healthSlider == null)
            healthSlider = GetComponent<Slider>() ?? GetComponentInChildren<Slider>();

        if (ghostSlider == null)
        {
            var sliders = GetComponentsInChildren<Slider>(true);
            foreach (var s in sliders)
            {
                if (s != healthSlider)
                {
                    ghostSlider = s;
                    break;
                }
            }
        }

        if (healthText == null)
            healthText = GetComponentInChildren<TMP_Text>(true);

        if (fillImage == null && healthSlider != null && healthSlider.fillRect != null)
            fillImage = healthSlider.fillRect.GetComponent<Image>();

        if (iconTransform != null)
            originalIconScale = iconTransform.localScale;
    }

    private void Start()
    {
        EnsurePlayerStatsBound();

        if (playerStats != null)
        {
            UpdateHealthVisual(playerStats.currentHealth, playerStats.maxHealth);
            if (ghostSlider != null)
            {
                ghostSlider.maxValue = playerStats.maxHealth;
                ghostSlider.value = playerStats.currentHealth;
            }
        }
    }

    private void OnEnable()
    {
        EnsurePlayerStatsBound();

        if (playerStats != null)
        {
            playerStats.OnHealthChanged += UpdateHealthVisual;
            playerStats.OnDamageTaken += HandleDamageTaken;
            playerStats.OnHealed += HandleHealed;

            UpdateHealthVisual(playerStats.currentHealth, playerStats.maxHealth);
            if (ghostSlider != null)
            {
                ghostSlider.maxValue = playerStats.maxHealth;
                ghostSlider.value = playerStats.currentHealth;
            }
        }
    }

    private void OnDisable()
    {
        if (playerStats != null)
        {
            playerStats.OnHealthChanged -= UpdateHealthVisual;
            playerStats.OnDamageTaken -= HandleDamageTaken;
            playerStats.OnHealed -= HandleHealed;
        }

        StopAllCoroutines();
        ghostCoroutine = null;
        healFlashCoroutine = null;
        criticalPulseCoroutine = null;
    }

    private void EnsurePlayerStatsBound()
    {
        if (playerStats == null)
            playerStats = FindFirstObjectByType<PlayerStats>();
    }

    private void UpdateHealthVisual(int current, int max)
    {
        if (healthSlider != null)
        {
            healthSlider.maxValue = max;
            healthSlider.value = current;
        }

        if (ghostSlider != null)
        {
            ghostSlider.maxValue = max;
            // Jika healing, sinkronkan ghost slider seketika ke atas
            if (current >= ghostSlider.value)
            {
                if (ghostCoroutine != null) StopCoroutine(ghostCoroutine);
                ghostSlider.value = current;
            }
        }

        if (healthText != null)
        {
            healthText.text = $"{current} / {max}";
        }

        // Cek kondisi kritis
        float ratio = max > 0 ? (float)current / max : 0f;
        if (ratio <= criticalThresholdPercent && current > 0)
        {
            if (criticalPulseCoroutine == null)
                criticalPulseCoroutine = StartCoroutine(CriticalPulseRoutine());
        }
        else
        {
            if (criticalPulseCoroutine != null)
            {
                StopCoroutine(criticalPulseCoroutine);
                criticalPulseCoroutine = null;
                if (fillImage != null && healFlashCoroutine == null)
                    fillImage.color = normalColor;
            }
        }
    }

    private void HandleDamageTaken(int damageAmount)
    {
        if (ghostSlider != null && isActiveAndEnabled)
        {
            if (ghostCoroutine != null) StopCoroutine(ghostCoroutine);
            ghostCoroutine = StartCoroutine(GhostBarRoutine(healthSlider != null ? healthSlider.value : 0f));
        }

        // Sedikit hentakan skala pada ikon darah
        if (iconTransform != null && isActiveAndEnabled)
        {
            StartCoroutine(IconBumpRoutine());
        }
    }

    private void HandleHealed(int healAmount)
    {
        if (fillImage != null && isActiveAndEnabled)
        {
            if (healFlashCoroutine != null) StopCoroutine(healFlashCoroutine);
            healFlashCoroutine = StartCoroutine(HealFlashRoutine());
        }
    }

    private IEnumerator GhostBarRoutine(float targetValue)
    {
        yield return new WaitForSeconds(ghostDelay);

        if (ghostSlider == null) yield break;

        float startVal = ghostSlider.value;
        float elapsed = 0f;

        while (elapsed < ghostShrinkDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / ghostShrinkDuration);
            // Smooth step lerp
            ghostSlider.value = Mathf.Lerp(startVal, targetValue, t * t);
            yield return null;
        }

        ghostSlider.value = targetValue;
        ghostCoroutine = null;
    }

    private IEnumerator HealFlashRoutine()
    {
        if (fillImage == null) yield break;

        fillImage.color = healColor;
        float elapsed = 0f;
        float dur = 0.35f;

        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            fillImage.color = Color.Lerp(healColor, normalColor, elapsed / dur);
            yield return null;
        }

        fillImage.color = normalColor;
        healFlashCoroutine = null;
    }

    private IEnumerator CriticalPulseRoutine()
    {
        while (true)
        {
            if (fillImage != null)
            {
                float pingPong = Mathf.PingPong(Time.time * 3.5f, 1f);
                fillImage.color = Color.Lerp(normalColor, criticalColor, pingPong);
            }
            yield return null;
        }
    }

    private IEnumerator IconBumpRoutine()
    {
        if (iconTransform == null) yield break;

        iconTransform.localScale = originalIconScale * 1.25f;
        float elapsed = 0f;
        float dur = 0.2f;

        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            iconTransform.localScale = Vector3.Lerp(originalIconScale * 1.25f, originalIconScale, elapsed / dur);
            yield return null;
        }

        iconTransform.localScale = originalIconScale;
    }
}
using UnityEngine;
using UnityEngine.UI;

// Listener visual murni (Data-Driven) untuk bar kesehatan pemain.
// Otomatis mencari referensi Slider & PlayerStats jika belum di-assign.
public class PlayerHealthUI : MonoBehaviour
{
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private Slider healthSlider;

    private void Awake()
    {
        if (healthSlider == null)
            healthSlider = GetComponent<Slider>() ?? GetComponentInChildren<Slider>();
    }

    private void Start()
    {
        EnsurePlayerStatsBound();

        if (playerStats != null && healthSlider != null)
        {
            UpdateHealthVisual(playerStats.currentHealth, playerStats.maxHealth);
        }
    }

    private void OnEnable()
    {
        EnsurePlayerStatsBound();

        if (playerStats != null)
        {
            playerStats.OnHealthChanged += UpdateHealthVisual;
            UpdateHealthVisual(playerStats.currentHealth, playerStats.maxHealth);
        }
    }

    private void OnDisable()
    {
        if (playerStats != null)
            playerStats.OnHealthChanged -= UpdateHealthVisual;
    }

    private void EnsurePlayerStatsBound()
    {
        if (playerStats == null)
            playerStats = FindFirstObjectByType<PlayerStats>();
    }

    private void UpdateHealthVisual(int current, int max)
    {
        if (healthSlider == null)
            return;

        healthSlider.maxValue = max;
        healthSlider.value = current;
    }
}
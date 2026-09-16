using UnityEngine;
using UnityEngine.UI;

// Listener visual untuk bar rasa lapar (hunger) pemain.
// Otomatis mencari referensi Slider & PlayerStats jika belum di-assign.
public class PlayerHungerUI : MonoBehaviour
{
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private Slider hungerSlider;

    private void Awake()
    {
        if (hungerSlider == null)
            hungerSlider = GetComponent<Slider>() ?? GetComponentInChildren<Slider>();
    }

    private void Start()
    {
        EnsurePlayerStatsBound();

        if (playerStats != null && hungerSlider != null)
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
    }

    private void EnsurePlayerStatsBound()
    {
        if (playerStats == null)
            playerStats = FindFirstObjectByType<PlayerStats>();
    }

    private void UpdateHungerVisual(float current, float max)
    {
        if (hungerSlider == null)
            return;

        hungerSlider.maxValue = max;
        hungerSlider.value = current;
    }
}

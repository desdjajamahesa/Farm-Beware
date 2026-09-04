using UnityEngine;
using UnityEngine.UI;

public class PlayerHungerUI : MonoBehaviour
{
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private Slider hungerSlider;

    private void Start()
    {
        if (playerStats == null)
            playerStats = FindFirstObjectByType<PlayerStats>();

        if (playerStats != null && hungerSlider != null)
        {
            UpdateHungerVisual(playerStats.currentHunger, playerStats.maxHunger);
        }
    }

    private void OnEnable()
    {
        if (playerStats == null)
            playerStats = FindFirstObjectByType<PlayerStats>();

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

    private void UpdateHungerVisual(float current, float max)
    {
        if (hungerSlider == null)
            return;

        hungerSlider.maxValue = max;
        hungerSlider.value = current;
    }
}

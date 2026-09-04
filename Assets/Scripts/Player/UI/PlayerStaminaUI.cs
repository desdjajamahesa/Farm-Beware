using UnityEngine;
using UnityEngine.UI;

public class PlayerStaminaUI : MonoBehaviour
{
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private Slider staminaSlider;

    private void Start()
    {
        if (playerStats == null)
            playerStats = FindFirstObjectByType<PlayerStats>();

        if (playerStats != null && staminaSlider != null)
        {
            UpdateStaminaVisual(playerStats.currentStamina, playerStats.maxStamina);
        }
    }

    private void OnEnable()
    {
        if (playerStats == null)
            playerStats = FindFirstObjectByType<PlayerStats>();

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
    }

    private void UpdateStaminaVisual(float current, float max)
    {
        if (staminaSlider == null)
            return;

        staminaSlider.maxValue = max;
        staminaSlider.value = current;
    }
}

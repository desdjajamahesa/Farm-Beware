using UnityEngine;
using UnityEngine.UI;

public class PlayerThirstUI : MonoBehaviour
{
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private Slider thirstSlider;

    private void Start()
    {
        if (playerStats == null)
            playerStats = FindFirstObjectByType<PlayerStats>();

        if (playerStats != null && thirstSlider != null)
        {
            UpdateThirstVisual(playerStats.currentThirst, playerStats.maxThirst);
        }
    }

    private void OnEnable()
    {
        if (playerStats == null)
            playerStats = FindFirstObjectByType<PlayerStats>();

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
    }

    private void UpdateThirstVisual(float current, float max)
    {
        if (thirstSlider == null)
            return;

        thirstSlider.maxValue = max;
        thirstSlider.value = current;
    }
}

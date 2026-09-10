using UnityEngine;
using UnityEngine.UI;

// Listener visual untuk bar rasa haus (thirst) pemain.
// Otomatis mencari referensi Slider & PlayerStats jika belum di-assign.
public class PlayerThirstUI : MonoBehaviour
{
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private Slider thirstSlider;

    private void Awake()
    {
        if (thirstSlider == null)
            thirstSlider = GetComponent<Slider>() ?? GetComponentInChildren<Slider>();
    }

    private void Start()
    {
        EnsurePlayerStatsBound();

        if (playerStats != null && thirstSlider != null)
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
    }

    private void EnsurePlayerStatsBound()
    {
        if (playerStats == null)
            playerStats = FindFirstObjectByType<PlayerStats>();
    }

    private void UpdateThirstVisual(float current, float max)
    {
        if (thirstSlider == null)
            return;

        thirstSlider.maxValue = max;
        thirstSlider.value = current;
    }
}

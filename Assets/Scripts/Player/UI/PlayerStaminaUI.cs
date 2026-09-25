using UnityEngine;
using UnityEngine.UI;

// Listener visual untuk bar stamina pemain.
// Otomatis mencari referensi Slider & PlayerStats jika belum di-assign.
public class PlayerStaminaUI : MonoBehaviour
{
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private Slider staminaSlider;

    private void Awake()
    {
        if (staminaSlider == null)
            staminaSlider = GetComponent<Slider>() ?? GetComponentInChildren<Slider>();
    }

    private void Start()
    {
        EnsurePlayerStatsBound();

        if (playerStats != null && staminaSlider != null)
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
    }

    private void EnsurePlayerStatsBound()
    {
        if (playerStats == null)
            playerStats = FindFirstObjectByType<PlayerStats>();
    }

    private void UpdateStaminaVisual(float current, float max)
    {
        if (staminaSlider == null)
            return;

        staminaSlider.maxValue = max;
        staminaSlider.value = current;
    }
}

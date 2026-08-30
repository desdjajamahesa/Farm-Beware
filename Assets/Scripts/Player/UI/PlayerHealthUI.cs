using UnityEngine;
using UnityEngine.UI;

// Listener visual murni (Data-Driven) untuk bar kesehatan pemain.
// Tidak menghitung HP apa pun; hanya bereaksi terhadap event OnHealthChanged
// dari PlayerStats untuk memperbarui tampilan Slider.
public class PlayerHealthUI : MonoBehaviour
{
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private Slider healthSlider;

    private void OnEnable()
    {
        if (playerStats != null)
            playerStats.OnHealthChanged += UpdateHealthVisual;
    }

    private void OnDisable()
    {
        if (playerStats != null)
            playerStats.OnHealthChanged -= UpdateHealthVisual;
    }

    private void UpdateHealthVisual(int current, int max)
    {
        if (healthSlider == null)
            return;

        healthSlider.maxValue = max;
        healthSlider.value = current;
    }
}
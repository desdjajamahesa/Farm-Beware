using System;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    public event Action<int, int> OnHealthChanged;

    public int maxHealth = 100;
    public int currentHealth;

    void Start()
    {
        currentHealth = maxHealth - 50;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void Heal(int amount)
    {
        currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        Debug.Log("HP Pemain sekarang: " + currentHealth + "/" + maxHealth);
    }

    public void TakeDamage(int amount)
    {
        currentHealth = Mathf.Clamp(currentHealth - amount, 0, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        Debug.Log(string.Format("Pemain terkena damage {0}. HP: {1}/{2}", amount, currentHealth, maxHealth));
    }
}
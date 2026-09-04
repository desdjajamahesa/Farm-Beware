using System;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    // Event Darah, Stamina, Hunger & Thirst
    public event Action<int, int> OnHealthChanged;
    public event Action<float, float> OnStaminaChanged;
    public event Action<float, float> OnHungerChanged;
    public event Action<float, float> OnThirstChanged;

    [Header("Health")]
    public int maxHealth = 100;
    public int currentHealth;

    [Header("Stamina")]
    public float maxStamina = 120f;
    public float currentStamina;
    public float staminaRegenRate = 20f; 
    public float staminaDrainRate = 20f;
    public float staminaRegenDelay = 1.2f;

    [Header("Hunger")]
    public float maxHunger = 100f;
    public float currentHunger;
    public float hungerDrainRate = 0.5f; // Pengurangan lapar per detik

    [Header("Thirst")]
    public float maxThirst = 100f;
    public float currentThirst;
    public float thirstDrainRate = 0.8f; // Pengurangan haus per detik

    [Header("Starvation & Dehydration Damage")]
    public bool takeDamageWhenEmpty = true;
    public int emptyPenaltyDamage = 5;
    public float damageInterval = 3f;
    private float nextDamageTime;

    private float lastStaminaUseTime;

    public bool IsExhausted => currentStamina <= 0.1f;
    public bool IsStarving => currentHunger <= 0.01f;
    public bool IsDehydrated => currentThirst <= 0.01f;

    void Start()
    {
        currentHealth = maxHealth;
        currentStamina = maxStamina;
        currentHunger = maxHunger;
        currentThirst = maxThirst;

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnStaminaChanged?.Invoke(currentStamina, maxStamina);
        OnHungerChanged?.Invoke(currentHunger, maxHunger);
        OnThirstChanged?.Invoke(currentThirst, maxThirst);
    }

    void Update()
    {
        // Regenerasi stamina otomatis setelah delay (hanya jika tidak kelaparan/kehausan parah)
        if (Time.time >= lastStaminaUseTime + staminaRegenDelay && currentStamina < maxStamina)
        {
            RegenStamina(staminaRegenRate * Time.deltaTime);
        }

        // Pengurangan Hunger & Thirst seiring waktu
        DrainHungerAndThirst(Time.deltaTime);

        // Penalti damage jika kelaparan atau kehausan habis
        HandleEmptyPenalty();

        // Testing input menggunakan New Input System 
        if (UnityEngine.InputSystem.Keyboard.current != null)
        {
            if (UnityEngine.InputSystem.Keyboard.current.kKey.wasPressedThisFrame)
            {
                TakeDamage(20);
            }

            if (UnityEngine.InputSystem.Keyboard.current.hKey.wasPressedThisFrame)
            {
                Heal(20);
            }

            if (UnityEngine.InputSystem.Keyboard.current.jKey.wasPressedThisFrame)
            {
                UseStamina(25f);
            }
        }
    }

    private void DrainHungerAndThirst(float deltaTime)
    {
        if (currentHunger > 0)
        {
            currentHunger = Mathf.Clamp(currentHunger - hungerDrainRate * deltaTime, 0, maxHunger);
            OnHungerChanged?.Invoke(currentHunger, maxHunger);
        }

        if (currentThirst > 0)
        {
            currentThirst = Mathf.Clamp(currentThirst - thirstDrainRate * deltaTime, 0, maxThirst);
            OnThirstChanged?.Invoke(currentThirst, maxThirst);
        }
    }

    private void HandleEmptyPenalty()
    {
        if (takeDamageWhenEmpty && (IsStarving || IsDehydrated))
        {
            if (Time.time >= nextDamageTime)
            {
                TakeDamage(emptyPenaltyDamage);
                nextDamageTime = Time.time + damageInterval;
            }
        }
    }

    // --- HEALTH METHODS ---
    public void Heal(int amount)
    {
        currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void TakeDamage(int amount)
    {
        currentHealth = Mathf.Clamp(currentHealth - amount, 0, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    // --- STAMINA METHODS ---
    public bool UseStamina(float amount)
    {
        if (currentStamina > 0)
        {
            currentStamina = Mathf.Clamp(currentStamina - amount, 0, maxStamina);
            lastStaminaUseTime = Time.time;
            OnStaminaChanged?.Invoke(currentStamina, maxStamina);
            return currentStamina > 0;
        }
        return false; // Stamina habis
    }

    public void RegenStamina(float amount)
    {
        currentStamina = Mathf.Clamp(currentStamina + amount, 0, maxStamina);
        OnStaminaChanged?.Invoke(currentStamina, maxStamina);
    }

    // --- HUNGER & THIRST METHODS ---
    public void Eat(float amount)
    {
        currentHunger = Mathf.Clamp(currentHunger + amount, 0, maxHunger);
        OnHungerChanged?.Invoke(currentHunger, maxHunger);
    }

    public void Drink(float amount)
    {
        currentThirst = Mathf.Clamp(currentThirst + amount, 0, maxThirst);
        OnThirstChanged?.Invoke(currentThirst, maxThirst);
    }
}

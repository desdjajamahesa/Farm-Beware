using System;
using UnityEngine;

public class PlayerStats : MonoBehaviour, FeaturesCombat.IDamageable
{
    // Event Darah, Stamina, Hunger & Thirst
    public event Action<int, int> OnHealthChanged;
    public event Action<int> OnDamageTaken;
    public event Action<int> OnHealed;
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

    [Header("Debug & Cheats")]
    public bool isGodMode = false;

    private float lastStaminaUseTime;

    private int baseMaxHealth;
    private float baseMaxStamina;
    private float baseStaminaRegenRate;

    public bool IsExhausted => currentStamina <= 0.1f;
    public bool IsStarving => currentHunger <= 0.01f;
    public bool IsDehydrated => currentThirst <= 0.01f;

    void Awake()
    {
        baseMaxHealth = maxHealth;
        baseMaxStamina = maxStamina;
        baseStaminaRegenRate = staminaRegenRate;
    }

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

    public void ApplyBuffModifiers(float healthModPercent, float staminaModPercent, float staminaRegenModPercent)
    {
        int newMaxHealth = Mathf.Max(1, Mathf.RoundToInt(baseMaxHealth * (1f + healthModPercent)));
        float newMaxStamina = Mathf.Max(1f, baseMaxStamina * (1f + staminaModPercent));
        staminaRegenRate = baseStaminaRegenRate * (1f + staminaRegenModPercent);

        if (newMaxHealth != maxHealth)
        {
            maxHealth = newMaxHealth;
            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        if (Mathf.Abs(newMaxStamina - maxStamina) > 0.01f)
        {
            maxStamina = newMaxStamina;
            currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina);
            OnStaminaChanged?.Invoke(currentStamina, maxStamina);
        }
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

            if (UnityEngine.InputSystem.Keyboard.current.gKey.wasPressedThisFrame)
            {
                ToggleGodMode();
            }
        }
    }

    public void ToggleGodMode()
    {
        isGodMode = !isGodMode;
        if (isGodMode)
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

        string msg = isGodMode ? "🛡️ GOD MODE: ON (Invincible + 1-Hit Kill)" : "🛡️ GOD MODE: OFF";
        Color col = isGodMode ? new Color(1f, 0.85f, 0.15f) : new Color(0.7f, 0.7f, 0.7f);

        if (PlayerUI.FloatingCombatTextManager.Instance != null)
        {
            PlayerUI.FloatingCombatTextManager.Instance.SpawnText(
                transform.position + Vector3.up * 2f,
                msg,
                col);
        }
        Debug.Log($"[PlayerStats] {msg}");
    }

    private float lastNotifiedHunger = -999f;
    private float lastNotifiedThirst = -999f;

    private void DrainHungerAndThirst(float deltaTime)
    {
        if (isGodMode)
        {
            currentHunger = maxHunger;
            currentThirst = maxThirst;
            return;
        }
        if (currentHunger > 0)
        {
            currentHunger = Mathf.Clamp(currentHunger - hungerDrainRate * deltaTime, 0, maxHunger);
            if (Mathf.Abs(currentHunger - lastNotifiedHunger) >= 0.2f || currentHunger <= 0)
            {
                lastNotifiedHunger = currentHunger;
                OnHungerChanged?.Invoke(currentHunger, maxHunger);
            }
        }

        if (currentThirst > 0)
        {
            currentThirst = Mathf.Clamp(currentThirst - thirstDrainRate * deltaTime, 0, maxThirst);
            if (Mathf.Abs(currentThirst - lastNotifiedThirst) >= 0.2f || currentThirst <= 0)
            {
                lastNotifiedThirst = currentThirst;
                OnThirstChanged?.Invoke(currentThirst, maxThirst);
            }
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
        if (amount <= 0) return;
        int prev = currentHealth;
        currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);
        int actualHealed = currentHealth - prev;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        if (actualHealed > 0)
            OnHealed?.Invoke(actualHealed);
    }

    public void TakeDamage(int amount)
    {
        if (isGodMode || amount <= 0) return;

        var weaponUpgrade = FeaturesWorkbench.PlayerWeaponUpgradeState.Instance;
        if (weaponUpgrade != null && weaponUpgrade.taroPathUnlocked)
        {
            // Taro Path: +15 Armor (Damage reduction, minimal 1 damage jika serangan terkena)
            amount = Mathf.Max(1, amount - 15);
        }

        currentHealth = Mathf.Clamp(currentHealth - amount, 0, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnDamageTaken?.Invoke(amount);
    }

    public void TakeDamage(int damage, Vector3 hitPoint, Vector3 hitDirection)
    {
        TakeDamage(damage);
    }

    public bool IsDead => currentHealth <= 0;

    // --- STAMINA METHODS ---
    public bool UseStamina(float amount)
    {
        if (isGodMode)
        {
            currentStamina = maxStamina;
            OnStaminaChanged?.Invoke(currentStamina, maxStamina);
            return true;
        }

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
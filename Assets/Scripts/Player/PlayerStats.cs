using System;
using UnityEngine;
using FarmBeware.Core.Runtime;

public class PlayerStats : MonoBehaviour, FeaturesCombat.IDamageable, IPlayerDamageNotifier
{
    // Event Darah, Stamina, Hunger & Thirst
    public event Action<int, int> OnHealthChanged;
    public event Action<int> OnDamageTaken;
    public event Action<int> OnHealed;
    public event Action OnPlayerDied;
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

    public FeaturesCombat.Core.PureLogic.DefenseEvaluator DefenseEvaluator { get; } = new FeaturesCombat.Core.PureLogic.DefenseEvaluator();

    [Header("Visual Feedback Saat Terkena Hit")]
    [Tooltip("Warna kedipan merah saat karakter pemain terkena hit monster.")]
    [SerializeField] private Color hurtFlashColor = new Color(1f, 0.22f, 0.22f, 1f);
    [Tooltip("Durasi kedipan merah tubuh pemain (detik).")]
    [SerializeField] private float hurtFlashDuration = 0.12f;

    private static readonly int HitFlashAmountPropertyId = Shader.PropertyToID("_HitFlashAmount");
    private static readonly int HitFlashColorPropertyId = Shader.PropertyToID("_HitFlashColor");
    private static readonly int BaseColorPropertyId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorPropertyId = Shader.PropertyToID("_Color");

    private Renderer[] playerRenderers;
    private MaterialPropertyBlock hurtPropBlock;
    private Coroutine hurtFlashCoroutine;
    private Animator playerAnimator;

    void Awake()
    {
        baseMaxHealth = maxHealth;
        baseMaxStamina = maxStamina;
        baseStaminaRegenRate = staminaRegenRate;

        playerRenderers = GetComponentsInChildren<Renderer>(true);
        hurtPropBlock = new MaterialPropertyBlock();
        playerAnimator = GetComponentInChildren<Animator>();

        ServiceLocator.Register<IPlayerDamageNotifier>(this);

        if (GetComponent<PlayerRespawnController>() == null)
        {
            gameObject.AddComponent<PlayerRespawnController>();
        }
    }

    private void OnDestroy()
    {
        ServiceLocator.Unregister<IPlayerDamageNotifier>();
    }

    private bool isStatsRestored = false;
    private bool isDeadHandled = false;

    void Start()
    {
        if (!isStatsRestored)
        {
            currentHealth = maxHealth;
            currentStamina = maxStamina;
            currentHunger = maxHunger;
            currentThirst = maxThirst;
        }

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
        DefenseEvaluator.Tick(Time.deltaTime);

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
        if (Time.timeScale > 0f && UnityEngine.InputSystem.Keyboard.current != null)
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
        if (currentHealth > 0) isDeadHandled = false;
        int actualHealed = currentHealth - prev;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        if (actualHealed > 0)
            OnHealed?.Invoke(actualHealed);
    }

    public FeaturesCombat.Core.PureLogic.CombatStatModifiers GetCombatStatModifiers()
    {
        var buffMgr = GetComponent<PlayerBuffManager>();
        bool hasAgility = buffMgr != null && (buffMgr.HasBuffKeyword("Agility") || buffMgr.HasBuffKeyword("Surge"));
        bool hasIronRoot = buffMgr != null && (buffMgr.HasBuffKeyword("Iron Root") || buffMgr.HasBuffKeyword("Stance"));
        bool hasBerserker = buffMgr != null && (buffMgr.HasBuffKeyword("Berserker") || buffMgr.HasBuffKeyword("Smite"));
        bool hasReflective = buffMgr != null && (buffMgr.HasBuffKeyword("Reflective") || buffMgr.HasBuffKeyword("Shell") || buffMgr.HasBuffKeyword("Deflect"));

        var weaponUpgrade = FeaturesWorkbench.PlayerWeaponUpgradeState.Instance;
        bool sweetPotato = weaponUpgrade != null && weaponUpgrade.sweetPotatoPathUnlocked;
        bool taro = weaponUpgrade != null && weaponUpgrade.taroPathUnlocked;

        return FeaturesCombat.Core.PureLogic.CombatStatModifiers.Evaluate(
            hasAgility, hasIronRoot, hasBerserker, hasReflective, sweetPotato, taro);
    }

    public void TakeDamage(int amount)
    {
        if (isGodMode || amount <= 0 || currentHealth <= 0 || isDeadHandled) return;

        var mods = GetCombatStatModifiers();

        // 0. Defense Evaluator: Check precision parry and dodge roll i-frames
        if (DefenseEvaluator != null && DefenseEvaluator.EvaluateIncomingDamage(amount, false, out bool parried, out bool dodged))
        {
            if (parried)
            {
                if (PlayerUI.FloatingCombatTextManager.Instance != null)
                {
                    PlayerUI.FloatingCombatTextManager.Instance.SpawnText(
                        transform.position + Vector3.up * 2f,
                        "PARRY!",
                        new Color(1f, 0.85f, 0.1f));
                }
                FeaturesCombat.Adapters.TraumaCameraShake.Instance?.AddTrauma(0.35f);
                FeaturesCombat.Adapters.HitstopCoordinator.Instance?.RegisterHitstop(gameObject, 0.12f);
                return;
            }

            if (dodged)
            {
                if (PlayerUI.FloatingCombatTextManager.Instance != null)
                {
                    PlayerUI.FloatingCombatTextManager.Instance.SpawnText(
                        transform.position + Vector3.up * 1.8f,
                        "DODGE!",
                        new Color(0.4f, 0.9f, 1f));
                }
                return;
            }
        }

        // Apply Stat Modifier damage mitigation ratio (e.g. Iron Root Stance 25%, Taro Path 10%)
        if (mods.DamageMitigationRatio > 0f)
        {
            amount = Mathf.Max(1, Mathf.RoundToInt(amount * (1f - mods.DamageMitigationRatio)));
        }

        var weaponUpgrade = FeaturesWorkbench.PlayerWeaponUpgradeState.Instance;
        if (weaponUpgrade != null && weaponUpgrade.taroPathUnlocked)
        {
            // Taro Path: +15 Armor (Damage reduction, minimal 1 damage jika serangan terkena)
            amount = Mathf.Max(1, amount - 15);
        }

        currentHealth = Mathf.Clamp(currentHealth - amount, 0, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnDamageTaken?.Invoke(amount);

        if (currentHealth <= 0 && !isDeadHandled)
        {
            isDeadHandled = true;
            OnPlayerDied?.Invoke();
        }

        // 1. Visual flash merah pada tubuh karakter pemain (agar jelas bahwa pemain yang terkena luka)
        TriggerHurtFlash();

        // 2. Camera trauma shake saat pemain menerima damage
        FeaturesCombat.Adapters.TraumaCameraShake.Instance?.AddTrauma(0.20f);
        if (FeaturesCamera.IsometricCameraController.Instance != null)
        {
            FeaturesCamera.IsometricCameraController.Instance.TriggerShake(0.14f, 0.20f);
        }

        // 3. Reaksi tubuh terhuyung mundur jika menerima luka berat (>= 20 HP), sedang tidak menyerang, dan tidak punya Super Armor
        if (amount >= 20 && !mods.HasPassiveSuperArmor)
        {
            var pc = GetComponent<PlayerControl>();
            if (pc != null && !pc.IsAttacking && playerAnimator != null)
            {
                playerAnimator.ResetTrigger("HitReact");
                playerAnimator.SetTrigger("HitReact");
            }
        }
    }

    private void TriggerHurtFlash()
    {
        if (playerRenderers == null || playerRenderers.Length == 0)
        {
            playerRenderers = GetComponentsInChildren<Renderer>(true);
        }

        if (playerRenderers == null || playerRenderers.Length == 0) return;

        if (hurtFlashCoroutine != null)
            StopCoroutine(hurtFlashCoroutine);

        if (isActiveAndEnabled)
            hurtFlashCoroutine = StartCoroutine(RoutineHurtFlash());
    }

    private System.Collections.IEnumerator RoutineHurtFlash()
    {
        if (hurtPropBlock == null) hurtPropBlock = new MaterialPropertyBlock();

        hurtPropBlock.SetFloat(HitFlashAmountPropertyId, 1.0f);
        hurtPropBlock.SetColor(HitFlashColorPropertyId, hurtFlashColor);
        hurtPropBlock.SetColor(BaseColorPropertyId, hurtFlashColor);
        hurtPropBlock.SetColor(ColorPropertyId, hurtFlashColor);

        foreach (var r in playerRenderers)
        {
            if (r != null && !(r is ParticleSystemRenderer) && !(r is LineRenderer))
            {
                r.SetPropertyBlock(hurtPropBlock);
            }
        }

        yield return new WaitForSeconds(hurtFlashDuration);

        foreach (var r in playerRenderers)
        {
            if (r != null && !(r is ParticleSystemRenderer) && !(r is LineRenderer))
            {
                r.SetPropertyBlock(null);
            }
        }

        hurtFlashCoroutine = null;
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

    /// <summary>
    /// Restores all player vital stats at once (used by Save/Load system).
    /// </summary>
    public void RestoreStats(int hp, float stamina, float hunger, float thirst)
    {
        isStatsRestored = true;
        currentHealth = Mathf.Clamp(hp, 1, maxHealth);
        if (currentHealth > 0) isDeadHandled = false;
        currentStamina = Mathf.Clamp(stamina, 0f, maxStamina);
        currentHunger = Mathf.Clamp(hunger, 0f, maxHunger);
        currentThirst = Mathf.Clamp(thirst, 0f, maxThirst);
        lastNotifiedHunger = currentHunger;
        lastNotifiedThirst = currentThirst;

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnStaminaChanged?.Invoke(currentStamina, maxStamina);
        OnHungerChanged?.Invoke(currentHunger, maxHunger);
        OnThirstChanged?.Invoke(currentThirst, maxThirst);
    }

    private void OnDestroy()
    {
        ServiceLocator.Unregister<IPlayerDamageNotifier>();
    }
}
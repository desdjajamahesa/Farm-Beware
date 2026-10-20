using System;
using System.Collections.Generic;
using UnityEngine;
using FarmBeware.Core.Runtime;
using FeaturesTime;

public class PlayerBuffManager : MonoBehaviour
{
    [Serializable]
    public class ActiveBuff
    {
        public BuffEffectData data;
        public float remainingDuration;
        public float tickIntervalTimer;

        public ActiveBuff(BuffEffectData buff)
        {
            data = buff;
            remainingDuration = buff.duration;
            tickIntervalTimer = 1f; // Default 1 detik per tick untuk efek periodik
        }
    }

    public event Action OnBuffsChanged;

    [Header("Runtime Buffs")]
    [SerializeField] private List<ActiveBuff> activeBuffs = new List<ActiveBuff>();

    private PlayerStats playerStats;
    public PlayerStats PlayerStatsRef => playerStats != null ? playerStats : (playerStats = GetComponent<PlayerStats>());

    public IReadOnlyList<ActiveBuff> ActiveBuffs => activeBuffs;

    private void Awake()
    {
        if (playerStats == null)
            playerStats = GetComponent<PlayerStats>();
    }

    private void Start()
    {
        SubscribeTimeEvents();
    }

    private void OnEnable()
    {
        SubscribeTimeEvents();
    }

    private void OnDisable()
    {
        UnsubscribeTimeEvents();
    }

    private void SubscribeTimeEvents()
    {
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.OnDayChanged -= HandleDayChanged;
            TimeManager.Instance.OnDayChanged += HandleDayChanged;
        }
        if (DayNightTimeManager.Instance != null)
        {
            DayNightTimeManager.Instance.OnDayChanged -= HandleDayChanged;
            DayNightTimeManager.Instance.OnDayChanged += HandleDayChanged;
        }
        var timeService = ServiceLocator.Resolve<ITimeService>();
        if (timeService != null)
        {
            timeService.OnDayChanged -= HandleDayChanged;
            timeService.OnDayChanged += HandleDayChanged;
        }
    }

    private void UnsubscribeTimeEvents()
    {
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.OnDayChanged -= HandleDayChanged;
        }
        if (DayNightTimeManager.Instance != null)
        {
            DayNightTimeManager.Instance.OnDayChanged -= HandleDayChanged;
        }
        var timeService = ServiceLocator.Resolve<ITimeService>();
        if (timeService != null)
        {
            timeService.OnDayChanged -= HandleDayChanged;
        }
    }

    private void HandleDayChanged(int newDay)
    {
        ClearAllBuffs();
    }

    /// <summary>
    /// Menghapus seluruh buff aktif seketika saat pemain tidur atau pergantian hari,
    /// mengembalikan modifier status player ke nilai dasar, dan mengosongkan HUD buff.
    /// </summary>
    public void ClearAllBuffs()
    {
        if (activeBuffs == null || activeBuffs.Count == 0) return;

        activeBuffs.Clear();
        ApplyBuffModifiersToPlayer();
        OnBuffsChanged?.Invoke();
        Debug.Log("[PlayerBuffManager] ☀️ All food buffs have been cleared upon sleeping. Player wakes up fresh!");
    }

    [Header("Fast-Forward Time Integration")]
    [Tooltip("Pengali waktu kustom untuk keperluan testing/override (nilai <= 0 mengabaikan override).")]
    [SerializeField] private float timeMultiplierOverride = -1f;

    public float TimeMultiplierOverride
    {
        get => timeMultiplierOverride;
        set => timeMultiplierOverride = value;
    }

    /// <summary>
    /// Mengambil pengali kecepatan waktu yang aktif (mis. mode x8 Fast-Forward via Shortcut 0).
    /// </summary>
    public float GetActiveTimeMultiplier()
    {
        if (timeMultiplierOverride > 0f)
            return timeMultiplierOverride;

        if (FeaturesTime.DayNightTimeManager.Instance != null)
        {
            return FeaturesTime.DayNightTimeManager.Instance.FastForwardMultiplier;
        }

        var dnt = FindFirstObjectByType<FeaturesTime.DayNightTimeManager>();
        if (dnt != null)
        {
            return dnt.FastForwardMultiplier;
        }

        return 1.0f;
    }

    private void Update()
    {
        TickBuffs(Time.deltaTime);
    }

    /// <summary>
    /// Memperbarui durasi buff aktif berdasarkan deltaTime tertentu (mendukung percepatan waktu x8 dan pengujian unit).
    /// </summary>
    public void TickBuffs(float deltaSeconds)
    {
        if (activeBuffs.Count == 0) return;

        bool changed = false;
        float dt = deltaSeconds * GetActiveTimeMultiplier();

        for (int i = activeBuffs.Count - 1; i >= 0; i--)
        {
            var buff = activeBuffs[i];
            buff.remainingDuration -= dt;

            // Handle periodic tick (HealthRegenTick)
            if (buff.data != null && buff.data.buffType == BuffType.HealthRegenTick)
            {
                buff.tickIntervalTimer -= dt;
                while (buff.tickIntervalTimer <= 0f)
                {
                    buff.tickIntervalTimer += 1f;
                    var ps = PlayerStatsRef;
                    if (ps != null)
                    {
                        int healPerTick = Mathf.RoundToInt(buff.data.value);
                        ps.Heal(healPerTick);
                    }
                }
            }

            // Cek kedaluwarsa
            if (buff.remainingDuration <= 0f)
            {
                activeBuffs.RemoveAt(i);
                changed = true;
            }
        }

        if (changed)
        {
            ApplyBuffModifiersToPlayer();
            OnBuffsChanged?.Invoke();
        }
    }

    public void ApplyBuff(BuffEffectData newBuff)
    {
        if (newBuff == null) return;

        // Cek apakah buff serupa sudah ada
        var existing = activeBuffs.Find(b => b.data.buffType == newBuff.buffType && b.data.buffName == newBuff.buffName);
        if (existing != null)
        {
            // Refresh durasi dan ambil nilai tertinggi
            existing.remainingDuration = Mathf.Max(existing.remainingDuration, newBuff.duration);
            existing.data.value = Mathf.Max(existing.data.value, newBuff.value);
        }
        else
        {
            activeBuffs.Add(new ActiveBuff(newBuff));
        }

        ApplyBuffModifiersToPlayer();
        OnBuffsChanged?.Invoke();
    }

    public void ApplyBuffs(IEnumerable<BuffEffectData> buffs)
    {
        if (buffs == null) return;

        foreach (var buff in buffs)
        {
            ApplyBuff(buff);
        }
    }

    public bool HasBuff(string name)
    {
        if (string.IsNullOrEmpty(name)) return false;
        for (int i = 0; i < activeBuffs.Count; i++)
        {
            if (activeBuffs[i].data != null && 
                string.Equals(activeBuffs[i].data.buffName, name, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    public bool HasBuffKeyword(string keyword)
    {
        if (string.IsNullOrEmpty(keyword)) return false;
        for (int i = 0; i < activeBuffs.Count; i++)
        {
            if (activeBuffs[i].data != null && 
                !string.IsNullOrEmpty(activeBuffs[i].data.buffName) &&
                activeBuffs[i].data.buffName.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }
        return false;
    }

    public float GetTotalModifier(BuffType type)
    {
        float total = 0f;
        for (int i = 0; i < activeBuffs.Count; i++)
        {
            if (activeBuffs[i].data.buffType == type)
            {
                total += activeBuffs[i].data.value;
            }
        }
        return total;
    }

    public float GetSpeedMultiplier()
    {
        return 1f + GetTotalModifier(BuffType.MoveSpeedPercent);
    }

    public float GetAttackDamageMultiplier()
    {
        return 1f + GetTotalModifier(BuffType.AttackDamagePercent);
    }

    public float GetAttackSpeedMultiplier()
    {
        return 1f + GetTotalModifier(BuffType.AttackSpeedPercent);
    }

    private void ApplyBuffModifiersToPlayer()
    {
        var ps = PlayerStatsRef;
        if (ps != null)
        {
            ps.ApplyBuffModifiers(
                GetTotalModifier(BuffType.MaxHealthPercent),
                GetTotalModifier(BuffType.MaxStaminaPercent),
                GetTotalModifier(BuffType.StaminaRegenPercent)
            );
        }
    }
}

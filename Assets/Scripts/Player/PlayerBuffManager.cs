using System;
using System.Collections.Generic;
using UnityEngine;

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

    public IReadOnlyList<ActiveBuff> ActiveBuffs => activeBuffs;

    private void Awake()
    {
        playerStats = GetComponent<PlayerStats>();
    }

    private void Update()
    {
        if (activeBuffs.Count == 0) return;

        bool changed = false;
        float dt = Time.deltaTime;

        for (int i = activeBuffs.Count - 1; i >= 0; i--)
        {
            var buff = activeBuffs[i];
            buff.remainingDuration -= dt;

            // Handle periodic tick (HealthRegenTick)
            if (buff.data.buffType == BuffType.HealthRegenTick)
            {
                buff.tickIntervalTimer -= dt;
                if (buff.tickIntervalTimer <= 0f)
                {
                    buff.tickIntervalTimer = 1f;
                    if (playerStats != null)
                    {
                        int healPerTick = Mathf.RoundToInt(buff.data.value);
                        playerStats.Heal(healPerTick);
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
        if (playerStats != null)
        {
            playerStats.ApplyBuffModifiers(
                GetTotalModifier(BuffType.MaxHealthPercent),
                GetTotalModifier(BuffType.MaxStaminaPercent),
                GetTotalModifier(BuffType.StaminaRegenPercent)
            );
        }
    }
}

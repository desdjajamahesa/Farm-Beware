using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Decoupled contract for applying food buff effects without SendMessage.
/// </summary>
public interface IFoodBuffApplier
{
    void ApplyBuffs(IEnumerable<BuffEffectData> buffs);
}

public enum BuffType
{
    MaxHealthPercent,       // Max HP percentage bonus (+0.10 = +10%)
    MaxStaminaPercent,      // Max Stamina percentage bonus (+0.10 = +10%)
    HealthRegenTick,        // Flat HP regeneration per second / tick (+4 = +4 HP/tick)
    HealthRegenPercent,     // HP regeneration percentage bonus
    StaminaRegenPercent,    // Stamina regeneration rate percentage bonus (+0.10 = +10%)
    MoveSpeedPercent,       // Walk & sprint speed percentage bonus (+0.10 = +10%)
    AttackDamagePercent,    // Attack damage percentage bonus (+0.12 = +12%)
    AttackSpeedPercent      // Attack speed percentage bonus (+0.12 = +12%)
}

[Serializable]
public class BuffEffectData
{
    [Tooltip("Jenis buff yang diberikan.")]
    public BuffType buffType;

    [Tooltip("Nilai buff. Gunakan format desimal untuk persentase (misal 0.10 untuk +10%), atau angka bulat untuk flat (misal 4 untuk +4 HP).")]
    public float value;

    [Tooltip("Durasi buff dalam detik.")]
    public float duration = 30f;

    [Tooltip("Nama buff untuk keperluan UI/display (opsional).")]
    public string buffName;

    [TextArea(1, 3)]
    [Tooltip("Deskripsi efek buff untuk tooltip/UI.")]
    public string description;

    [Tooltip("Ikon visual buff untuk ditampilkan di UI (opsional, jika kosong akan menggunakan ikon default berdasarkan BuffType).")]
    public Sprite buffIcon;

    public BuffEffectData() { }

    public BuffEffectData(BuffType type, float val, float dur, string name = "", string desc = "", Sprite icon = null)
    {
        buffType = type;
        value = val;
        duration = dur;
        buffName = name;
        description = desc;
        buffIcon = icon;
    }
}

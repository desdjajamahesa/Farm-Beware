using System;
using UnityEngine;

public enum BuffType
{
    MaxHealthPercent,       // Penambahan persentase Max HP (+0.10 = +10%)
    MaxStaminaPercent,      // Penambahan persentase Max Stamina (+0.10 = +10%)
    HealthRegenTick,        // Penambahan flat HP per detik / tick (+4 = +4 HP/tick)
    HealthRegenPercent,     // Penambahan persentase regen HP
    StaminaRegenPercent,    // Penambahan persentase kecepatan regenerasi stamina (+0.10 = +10%)
    MoveSpeedPercent,       // Penambahan persentase kecepatan jalan & lari (+0.10 = +10%)
    AttackDamagePercent,    // Penambahan persentase damage serangan (+0.12 = +12%)
    AttackSpeedPercent      // Penambahan persentase kecepatan serangan (+0.12 = +12%)
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

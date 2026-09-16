# MVP Version Guideline V0.1

Duration (sec): X seconds
Status: Not started
Decorative Color Band: $\colorbox{fffde7}{~~}
\colorbox{fff9c4}{~~}
\colorbox{fff59d}{~~}
\colorbox{fff176}{~~}
\colorbox{ffee58}{~~}
\colorbox{ffeb3b}{~~}
\colorbox{fdd835}{~~}
\colorbox{fbc02d}{~~}
\colorbox{f9a825}{~~}
\colorbox{f57f17}{~~}$

# FARM

#### Seed Economy

The farming economy is built around a simple resource loop:

**Seed → Plant → Harvest → Sell → Gold**

Or

**Harvest → Cooking → Food Buff**

| Seed | Growth | Harvest Value | Monster |
| --- | --- | --- | --- |
| Sweet Potato | Fast | 750-1000 Gold | Tuber Maw |
| Taro | Medium | X-Y Gold (TBD) | Taro Brute |

Each crop have 2 purpose:

1. Economic value:
- Harvest and sell them for Gold.
- Golds are used for upgrade, cooking and preparation.
1. Combat preparation:
- Keep harvested resources to create food buffs before nighttime battles.

# Cooking

Cooking is performed inside the player’s kitchen.

The MVP version focuses on simplicity:

**3 Ingredients → 1 Food**

MVP start with **3  core recipes**:

| Food Type | Fungsi |
| --- | --- |
| Recovery Food | Restore HP(small recovery+10%, medium recovery+25%, large recovery+50%) |
| Stamina Food | Restore / improve total stamina |
| Battle Food | Temporary combat buff for 25%(Dmg+, Atk spd+, Spd+)   |

Cooking creates another layer of resource management:

**Farm Resources → Food Preparation → Night Combat Advantage**

Players must decide whether resources are more valuable as:

- Immediate Gold income
- Long-term survival preparation

[Food buff:](MVP%20Version%20Guideline%20V0%201/Food%20buff%203da97560e11a808da4dfde70b8c9607a.csv)

# Brawl

The MVP combat system focuses on:

- 1 Weapon
- 2 Base Enemies
- 2 Boss Forms
- Wave System
- Monster Resources

---

### Enemy Evolution System:

The system expands existing enemies through evolution.

**2 Seeds → 4 Forms**

| Seed | Normal | Boss Evolution |
| --- | --- | --- |
| Sweet Potato | Tuber Maw | Cyclops Tuber Maw |
| Taro | Taro Brute | Taro Colossus |

Jadi guideline **4 Enemy Variant Based on Seeds** tetap terpenuhi tanpa menambah jumlah seed.

---

## Wave Progression

Jumlah wave sekaligus menunjukkan progression harian:

**Day 1 → 1 Wave**

**Day 2 → 2 Waves**

**Day 3 → 3 Waves**

**Day 4 → 4 Waves**

**Day 5 → 5 Waves**

### Day 5 Structure

- Wave 1–3:
    - Normal enemy encounters
- Wave 4–5:
    - Boss encounters

Boss progression:

- Day 4:
    - First Boss Introduction + 4 normal enemy
- Day 5:
    - Two Boss Encounters + 3 normal enemy

## BRAWL RESOURCE

### Monster Material

**Fight → Monster Drop → Weapon Upgrade**

Sweet Potato dan Taro memberikan material berbeda sehingga menentukan **jenis upgrade**, bukan hanya nilai damage.

| Enemy | Drop | Type | Kegunaan |
| --- | --- | --- | --- |
| **Tuber Maw** | **Mutated Root**   | Normal Material | Upgrade jalur **Speed / Mobility** |
| **Cyclops Tuber Maw** | **Cyclops Eye** | Boss Material | Membuka special effect berbasis **Precision / Tracking** |
| **Taro Brute** | **Hardened Root** | Normal Material | Upgrade jalur **Power / Defense / Knockback** |
| **Taro Colossus** | **Colossus Core** | Boss Material | Membuka special effect berbasis **Heavy Impact / Armor** |

## Weapon Upgrade

Upgrade dilakukan di **Workbench / Garage**.

Cost:

**Gold + Monster Material → Upgrade**

Ada dua jenis progression:

### Basic Upgrade

Meningkatkan statistik dasar.

Contoh:

**Damage Lv.1 → Lv.2 → Lv.3** 

### Special Upgrade

Monster material memberikan efek tertentu.

Contoh konsep:

**Sweet Potato Path**

→ Attack Speed / Dash Attack / Mobility Effect

**Taro Path**

→ Heavy Hit / Knockback / Defensive Effect

## Time Management

Game hanya mempunyai dua state utama:

### Day phase

Farm • Trade • Cooking • Upgrade • Preparation

Player menentukan kapan siap dengan menggunakan kasur.

**Sleep → Start Nigzht Phase**

### Night phase

Brawl / Waves

Setelah encounter selesai:

**Return Home → Sleep → Next Day**

Jadi kasur sebenarnya menjadi **kontrol progression MVP**.

![Area Rumah Harvest Pipeline-2026-09-10-105210.png](MVP%20Version%20Guideline%20V0%201/Area_Rumah_Harvest_Pipeline-2026-09-10-105210.png)
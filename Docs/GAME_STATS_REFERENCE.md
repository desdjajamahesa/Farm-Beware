# Referensi Lengkap Statistik Game — Farm-Beware (`Alpha 0.3.0`)

Dokumen ini adalah **Single Source of Truth** statistik runtime yang diekstrak langsung dari skrip C# (`PureLogic`, `Player`, `FeaturesCombat`, `FeaturesWorkbench`, `FeaturesKitchen`, `FeaturesEconomy`, `FeaturesTime`) dan aset `ScriptableObject` (`Assets/Data/**/*.asset`) per **28 November 2026** (**123/123 Automated Audit Tests Passed**).

---

## Daftar Isi
1. [Statistik Dasar, Locomotion & Pertahanan Aktif Player](#1-statistik-dasar-locomotion--pertahanan-aktif-player)
2. [Sistem Armor, Formula Defense & Perbandingan Hit Monster (Sebelum vs Sesudah)](#2-sistem-armor-formula-defense--perbandingan-hit-monster-sebelum-vs-sesudah)
3. [Senjata, Alat & Formula Damage Player](#3-senjata-alat--formula-damage-player)
4. [Frame-Data Combat 60 FPS, Cancel Matrix & Kinesthetics](#4-frame-data-combat-60-fps-cancel-matrix--kinesthetics)
5. [Pohon Spesialisasi Senjata (3 Seed Doctrines)](#5-pohon-spesialisasi-senjata-3-seed-doctrines)
6. [Monster, Boss, Skill Khusus, Poise & Wave Progression](#6-monster-boss-skill-khusus-poise--wave-progression)
7. [Pertanian, Ekonomi, Merchant & Order Board](#7-pertanian-ekonomi-merchant--order-board)
8. [Resep Memasak, Food Buffs & Logistik Air (MVP Calibrated)](#8-resep-memasak--food-buffs--logistik-air-mvp-calibrated)
9. [Siklus Waktu Siang & Malam](#9-siklus-waktu-siang--malam)

---

## 1. Statistik Dasar, Locomotion & Pertahanan Aktif Player

Sumber: [`PlayerStats.cs`](../Assets/Scripts/Player/PlayerStats.cs), [`PlayerControl.cs`](../Assets/Scripts/Player/PlayerControl.cs), [`DefenseEvaluator.cs`](../Assets/Scripts/Features/Combat/Core/PureLogic/DefenseEvaluator.cs)

### 1.1 Vitals & Kelaparan/Kehausan
| Parameter | Nilai Dasar | Catatan / Mekanik |
|---|---:|---|
| **Base Max HP** | `100` | Ditambah bonus HP Armor (hingga `+100` dari 5-piece set = `200 HP`) & Food Buff `%` (`round(100 * (1 + foodBuff%)) + armorBonusHP`) |
| **Max Stamina** | `120` | Dapat ditingkatkan lewat Food Buff `MaxStaminaPercent` |
| **Stamina Regen Rate** | `20 / detik` | Aktif setelah delay berakhir |
| **Sprint Stamina Drain** | `20 / detik` | Berhenti sprint otomatis saat exhausted (`currentStamina <= 0.1`) |
| **Stamina Regen Delay** | `1.2 s` | Jeda setelah pemakaian stamina terakhir |
| **Max Hunger** | `100` | Drain `0.20 / detik` (~`8m 20s` penuh ke kosong); pause di Day 0 |
| **Max Thirst** | `100` | Drain `0.25 / detik` (~`6m 40s` penuh ke kosong); pause di Day 0 |
| **Starvation / Dehydration Penalty** | `5 DMG / 3.0 s` | Berlaku saat `Hunger <= 0.01` atau `Thirst <= 0.01` (kecuali Day 0 & God Mode) |
| **Hurt Flash Duration** | `0.12 s` | Feedback visual merah saat terkena hit (`_HitFlashColor`) |

### 1.2 Locomotion & Mobilitas
| Parameter | Nilai Dasar | Modifikasi Armor / State |
|---|---:|---|
| **Walk Speed** | `5.0 m/s` | Full Set Armor (`+17%`): **`5.85 m/s`** |
| **Run / Sprint Speed** | `8.0 m/s` | Full Set Armor (`+17%`): **`9.36 m/s`** |
| **Turn Speed** | `18` | Rotasi karakter saat bergerak |
| **Attack Move Speed Multiplier** | `0.40×` | Kecepatan gerak saat mengayunkan serangan melee |
| **Attack Turn Speed** | `14` | Kecepatan putar selama startup/recovery serangan |
| **Jump Force** | `5.0` | Gaya impuls vertikal lompatan |
| **Skill Leap Impulse (Fwd / Up)** | `6.5 / 2.5` | Impuls serangan lompat spesial |
| **Farming Lock Duration (Plant / Water)** | `2.90 s` / `3.95 s` | Animasi menyiram dapat langsung dibatalkan dengan WASD |

### 1.3 Dodge Roll & Precision Parry (`DefenseEvaluator`)
| Mekanik | Parameter | Nilai | Efek Tambahan |
|---|---|---:|---|
| **Dodge Roll** (`C` / `Left Alt`) | Stamina Cost | `15` | Dash velocity **`6.5 m/s`**, durasi fisik **`0.62 s`** |
| | Invulnerability (i-Frames) | `0.30 s` *(PureLogic)* / `0.32 s` *(PlayerControl)* | Menahan 100% damage; `+0.07s` dengan buff *Agility Surge* |
| | Dodge-End Cancel Threshold | `78%` (`0.78`) | Bisa langsung cancel ke Parry, Light, Heavy, atau Dash Attack |
| | **Phantom Dodge Empower** | `3.0 s` | Aktif saat berhasil menghindari hit di dalam i-Frame: **`+15% DMG`** & **`+10% Atk Spd`** (`+30% DMG` total dengan 4-piece Sweet Potato Set) |
| **Precision Parry** (`V` / `Left Ctrl`) | Stamina Cost | `10` | Active window **`0.35 s`** (`+0.06s` dengan *Reflective Shell*) |
| | Whiff Recovery Lockout | `0.35 s` | Penalti jika parry tidak memblokir serangan |
| | **Parry Shockwave** | Radius `2.8 m` | Memblokir 100% damage + memantulkan `max(8, round(blockedDmg * 0.35))` DMG + pushback `1.8` + `0.10s` hitstop + `0.38` camera trauma |
| | **Riposte Window** | `1.20 s` | Serangan berikutnya menjadi **Riposte** (`0` stamina, **`2.5×` damage**, tier `ParryRiposte`) |

---

## 2. Sistem Armor, Formula Defense & Perbandingan Hit Monster (Sebelum vs Sesudah)

Sumber: [`EquipmentUpgradeEvaluator.cs`](../Assets/Scripts/Features/Workbench/Core/EquipmentUpgradeEvaluator.cs), [`PlayerArmorUpgradeState.cs`](../Assets/Scripts/Features/Workbench/PlayerArmorUpgradeState.cs), [`PlayerStats.cs`](../Assets/Scripts/Player/PlayerStats.cs), [`Assets/Data/Items/Equipment/item_*.asset`](../Assets/Data/Items/Equipment/)

### 2.1 Urutan Pipeline Mitigasi Damage Masuk (`PlayerStats.TakeDamage`)
Setiap serangan monster yang mengenai pemain diproses secara berurutan melalui 4 tahap berikut:
1. **Tahap 1 — Parry / Dodge i-Frames (`DefenseEvaluator.EvaluateIncomingDamage`)**: Jika aktif dan serangan dapat diblokir, **100% damage dibatalkan (`0` DMG)**.
2. **Tahap 2 — Combat Modifier Mitigation (`CombatStatModifiers.DamageMitigationRatio`)**: Diterapkan bila buff aktif (*Iron Root Stance*: `25%`, *Taro Weapon Path*: `10%`).
3. **Tahap 3 — Combined Armor Mitigation (`EquipmentUpgradeEvaluator`)**:
   - **Gear Defense Mitigation** *(diminishing returns)*:
     $$\text{Gear DR} = \frac{\text{DEF}}{\text{DEF} + 40}$$
   - **Workbench Plating Mitigation** *(aktif hanya jika minimal 1 piece armor terpasang)*: naik dari `0%` (Lv 1–2) hingga `32%` (Lv 10).
   - **Combined Mitigation Ratio** *(perkalian multiplikatif, hard cap `75%`)*:
     $$\text{Total DR} = \min\Bigl(0.75,\; 1 - (1 - \text{Gear DR}) \times (1 - \text{Plating DR})\Bigr)$$
   - **Final Damage & Effective HP (EHP)**:
     $$\text{Final DMG} = \max\Bigl(1,\; \text{round}\bigl(\text{Raw DMG} \times (1 - \text{Total DR})\bigr)\Bigr), \qquad \text{EHP} = \frac{\text{Total Max HP}}{1 - \text{Total DR}}$$
4. **Tahap 4 — Taro Weapon Path Flat Armor**: Jika *Taro Path* aktif pada senjata, damage dikurangi flat **`15`** lagi (minimum tetap `1` damage per hit).

---

### 2.2 Tabel Lengkap 5 Piece Armor (`Sweet Potato` Combat Set)

| Nama Item | Asset ID | Slot | Harga Beli / Jual | Bonus HP | Bonus DEF | Gear DR (`D/(D+40)`) | Move Spd | Atk Spd | Crit Chance |
|---|---|---|---:|---:|---:|---:|---:|---:|---:|
| **Burrow Hood** | `item_burrow_hood` | Head | `3,500 G` / `1,200 G` | `+15` | `+3` | `6.98%` | `+0%` | `+0%` | **`+5%`** |
| **Swift Root Armor** | `item_swift_root_armor` | Chest | `7,000 G` / `2,400 G` | **`+35`** | **`+6`** | **`13.04%`** | `+4%` | `+0%` | `+0%` |
| **Fang Grips** | `item_fang_grips` | Gloves | `4,500 G` / `1,500 G` | `+10` | `+2` | `4.76%` | `+0%` | **`+8%`** | `+4%` |
| **Underground Striders** | `item_underground_striders` | Boots | `4,500 G` / `1,500 G` | `+15` | `+3` | `6.98%` | **`+8%`** | `+0%` | `+0%` |
| **Sweet Potato Heart** | `item_sweet_potato_heart` | Accessory | `9,000 G` / `3,000 G` | `+25` | `+2` | `4.76%` | `+5%` | `+5%` | `+3%` |
| **TOTAL GEAR (5/5 Piece)** | `Sweet Potato Set` | All 5 Slots | **`28,500 G`** *(total beli)* | **`+100` (`200 HP`)** | **`+16`** | **`28.57%`** | **`+17%`** | `+13%` *(+10% Set = **`+23%`**)* | **`+12%`** |

#### Set Bonus `Sweet Potato` (`EquipmentUpgradeEvaluator.EvaluateSweetPotatoSetBonus`)
- **2 Pieces**: **`+10% Attack Speed`** *(otomatis tergabung ke total Atk Spd saat ≥2 piece terpasang)*.
- **4 Pieces**: **`+15% Bonus Damage` selama `3.0s`** setelah sukses dodge di i-Frame *(menumpuk dengan Phantom Dodge `+15%`, menjadi `+30% DMG`)*.
- **5+ Pieces (5 Armor / 6 incl. Root Cleaver)**: **Shadow Dash Reset** — me-reset cooldown dodge saat membunuh monster (**ICD `5.0s`**).

---

### 2.3 Tabel Progresi Workbench Armor Plating (`Lv 1` – `Lv 10`)

| Plating Level | Biaya Upgrade Gold | Total Biaya Kumulatif | Plating DR *(≥1 piece)* | Combined DR w/ 1 Pc Chest (`6 DEF`) | Combined DR w/ Full Set (`16 DEF`) | EHP Full Set (`200 Max HP`) |
| :---: |---:|---:|---:|---:|---:|---:|
| **Lv 1 (Base)** | — | `0 G` | `0%` | `13.04%` | `28.57%` | `280.0` |
| **Lv 2** | `300 G` | `300 G` | `0%` | `13.04%` | `28.57%` | `280.0` |
| **Lv 3** | `500 G` | `800 G` | `2%` | `14.78%` | `30.00%` | `285.7` |
| **Lv 4** | `800 G` | `1,600 G` | `4%` | `16.52%` | `31.43%` | `291.7` |
| **Lv 5** | `1,200 G` | `2,800 G` | `6%` | `18.26%` | `32.86%` | `297.9` |
| **Lv 6** | `1,800 G` | `4,600 G` | `8%` | `20.00%` | `34.29%` | `304.3` |
| **Lv 7** | `12,500 G` | `17,100 G` | `12%` | `23.48%` | `37.14%` | `318.2` |
| **Lv 8** | `23,500 G` | `40,600 G` | `17%` | `27.83%` | `40.71%` | `337.3` |
| **Lv 9** | `35,000 G` | `75,600 G` | `22%` | `32.17%` | `44.29%` | `359.0` |
| **Lv 10 (Max)** | `47,000 G` | **`122,600 G`** | **`32%`** | **`40.87%`** | **`51.43%`** | **`411.8`** |

---

### 2.4 Tabel Ringkasan Build: Tanpa Armor vs Per-Piece vs Full Set

| Konfigurasi Armor | Max HP | DEF | Gear DR | Plating DR | Total DR | EHP | Daya Tahan vs Naked | Walk / Run Speed | Total Atk Spd | Crit% |
|---|---:|---:|---:|---:|---:|---:|---:|---|---:|---:|
| **Tanpa Armor (Naked)** | `100` | `0` | `0.00%` | `0%` *(tidak aktif)* | `0.00%` | `100.0` | `1.00×` | `5.00 / 8.00 m/s` | `+0%` | `0%` |
| **1 Pc: Fang Grips (Gloves)** | `110` | `2` | `4.76%` | `0%` *(Lv 1)* | `4.76%` | `115.5` | `1.16×` | `5.00 / 8.00 m/s` | `+8%` | `+4%` |
| **1 Pc: Burrow Hood (Head)** | `115` | `3` | `6.98%` | `0%` *(Lv 1)* | `6.98%` | `123.6` | `1.24×` | `5.00 / 8.00 m/s` | `+0%` | `+5%` |
| **1 Pc: Underground Striders (Boots)** | `115` | `3` | `6.98%` | `0%` *(Lv 1)* | `6.98%` | `123.6` | `1.24×` | `5.40 / 8.64 m/s` | `+0%` | `+0%` |
| **1 Pc: Sweet Potato Heart (Acc)** | `125` | `2` | `4.76%` | `0%` *(Lv 1)* | `4.76%` | `131.2` | `1.31×` | `5.25 / 8.40 m/s` | `+5%` | `+3%` |
| **1 Pc: Swift Root Armor (Chest)** | `135` | `6` | `13.04%` | `0%` *(Lv 1)* | `13.04%` | `155.2` | `1.55×` | `5.20 / 8.32 m/s` | `+0%` | `+0%` |
| **Full Set 5/5 (Plating Lv 1)** | `200` | `16` | `28.57%` | `0%` | `28.57%` | `280.0` | **`2.80×`** | `5.85 / 9.36 m/s` | **`+23%`** | **`+12%`** |
| **Full Set 5/5 (Plating Lv 6)** | `200` | `16` | `28.57%` | `8%` | `34.29%` | `304.3` | **`3.04×`** | `5.85 / 9.36 m/s` | **`+23%`** | **`+12%`** |
| **Full Set 5/5 (Plating Lv 10)** | `200` | `16` | `28.57%` | `32%` | **`51.43%`** | **`411.8`** | **`4.12×`** | `5.85 / 9.36 m/s` | **`+23%`** | **`+12%`** |

---

### 2.5 Perbandingan Damage Diterima & Jumlah Hit Sampai Mati Terhadap Monster (Sebelum vs Sesudah Pakai Armor)

Format setiap sel: **`Damage/Hit` (`Jumlah Hit hingga Mati` pada HP penuh)**. Perhitungan memakai `MathF.Round` C# exact seperti di `EquipmentUpgradeEvaluator.CalculateMitigatedDamage`.

#### A. Perbandingan Lengkap vs Tanpa Armor, Chest Saja, dan Full Set (Lv 1 / Lv 6 / Lv 10)
| Sumber Serangan Monster / Boss | Raw DMG | Tanpa Armor (`100 HP`, `0%`) | 1 Pc Chest (`135 HP`, `13.04%`) | Full Set Lv 1 (`200 HP`, `28.57%`) | Full Set Lv 6 (`200 HP`, `34.29%`) | Full Set Lv 10 (`200 HP`, `51.43%`) | Reduksi DMG & Tambahan Hit *(Full Lv 10 vs Naked)* |
|---|---:|---|---|---|---|---|---|
| **Tuber Maw** *(Contact)* | `15` | `15` *(7 hit)* | `13` *(11 hit)* | `11` *(19 hit)* | `10` *(20 hit)* | **`7`** *(29 hit)* | **`-53.3% DMG` (`+22` hit)** |
| **Corn Musketeer** *(Shot / Contact)* | `15` | `15` *(7 hit)* | `13` *(11 hit)* | `11` *(19 hit)* | `10` *(20 hit)* | **`7`** *(29 hit)* | **`-53.3% DMG` (`+22` hit)** |
| **Taro Brute** *(Contact)* | `25` | `25` *(4 hit)* | `22` *(7 hit)* | `18` *(12 hit)* | `16` *(13 hit)* | **`12`** *(17 hit)* | **`-52.0% DMG` (`+13` hit)** |
| **Cyclops Tuber Maw** *(Boss Contact)* | `30` | `30` *(4 hit)* | `26` *(6 hit)* | `21` *(10 hit)* | `20` *(10 hit)* | **`15`** *(14 hit)* | **`-50.0% DMG` (`+10` hit)** |
| **Taro Colossus** *(Boss Contact)* | `45` | `45` *(3 hit)* | `39` *(4 hit)* | `32` *(7 hit)* | `30` *(7 hit)* | **`22`** *(10 hit)* | **`-51.1% DMG` (`+7` hit)** |
| **Taro Colossus** *(Airborne Slam AoE)* | `40` | `40` *(3 hit)* | `35` *(4 hit)* | `29` *(7 hit)* | `26` *(8 hit)* | **`19`** *(11 hit)* | **`-52.5% DMG` (`+8` hit)** |
| **Taro Colossus** *(Grapple Slam)* | `35` | `35` *(3 hit)* | `30` *(5 hit)* | `25` *(8 hit)* | `23` *(9 hit)* | **`17`** *(12 hit)* | **`-51.4% DMG` (`+9` hit)** |
| **The Ranger** *(Boss Contact / Mortar)* | `60` | `60` *(2 hit)* | `52` *(3 hit)* | `43` *(5 hit)* | `39` *(6 hit)* | **`29`** *(7 hit)* | **`-51.7% DMG` (`+5` hit)** |
| **The Ranger** *(Enrage Kernel Burst / proj)* | `18` | `18` *(6 hit)* | `16` *(9 hit)* | `13` *(16 hit)* | `12` *(17 hit)* | **`9`** *(23 hit)* | **`-50.0% DMG` (`+17` hit)** |

#### B. Rincian Per-Piece Tunggal (Plating Lv 1, 1 Slot Terpasang vs Naked)
| Piece Tunggal yang Dipakai | Max HP / DR | Tuber Maw / Corn (`15`) | Taro Brute (`25`) | Cyclops Boss (`30`) | Colossus Contact (`45`) | The Ranger (`60`) |
|---|---|---:|---:|---:|---:|---:|
| *Tanpa Armor (Pembanding)* | `100 HP / 0.00%` | `15` *(7 hit)* | `25` *(4 hit)* | `30` *(4 hit)* | `45` *(3 hit)* | `60` *(2 hit)* |
| **Fang Grips (Gloves)** | `110 HP / 4.76%` | `14` *(8 hit)* | `24` *(5 hit)* | `29` *(4 hit)* | `43` *(3 hit)* | `57` *(2 hit)* |
| **Burrow Hood (Head)** | `115 HP / 6.98%` | `14` *(9 hit)* | `23` *(5 hit)* | `28` *(5 hit)* | `42` *(3 hit)* | `56` *(3 hit)* |
| **Underground Striders (Boots)** | `115 HP / 6.98%` | `14` *(9 hit)* | `23` *(5 hit)* | `28` *(5 hit)* | `42` *(3 hit)* | `56` *(3 hit)* |
| **Sweet Potato Heart (Acc)** | `125 HP / 4.76%` | `14` *(9 hit)* | `24` *(6 hit)* | `29` *(5 hit)* | `43` *(3 hit)* | `57` *(3 hit)* |
| **Swift Root Armor (Chest)** | `135 HP / 13.04%` | `13` *(11 hit)* | `22` *(7 hit)* | `26` *(6 hit)* | `39` *(4 hit)* | `52` *(3 hit)* |

> **Contoh Sinergi Maksimal (*Full Set Lv 10 + Iron Root Buff 25% + Taro Path 10% + Taro Flat -15*):**
> Serangan boss **The Ranger (`60 DMG`)** turun menjadi: `60 × (1 - 0.35)` = `39` → `39 × (1 - 0.5143)` = `19` → `max(1, 19 - 15)` = **`4 DMG` per hit** (tahan **`50` hit** dari `200 HP`!).

---

## 3. Senjata, Alat & Formula Damage Player

Sumber: [`Assets/Data/Items/Equipment/`](../Assets/Data/Items/Equipment/), [`PlayerEquipment.cs`](../Assets/Scripts/Player/PlayerEquipment.cs), [`EquipmentUpgradeEvaluator.cs`](../Assets/Scripts/Features/Workbench/Core/EquipmentUpgradeEvaluator.cs), [`RangedWeaponEvaluator.cs`](../Assets/Scripts/Features/Combat/Core/PureLogic/RangedWeaponEvaluator.cs)

### 3.1 Daftar Senjata & Alat Tangan
| Item | Asset ID | Tipe | Base DMG | Knockback | Harga Beli / Jual | Parameter Khusus |
|---|---|---|---:|---:|---:|---|
| **Root Cleaver** | `tool_weapon_root_cleaver` | Melee | `25` | `6` | `350 G` / `120 G` | Senjata melee utama; scaling hingga `63` base DMG di Lv 10 |
| **Dummy Sword** | `dummysword` | Melee | `25` | `7` | `150 G` / `50 G` | Senjata latihan awal |
| **Homestead Slingshot** | `tool_weapon_corn_repeater` | Ranged | `22` | `5` | `450 G` / `180 G` | Projectile speed `22 m/s`, range `12m`; reward dari *Corn Node 1* |
| **Hoe** | `tool_hoe` | Tool / Melee | `12` | `4` | `500 G` / `250 G` | Menggarap tanah (`2.90s` tanam); tidak bisa dijual ke merchant |
| **Plant Waterer** | `tool_plant_waterer` | Tool / Melee | `5` | `2` | `300 G` / `150 G` | Kapasitas `100L`; tidak bisa dijual ke merchant |

### 3.2 Progresi Level Senjata (`Lv 1` – `Lv 10`)
| Level | Biaya Upgrade Gold | Melee Bonus (`EquipmentUpgradeEvaluator`) | `Root Cleaver` Effective DMG | Ranged Bonus (`RangedWeaponEvaluator`) | `Slingshot` Effective DMG |
| :---: |---:|---:|---:|---:|---:|
| **Lv 1** | — | `+0%` (`1.00×`) | `25` | `+0%` (`1.00×`) | `22` |
| **Lv 2** | `300 G` | `+15%` (`1.15×`) | `29` | `+10%` (`1.10×`) | `24` |
| **Lv 3** | `600 G` | `+30%` (`1.30×`) | `33` | `+22%` (`1.22×`) | `27` |
| **Lv 4** | `1,200 G` | `+45%` (`1.45×`) | `36` | `+35%` (`1.35×`) | `30` |
| **Lv 5** | `2,200 G` | `+60%` (`1.60×`) | `40` | `+50%` (`1.50×`) | `33` |
| **Lv 6** | `4,000 G` | `+75%` (`1.75×`) | `44` | `+68%` (`1.68×`) | `37` |
| **Lv 7** | `7,000 G` | `+90%` (`1.90×`) | `48` | `+88%` (`1.88×`) | `41` |
| **Lv 8** | `11,000 G` | `+105%` (`2.05×`) | `51` | `+110%` (`2.10×`) | `46` |
| **Lv 9** | `16,000 G` | `+120%` (`2.20×`) | `55` | `+130%` (`2.30×`) | `51` |
| **Lv 10** | `22,000 G` *(Total `64,300 G`)* | **`+150%` (`2.50×`)** | **`63`** | **`+150%` (`2.50×`)** | **`55`** |

### 3.3 Profil Volley Ketapel (`Homestead Slingshot` — `RangedWeaponEvaluator`)
| Mode Tembak | Jumlah Proyektil | Spread Angle | Pengali Damage | Pengali Knockback | Bonus Corn Specialization |
|---|---:|---:|---:|---:|---|
| **Light Shot 1–2** | `1` | `0°` | `1.00×` | `1.00×` | Base Crit `10%` |
| **Light Shot 3 (Twin Burst)** | `2` | `8°` | `1.10×` | `1.00×` | Node 1: `+10% DMG`; Node 2: `+5% Crit` (`15%`) |
| **Heavy Volley** | `3` | `16°` | `1.35×` | `1.50×` | Node 3: `1` Pierce; Node 4: `+15% Proj Speed` |
| **Skill Barrage** | `5` | `28°` | `1.15×` | `1.35×` | Node 5: Crit Explosion `r=2.5m`; Node 6 (Ult): `150% DMG`, `3` Pierce, `r=3.2m`, `+15% Crit` (`30%`) |

### 3.4 Pengali Damage & Critical Melee (`PlayerEquipment.cs`)
- **Critical Hit Melee**: Peluang roll diambil dari total `BonusCriticalChance` armor (maks `+12%` dari Sweet Potato set). Jika sukses, **`dmgMultiplier *= 1.5f`**.
- **Parry Riposte**: Gratis stamina (`0`), guaranteed critical dengan **`dmgMultiplier *= 2.5f`**.
- **Skill Leap Strike**: Biaya stamina **`25`**, pengali damage **`2.0×`**, radius hantam **`2.5 m`**, knockback **`8.0`**.
- **Occlusion Check**: Linecast Zero-GC dari tinggi torso (`0.8m`) menolak hit menembus dinding/struktur (`NonAllocPhysics.IsLineOfSightBlocked`).

---

## 4. Frame-Data Combat 60 FPS, Cancel Matrix & Kinesthetics

Sumber: [`CombatFrameDataSheet.cs`](../Assets/Scripts/Features/Combat/Core/PureLogic/CombatFrameDataSheet.cs), [`ImpactKinematicsProfile.cs`](../Assets/Scripts/Features/Combat/Core/PureLogic/ImpactKinematicsProfile.cs), [`CombatStatModifiers.cs`](../Assets/Scripts/Features/Combat/Core/PureLogic/CombatStatModifiers.cs)

### 4.1 Move Sheet & 2D Cancel Windows (`CombatFrameDataSheet`)
Semua durasi dalam detik pada baseline `attackAnimationSpeed = 1.0`. Kolom **Cancel (Parry / Dodge / Chain / Move)** adalah rasio waktu ternormalisasi (`0.00` – `1.00`) mulai dibukanya window cancel.

| Move (`CombatActionID`) | Clip (s) | Startup (s) | Active (s) | Recovery (s) | DMG Mult | Poise DMG | Knockback | Stamina | Reach | 360° Sweep | Cancel (`Parry / Dodge / Chain / Move`) | Micro-Lunge (`Spd / Dur`) | Tier |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|:---:|---|---|---|
| **Light1** | `0.333` | `0.12` | `0.16` | `0.053` | `1.00×` | `18` | `1.8` | `10` | `1.00×` | — | `0.18 / 0.22 / 0.50 / 0.80` | `2.8 m/s` / `0.10s` | `LightSlash` |
| **Light2** | `0.700` | `0.20` | `0.22` | `0.280` | `1.20×` | `24` | `2.2` | `10` | `1.05×` | — | `0.20 / 0.24 / 0.50 / 0.68` | `3.2 m/s` / `0.12s` | `LightSlash` |
| **Light3** | `1.033` | `0.18` | `0.48` | `0.373` | `1.60×` | `42` | `4.8` | `15` | `1.15×` | Ya | `0.22 / 0.28 / 0.55 / 0.72` | `3.8 m/s` / `0.15s` | `ComboFinisher` |
| **Light4** | `1.033` | `0.20` | `0.45` | `0.383` | `1.85×` | `48` | `5.2` | `15` | `1.20×` | Ya | `0.22 / 0.28 / 0.65 / 0.74` | `4.0 m/s` / `0.15s` | `ComboFinisher` |
| **ChargedThrust (H1)** | `0.833` | `0.28` | `0.28` | `0.273` | `2.00×` | `55` | `4.8` | `21` | `1.25×` | Ya | `0.22 / 0.28 / 0.55 / 0.74` | `4.4 m/s` / `0.14s` | `HeavyCleave` |
| **HeavyFinisher1** | `0.400` | `0.12` | `0.24` | `0.040` | `1.90×` | `50` | `5.0` | `21` | `1.25×` | Ya | `0.22 / 0.28 / 0.55 / 0.84` | `4.0 m/s` / `0.11s` | `HeavyCleave` |
| **HeavyChain2** | `0.400` | `0.12` | `0.24` | `0.040` | `2.20×` | `55` | `5.2` | `21` | `1.25×` | Ya | `0.22 / 0.28 / 0.60 / 0.84` | `4.0 m/s` / `0.11s` | `HeavyCleave` |
| **HeavyFinisher2** | `0.800` | `0.38` | `0.26` | `0.160` | `2.40×` | `62` | `5.8` | `21` | `1.30×` | Ya | `0.24 / 0.30 / 0.60 / 0.80` | `4.6 m/s` / `0.14s` | `HeavyCleave` |
| **HeavyChain3** | `0.800` | `0.38` | `0.26` | `0.160` | `2.50×` | `65` | `6.0` | `21` | `1.30×` | Ya | `0.24 / 0.30 / 0.62 / 0.80` | `4.8 m/s` / `0.14s` | `HeavyCleave` |
| **HeavyChain4** | `0.933` | `0.40` | `0.28` | `0.253` | **`3.00×`** | **`85`** | **`7.2`** | `21` | `1.35×` | Ya | `0.25 / 0.32 / 0.70 / 0.76` | `5.2 m/s` / `0.16s` | `PoiseCrushOrWallSplat` |
| **DashSweep** | `0.700` | `0.08` | `0.20` | `0.420` | `1.40×` | `30` | `4.2` | `18` | `1.25×` | — | `0.18 / 0.22 / 0.42 / 0.62` | `7.5 m/s` / `0.16s` | `ComboFinisher` |
| **FrontKick** | `0.460` | `0.10` | `0.14` | `0.220` | `0.80×` | `55` | `6.5` | `12` | `1.05×` | — | `0.18 / 0.20 / 0.45 / 0.65` | `3.0 m/s` / `0.10s` | `ComboFinisher` |

- **Combo Reset Window**: `1.8 s` antar pukulan.
- **Input Buffer (`CombatInputBuffer`)**: Prioritas **`Parry (8) > Dodge (7) > Heavy (6) > FrontKick (5) > DashAttack (4) > Light (3) > Jump (2)`**. Defense/Jump buffer window: **`0.22 s`**; Attack buffer window: **`0.35 s`**.

### 4.2 Kinesthetics Feedback (`ImpactKinematicsProfile`)
| Impact Tier | Attacker Hitstop | Victim Hitstop | Camera Trauma | Sparks | Audio Pitch Range |
|---|---:|---:|---:|---:|---|
| **LightSlash** | `0.035 s` *(`×1.25` crit)* | `0.065 s` *(`×1.25` crit)* | `0.18` *(max `0.45`)* | `12` *(crit `22`)* | `0.96 – 1.06` |
| **ComboFinisher** | `0.055 s` *(`×1.25` crit)* | `0.105 s` *(`×1.25` crit)* | `0.30` *(max `0.55`)* | `20` *(crit `26`)* | `0.90 – 0.96` |
| **HeavyCleave** | `0.075 s` *(`×1.25` crit)* | `0.140 s` *(`×1.25` crit)* | `0.42` *(max `0.65`)* | `28` *(crit `34`)* | `0.82 – 0.88` |
| **PoiseCrushOrWallSplat** | `0.085 s` | `0.180 s` | `0.52` | `35` | `0.80 – 0.88` |
| **ParryRiposte** | `0.095 s` | `0.220 s` | `0.60` | `42` | `0.85 – 0.92` |

- **Wall-Splat Collision Damage**: Aktif saat musuh terpental ke dinding solid dengan kecepatan $\ge$ **`4.5 m/s`**; base **`22` DMG** berskala hingga **`1.75×`** (`39 DMG`).

### 4.3 Pengali Stat Tempur (`CombatStatModifiers`)
| Sumber Buff / Jalur | Efek ke Statistik Tempur |
|---|---|
| **Agility Surge** *(Gastronomy)* | `+20% Atk Spd`, `+0.07s` Dodge i-Frames |
| **Iron Root Stance** *(Gastronomy)* | `Passive Super Armor`, `+25% Damage Mitigation` |
| **Berserker Harvest** *(Gastronomy)* | `+40% Finisher Damage` |
| **Reflective Shell** *(Gastronomy)* | `+0.06s` Parry Active Window |
| **Sweet Potato Path** *(Weapon)* | `+15% Atk Spd`, `+10% DMG` |
| **Taro Path** *(Weapon)* | `+35% Poise DMG`, `+30% Knockback`, `+10% Damage Mitigation`, **`+15` Flat Armor** (`TakeDamage`) |
| **Phantom Dodge Empower** | `+15% DMG`, `+10% Atk Spd` (`3.0s`); ditambah `+15% DMG` bila 4-pc Sweet Potato terpasang |

---

## 5. Pohon Spesialisasi Senjata (3 Seed Doctrines)

Sumber: [`EquipmentUpgradeEvaluator.cs`](../Assets/Scripts/Features/Workbench/Core/EquipmentUpgradeEvaluator.cs)

### 5.1 Sweet Potato Path — Speed & Mobility
| Node | Nama | Efek | Biaya Gold | Material Monster | Boss Kills Req |
| :---: |---|---|---:|---|---:|
| **Lv 1** | **Quick Roots** | `+10% Attack Speed` | `10,000 G` | `2× Mutated Root` | `0` |
| **Lv 2** | **Swift Movement** | `+10% Movement Speed` | `20,000 G` | `1× Cyclops Eye` | `0` |
| **Lv 3** | **Dash Strike** | Serangan saat dash menembus musuh | `40,000 G` | — | `0` |
| **Lv 4** | **Root Frenzy** | Serangan beruntun `+15% Attack Speed` | `45,000 G` | `2× Cyclops Eye` | `0` |
| **Lv 5** | **Burrow Instinct** | Dodge sukses memberi DR sementara | `60,000 G` | — | `0` |
| **Lv 6 (Ult)** | **Underground Predator** | `150%` Dash DMG; dodge beri `+20% Atk Spd` (`3s`) | `100,000 G` | `3× Cyclops Eye` | `5` |

### 5.2 Taro Path — Heavy Damage & Defense
| Node | Nama | Efek | Biaya Gold | Material Monster | Boss Kills Req |
| :---: |---|---|---:|---|---:|
| **Lv 1** | **Heavy Roots** | `+10% Heavy Attack Damage` | `10,000 G` | `2× Hardened Root` | `0` |
| **Lv 2** | **Thick Skin** | `+10% Defense` | `20,000 G` | `1× Colossus Core` | `0` |
| **Lv 3** | **Ground Breaker** | Heavy attack mendorong musuh mundur | `40,000 G` | — | `0` |
| **Lv 4** | **Iron Taro** | `+15% Max HP` | `45,000 G` | `2× Colossus Core` | `0` |
| **Lv 5** | **Unbreakable Root** | Menerima hit memberi resistensi adaptif sementara | `60,000 G` | — | `0` |
| **Lv 6 (Ult)** | **Ancient Guardian** | `150%` Heavy DMG + shockwave; `20%` pengurangan DMG (`3s`) | `100,000 G` | `3× Colossus Core` | `5` |

### 5.3 Corn Path — Ranged Damage & Critical Hits
| Node | Nama | Efek | Biaya Gold | Material Monster | Boss Kills Req |
| :---: |---|---|---:|---|---:|
| **Lv 1** | **Golden Kernel** | `+10% Projectile Damage` *(membuka Homestead Slingshot)* | `10,000 G` | `2× Kernel Shrapnel` | `0` |
| **Lv 2** | **Sharp Kernel** | `+5% Critical Chance` | `20,000 G` | `1× Cob Core` | `0` |
| **Lv 3** | **Piercing Shot** | Proyektil menembus `1` musuh tambahan | `40,000 G` | — | `0` |
| **Lv 4** | **Rapid Harvest** | `+15% Projectile Speed` | `45,000 G` | `2× Cob Core` | `0` |
| **Lv 5** | **Kernel Explosion** | Critical hit memicu ledakan AoE (`r=2.5m`) | `60,000 G` | — | `0` |
| **Lv 6 (Ult)** | **Golden Storm** | `150%` Projectile DMG, crit ledakan `r=3.2m`, pierce `3` musuh | `100,000 G` | `3× Cob Core` | `5` |

---

## 6. Monster, Boss, Skill Khusus, Poise & Wave Progression

Sumber: [`Assets/Data/Enemies/*.asset`](../Assets/Data/Enemies/), [`EnemyBase.cs`](../Assets/Scripts/Features/Combat/Enemy/EnemyBase.cs), [`EnemyCombatProfile.cs`](../Assets/Scripts/Features/Combat/Core/PureLogic/EnemyCombatProfile.cs), [`BossAbilities/`](../Assets/Scripts/Features/Combat/Enemy/BossAbilities/), [`WaveProgressionEngine.cs`](../Assets/Scripts/Features/Combat/Wave/WaveProgressionEngine.cs)

### 6.1 Statistik Dasar 6 Arketipe Monster & Boss
Formula armor musuh (`EnemyBase.cs:943`): `effectiveDmg = max(1, incomingDamage - round(armor * 0.25))`

| Monster | Tipe | Max HP | Armor (`Reduksi Flat`) | Move Speed | Contact DMG | Atk Rate | Atk Range | Aggro Range | KB Res | Drop Gold | Drop Material & Seed |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---|---|
| **Tuber Maw** | Normal | `100` | `20` (`-5 DMG`) | `3.0 m/s` | `15` | `0.4/s` | `1.5 m` | `6 m` | `20%` | `200–500 G` (`50%` vs Mat) | `1–3` Mutated Root (`50%`) + `1–4` SP Seed (`60%`) |
| **Taro Brute** | Normal | `150` | `15` (`-4 DMG`) | `1.5 m/s` | `25` | `0.5/s` | `1.8 m` | `6 m` | `80%` | `200–500 G` (`50%` vs Mat) | `1–3` Hardened Root (`50%`) + `1–3` Taro Seed (`70%`) |
| **Corn Musketeer** | Ranged | `85` | `0` (`0 DMG`) | `3.0 m/s` | `15` | `0.8/s` | `10 m` | `10 m` | `15%` | `200–500 G` (`50%` vs Mat) | `1–3` Kernel Shrapnel (`50%`) + `1–3` Corn Seed (`70%`) |
| **Cyclops Tuber Maw** | **Boss** | `600` | `40` (`-10 DMG`) | `3.0 m/s` | `30` | `0.4/s` | `2.4 m` | `12 m` | `35%` | `700–1,000 G` (`70%`) | `1` Cyclops Eye (**`100%`**) + `2–4` SP Seed (`100%`) |
| **Taro Colossus** | **Boss** | `900` | `55` (`-14 DMG`) | `2.0 m/s` | `45` | `0.5/s` | `2.0 m` | `12 m` | `100%` *(immune)* | `700–1,000 G` (`80%`) | `1` Colossus Core (**`100%`**) |
| **The Ranger** | **Boss** | `750` | `30` (`-8 DMG`) | `0 m/s` *(Stationary)* | `60` | `0.5/s` | `10 m` | `14 m` | `100%` *(immune)* | `700–1,000 G` (`85%`) | `1` Cob Core (**`100%`**) |

> Catatan Loot: Monster normal menggunakan roll **mutually exclusive** antara Gold vs Material (`50%` / `50%`). Ketiga Boss menjamin drop Boss Core **`100%`**.

### 6.2 Readability, Tracking Lock-Off & Poise (`EnemyCombatProfile` & `AttackTokenDispatcher`)
Batas token serentak default (`AttackTokenDispatcher.cs`): maks **`3` Melee** & **`2` Ranged** bersamaan. Boss/Elite dapat meng-evict minion; stagger otomatis memutus attack token (`EvictTokenOnStagger = true` untuk semua arketipe).

| Monster | Windup (s) | Tracking Lock-Off | Active (s) | Recovery Punish (s) | Max Poise | Poise Regen / Delay | Super Armor (Innate / Windup) | Move Accel | Turn Speed |
|---|---:|---:|---:|---:|---:|---:|:---:|---:|---:|
| **Tuber Maw** | `0.42` | `55%` | `0.08` | `0.28` | `35` | `15/s` (`2.5s`) | Tidak / Tidak | `18` | `360°/s` |
| **Taro Brute** | `0.62` | `55%` | `0.10` | `0.48` | `85` | `18/s` (`3.0s`) | Ya / Ya | `10` | `220°/s` |
| **Corn Musketeer** | `0.58` | `65%` | `0.08` | `0.34` | `28` | `12/s` (`2.5s`) | Tidak / Tidak | `16` | `340°/s` |
| **Cyclops Tuber Maw** | `0.56` | `58%` | `0.10` | `0.52` | `160` | `25/s` (`3.2s`) | Ya / Ya | `14` | `260°/s` |
| **Taro Colossus** | `0.78` | `52%` | `0.12` | `0.68` | `240` | `28/s` (`3.5s`) | Ya / Ya | `8` | `180°/s` |
| **The Ranger** | `0.65` | `65%` | `0.10` | `0.55` | `180` | `24/s` (`3.0s`) | Ya / Ya | `0` | `200°/s` |

### 6.3 Kemampuan Khusus Monster & Boss
| Pemilik | Skill | Parameter Terverifikasi (`EnemyBase` & `BossAbilities/`) |
|---|---|---|
| **Tuber Maw** | Burrow Strike | Cooldown `5s`, kecepatan burrow **`1.8×`** `moveSpeed`, lock target lalu serang |
| **Taro Brute** | Root Guard | Cooldown `12s`, meningkatkan armor **`+50%`** (`15 -> 23`) selama **`6.0s`** |
| **Corn Musketeer** | Aimed Shot & Kernel Burst | Cooldown `10s`; laser aim `0.55s` (`0.40s` track + `0.15s` lock); `0.4s` charge lalu 3 proyektil (`contactDamage = 15`) |
| **Cyclops Tuber Maw** | Tracking Beam | Cooldown `15s`, menembak laser terarah ke pemain |
| **Cyclops Tuber Maw** | Summon Minions (`BossSummonAbility`) | Cooldown `45s` (`initialDelay 4s`, `channel 1.8s`), spawn **`3` Tuber Maw** (maks aktif `6`) |
| **Taro Colossus** | Airborne Slam (`ColossusAirborneAbility`) | Cooldown `20s`, track bayangan `3.5s` (ketinggian `9m`), **`40` AoE DMG** radius `4.5m` + proyektil batu **`20` DMG**; bisa di-interrupt setelah **`4` hit** |
| **Taro Colossus** | Grapple Slam (`ColossusGrappleAbility`) | Cooldown `24s`, jarak grab `2.2m`, hold `0.7s`, **`35` DMG** + **Stun `1.2s`** |
| **The Ranger** | Mortar / Heavens Fall | Cooldown `14s`; mortar laser `0.45s` (`60` contact DMG); Heavens Fall track `0.65s` + lock `0.55s` (blast `r=3.0m`) |
| **The Ranger** | Kernel Enrage (`RangerEnrageAbility`) | Aktif saat HP `< 50%`: menembakkan **`20` proyektil**, masing-masing **`18` DMG**, kecepatan `13.5 m/s` |

### 6.4 Progresi Wave Malam (`Day 1` – `Day 5`)
- **Intermission Antar Wave**: **`30.0 s`** (`waveIntermissionDelay`)
- **Interval Spawn Dalam Wave**: `2.0 s – 3.5 s` (auto-clamped `1.0 s – 5.0 s`)

| Hari | Jumlah Wave | Komposisi Wave Detail (`WaveProgressionEngine`) | Total Musuh Terjadwal |
| :---: | :---: |---|---:|
| **Day 1** | `1` | W1: `4` Tuber Maw + `1` Taro Brute | **`5`** |
| **Day 2** | `2` | W1: `2` Tuber Maw + `1` Taro Brute + `1` Corn Musketeer (`4`)<br>W2: `2` Taro Brute + `2` Tuber Maw + `1` Corn Musketeer (`5`) | **`9`** |
| **Day 3** | `3` | W1: `2` Corn + `2` Tuber (`4`) • W2: `2` Brute + `1` Corn + `2` Tuber (`5`)<br>W3: `2` Brute + `2` Corn + `1` Tuber + **Cyclops Tuber Maw** (`6`) | **`15`** |
| **Day 4** | `4` | W1: `2` Corn + `1` Brute + `1` Tuber (`4`) • W2: `2` Corn + `1` Brute + `1` Tuber (`4`) • W3: `2` Tuber + `2` Brute + `1` Corn (`5`)<br>W4: `2` Brute + `1` Corn + `1` Tuber + **Taro Colossus** (`5`) | **`18`** |
| **Day 5** | `5` | W1: `4` normal • W2: `5` normal • W3: `6` normal<br>W4: **Taro Colossus** (`1`) • W5: `3` normal + **The Ranger** (`4`) | **`20`** |

---

## 7. Pertanian, Ekonomi, Merchant & Order Board

Sumber: [`Assets/Data/Items/`](../Assets/Data/Items/), [`DailyEconomyManager.cs`](../Assets/Scripts/Features/Economy/DailyEconomyManager.cs), [`BoardOrderEvaluator.cs`](../Assets/Scripts/Features/Economy/PureLogic/BoardOrderEvaluator.cs)

### 7.1 Bibit & Hasil Panen
| Tanaman | Waktu Tumbuh (Siang) | Panen / Plot | Beli Bibit / Jual | Harga Pasar Clean (`DailyEconomyManager`) | Harga Dirty (`50%`) | Order Board (`1.2× Clean`) |
|---|---:|---|---:|---|---|---|
| **Sweet Potato** | `360 s` (**6 menit**) | `1–3` *(rata-rata `2`)* | `150 G` / `75 G` | `375 – 500 G` *(base asset `375 G`)* | `~188 – 250 G` | `450 – 600 G / unit` |
| **Taro** | `900 s` (**15 menit**) | `1–3` *(rata-rata `2`)* | `250 G` / `120 G` | `700 – 900 G` *(base asset `800 G`)* | `~350 – 450 G` | `840 – 1,080 G / unit` |
| **Corn** | `900 s` (**15 menit**) | `1–3` *(rata-rata `2`)* | `250 G` / `50 G` | `700 – 900 G` *(base asset `800 G`)* | `~350 – 450 G` | `840 – 1,080 G / unit` |

- **Ekspansi Lahan**: Awal **`6` plot** terbuka (maks `16` plot di farm, tiap **Land Expansion Deed** seharga **`2,500 G`** membuka **`+2` plot**).

### 7.2 Bahan Dapur Toko & Nilai Jual Monster Drop
| Kategori | Item | Harga Beli | Harga Jual |
|---|---|---:|---:|
| **Bahan Dapur (Toko)** | Coconut (`mat_coconut`) | `100 G` | `50 G` |
| | Cooking Oil (`mat_cooking_oil`) | `90 G` | `45 G` |
| | Flour (`mat_flour`) | `80 G` | `40 G` |
| | Seasoning & Spices (`mat_seasoning`) | `75 G` | `35 G` |
| **Monster Drop (Normal)** | Mutated Root (`mat_mutated_root`) | `200 G` | `150 G` |
| | Hardened Root (`mat_hardened_root`) | `250 G` | `180 G` |
| | Kernel Shrapnel (`mat_kernel_shrapnel`) | — | `300 G` |
| **Boss Core** | Cyclops Eye (`mat_cyclops_eye`) | `1,000 G` | `600 G` |
| | Cob Core (`mat_cob_core`) | — | `850 G` |
| | Colossus Core (`mat_colossus_core`) | `1,500 G` | `900 G` |

---

## 8. Resep Memasak, Food Buffs & Logistik Air (MVP Calibrated)

Sumber: [`Assets/Data/Recipes/Cooking/*.asset`](../Assets/Data/Recipes/Cooking/), [`Assets/Data/Items/Food/*.asset`](../Assets/Data/Items/Food/), [`KitchenStove.cs`](../Assets/Scripts/Features/Kitchen/KitchenStove.cs), [`KitchenSinkInteractable.cs`](../Assets/Scripts/Features/Kitchen/KitchenSinkInteractable.cs), [`PlayerWaterBottle.cs`](../Assets/Scripts/Features/Kitchen/PlayerWaterBottle.cs), [`WaterWellInteractable.cs`](../Assets/Scripts/Features/Farming/WaterWellInteractable.cs)

### 8.1 Tabel Lengkap 9 Resep Memasak (`KitchenStove` — 3 Burner Paralel)
| Resep | Tier | Durasi Masak | Air | Bahan Resep | Output | Hunger / Hydration | Efek Food Buff & Durasi |
|---|---|---:|---:|---|---:|---|---|
| **Taro Chip** | Small | **`90 s`** (`1m 30s`) | `0 L` | `1× Taro`, `1× Oil`, `1× Seasoning` | `2×` | `+10` / `-5` | `+5% Max Stamina` & `+2 HP/tick Regen` (`30s`) |
| **Roasted Sweet Potato Slice** | Small | **`90 s`** (`1m 30s`) | `0 L` | `1× Sweet Potato`, `1× Oil`, `1× Seasoning` | `2×` | `+10` / `-8` | `+10% Max Stamina` & `+10% Move Spd` (`30s`) |
| **Corn Porridge** | Small | **`90 s`** (`1m 30s`) | `10 L` | `1× Corn`, `1× Flour`, `1× Water` | `1×` | `+10` / `+10` | `+15% Stamina Recovery` (`90s`) |
| **Taro Dumpling** | Small | **`120 s`** (`2m`) | `15 L` | `1× Taro`, `1× Flour`, `1× Water` | `2×` | `+10` / `+5` | `+4 HP/tick Regen` (`10s`) |
| **Purple Taro Cake** | Medium | **`180 s`** (`3m`) | `15 L` | `3× Taro`, `1× Corn`, `1× Oil` | `1×` | `+15` / `0` | `+10% Max HP` (`60s`) |
| **Sweet Potato Porridge Bowl** | Medium | **`180 s`** (`3m`) | `10 L` | `3× Sweet Potato`, `1× Flour`, `1× Water` | `1×` | `+10` / `+15` | `+5% Max HP` & `+10% Max Stamina` (`90s`) |
| **Taro Coconut Stew Bowl** | Large | **`300 s`** (`5m`) | `20 L` | `1× Taro`, `1× Coconut`, `1× Water` | `1×` | `+20` / `+10` | **`+12% ATK & +12% ATK Spd`** (`120s`) |
| **Taro Corn Stew Bowl** | Large | **`300 s`** (`5m`) | `20 L` | `1× Taro`, `1× Corn`, `1× Water` | `1×` | `+15` / `+10` | **`+8% ATK & +8% ATK Spd`** (`120s`) |
| **Whole Roasted Sweet Potato Feast** | Large | **`420 s`** (`7m`) | `0 L` | `1× Sweet Potato`, `1× Taro`, `1× Seasoning` | `1×` | `+45` / `+20` | `+20% Move Spd`, `+10% Stam Regen`, **`+10% ATK & +10% ATK Spd`** (`120s`) |

### 8.2 Stasiun Dapur & Logistik Air
- **Bottle of Water** (`food_bottle_water`): Kapasitas **`100 L`**, memulihkan **`+50` Hydration** saat diminum (`25 L` per teguk = `4` charges); harga beli ulang di Merchant **`2,000 G`** (tidak bisa dijual).
- **Kitchen Sink (`KitchenSinkInteractable`)**:
  - Durasi mencuci crop kotor → bersih: **`10 s / item`**.
  - Durasi isi ulang Botol Air di wastafel: **`30 s`** (maks **`2× / hari`** sesuai MVP).
- **Water Well (`WaterWellInteractable`)**: Mengisi ulang *Plant Waterer* (`100 L`) dalam **`10 s`** (maks **`2× / hari`**).

---

## 9. Siklus Waktu Siang & Malam

Sumber: [`DayNightTimeManager.cs`](../Assets/Scripts/Features/Time/DayNightTimeManager.cs), [`CombatPhaseTrackerUI.cs`](../Assets/Scripts/Features/Time/UI/CombatPhaseTrackerUI.cs)

| Parameter | Nilai | Keterangan |
|---|---:|---|
| **Durasi Fase Siang** | `1080 s` (**18 menit real**) | `06:00 -> 18:00` in-game (`1 menit real = 40 menit in-game`) |
| **Dusk Bell** | Menit ke-`15:00` (`900 s`) | `16:00` in-game; peringatan 3 menit menuju malam |
| **Auto-Sleep / Curfew** | Menit ke-`18:00` (`1080 s`) | `18:00` in-game; transisi ke Night Brawl & curfew dapur |
| **Mode Jam HUD** (`T` / `9`) | `60s` / `30s` / `15s` | Mengubah langkah tampilan jam HUD tanpa mengubah laju hari |
| **Fast-Forward Testing** (`0`) | `8×` speed multiplier | Mempercepat waktu siang, pertumbuhan tanaman, sink, dan kompor |

# Roadmap.md

Rencana pengembangan, target fitur, dan tonggak pencapaian (*milestones*) untuk proyek **Farm-Beware** berdasarkan dokumen *MVP Version Guideline V0.1*.

---

## 1. Visi Utama Game Loop MVP

```
                                  ┌────────────────────────┐
                                  │       SIKLUS PAGI      │
                                  │      (Day Phase)       │
                                  └───────────┬────────────┘
                                              │
                    ┌─────────────────────────┴─────────────────────────┐
                    ▼                                                   ▼
         ┌─────────────────────┐                             ┌─────────────────────┐
         │     BERKEBUN        │                             │    TRANSAKSI        │
         │ (Tanam Ubi & Talas) │                             │ (Jual Panen → Gold) │
         └──────────┬──────────┘                             └──────────┬──────────┘
                    │                                                   │
                    ▼                                                   ▼
         ┌─────────────────────┐                             ┌─────────────────────┐
         │     MEMASAK         │                             │     WORKBENCH       │
         │ (Buat Food Buffs)   │                             │  (Upgrade Senjata)  │
         └──────────┬──────────┘                             └──────────┬──────────┘
                    │                                                   │
                    └─────────────────────────┬─────────────────────────┘
                                              │ Tidur di Kasur
                                              ▼
                                  ┌────────────────────────┐
                                  │      SIKLUS MALAM      │
                                  │     (Night Phase)      │
                                  └───────────┬────────────┘
                                              │
                                              ▼
                                  ┌────────────────────────┐
                                  │      NIGHT BRAWL       │
                                  │   (Gelombang Monster)  │
                                  └───────────┬────────────┘
                                              │
                                              ▼
                                  ┌────────────────────────┐
                                  │     MONSTER DROPS      │
                                  │ (Material Upgrade Baru)│
                                  └────────────────────────┘
```

---

## 2. Milestone Proyek

### Milestone 1: Fondasi Rumah, Dapur, & Sistem Inti (SELESAI ✅)
- [x] **Arsitektur Kamera Terpusat**: `CameraManager` dengan transisi mulus dan proteksi input.
- [x] **Sistem Memasak Instan**: `GenshinStove` & `StoveUIManager` berbasis TextMeshPro dengan layout Single Central Axis.
- [x] **Sistem Cuci Bahan Dinamis**: `KitchenSinkInteractable` berbasis item-level dirty/clean data.
- [x] **Pemberian Item MVP & Database Terpusat**: 33 item valid di `ItemDatabase` (8 Makanan, 2 Hasil Panen, 2 Bibit, 4 Bahan Masak, 4 Monster Drops, 1 Pedang Dummy, 12 Trophy).
- [x] **Kustomisasi Pakaian**: `PlayerOutfit` dan in-world `MirrorCamera` live preview.
- [x] **UI Scaling & ESC Priority Stack**: Resolusi referensi 1920×1080 dan penanganan tombol ESC berprioritas modal.
- [x] **Restrukturisasi Folder & Pembersihan Aset**: Repositori bersih, aset usang dihapus, dan diagram desain dipreservasi ke `Docs/MVP_Design/`.

---

### Milestone 2: Sistem Pertanian (Farming System) (BERIKUTNYA ⏳)
- [ ] **Sistem Lahan & Petak Tanah**: Petak tanah yang dapat dicangkul dan disiram air.
- [ ] **Siklus Pertumbuhan Tanaman**:
  - **Ubi Jalar (Sweet Potato)**: Waktu tumbuh cepat (*Fast*), nilai jual 750–1000 Gold.
  - **Talas (Taro)**: Waktu tumbuh sedang (*Medium*), nilai panen lebih tinggi.
- [ ] **Alur Panen (Harvesting)**: Menghasilkan item `Crop_SweetPotato` dan `Crop_Taro` yang masuk ke inventory pemain untuk dijual atau dimasak.

---

### Milestone 3: Sistem Pertarungan Malam (Night Brawl Combat)
- [ ] **Mekanik Senjata Utama (Weapon System)**:
  - 1 Tipe Senjata Melee awal (Pedang / Cangkul Tempur) dengan kombo serangan ringan, berat, dan dash attack.
- [ ] **Sistem Gelombang Musuh (5-Day Wave Progression)**:
  - **Day 1**: 1 Wave (Normal encounters).
  - **Day 2**: 2 Waves.
  - **Day 3**: 3 Waves.
  - **Day 4**: 4 Waves (Pengenalan Boss pertama + 4 musuh normal).
  - **Day 5**: 5 Waves (Pertarungan puncak: 2 Boss + 3 musuh normal).
- [ ] **Evolusi Musuh (2 Bibit → 4 Varian Musuh)**:
  - *Sweet Potato Form*: **Tuber Maw** (Normal) → **Cyclops Tuber Maw** (Boss).
  - *Taro Form*: **Taro Brute** (Normal) → **Taro Colossus** (Boss).
- [ ] **Sistem Loot Monster**:
  - Tuber Maw → `Mutated Root` (Normal Material).
  - Cyclops Tuber Maw → `Cyclops Eye` (Boss Material).
  - Taro Brute → `Hardened Root` (Normal Material).
  - Taro Colossus → `Colossus Core` (Boss Material).

---

### Milestone 4: Workbench & Upgrade Senjata
- [ ] **Interaksi Meja Kerja (Workbench / Garage)**: Antarmuka pembuatan dan peningkatan senjata.
- [ ] **Formula Biaya**: `Gold + Monster Material → Upgrade Senjata`.
- [ ] **Dua Jalur Peningkatan Cabang**:
  - **Jalur Ubi Jalar (Sweet Potato Path)**: Meningkatkan *Speed*, *Attack Speed*, dan *Mobility*.
  - **Jalur Talas (Taro Path)**: Meningkatkan *Power*, *Knockback*, *Armor*, dan efek defensif.

---

### Milestone 5: Audio, Visual Polish, & Game Feel
- [ ] **Efek Masak**: Partikel asap/api di kompor, animasi popup UI "Dish Cooked!", SFX menggoreng/merebus.
- [ ] **Modularisasi Dinding Dapur**: Memisahkan mesh dinding dapur menjadi segmen-segmen Layer 12 agar fade `WallOccluder` bekerja sempurna per sudut kamera.
- [ ] **Penggantian Model Trophy**: Mengganti kubus warna placeholder dengan model 3D piala dan monumen hasil buruan boss.

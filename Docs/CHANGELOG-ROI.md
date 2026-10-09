# Changelog (Roi)

Semua perubahan penting pada proyek ini oleh M-Roihan (Roi) dari `branch-roi-1` dicatat di halaman ini.
## - 2026-10-09

### Ditambahkan (Added)
- **Sinkronisasi Linimasa Waktu Siang (Day Phase Timeline)**:
  - Rekalibrasi kurva progresi waktu fase siang di [`DayNightTimeManager.cs`](file:///f:/unity/Farm-Beware/Assets/Scripts/Features/Time/DayNightTimeManager.cs) (16 menit real = 12 jam in-game, 06:00 hingga 18:00) yang memetakan jam in-game secara presisi dengan linimasa aktivitas.
  - Sistem Lonceng Senja (*Dusk Bell*) pada jam 15:45 in-game (13:30 menit real) dengan audio chime prosedural 4 harmoni lonceng desa (*Westminster chime*) pada [`DayNightAudioController.cs`](file:///f:/unity/Farm-Beware/Assets/Scripts/Features/Time/Atmosphere/DayNightAudioController.cs) dan floating text peringatan sisa 2.5 menit menuju malam.
  - Sistem *Auto-Sleep* pada jam 18:00 in-game (16:00 menit real) untuk transisi otomatis ke fase malam (*Night Brawl*).
  - Tampilan jam digital format `HH:mm` (mis. `06:00`, `15:45`) pada [`CombatPhaseTrackerUI.cs`](file:///f:/unity/Farm-Beware/Assets/Scripts/Features/Time/UI/CombatPhaseTrackerUI.cs) di pojok kanan atas HUD.

## - 2026-10-08

### Diubah (Changed)
- **Pembaruan Dependensi Paket & Sinkronisasi Editor**:
  - Memperbarui dependensi [`com.coplaydev.unity-mcp`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Packages/packages-lock.json) ke versi `v10.3.0` pada `Packages/packages-lock.json` untuk stabilitas integrasi Unity MCP toolset.
  - Sinkronisasi penuh cabang kerja `roi-branch` / `branch-roi-1` dengan cabang integrasi utama `Sprint-branch`.

## - 2026-10-01

### Ditambahkan (Added)
- **Sistem UI Character Sheet 3 Kolom (Tab UI)**:
  - Implementasi komponen [`PlayerStatsDisplayUI`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Player/UI/PlayerStatsDisplayUI.cs) untuk menampilkan atribut stat pemain secara real-time (Max Health, Max Stamina, Base Damage, Move Speed, Armor/Defense).
  - Implementasi komponen [`EquipmentSlotUI`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Player/UI/EquipmentSlotUI.cs) untuk 11 slot perlengkapan karakter: Helmet, Chest, Pants, Boots, Weapon, Shield, Gloves, Belt, Ring, Necklace, Cape.
  - Aset ikon perlengkapan di `Assets/Textures/Icons/Equipment/` (11 ikon slot + gambar preview statis karakter `Character_Static_Preview.png`).
  - Integrasi 3 kolom pada [`InventoryManagerUI`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Inventory/UI/InventoryManagerUI.cs): Kolom kiri (Stats), Kolom tengah (Player Inventory), dan Kolom kanan (Equipment) saat menekan tombol `Tab`.
- **Struktur Kompleks Pekarangan & Pagar Perimeter (Homestead Compound Yard)**:
  - Prefab [`HomesteadPerimeter.prefab`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Prefabs/Compound/HomesteadPerimeter.prefab): Pagar kayu pembatas pekarangan luar keliling dan gerbang utama di sektor utara.
  - Prefab [`GardenEnclosure.prefab`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Prefabs/Compound/GardenEnclosure.prefab) dan `GardenEnclosure_East.prefab`: Pagar pembatas area perkebunan (Farmland) sisi barat dan timur.
  - Prefab [`CobblestonePathways.prefab`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Prefabs/Compound/CobblestonePathways.prefab): Jalur setapak bebatuan yang menghubungkan pintu masuk teras depan rumah, gerbang utara, sumur air, dan area api unggun.
  - Prefab [`WaterWell.prefab`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Prefabs/Compound/WaterWell.prefab): Model sumur air di pekarangan timur lengkap dengan atap sirap dan ember tali.
  - Prefab [`RelaxationArea_Campfire.prefab`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Prefabs/Compound/RelaxationArea_Campfire.prefab): Area peristirahatan luar ruangan dengan susunan batu api unggun, abu, bara, dan bangku kayu.
  - Komponen [`CampfireLightFlicker.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Environment/CampfireLightFlicker.cs): Efek kedip cahaya api unggun dinamis menggunakan Perlin noise.
  - Prefab [`MerchantTable_Outdoor.prefab`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Prefabs/Compound/MerchantTable_Outdoor.prefab): Penataan modular untuk lapak meja pedagang di luar ruangan.
- **Vegetasi & Pengecatan Jalur Terrain (Foliage & Painted Terrain Paths)**:
  - Prefab pepohonan dan semak modular: [`Tree_Stylized_Pine.prefab`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Prefabs/Environment/Tree_Stylized_Pine.prefab), [`Tree_Stylized_Oak.prefab`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Prefabs/Environment/Tree_Stylized_Oak.prefab), dan [`Bush_Stylized.prefab`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Prefabs/Environment/Bush_Stylized.prefab).
  - Layer tekstur terrain baru: `TL_DirtPath.terrainlayer` (jalur tanah setapak) dan `TL_WetMud.terrainlayer` (lumpur basah).
  - Pengecatan tekstur jalur setapak dan batas pekarangan pada `New Terrain 1.asset`.
- **Integrasi Batas Compound pada Night Brawl**:
  - Penambahan metode `IsInsideCompound(Vector3 pos)` pada [`NightBrawlManager.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Combat/NightBrawlManager.cs) untuk memvalidasi batas pagar pekarangan (`X: [4.0, 38.0], Z: [2.0, 48.0]`).
  - Rekalibrasi titik tengah arena (`arenaCenter = (21f, 0.5f, 36f)`).
  - Pembagian 4 sektor spawn luar baru yang selaras dengan struktur compound:
    - Sektor 0: Penyerbu luar gerbang utama utara (`Z: 49-56, X: 17-25`).
    - Sektor 1: Pekarangan terbuka timur dekat sumur air (`X: 32-36.5, Z: 32-44`).
    - Sektor 2: Lorong pekarangan barat dekat pagar dan api unggun (`X: 5.5-7.5, Z: 32-42`).
    - Sektor 3: Plaza halaman depan teras rumah (`X: 18-24, Z: 29.5-33.5`).

### Diubah (Changed)
- **Optimasi Performa Rendering (GPU Instancing)**:
  - Mengaktifkan varian GPU Instancing (`m_EnableInstancingVariants: 1`) pada lebih dari 60 material proyek (material furnitur, lantai, dinding, tanaman, tanah, piala, dan TextMesh Pro).
  - Mengaktifkan rendering instanced pada terrain draw calls untuk memangkas overhead CPU saat me-render kompleks pekarangan dan pepohonan.

### Diperbaiki (Fixed)
- Mengatasi rintangan pergerakan pemain di pekarangan dengan memindahkan bollard batu ke tepian rumput dan menghapus collider non-esensial pada dekorasi jalan setapak.
- Memperbaiki ukuran collider petak tanah pada [`FarmlandTile.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Farming/FarmlandTile.cs) (`col.size = (1.2f, 0.18f, 1.2f)`) agar pas dengan gundukan tanah dan tidak memblokir langkah kaki pemain.

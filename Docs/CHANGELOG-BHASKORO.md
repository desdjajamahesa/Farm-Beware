# CHANGELOG - BHASKORO BRANCH

Dokumentasi perubahan fitur, integrasi sistem, dan penyesuaian aset dari branch `Bhaskoro-branch` yang diintegrasikan ke dalam `Sprint-branch`.

---

## 1. Multi-Slot Save & Load System (Fitur Utama)

Sistem penyimpanan data permainan modular berbasis JSON (`FeaturesSaveSystem`) yang mendukung slot penyimpanan tak terbatas, snapshot otomatis, dan restorasi status dunia permainan.

### Komponen & Arsitektur:
- [`GameSaveData.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/SaveSystem/GameSaveData.cs):
  - Model data komprehensif serializable untuk menyimpan:
    - **Player Snapshot**: Posisi 3D, rotasi, Health, Stamina, Hunger, Thirst.
    - **Inventory & Equipment**: Slot item tas, quickslot, item name, quantity, dan durabilitas.
    - **Economy**: Jumlah koin dan saldo `PlayerWallet`.
    - **Time & Day**: Nomor hari (`currentDay`), jam/waktu (`timeOfDay`), serta fase siang/malam.
    - **Farming**: Status 16 petak lahan (`TileState`), bibit yang tertanam (`SeedItemData`), progres pertumbuhan (`growthProgress`), dan sisa timer panen.
    - **Combat & Night Brawl**: Gelombang malam aktif (`currentWave`, `totalWaves`), status penyelesaian (`isNightEncounterCleared`), serta daftar entitas musuh aktif (`SavedEnemyData`: tipe musuh, HP, koordinat 3D, dan rotasi).
    - **Kitchen & Environment**: Status air botol, sink, dan kompor.
- [`SaveSystemManager.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/SaveSystem/SaveSystemManager.cs):
  - Singleton pengelola I/O penyimpanan ke file JSON pada direktori `Application.persistentDataPath`.
  - Fungsi Save Game, Load Game, Delete Slot, QuickSave, dan QuickLoad.
  - Penanganan transisi scene otomatis (`LoadAndApplySaveToScene`).
- [`SaveSystemUI.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/SaveSystem/SaveSystemUI.cs):
  - Tampilan visual pop-up menu Save & Load dengan daftar slot bergaya fantasy board.
  - Memiliki fitur auto-procedural UI generator (`BuildUI`) sehingga dapat dibangun secara otomatis pada `Canvas` tanpa ketergantungan hierarki statis manual.
  - Dilengkapi dialog konfirmasi Overwrite / Delete slot.
- [`SaveStationInteractable.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/SaveSystem/SaveStationInteractable.cs):
  - Titik interaksi di dalam rumah pemain (meja kamar tidur) yang mengimplementasikan `IInteractable` dengan label interaksi `"Save / Load"`.

---

## 2. Fantasy Main Menu & Pause Menu Overhaul

Pembaruan antarmuka menu utama dan pause menu dengan estetika stylized fantasy bernuansa kayu, pencahayaan glow, dan animasi interaktif.

### Aset & Scene:
- [`MainMenuScene.unity`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scenes/MainMenuScene.unity): Scene menu utama dengan pencahayaan dan tata letak baru.
- [`MainMenuUI.prefab`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Prefabs/UI/MainMenuUI.prefab) & [`PauseMenuUI.prefab`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Prefabs/UI/PauseMenuUI.prefab): Prefab UI menu baru dengan tekstur high-resolution.
- [`FantasyMenuAnimator.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/MainMenu/FantasyMenuAnimator.cs) & [`PauseMenuAnimator.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/MainMenu/PauseMenuAnimator.cs):
  - Animasi transisi tombol hover, scale bounce, rotasi plank signpost, dan glow pulsing.
- **Koleksi Tekstur UI**:
  - `Assets/Textures/MainMenu/*`: Background dual-world, vortex/galaxy glow, tombol Play, Exit, Settings bernuansa plank kayu, signpost, dan komponen slider/toggle kustom.
  - `Assets/Textures/PauseMenu/*`: Wooden card background, pause text glowing, round/square pause buttons, hover plank glow, dan selection arrows.

---

## 3. Integrasi & Additive Hooks pada Komponen Inti

Untuk mendukung persistensi Save/Load tanpa merusak logika gameplay di `Sprint-branch`:
- [`PlayerControl.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Player/PlayerControl.cs):
  - Penambahan method `Teleport(Vector3 position, Quaternion rotation)` untuk merestorasi posisi pemain tanpa glitch fisik Rigidbody.
- [`PlayerStats.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Player/PlayerStats.cs):
  - Penambahan method `RestoreStats(int hp, float stamina, float hunger, float thirst)`.
  - Pengecekan `Time.timeScale > 0f` untuk input debug (K / J).
- [`FarmlandTile.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Farming/FarmlandTile.cs):
  - Penambahan getter properti `GrowthDuration` dan `CurrentTimer`.
  - Penambahan method `RestoreCropState(TileState state, SeedItemData seed, float progress, float timer)`.
  - Pengecekan `Time.timeScale > 0f` untuk input debug H.
- [`DayNightTimeManager.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Time/DayNightTimeManager.cs):
  - Penambahan method `SetDayAndTime(int day, float targetHour)`.
  - Penundaan tick waktu saat Save UI terbuka (`SaveSystemUI.Instance.IsOpen`).
- [`CombatPhaseTrackerUI.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Time/UI/CombatPhaseTrackerUI.cs):
  - Visibilitas method `UpdatePhaseDisplay` diubah menjadi `public`.
- [`NightBrawlManager.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Combat/NightBrawlManager.cs):
  - Penambahan getter `CurrentWave` dan `TotalWaves`.
  - Dukungan overload coroutine `RoutineWaveLoop(int startWave)`.
  - Penambahan method `RestoreNightBrawlState(...)` dan coroutine `RoutineResumeWaveLoop()` untuk melanjutkan pertempuran malam yang tersimpan.
  - Pembersihan menyeluruh instance `EnemyBase` saat pertempuran berakhir.

---

## 4. Penanganan Konflik & Proteksi Proyek

- **ProjectSettings/TimeManager.asset**:
  - Ditemukan nilai `m_TimeScale: 0` pada Bhaskoro-branch yang dapat membekukan game secara permanen saat peluncuran.
  - Proteksi: Mempertahankan konfigurasi `Sprint-branch` (`m_TimeScale: 1`).
- **ProjectSettings/ProjectSettings.asset**:
  - Mempertahankan nama proyek orisinal `Farm-Beware` dari `Sprint-branch`.
- **Assets/Scenes/StagingScene.unity**:
  - Mempertahankan seluruh layout compound terbaru (Pond, Crafting Shelter, Scarecrow, Stepping Stones, 16 Plot Pertanian, Sistem Occlusion Pohon).
  - Melakukan grafting murni pada 3 GameObject Save System (`SaveSystemManager`, `SaveSystemUI`, dan meja interaksi `SaveStationInteractable`).
- **File Scratch**:
  - Pembersihan 34 file gambar sementara di root repository (`crop_*.png`, `scratch_*.png`, `test_*.png`, `*.bak`).

---

## 5. QA Bug Fixes, Console Teardown Resolution, & Water Mechanics Overhaul

### A. Console Assertion & Scene Teardown Guards:
- [`SaveSystemUI.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/SaveSystem/SaveSystemUI.cs):
  - Penambahan properti `HasInstance => instance != null` dan guard `isApplicationQuitting` pada `Instance` getter.
  - Mematikan pembuatan otomatis GameObject baru saat aplikasi atau scene sedang di-teardown/unloaded, menyelesaikan error `go.IsActive()` dan `SaveSystemUI scene cleanup leak`.
- [`DeathScreenUI.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Player/UI/DeathScreenUI.cs):
  - Memvalidasi `SaveSystemUI.HasInstance` sebelum melepas event `OnSaveUIClosed` pada `OnDisable()`.
- [`MainMenuController.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/MainMenu/MainMenuController.cs):
  - Memvalidasi `SaveSystemUI.HasInstance` pada `CloseAllActiveOverlays()`.

### B. Pembersihan UI Bar Darah Monster saat Load Game:
- [`EnemyHealthBarManager.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Combat/UI/EnemyHealthBarManager.cs):
  - Method `ReleaseAllBars()` diubah menjadi `public` dengan null-safety.
- [`BossHealthBarManager.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Combat/UI/BossHealthBarManager.cs):
  - Method `ClearAllBosses()` diubah menjadi `public` dengan null-safety.
- [`EnemyBase.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Combat/Enemy/EnemyBase.cs):
  - Penambahan lifecycle `OnDestroy()` untuk melepaskan bar darah melayang dan boss HUD secara otomatis saat musuh di-destroy langsung.
- [`NightBrawlManager.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Combat/NightBrawlManager.cs) & [`SaveSystemManager.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/SaveSystem/SaveSystemManager.cs):
  - Pemanggilan eksplisit `ReleaseAllBars()` dan `ClearAllBosses()` saat memuat data save game in-place.

### C. Audit & Perbaikan Night Brawl Spawning:
- [`NightBrawlManager.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Combat/NightBrawlManager.cs):
  - Mengoreksi evaluasi `isEncounterDone`: kondisi `(!isBrawlActive && !hasSavedEnemies)` tidak lagi salah menandai malam sebagai selesai saat memuat permainan di fase malam.
  - Me-reset `isWaveInProgress = false` saat memuat sesi malam agar pemanggilan wave dan suspense delay tidak terblokir.

### D. Inventory TAB Toggle (Buka & Tutup):
- [`InventoryManagerUI.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Inventory/UI/InventoryManagerUI.cs):
  - Menyatukan listener tombol TAB dan I pada method `Update()` dengan guard `frameInventoryOpened` untuk mencegah double-toggle pada frame yang sama.
  - Pemain kini dapat membuka tas dengan TAB dan menutupnya kembali secara instan dengan menekan tombol TAB.

### E. Pemisahan Item Air (Water Bottle vs Plant Waterer):
- [`PlayerWaterBottle.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Kitchen/PlayerWaterBottle.cs):
  - Diformulasikan khusus untuk minum pemain dengan kapasitas **4 Charges (tegukan)**.
  - Setiap tegukan memulihkan +25 Thirst/Hydration.
  - **HANYA DAPAT DIISI ULANG DI KITCHEN SINK** (tidak dapat mengisi ulang di Garden Well).
- [`PlantWaterer.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Farming/PlantWaterer.cs):
  - Komponen baru pengelola air perkakas kebun dengan kapasitas **100/100 L** (10L per siraman petak).
  - Terdaftar sebagai singleton dan service mandiri.
- [`WaterWellInteractable.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Farming/WaterWellInteractable.cs):
  - Komponen interaksi baru pada `WaterWell` di kebun (`IInteractable` dengan label `[ E ] Refill Plant Waterer`).
  - Memvalidasi kepemilikan alat `Plant Waterer` dan mengisinya hingga 100L.
  - Menolak pengisian botol minum pemain.
- [`FarmlandTile.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Farming/FarmlandTile.cs) & [`PlayerFarmInteraction.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Farming/Adapters/PlayerFarmInteraction.cs):
  - Wajib memegang **Plant Waterer** (`tool_plant_waterer`) di slot hotbar aktif untuk dapat menyiram tanaman kering (`TileState.PlantedDry`).
  - Menampilkan feedback visual jika pemain mencoba menyiram tanpa memegang Plant Waterer atau jika air di dalam Plant Waterer habis.
  - Auto-wiring `WaterWellInteractable` pada GameObject `WaterWell` saat inisialisasi dependensi pertanian.
- [`Tool_PlantWaterer.asset`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Data/Items/Equipment/Tool_PlantWaterer.asset):
  - ScriptableObject perkakas baru terdaftar pada `ItemDatabase.asset` dan `ItemRegistrySO.asset`.
- [`GameSaveData.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Data/GameSaveData.cs) & [`SaveSystemManager.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/SaveSystem/SaveSystemManager.cs):
  - Menyimpan dan memuat `plantWatererAmount` (0-100L) dan `waterBottleAmount` (0-4 charges) dengan migrasi kompatibilitas save lama.
- [`PlayerStatsDisplayUI.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Player/UI/PlayerStatsDisplayUI.cs):
  - Menampilkan status air terpisah pada jendela Character Sheet (TAB): `{cur}/{max} Sips | {cur}/{max}L Farm`.
- [`SinkManager.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Kitchen/SinkManager.cs):
  - Tombol refill wastafel dapur kini memperbarui Bottle of Water hingga 4 Charges.


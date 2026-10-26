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

## 5. Organic Cloud Screen Transitions (2026-10-08)

Sistem transisi visual sinematik berbasis awan organik berbulu (*organic feathered clouds*) yang menutupi seluruh viewport layar saat berpindah dari Menu Utama ke dalam game.

### Komponen & Aset:
- [`CloudTransitionManager.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Common/CloudTransitionManager.cs):
  - Komponen singleton mandiri dengan metode `TransitionToScene(string sceneName, Action onCovered)` dan `EnsureInstance()`.
  - Mengatur interpolasi posisi lapisan formasi awan kiri dan kanan secara horizontal dengan timing sinematik (`coverDuration = 0.40f`, `revealDuration = 0.40f`, `revealDelayAfterSceneLoad = 0.05f`).
- [`CloudTransitionCanvas.prefab`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Prefabs/CloudTransitionCanvas.prefab) & [`CloudTransitionCanvas.prefab`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Resources/Prefabs/CloudTransitionCanvas.prefab):
  - Prefab canvas overlay dengan hierarki layer awan berlapis (*Bank Bottom/Top/Left/Right, Billow, Organic Large/Medium/Puff/Wisp*).
- **Aset Tekstur Awan**:
  - 44 aset sprite tekstur awan resolusi tinggi dengan transparansi alpha lembut di `Assets/Art/Textures/Clouds/` dan `Assets/Resources/Textures/Clouds/`.
- **Integrasi Gameplay & Menu**:
  - [`MainMenuController.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/MainMenu/MainMenuController.cs): Mengarahkan `StartGameRoutine` untuk memanggil `CloudTransitionManager.EnsureInstance().TransitionToScene(targetSceneName)`.
  - [`FantasyMenuAnimator.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/MainMenu/FantasyMenuAnimator.cs): Menyelaraskan durasi `enterTransitionDuration = 0.40f` dengan animasi awan.
  - [`MainMenuScene.unity`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scenes/MainMenuScene.unity): Menautkan hierarki transisi ke scene menu utama.

---

## 6. Death Screen UI, Player Respawn Lifecycle & Save System Hardening (2026-10-15)

Pengembangan sistem penanganan kematian pemain (*Death Scene & Respawn Lifecycle*), proteksi kondisi kegagalan pada antarmuka penyimpanan, koordinasi ekonomi harian, dan hardening restorasi data save/load (`bas-branch`, diintegrasikan via commit `86ebfd7` & merge commit `224f365`).

### Komponen & Arsitektur:
- [`DeathScreenUI.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Player/UI/DeathScreenUI.cs):
  - Layar modal kekalahan pemain saat Health mencapai 0, menampilkan efek redup dramatis, pesan narasi pingsan, dan opsi memuat simpanan terakhir (*Load Game*) atau respawn keesokan harinya di tempat tidur.
  - Penanganan transisi bersih saat load save berhasil (`HandleLoadCompleted`): me-reset state browsing, membuka interaktivitas tombol (`SetButtonsInteractable(true)`), dan menutup layar seketika.
- [`PlayerRespawnController.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Player/PlayerRespawnController.cs):
  - Mengelola pemulihan pemain setelah pingsan: memindahkan koordinat pemain ke tempat tidur rumah (`BedInteractable`), menyetel jam menjadi pukul 06:00 pagi keesokan harinya, dan merestorasi status vital pemain.
  - Memastikan re-aktivasi input controller (`playerControl.SetInputActive(true)`) sehingga pemain dapat langsung bergerak setelah transisi bangun tidur selesai.
- [`SaveSystemManager.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/SaveSystem/SaveSystemManager.cs):
  - **Void Fallback Protection**: Mendeteksi jika posisi koordinat hasil load berada di (0,0,0) atau di bawah batas jurang tanah (`y < -20f`), secara otomatis mengarahkan titik teleport ke posisi tempat tidur (`(27.1f, 0.5f, 21.04f)`) untuk mencegah pemain jatuh ke dalam void.
  - **Vital Floor Guarantees**: Menjamin nilai HP minimal 1 dan Stamina/Hunger/Thirst minimal 10 unit saat data dimuat, mencegah pemain mengalami *death loop* langsung setelah memuat file save.
  - **Input State Unlock**: Menghapus kunci kontrol (`playerCtx.IsInputLocked = false`, `playerCtx.SetControlLock(false)`) saat restorasi save tuntas.
- [`SaveSystemUI.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/SaveSystem/SaveSystemUI.cs):
  - **Dead State Guard**: Mencegah operasi Save atau Overwrite ketika pemain sedang dalam kondisi pingsan/mati (`playerCtx.Health <= 0`) dan menampilkan pesan peringatan merah `"Cannot save/overwrite while collapsed!"`.
  - **Hierarki Dialog Konfirmasi**: Menjamin inisialisasi hierarki dan pendengar tombol pada `ShowConfirmDialog` dengan penempatan `modalPanel.transform.SetAsLastSibling()`.
- [`DailyEconomyManager.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Economy/DailyEconomyManager.cs):
  - Mengintegrasikan kalkulasi penalti biaya perawatan dan sinkronisasi laporan ekonomi harian saat siklus pergantian hari dipicu oleh respawn kematian.
- [`MainMenuController.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/MainMenu/MainMenuController.cs):
  - Integrasi pemilihan slot penyimpanan langsung dari menu utama dengan pengecekan ketersediaan data simpanan sebelum memulai sesi permainan.

---

## 7. Main Menu Button Art Overhaul & Animated Corner Leaf/Rice Decoration (2026-10-17)

Pembaruan grafis tombol menu utama dan pengayaan visual menggunakan ornamen tanaman padi dan daun beranimasi prosedural di sudut setiap tombol (`bas-branch`, diintegrasikan via commit `dec870e` & merge commit `8408d2c`).

### Komponen & Arsitektur:
- [`PadiDaunAnimator.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/MainMenu/PadiDaunAnimator.cs):
  - Mengelola animasi bergoyang (*procedural sway & breathing motion*) untuk ornamen padi dan daun di sudut tombol menu utama.
  - Memanfaatkan fungsi sinusoidal (`Mathf.Sin`), variasi rotasi sudut Z, dan interpolasi skala halus agar ornamen tampak hidup dan merespons angin latar.
- [`UIAnimationHandler.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/MainMenu/UIAnimationHandler.cs):
  - Event listener kustom untuk interaksi tombol UI, menangani event `OnPointerEnter`, `OnPointerExit`, `OnPointerDown`, dan `OnPointerUp`.
  - Memberikan transisi pembesaran skala halus (*scale bounce feedback*) saat kursor mouse menyorot atau menekan tombol menu.

### Aset Seni, Tekstur & Prefab:
- **Koleksi Tekstur Sudut Dekoratif**:
  - `Assets/Art/Textures/MainMenu/Btn_LeafRice_Corner.png`: Aset ornamen tangkai padi dan daun untuk sudut tombol.
  - `Assets/Art/Textures/MainMenu/Padi_Daun_Decoration.png`: Aset ilustrasi tanaman padi beresolusi tinggi.
- **Pembaruan Artwork Tombol Menu Utama**:
  - `Assets/Art/Textures/MainMenu/Btn_NewGame.png`: Desain visual baru tombol New Game.
  - `Assets/Art/Textures/MainMenu/Btn_LoadGame.png`: Desain visual baru tombol Load Game.
  - `Assets/Art/Textures/MainMenu/Btn_Settings.png` & `Btn_Setting.png`: Desain visual baru tombol Pengaturan.
  - `Assets/Art/Textures/MainMenu/Btn_Exit.png`: Desain visual baru tombol Keluar.
  - `Assets/Art/Textures/MainMenu/Btn_Play.png`: Desain tombol cadangan/alternatif.
- [`MainMenuUI.prefab`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Prefabs/UI/MainMenuUI.prefab):
  - Rekonfigurasi hierarki layout canvas menu utama dengan ukuran tombol yang lebih proporsional, perataan anchor terpusat, dan penyematan GameObject dekorasi sudut beranimasi pada tiap tombol.
- [`MainMenuScene.unity`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scenes/MainMenuScene.unity):
  - Sinkronisasi instansiasi prefab UI dan event trigger pada scene menu utama.

---

## 8. Subsistem Side Quest 'First Steps' & Integrasi Onboarding Pertanian (2026-10-26)

Implementasi alur misi pembuka (*Side Quest: First Steps*) yang membimbing pemain baru melalui langkah-langkah dasar berkebun di hari pertama (`bhaskoro-branch`, diintegrasikan ke `Rafi-branch` via commit `f31db09`).

### Komponen & Arsitektur:
- [`QuestProgressModel.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Quest/Core/QuestProgressModel.cs):
  - Model data quest deterministik murni POCO Zero-GC (`FeaturesQuest.Core`) yang melacak status pencapaian objektif:
    1. Mengambil cangkul (`tool_hoe`).
    2. Mengambil kaleng penyiram (`tool_plant_waterer`).
    3. Mengambil botol air bersih (`food_bottle_water`).
    4. Mengambil bibit ubi jalar (`seed_sweet_potato`).
    5. Mencangkul tanah, menanam bibit, dan menyiram petak lahan.
- [`QuestManager.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Quest/QuestManager.cs):
  - Manager singleton yang memantau perubahan inventori pemain (`OnInventoryChanged`) dan mendengarkan event sinyal pertanian dari `FarmlandTile`.
- [`QuestHUDUI.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Quest/UI/QuestHUDUI.cs):
  - Kartu antarmuka HUD otomatis berestetika glassmorphism di sudut kanan atas layar di bawah jam digital, menampilkan daftar checklist interaktif secara real-time.
- [`PlayerStarterKit.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Player/PlayerStarterKit.cs):
  - Penyesuaian distribusi perlengkapan awal di peti penyimpanan rumah dan halaman untuk mendukung alur pengambilan item side quest.
- [`GameplayAndHUDTests.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Editor/GameplayAndHUDTests.cs):
  - Penambahan unit test otomatis **Test 62** untuk memvalidasi evaluasi logika quest, transisi checklist, dan deteksi sinyal petak lahan.




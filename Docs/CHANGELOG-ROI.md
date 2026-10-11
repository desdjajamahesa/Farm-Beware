# Changelog (Roi)

Semua perubahan penting pada proyek ini oleh M-Roihan (Roi) dari `branch-roi-1` dicatat di halaman ini.

## - 2026-10-11

### Ditambahkan (Added)
- **Sistem Linimasa Aktivitas Siang Hari & Widget HUD Persisten (`DayActivityTimelineManager.cs`)**:
  - Mengimplementasikan [`DayActivityTimelineManager.cs`](file:///f:/unity/Farm-Beware/Assets/Scripts/Features/Time/DayActivityTimelineManager.cs) yang mengelola 10 tahapan linimasa aktivitas harian ideal (18 menit / 06:00 - 18:00) sesuai tabel desain game:
    1. `06:00 - 06:40` (0-60 detik real): *Collect overnight harvest, sell, plant Taro/Corn + Sweet Potato*
    2. `06:40 - 08:00` (60-180 detik real): *Fetch water, start the pot (cooking runs in the background)*
    3. `08:00 - 10:20` (180-390 detik real): *Upgrade at the Workbench, trade*
    4. `10:20 - 11:00` (390-450 detik real): *Sweet Potato cycle 1 ripens: harvest, sell, replant*
    5. `11:00 - 14:20` (450-750 detik real): *Cook food, upgrade, expand*
    6. `14:20 - 15:00` (750-810 detik real): *Sweet Potato cycle 2 ripens: harvest, replant*
    7. `15:00 - 16:00` (810-900 detik real): *Finalize 3 food slots, check water*
    8. `16:00 - 16:20` (900-930 detik real): *Dusk bell DING DONG: 3 minutes until nightfall!*
    9. `16:20 - 17:00` (930-990 detik real): *Taro/Corn ripen: harvest, sell, replant*
    10. `17:00 - 18:00` (990-1080 detik real): *Go home, eat buff food, go to bed*
  - Menghubungkan kartu HUD aktivitas persisten pada [`CombatPhaseTrackerUI.cs`](file:///f:/unity/Farm-Beware/Assets/Scripts/Features/Time/UI/CombatPhaseTrackerUI.cs) di bawah jam digital. Teks panduan aktivitas tidak menghilang dan terus bertahan hingga aktivitas berikutnya aktif; kartu otomatis disembunyikan saat malam hari tiba.
  - Menambahkan alert transisi aktivitas dengan suara synthesizer melodik 2-nada (G5 -> C6) dan toast floating text di atas kepala pemain saat pergantian aktivitas terjadi.

### Diubah (Changed)
- **Rekalibrasi Linimasa Fase Siang Menjadi 18 Menit (1080 Detik Real)**:
  - Mengubah durasi fase siang hari (`daytimeDurationRealSeconds`) dari 960 detik (16 menit) menjadi **1080 detik (18 menit)** pada [`DayNightTimeManager.cs`](file:///f:/unity/Farm-Beware/Assets/Scripts/Features/Time/DayNightTimeManager.cs) dan [`StagingScene.unity`](file:///f:/unity/Farm-Beware/Assets/Scenes/StagingScene.unity).
  - Mengkalibrasi peringatan *Dusk Bell* menjadi jam **16:00 in-game (menit ke-15:00 real / 900 detik)** dengan notifikasi `"🔔 Dusk Bell: 3 minutes until nightfall! Prepare for battle!"`.
- **Penghapusan Mode Cepat 12 Menit (Keybind '9')**:
  - Menghapus shortcut keybind tombol `9` dan field `fastForwardMultiplier12Min` pada [`DayNightTimeManager.cs`](file:///f:/unity/Farm-Beware/Assets/Scripts/Features/Time/DayNightTimeManager.cs) serta `.agents/rules/run.md`.
  - Mempertahankan mode fast-forward **8x (Keybind '0')** untuk keperluan testing dan fast-forwarding eksplorasi siang.
- **Rekalibrasi Durasi Pertumbuhan Tanaman & Integrasi Sistem Waktu Siang**:
  - Menyesuaikan `growthDuration` pada bibit tanaman sesuai spesifikasi tabel ekonomi:
    - **Sweet Potato** ([`Seed_SweetPotato.asset`](file:///f:/unity/Farm-Beware/Assets/Data/Items/Seeds/Seed_SweetPotato.asset)): 360 detik (Fast: 6 menit).
    - **Taro** ([`Seed_Taro.asset`](file:///f:/unity/Farm-Beware/Assets/Data/Items/Seeds/Seed_Taro.asset)): 900 detik (Medium: 15 menit).
    - **Corn** ([`Seed_Corn.asset`](file:///f:/unity/Farm-Beware/Assets/Data/Items/Seeds/Seed_Corn.asset)): 900 detik (Medium: 15 menit).
  - Menyinkronkan laju pertumbuhan tanaman di [`FarmlandTile.cs`](file:///f:/unity/Farm-Beware/Assets/Scripts/Features/Farming/FarmlandTile.cs) dengan `DayNightTimeManager.Instance.FastForwardMultiplier` sehingga saat waktu siang dipercepat (shortcut `9` [1.33x] atau `0` [8x]), pertumbuhan tanaman ikut terakselerasi secara proporsional.
  - Memperbarui format tampilan hitungan mundur di [`CropGrowthCountdownUI.cs`](file:///f:/unity/Farm-Beware/Assets/Scripts/Features/Farming/UI/CropGrowthCountdownUI.cs) dan label petak tanah agar menampilkan menit:detik rapi (contoh: `6:00`, `14:59`) ketika sisa waktu $\ge 60$ detik.

### Diperbaiki (Fixed)
- **Perbaikan Animasi Heavy Attack Berjalan Dua Kali**:
  - Menghapus trigger ganda `attackTriggerName` pada `TryPerformHeavyAttack()` di [`PlayerEquipment.cs`](file:///f:/unity/Farm-Beware/Assets/Scripts/Player/PlayerEquipment.cs) dan membersihkan transisi duplikat di [`AsepAnimator.controller`](file:///f:/unity/Farm-Beware/Assets/Resources/Player/AsepAnimator.controller) dari AnyState ke `Attack_Heavy` agar animasi charged strike hanya dieksekusi 1 kali saat tombol dilepas.
  - Menghubungkan pelacakan coroutine `RoutineHeavyAttack()` di [`PlayerControl.cs`](file:///f:/unity/Farm-Beware/Assets/Scripts/Player/PlayerControl.cs) ke `attackCoroutine`.

## - 2026-10-10

### Ditambahkan (Added)
- **Integrasi Animasi Slide ('C') dan Parry ('V') pada Player**:
  - Menghubungkan klip animasi dari `Assets/Art/Animations/Player/Mixamo/` ke [`AsepAnimator.controller`](file:///f:/unity/Farm-Beware/Assets/Resources/Player/AsepAnimator.controller):
    - **Slide ('C')**: Menggunakan motion klip dari `crouch idle.fbx` pada state `Sliding`, bertransisi reaktif dari `AnyState` saat parameter `Sliding == true` dan kembali ke `Idle`/`Moving` saat `Sliding == false`.
    - **Parry ('V')**: Menggunakan motion klip dari `standing block idle.fbx` pada state baru `Parry_Guard` dengan trigger parameter `Parry`. Memperbaiki method `PerformParry()` pada [`PlayerControl.cs`](file:///f:/unity/Farm-Beware/Assets/Scripts/Player/PlayerControl.cs) yang sebelumnya secara keliru memicu animasi serang (`Attack`).
- **Tampilan Jam Digital Berkelipatan 15 Menit In-Game (`CombatPhaseTrackerUI.cs`)**:
  - Menambahkan konfigurasi `clockMinuteStep = 15` pada [`CombatPhaseTrackerUI.cs`](file:///f:/unity/Farm-Beware/Assets/Scripts/Features/Time/UI/CombatPhaseTrackerUI.cs) sehingga jarum menit jam digital HUD bergerak setiap 15 menit in-game (`:00`, `:15`, `:30`, `:45`).
  - Berdasarkan linimasa 16 menit (960 detik = 12 jam), 15 menit in-game berganti tepat setiap **20 detik di dunia nyata** (dan 2.5 detik saat fast-forward 8x). Ini memberikan estetika RPG santai (seperti *Stardew Valley* / *Animal Crossing*) sekaligus memangkas operasi redraw teks UI hingga 93%.
- **Integrasi 18 Modul Resmi Unity Technologies Skills (`.agents/skills/`)**:
  - Memasang 18 skill resmi dari Unity Technologies yang disaring khusus untuk kebutuhan game *Farm-Beware* (2D pixel perfect, 2D physics, tile palette/rule tiles, sprite atlas, sprite editor, audio mixers, optimize audio, UI uGUI/UITK, TextMeshPro, URP post-processing, URP render graph, NavMesh AI, Project Auditor, dan Localization).

### Diubah (Changed)
- **Sistem Dual Fast-Forward Waktu Siang (Shortcut '9' untuk Mode 12 Menit & Shortcut '0' untuk Mode 8x) (`DayNightTimeManager.cs`)**:
  - Menetapkan durasi normal siang hari tetap pada basis default **16 menit (960 detik)**.
  - Menambahkan shortcut tombol **`9`** (Alpha 9 & Numpad 9) untuk mengaktifkan **Mode Siang 12 Menit** dengan rasio percepatan **1.33x** (`16f / 12f = ~1.333x`), sehingga waktu siang 16 menit dipercepat dan selesai tepat dalam 12 menit waktu nyata.
  - Mempertahankan shortcut tombol **`0`** (Alpha 0 & Numpad 0) untuk mode **Fast-Forward 8x** (siang hari selesai dalam 2 menit / 120 detik).
  - Tampilan indikator kecepatan pada jam digital HUD ([`CombatPhaseTrackerUI.cs`](file:///f:/unity/Farm-Beware/Assets/Scripts/Features/Time/UI/CombatPhaseTrackerUI.cs)) otomatis menampilkan badge cyan `▶▶ 1.33x` saat mode 9 aktif dan `▶▶ 8x` saat mode 0 aktif.
  - Floating text di atas kepala pemain memberi feedback spesifik (`⏩ Time Speed: 1.33x (12-Min Day Mode)` atau `⏩ Time Speed: 8x (Fast-Forward Mode)`).
  - Kedua mode percepatan otomatis di-reset kembali normal (1x) saat malam hari (*Night Brawl*) dimulai.

### Diperbaiki (Fixed)
- **Pembersihan Guard Jam Pagi 07:00-07:15 pada Save/Load & Runtime**:
  - Menghapus pengecekan hardcoded `targetHour >= 6.99f && targetHour <= 7.25f` pada [`DayNightTimeManager.cs`](file:///f:/unity/Farm-Beware/Assets/Scripts/Features/Time/DayNightTimeManager.cs) dan [`SaveSystemManager.cs`](file:///f:/unity/Farm-Beware/Assets/Scripts/Features/SaveSystem/SaveSystemManager.cs) sehingga pemain yang menyimpan game pada rentang pagi hari (07:00 - 07:15 in-game) tidak lagi ter-reset mundur ke 06:00.

### Dihapus (Removed)
- **Animasi & Logika Tendangan 'Q' dan Emote 'T' Player**:
  - Menghapus input keybind 'Q' (tendangan knockback) dan 'T' (battlecry taunt emote) pada [`PlayerControl.cs`](file:///f:/unity/Farm-Beware/Assets/Scripts/Player/PlayerControl.cs) serta coroutine `RoutineKick()`.
  - Menghapus konfigurasi tendangan (`kickStaminaCost`, `kickDamage`, dll.) dan method `TryPerformKick()` pada [`PlayerEquipment.cs`](file:///f:/unity/Farm-Beware/Assets/Scripts/Player/PlayerEquipment.cs).
  - Menyembunyikan widget `txtKickDamage` pada [`PlayerStatsDisplayUI.cs`](file:///f:/unity/Farm-Beware/Assets/Scripts/Player/UI/PlayerStatsDisplayUI.cs).
  - Menghapus state `Attack_Kick`, state `Taunt_Battlecry`, beserta parameter trigger `Kick` dan `Taunt` dari Animator Controller [`AsepAnimator.controller`](file:///f:/unity/Farm-Beware/Assets/Resources/Player/AsepAnimator.controller).

## - 2026-10-09

### Ditambahkan (Added)
- **Integrasi Suite Skill Antigravity Ponytail (`.agents/skills/`)**:
  - Memasang 6 modul skill Ponytail ([`ponytail`](file:///f:/unity/Farm-Beware/.agents/skills/ponytail/SKILL.md), [`ponytail-review`](file:///f:/unity/Farm-Beware/.agents/skills/ponytail-review/SKILL.md), [`ponytail-audit`](file:///f:/unity/Farm-Beware/.agents/skills/ponytail-audit/SKILL.md), [`ponytail-debt`](file:///f:/unity/Farm-Beware/.agents/skills/ponytail-debt/SKILL.md), [`ponytail-gain`](file:///f:/unity/Farm-Beware/.agents/skills/ponytail-gain/SKILL.md), [`ponytail-help`](file:///f:/unity/Farm-Beware/.agents/skills/ponytail-help/SKILL.md)) pada direktori [`.agents/skills/`](file:///f:/unity/Farm-Beware/.agents/skills) untuk evaluasi kode lean, deteksi technical debt, dan code review otomatis berbasis AI agent.
- **Shortcut Percepatan Waktu 8x Siang Hari (Keybind '0')**:
  - Menambahkan shortcut toggle tombol `0` (Alpha 0 & Numpad 0) pada [`DayNightTimeManager.cs`](file:///f:/unity/Farm-Beware/Assets/Scripts/Features/Time/DayNightTimeManager.cs) untuk mempercepat laju waktu fase siang sebanyak 8x lipat (`fastForwardMultiplier = 8.0f`) demi mempermudah testing dan fast-forwarding ke senja/malam.
  - Memberikan feedback visual langsung berupa floating text di atas kepala pemain (`⏩ Time Speed: 8x (Fast-Forward)`) dan lencana status `▶▶ 8x` berwarna cyan di samping jam digital HUD pada [`CombatPhaseTrackerUI.cs`](file:///f:/unity/Farm-Beware/Assets/Scripts/Features/Time/UI/CombatPhaseTrackerUI.cs).
  - Kecepatan otomatis di-reset kembali normal (1x) saat malam hari (*Night Brawl*) dimulai.
- **Sinkronisasi Linimasa Waktu Siang (Day Phase Timeline)**:
  - Rekalibrasi kurva progresi waktu fase siang di [`DayNightTimeManager.cs`](file:///f:/unity/Farm-Beware/Assets/Scripts/Features/Time/DayNightTimeManager.cs) (16 menit real = 12 jam in-game, 06:00 hingga 18:00) yang memetakan jam in-game secara presisi dengan linimasa aktivitas.
  - Sistem Lonceng Senja (*Dusk Bell*) pada jam 15:45 in-game (13:30 menit real) dengan audio chime prosedural 4 harmoni lonceng desa (*Westminster chime*) pada [`DayNightAudioController.cs`](file:///f:/unity/Farm-Beware/Assets/Scripts/Features/Time/Atmosphere/DayNightAudioController.cs) dan floating text peringatan sisa 2.5 menit menuju malam.
  - Sistem *Auto-Sleep* pada jam 18:00 in-game (16:00 menit real) untuk transisi otomatis ke fase malam (*Night Brawl*).
  - Tampilan jam digital format `HH:mm` (mis. `06:00`, `15:45`) pada [`CombatPhaseTrackerUI.cs`](file:///f:/unity/Farm-Beware/Assets/Scripts/Features/Time/UI/CombatPhaseTrackerUI.cs) di pojok kanan atas HUD.

### Diperbaiki (Fixed)
- **Perbaikan Jam Digital Diam (07:08) & Aktivasi Waktu Kontinu**:
  - Mengatasi nilai ter-serialize lama `useContinuousTime: 0` pada [`StagingScene.unity`](file:///f:/unity/Farm-Beware/Assets/Scenes/StagingScene.unity) yang menyebabkan loop `Update()` di [`DayNightTimeManager.cs`](file:///f:/unity/Farm-Beware/Assets/Scripts/Features/Time/DayNightTimeManager.cs) langsung return dan waktu terhenti.
  - Memperbarui inisialisasi `Awake()` dan `Start()` untuk meng-enforce `useContinuousTime = true`, `dayStartHour = 6.0f`, dan memigrasi jam pagi legacy `07:08` (`7.14f`) dari save file lama agar kembali ke awal pagi `06:00`.
  - Menambahkan loop sinkronisasi `Update()` pada [`CombatPhaseTrackerUI.cs`](file:///f:/unity/Farm-Beware/Assets/Scripts/Features/Time/UI/CombatPhaseTrackerUI.cs) sehingga jarum jam digital selalu ter-refresh secara real-time.

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

## [Unreleased] - 2026-10-12

### Ditambahkan (Added)
- **Sistem Timeline Aktivitas Fase Siang (18 Menit)**:
  - Implementasi jadwal 10 aktivitas siang pada [`DayActivityTimelineManager.cs`](file:///f:/unity/Farm-Beware/Assets/Scripts/Features/Time/DayActivityTimelineManager.cs) yang persis dengan tabel spesifikasi dokumen desain (06:00 - 18:00 in-game, 18 menit real time).
  - Tampilan banner notifikasi aktivitas persisten yang tetap aktif hingga masuk ke blok aktivitas berikutnya.
  - Sinyal audio & visual lonceng senja (Dusk Bell) pada menit ke-15:00 (16:00 in-game) sebagai peringatan 3 menit tersisa.
  - Fitur tidur paksa otomatis (Auto-Sleep) pada menit ke-18:00 (18:00 in-game).
- **Dialog Modal Konfirmasi Tidur Siang Lebih Awal (Early Sleep Confirmation UI)**:
  - Komponen [`BedSleepConfirmationUI.cs`](file:///f:/unity/Farm-Beware/Assets/Scripts/Features/Interaction/BedSleepConfirmationUI.cs) yang menampilkan peringatan saat pemain mencoba tidur siang jika terdapat tanaman yang hampir panen ($\le$ 180 detik atau siap panen).
  - Integrasi [`IFarmingService.cs`](file:///f:/unity/Farm-Beware/Assets/Scripts/Core/Contracts/IFarmingService.cs) dan [`FarmingManager.cs`](file:///f:/unity/Farm-Beware/Assets/Scripts/Features/Farming/FarmingManager.cs) via `ServiceLocator` pada [`BedInteractable.cs`](file:///f:/unity/Farm-Beware/Assets/Scripts/Features/Interaction/BedInteractable.cs) dengan tombol konfirmasi "Sleep Anyway" dan "Cancel" (termasuk tombol Escape).
- **Pembatasan Slot Makanan Fase Malam**:
  - Validasi maksimal 3 slot makanan aktif saat transisi memasuki Night Brawl pada [`NightBrawlManager.cs`](file:///f:/unity/Farm-Beware/Assets/Scripts/Features/Combat/NightBrawlManager.cs).

### Diubah (Changed)
- **Kalibrasi Resep Memasak & Item Makanan (Cooking & Food Balance)**:
  - Durasi memasak dan konsumsi air:
    - Kategori Recovery (20 detik masak, 10L air): `Recipe_CornPorridge`, `Recipe_SweetPotatoPorridge`, `Recipe_RoastedSweetPotato`.
    - Kategori Stamina (30 detik masak, 15L air): `Recipe_PurpleYamCake`, `Recipe_TaroChip`, `Recipe_TaroDumpling`.
    - Kategori Battle (45 detik masak, 20L air): `Recipe_SweetPotatoFeast`, `Recipe_TaroCoconutStew`, `Recipe_TaroCornStew`.
  - Efek konsumsi makanan:
    - Recovery: Instant heal diselaraskan (`Food_RoastedSweetPotato`: 10 HP, `Food_SweetPotatoPorridge`: 25 HP, `Food_CornPorridge`: 50 HP).
    - Stamina: Durasi buff 4 menit (240 detik).
    - Battle: Durasi buff 3 menit (180 detik) dengan +25% bonus attack damage.
- **Kapasitas Botol Air & Pengisian Sumur**:
  - Peningkatan kapasitas [`PlayerWaterBottle.cs`](file:///f:/unity/Farm-Beware/Assets/Scripts/Features/Kitchen/PlayerWaterBottle.cs) menjadi 100L dengan tegukan 25L.
  - Penambahan kemampuan [`WaterWellInteractable.cs`](file:///f:/unity/Farm-Beware/Assets/Scripts/Features/Farming/WaterWellInteractable.cs) untuk mengisi ulang botol minum pemain (100L) dan penyiram tanaman dengan cooldown 10 detik.
- **Jadwal Gelombang & Jeda Istirahat Night Brawl (Night Waves & 30s Prep Window)**:
  - Jeda antar gelombang (`waveIntermissionDelay`) diatur menjadi 30 detik pada [`NightBrawlManager.cs`](file:///f:/unity/Farm-Beware/Assets/Scripts/Features/Combat/NightBrawlManager.cs) dan [`CombatPhaseTrackerUI.cs`](file:///f:/unity/Farm-Beware/Assets/Scripts/Features/Time/UI/CombatPhaseTrackerUI.cs).
  - Kalibrasi komposisi monster pada [`WaveProgressionEngine.cs`](file:///f:/unity/Farm-Beware/Assets/Scripts/Features/Combat/Wave/WaveProgressionEngine.cs) untuk Hari 1 s/d Hari 5:
    - Hari 1: 1 wave (5 musuh).
    - Hari 2: 2 wave (W1: 4 musuh, W2: 5 musuh).
    - Hari 3: 3 wave (W1: 4 musuh, W2: 5 musuh, W3: 6 musuh).
    - Hari 4: 4 wave (W1: 4 musuh, W2: 4 musuh, W3: 5 musuh, W4: Boss Taro Colossus + 4 minion).
    - Hari 5: 5 wave (W1: 4 musuh, W2: 5 musuh, W3: 6 musuh, W4: Boss Taro Colossus, W5: Boss The Ranger + 3 minion).
- **Nilai Jual Tanaman (Economy Balance)**:
  - Ubi Jalar / Sweet Potato (`Crop_SweetPotato.asset`): 750 Gold.
  - Talas / Taro (`Crop_Taro.asset`): 1,600 Gold.


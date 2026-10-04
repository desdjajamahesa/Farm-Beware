# CHANGELOG - TECHNICAL OVERVIEW (Farm-Beware)

Konsolidasi dokumentasi teknis dari seluruh kontributor tim pengembang (`Rafi`, `Bhaskoro`, `Roi`) yang diintegrasikan ke dalam `Sprint-branch`, diurutkan secara kronologis berdasarkan tanggal rilis.

---

### Tim Kontributor & Domain Fokus:
- **[Rafi]**: *Core Architecture, Night Brawl Combat Engine, Farming POCO Backend, Dynamic Lighting & Shaders, Level Design & Occlusion System.*
- **[Bhaskoro]**: *Multi-Slot Save & Load Persistence, Stylized Fantasy Main Menu & Pause Menu, Additive Gameplay Hooks.*
- **[Roi]**: *Character Sheet 3-Column UI (Stats & Equipment), Compound Perimeter Structures & Foliage, GPU Instancing Optimizations.*

---

## Daftar Isi Kronologis
- [2026-10-04](#--2026-10-04)
- [2026-10-03](#--2026-10-03)
- [2026-10-02](#--2026-10-02)
- [2026-10-01](#--2026-10-01)
- [2026-09-30](#--2026-09-30)

---

## - 2026-10-04

### Ditambahkan & Diintegrasikan (Added & Integrated)

#### 1. Sistem UI Bar Darah Musuh Dual-Tier (Dual-Tier Enemy Health Bar System) `[Rafi]`
- **Floating Overhead Bar untuk Monster Biasa ([`EnemyOverheadBarUI.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Combat/UI/EnemyOverheadBarUI.cs) & [`EnemyHealthBarManager.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Combat/UI/EnemyHealthBarManager.cs))**:
  - Bilah kesehatan melayang individual di atas kepala setiap monster aktif menggunakan grafis *procedural sliced rounded bar* berbasis vektor runtime.
  - **Ghost Damage Bar (Amber Gold)**: Bilah tertinggal yang diam sesaat (0.22s) lalu meluncur turun (*lerp catch-up*), memberikan kepuasan feedback visual atas damage pemain.
  - **Bilah Darah Utama (Ruby Red & Crimson)**: Menggunakan gradien merah delima yang berdenyut/menyala lebih terang saat HP kritis (≤ 25%).
  - **Tampilan Kontekstual Tempur (Anti-Clutter)**: Bilah darah otomatis tersembunyi saat darah penuh (100%), seketika menyala (*Instant Snap*) saat menerima pukulan, dan memudar halus (*Fade Out*) jika monster di luar pertempuran selama 3–5 detik.
  - **Zero-GC Object Pooling**: Menyiapkan pool 20 widget bar secara efisien tanpa alokasi memori berulang di tengah pertempuran malam.
  - **Dynamic Head Height Offset**: Ketinggian pivot bilah otomatis beradaptasi dengan skala fisik monster (*Tuber Maw*: 1.15m, *Corn Musketeer*: 1.65m, *Taro Brute*: 2.25m), mencegah bilah saling bertumpuk saat monster berkerumun rapat.
  - **Camera Frustum Culling**: Hanya memproyeksikan dan memperbarui posisi bilah jika monster berada di depan dan dalam batas pandang kamera aktif (`MainCamera` / `ICameraService`).
- **Top Cinematic Boss Health Bar ([`BossHealthBarSlotUI.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Combat/UI/BossHealthBarSlotUI.cs) & [`BossHealthBarManager.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Combat/UI/BossHealthBarManager.cs))**:
  - Bilah kesehatan bos megah di layar atas tengah (*Top-Center HUD*) lengkap dengan frame ornamen bergaya gothic/fantasi.
  - **Gelar & Identitas Tematik**:
    - 👁️ **Cyclops Tuber Maw** — *Abyssal Eye of the Deep Hollow*
    - 👑 **Taro Colossus** — *Titan of the Primeval Roots*
    - 🏹 **The Ranger** — *Sentinel of the Cursed Harvest*
  - **Dukungan Multi-Boss Simultan (Hari ke-5)**: Otomatis beradaptasi antara layout *Single Boss* (lebar 560px) dan *Dual-Boss Stacked* (lebar 480px bertumpuk) saat Cyclops Tuber Maw dan The Ranger bertarung bersamaan pada Wave 5 Hari ke-5.
  - Efek getaran *impact shake/punch*, ghost fill lag (kuning keemasan), dan indikator garis fase amukan (*Enrage Divider*) khusus The Ranger pada 50% HP yang berdenyut saat fase amukan aktif.
  - Sekuens kekalahan sinematik: Memunculkan lencana emas `⚔️ DEFEATED ⚔️` dengan transisi fade out dramatis saat bos berhasil dikalahkan.
- **Inisialisasi Otomatis ([`NightBrawlManager.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Combat/NightBrawlManager.cs))**:
  - Menambahkan method `EnsureHealthBarManagers()` pada `Start()` dan `StartNightBrawl()` untuk menjamin kedua manager UI aktif otomatis di bawah `UI_Canvas` tanpa membutuhkan setup manual di scene.

#### 2. Integrasi Dinamis Spawner Monster Berbasis `MonsterSpawnPoints` `[Rafi]`
- Menghubungkan pemanggilan monster tiap wave pada [`NightBrawlManager.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Combat/NightBrawlManager.cs) agar 100% menghormati titik lokasi yang dipindahkan pengguna di scene:
  - `MonsterSpawnPoints` (Parent): `(22.20, 0.00, 52.20)`
  - `SpawnPoint_FrontGate_Left`: `(37.40, 0.00, 64.20)`
  - `SpawnPoint_FrontGate_Center`: `(21.90, 0.00, 64.60)`
  - `SpawnPoint_FrontGate_Right`: `(9.60, 0.00, 64.50)`
- Mengimplementasikan `GetAllActiveSpawnPoints()` yang secara dinamis mengumpulkan seluruh transform anak aktif di bawah `MonsterSpawnPoints` pada runtime.
- Mengalibrasi `CalculateRandomSpawnPoint()`: Memilih titik spawn acak secara ketat dari titik aktif tersebut, menerapkan *scatter radius* ringan (1.0m), *surface raycasting*, dan *NavMesh sampling* dengan penjagaan jarak (maksimum 2.5m) agar monster berdiri menapak tanah tepat di titik yang ditentukan.
- Memperbarui visualisasi `OnDrawGizmosSelected()` agar menampilkan bola gizmo di atas seluruh titik spawn anak.

#### 3. Integrasi & Penggabungan Fitur dari `Bas-branch` `[Bhaskoro / Rafi]`
- Menggabungkan seluruh pembaruan dari `Bas-branch` ke `Sprint-branch` secara bersih tanpa kehilangan fitur maupun regresi struktur baru.
- **Rekonsiliasi Konflik [`DayNightTimeManager.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Time/DayNightTimeManager.cs)**:
  - Melindungi arsitektur waktu berbasis antarmuka `ITimeService` dan injeksi dependensi via `ServiceLocator.Resolve<ITimeService>()`.
  - Menyelaraskan sistem interaksi tidur kasur (`BedInteractable`) yang sepenuhnya statis tanpa waktu berjalan otomatis (pemain berinteraksi dengan tempat tidur untuk beralih fase atau memajukan hari setelah gelombang malam tuntas).
  - Memposisikan `DayNightTimeManager` sebagai visual presenter murni tanpa benturan wewenang pergantian fase.
- **Rekonsiliasi Konflik [`StagingScene.unity`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scenes/StagingScene.unity)**:
  - Mempertahankan integritas objek save station, UI canvas, layout pekarangan simetris, dan pencahayaan URP.
- Melakukan commit merge (`386c69d`) dan push ke `origin/Sprint-branch`.

### Diperbaiki (Fixed)

- **Bug Teleportasi Ghost Monster ke Titik Kematian Lama saat Wave Baru Dimulai ([`EnemyObjectPool.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Combat/Enemy/EnemyObjectPool.cs) & [`EnemyBase.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Combat/Enemy/EnemyBase.cs)) `[Rafi]`**:
  - Mengatasi insiden di mana monster yang tereliminasi di sektor barat pekarangan (dekat pohon cemara `X: -3.2, Z: 10.0`) tiba-tiba muncul/teleport seketika ke titik kematian lamanya saat wave berikutnya dimulai, alih-alih muncul dari gerbang depan.
  - **Penyebab**: Di Unity PhysX, mengubah `transform.position` pada objek non-aktif dengan `Rigidbody` tidak menyinkronkan posisi internal mesin fisika. Saat objek diaktifkan kembali (`SetActive(true)`), PhysX mengembalikan objek ke posisi terakhir saat dinonaktifkan.
  - **Solusi**:
    - Pada `EnemyObjectPool.Spawn()`: Eksplisit mengatur `rb.position = position`, `rb.rotation = Quaternion.identity`, mereset `linearVelocity` & `angularVelocity` ke `Vector3.zero`, dan memanggil `Physics.SyncTransforms()` baik sebelum maupun sesudah `enemy.gameObject.SetActive(true)`.
    - Pada `EnemyBase.ResetEnemyState()`: Memastikan pembersihan momentum fisika dan penataan ulang transform.
- **Penghapusan Hardcoded Clamp Spawner yang Menimpa Posisi Level Designer ([`NightBrawlManager.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Combat/NightBrawlManager.cs)) `[Rafi]`**:
  - Menghapus batasan hardcoded lama `if (p.x >= 14f && p.x <= 28f && p.z >= 50f && p.z <= 60f)` dan `Mathf.Clamp(finalPos.x, 14f, 28f)` yang sebelumnya membuang koordinat baru yang dipindahkan pengguna di luar batas sempit tersebut.
- **Error Kompilasi CS1061 pada `NightBrawlManager` `[Rafi]`**:
  - Menambahkan deklarasi event publik `public event System.Action<EnemyBase> OnEnemyDied;` pada `NightBrawlManager` dan memanggilnya di dalam `HandleEnemyDied()`, menyelesaikan error kompilasi pada `BossHealthBarManager` dan `EnemyHealthBarManager`.
  - Mendaftarkan seluruh berkas `.meta` Unity yang dihasilkan ke Git tracking.
- **Sistem Rangkaian 3-Hit Combo & Input Buffering ([`PlayerControl.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Player/PlayerControl.cs), [`PlayerEquipment.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Player/PlayerEquipment.cs), [`MeleeCombatStateMachine.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Combat/Melee/MeleeCombatStateMachine.cs)) `[Rafi]`**:
  - Menyelesaikan masalah kombo tebasan 3 pukulan yang tidak pernah terpicu (selalu ter-reset menjadi pukulan pertama).
  - **Penyebab**:
    1. `PlayerControl.HandleAttackInput()` memotong klik saat `isAttacking == true`, menolak input pemain di sepanjang animasi (~1.05s).
    2. `comboResetWindow` terkonfigurasi terlalu singkat (0.9s), sehingga `MeleeCombatStateMachine.Update()` mereset kombo kembali ke Idle sebelum durasi lock animasi selesai.
    3. `TryPerformAttack()` membaca normalized time saat transisi belum selesai (`IsInTransition`), menyebabkan evaluasi timing kombo keliru.
  - **Solusi**:
    1. Memperpanjang batas waktu toleransi jeda pukulan kombo (`ComboResetWindow`) dari 0.9s menjadi 1.5s (baik di script maupun properti terserialisasi pada scene `StagingScene.unity`).
    2. Mengimplementasikan antrean input buffering (`hasBufferedAttack`, batas 0.45s) pada `PlayerControl.RoutineAttack()` sehingga klik pemain selama ayunan pedang tersimpan dan langsung meluncurkan pukulan kombo berikutnya begitu jendela kombo terbuka (`normalizedTime >= 0.35f`).
    3. Menambahkan sanitasi `animator.ResetTrigger(attackTriggerName)` sebelum memicu trigger baru untuk mencegah penumpukan trigger animasi.

---

## - 2026-10-03

### Ditambahkan & Disesuaikan (Added & Adjusted)

- **Kurasi & Eliminasi Redundansi Lampu Taman Outdoor ([`OutdoorGardenLamps`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scenes/StagingScene.unity)) `[Rafi]`**:
  - Mengaudit seluruh sumber cahaya pekarangan dan menghapus 8 lampu taman bertiang yang redundan dengan pencahayaan tematik eksisting:
    - **Gerbang Utama**: Menghapus `GardenLamp_MainGate_Left` dan `Right` (karena pilar gerbang sudah memiliki sepasang lentera amber [`Lamp_Gate`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scenes/StagingScene.unity)).
    - **Teras Depan**: Menghapus `GardenLamp_HousePorch` (karena dinding teras sudah memiliki sepasang lentera dinding rustic [`WallLamp_Porch`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Prefabs/Decorations/WallLamp_Rustic.prefab)).
    - **Plaza Api Unggun**: Menghapus `GardenLamp_Campfire` (karena perapian [`FirePit`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scenes/StagingScene.unity) sudah memancarkan cahaya api unggun radius 12m dan peti perbekalan memiliki lentera rustic).
    - **Sumur Air**: Menghapus `GardenLamp_WaterWell` (karena atap kanopi kayu sumur [`WaterWell`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Prefabs/Compound/WaterWell.prefab) sudah memiliki lentera gantung).
    - **Pintu Masuk Kebun**: Menghapus `GardenLamp_GardenEntrance` (karena gapura kebun sudah memiliki tiang lampu kayu terdedikasi `PoleLamp_Garden`).
    - **Gubuk Workshop**: Menghapus `GardenLamp_CraftingShelter` (karena di dalam gubuk kerja sudah terdapat [`ShelterLantern`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Prefabs/Compound/CraftingShelter_Stylized.prefab)).
    - **Jalur Kolam**: Menghapus `GardenLamp_PondPath` (mengeliminasi penumpukan lampu ganda pada jarak 4 meter di area kolam).
  - Mempertahankan **3 Titik Lampu Taman Esensial** yang menerangi zona tanpa sumber cahaya lain:
    1. **`GardenLamp_PondPier`** `(10.00, 0.00, 43.50)`: Menerangi dermaga kayu dan kolam teratai.
    2. **`GardenLamp_MidPath`** `(18.80, 0.00, 33.00)`: Menerangi koridor jalan batu tengah antara api unggun dan rumah.
    3. **`GardenLamp_MerchantTable`** `(4.20, 0.00, -10.50)`: Menerangi lapak dagang kayu dan area logistik halaman belakang.
  - Memperbarui daftar pendaftaran pada [`OutdoorLightingController`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Time/Atmosphere/OutdoorLightingController.cs) (kini mengelola 3 lampu), menghemat 16 runtime lights/shadow maps di shadow atlas URP.

### Diperbaiki (Fixed)

- **Koreksi Rotasi Terbalik Atap Pelana Sumur Air ([`WaterWell.prefab`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Prefabs/Compound/WaterWell.prefab) & [`StagingScene.unity`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scenes/StagingScene.unity)) `[Rafi]`**:
  - Memperbaiki cacat orientasi rotasi pada komponen `GableRoof` di mana kemiringan atap dan kasau (*rafters*) sebelumnya terbalik membentuk corong V cekung (menampung air hujan ke dalam).
  - Mengoreksi tanda sudut rotasi sumbu X (+34° / -34°) pada 8 elemen atap:
    - `RoofSlope_North` dan `RoofSlope_South` (kemiringan atap sirap kini menukik ke bawah membentuk atap pelana segitiga sempurna).
    - `UnderRafter_Mid_N` dan `UnderRafter_Mid_S` (balok kasau penopang tengah).
    - `GableTruss_West` dan `GableTruss_East` (`Rafter_N` & `Rafter_S` pada rangka kuda-kuda sisi barat dan timur).
  - Menerapkan perbaikan secara permanen ke Prefab Asset [`WaterWell.prefab`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Prefabs/Compound/WaterWell.prefab) dan instance scene.

---

## - 2026-10-02

### Ditambahkan (Added)

#### 1. Multi-Slot Save & Load System (Fitur Utama) `[Bhaskoro]`
- Sistem penyimpanan data permainan modular berbasis JSON (`FeaturesSaveSystem`) yang mendukung slot penyimpanan tak terbatas, snapshot otomatis, dan restorasi status dunia permainan.
- **Model Data Komprehensif ([`GameSaveData.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/SaveSystem/GameSaveData.cs))**:
  - **Player Snapshot**: Posisi 3D, rotasi, Health, Stamina, Hunger, Thirst.
  - **Inventory & Equipment**: Slot item tas, quickslot, item name, quantity, dan durabilitas.
  - **Economy**: Jumlah koin dan saldo `PlayerWallet`.
  - **Time & Day**: Nomor hari (`currentDay`), jam/waktu (`timeOfDay`), serta fase siang/malam.
  - **Farming**: Status 16 petak lahan (`TileState`), bibit yang tertanam (`SeedItemData`), progres pertumbuhan (`growthProgress`), dan sisa timer panen.
  - **Combat & Night Brawl**: Gelombang malam aktif (`currentWave`, `totalWaves`), status penyelesaian (`isNightEncounterCleared`), serta daftar entitas musuh aktif (`SavedEnemyData`: tipe musuh, HP, koordinat 3D, dan rotasi).
  - **Kitchen & Environment**: Status air botol, sink, dan kompor.
- **Singleton Pengelola I/O ([`SaveSystemManager.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/SaveSystem/SaveSystemManager.cs))**:
  - Penyimpanan file JSON pada direktori `Application.persistentDataPath`.
  - Fungsi Save Game, Load Game, Delete Slot, QuickSave, dan QuickLoad.
  - Penanganan transisi scene otomatis (`LoadAndApplySaveToScene`).
- **UI Pop-up Save/Load ([`SaveSystemUI.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/SaveSystem/SaveSystemUI.cs))**:
  - Tampilan visual pop-up menu Save & Load dengan daftar slot bergaya fantasy board.
  - Auto-procedural UI generator (`BuildUI`) pada `Canvas` tanpa ketergantungan hierarki statis manual.
  - Dialog konfirmasi Overwrite / Delete slot.
- **Titik Interaksi Rumah ([`SaveStationInteractable.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/SaveSystem/SaveStationInteractable.cs))**:
  - Meja kamar tidur interaktif dengan label `"Save / Load"`.

#### 2. Fantasy Main Menu & Pause Menu Overhaul `[Bhaskoro]`
- Pembaruan antarmuka menu utama dan pause menu dengan estetika stylized fantasy bernuansa kayu, pencahayaan glow, dan animasi interaktif.
- **Aset & Scene**:
  - [`MainMenuScene.unity`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scenes/MainMenuScene.unity): Scene menu utama dengan pencahayaan dan tata letak baru.
  - [`MainMenuUI.prefab`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Prefabs/UI/MainMenuUI.prefab) & [`PauseMenuUI.prefab`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Prefabs/UI/PauseMenuUI.prefab): Prefab UI menu baru dengan tekstur high-resolution.
- **Animasi Menu ([`FantasyMenuAnimator.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/MainMenu/FantasyMenuAnimator.cs) & [`PauseMenuAnimator.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/MainMenu/PauseMenuAnimator.cs))**:
  - Animasi transisi tombol hover, scale bounce, rotasi plank signpost, dan glow pulsing.
- **Koleksi Tekstur UI**:
  - `Assets/Textures/MainMenu/*`: Background dual-world, vortex/galaxy glow, tombol Play/Exit/Settings kayu, signpost, dan slider/toggle kustom.
  - `Assets/Textures/PauseMenu/*`: Wooden card background, pause text glowing, round/square buttons, hover plank glow, selection arrows.

#### 3. Restorasi Penataan Objek Indoor & Pemulihan Posisi Lampu Gantung `[Rafi]`
- **Pemulihan Posisi Lentera Gantung Workshop ([`ShelterLantern`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Prefabs/Compound/CraftingShelter_Stylized.prefab))**:
  - Memasang balok kayu penopang atap tengah (`Rafter_Center` pada `LocalPos: (0.00, 2.60, 0.00)`) yang miring 10° selaras dengan kanopi atap.
  - Menghubungkan rantai lentera gantung langsung ke bawah balok penopang (`LocalPos: (0.00, 2.25, 0.00)`), mengeliminasi celah melayang 25 cm.
- **Koreksi & Keseimbangan Lampu Teras Depan ([`WallLamp_Porch`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Prefabs/Decorations/WallLamp_Rustic.prefab))**:
  - Memindahkan `WallLamp_Porch` dari tengah kusen/kaca pintu masuk (`X = 20.25`) ke permukaan dinding solid kanan pintu (`(21.90, 2.10, 25.96)`).
  - Menambahkan lentera dinding kembar simetris di sisi kiri pintu (`WallLamp_Porch_Left` pada `(19.04, 2.10, 25.96)`), membebaskan akses jalan masuk 100%.
- **Eliminasi Penembusan Dinding Kulkas Dapur (`Fridge`)**:
  - Menggeser kulkas dan rangkaian stasiun dapur (`Kitchen_Sink`, `food_prep_station`, `Kitchen_Stove`) ke arah timur (`+1.5m` pada sumbu X). Kulkas kini berada di sudut barat laut dalam dapur (`X = 12.08, Z = 11.75`), mengeliminasi tonjolan 1,2 meter ke luar pekarangan barat.
- **Pemulihan Posisi Nakas Kamar Tidur (`SmallDrawer`)**:
  - Memindahkan nakas kayu kembali ke dalam kamar tidur (`BedroomZone`) tepat di samping kepala tempat tidur ([`Bed`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scenes/StagingScene.unity)) di `(31.75, 0.44, 19.49)`.
- **Relokasi & Kalibrasi Kamera Trophy Cabinet (`TrophyCabinetSystem`)**:
  - Menyelaraskan rak piala 12 snap point ke dinding barat kamar tidur (`(22.85, 0.875, 23.50)`) menghadap ke dalam ruangan.
  - Menata ulang kamera First-Person `TrophyCamera` di `(25.45, 1.45, 23.50)` menghadap langsung ke rak piala tanpa terhalang dinding partisi.
- **Penyelarasan Peti Logistik Tengah (`TestChest`)**:
  - Memindahkan peti penyimpanan lorong tengah ke dinding selatan lorong (`(27.20, 0.04, 13.00)`), menjamin akses sirkulasi tetap lapang.

#### 4. Harmonisasi & Penyesuaian Zona Workshop Halaman Belakang (Backyard Artisan Courtyard) `[Rafi]`
- **Penyelarasan Tangga Pintu Belakang ([`HousePorch_Steps (1)`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Prefabs/Compound/CobblestonePathways.prefab))**:
  - Membersihkan duplikasi hierarki nested ganda dari hasil salinan manual.
  - Menyelaraskan anak tangga batu dua tingkat tepat di tengah pintu belakang dapur (`door_kitchen.001` pada `X = 13.27, Z = 5.26`).
- **Jalur Setapak Bebatuan Baru Halaman Belakang (`Branch_Backyard_Workshop`)**:
  - Memisahkan 20+ batu pijakan ke cabang terdedikasi `CobblestonePathways/Branch_Backyard_Workshop`.
  - Merancang ulang jalur ganda organik dari kaki tangga pintu belakang (`(12.75, 0.03, 3.75)`) menuju teras depan [`CraftingShelter`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Prefabs/Compound/CraftingShelter_Stylized.prefab) di `(3.74, 0.0, -7.52)` dan lapak dagang [`MerchantTable_Outdoor`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Prefabs/Compound/MerchantTable_Outdoor.prefab).
  - Menghilangkan collider pada bebatuan jalur untuk mencegah tabrakan navigasi.
- **Restrukturisasi Transform & Material Meja Dagang ([`MerchantTable_Outdoor`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Prefabs/Compound/MerchantTable_Outdoor.prefab))**:
  - Menempatkan root transform ke `(6.42, 0.0, -10.98)` dengan local offset bersih.
  - Mengalokasikan material kayu hangat [`Mat_Wood.mat`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Materials/Kitchen/Mat_Wood.mat) serta taplak kain [`Mat_Decor_CarpetSquare.mat`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Materials/Furniture/Mat_Decor_CarpetSquare.mat).
- **Penyelarasan Peti Logistik & Pengecatan Splatmap**:
  - Penataan `OutdoorWorkshopStorage` di `(2.89, 0.0, -3.16)` dan `GardenLamp_CraftingShelter` di `(2.09, 0.0, -4.76)`.
  - Mengecat lapisan tanah organik lembut ([`TL_DirtPath`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Terrain/New%20Terrain%201.asset)) dan melakukan *rebake* NavMesh scene secara penuh.

#### 5. Perluasan Pagar Pekarangan & Penataan Rumah di Tengah (Centered House & Expanded Compound) `[Rafi]`
- Memperbesar dimensi batas pekarangan [`HomesteadPerimeter`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Prefabs/Compound/HomesteadPerimeter.prefab) menjadi `42.8m x 66.7m` (X: [0.0, 42.0], Z: [-17.0, 48.0]).
- Rumah berada tepat di tengah-tengah compound (*perfect symmetry*): kedalaman pekarangan depan = 23.2m, kedalaman halaman belakang = 22.4m, samping barat = 9.5m, samping timur = 10.5m.
- Melipatgandakan jumlah pohon dari 13 menjadi 28 pohon (14 Oak & 14 Pine), 100% terintegrasi dengan [`WallOccluder`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Camera/WallOccluder.cs) pada Layer 12 (`Wall`).
- Master Layout Redesign 4 Zona Luar Ruangan:
  - **Zona 1 (Production Wing)**: 16 petak lahan sawah terpadu, [`WaterWell`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Prefabs/Compound/WaterWell.prefab) di `(24.0, 0.0, 31.5)`, `GardenToolCorner`, `Scarecrow`.
  - **Zona 2 (Social Hub)**: [`RelaxationArea_Campfire`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Prefabs/Compound/RelaxationArea_Campfire.prefab) di poros sentral pekarangan `(20.5, 0.0, 35.5)` sebagai bundaran sirkulasi utama.
  - **Zona 3 (Relaxation Sanctuary)**: [`NaturalPond`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Prefabs/Compound/NaturalPond_Stylized.prefab) di `(9.5, 0.01, 42.0)` dengan dermaga kayu dan bangku santai.
  - **Zona 4 (Workshop & Logistics Outpost)**: [`CraftingShelter`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Prefabs/Compound/CraftingShelter_Stylized.prefab) di `(8.5, 0.0, 27.5)`.
- Rekonfigurasi hierarkis 106 batu setapak bebatuan ([`CobblestonePathways.prefab`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Prefabs/Compound/CobblestonePathways.prefab)) dan pengecatan tanah `TL_DirtPath`.

#### 6. Sistem Transparansi Pohon Penghalang Kamera (Dynamic Tree Occlusion Fading) `[Rafi]`
- Integrasi komponen [`WallOccluder`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Camera/WallOccluder.cs) pada seluruh pohon di [`StagingScene.unity`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scenes/StagingScene.unity) serta prefab dasar [`Tree_Stylized_Oak.prefab`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Prefabs/Environment/Tree_Stylized_Oak.prefab) dan [`Tree_Stylized_Pine.prefab`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Prefabs/Environment/Tree_Stylized_Pine.prefab).
- Konfigurasi Layer 12 (`Wall`) agar terdeteksi oleh sistem multi-height Line-of-Sight [`WallOcclusionManager`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Camera/WallOcclusionManager.cs).
- Batang dan kanopi pohon memudar secara serempak dan halus (*fade alpha* ke 0.22) saat berada di antara kamera dan karakter pemain, lalu kembali opaque tanpa memory leak via SRP Batcher.

### Diubah & Diintegrasikan (Changed & Integrated)

- **Integrasi & Additive Hooks pada Komponen Inti `[Bhaskoro]`**:
  - [`PlayerControl.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Player/PlayerControl.cs): Penambahan method `Teleport(Vector3 position, Quaternion rotation)` untuk restorasi posisi tanpa glitch fisika.
  - [`PlayerStats.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Player/PlayerStats.cs): Penambahan method `RestoreStats(int hp, float stamina, float hunger, float thirst)` dan pengecekan `Time.timeScale > 0f` untuk input debug.
  - [`FarmlandTile.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Farming/FarmlandTile.cs): Penambahan getter `GrowthDuration`, `CurrentTimer`, dan method `RestoreCropState(...)`.
  - [`DayNightTimeManager.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Time/DayNightTimeManager.cs): Penambahan method `SetDayAndTime(int day, float targetHour)` dan penundaan tick waktu saat Save UI terbuka.
  - [`CombatPhaseTrackerUI.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Time/UI/CombatPhaseTrackerUI.cs): Visibilitas method `UpdatePhaseDisplay` diubah menjadi `public`.
  - [`NightBrawlManager.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Combat/NightBrawlManager.cs): Penambahan getter `CurrentWave`, `TotalWaves`, overload `RoutineWaveLoop(int startWave)`, dan method `RestoreNightBrawlState(...)`.
- **Integrasi Branch `Bhaskoro-branch` ke `Sprint-branch` `[Rafi / Bhaskoro]`**:
  - Menghindari bug kritis `m_TimeScale: 0` pada `ProjectSettings/TimeManager.asset` dengan mempertahankan konfigurasi `Sprint-branch` (`m_TimeScale: 1`).
  - Mempertahankan nama proyek orisinal `Farm-Beware`.
  - Melakukan grafting murni pada 3 GameObject Save System (`SaveSystemManager`, `SaveSystemUI`, dan meja interaksi `SaveStationInteractable`).
  - Pembersihan 34 file scratch di root repository.
- **Integrasi Branch `branch-roi-1` ke `Sprint-branch` `[Rafi / Roi]`**:
  - Rekonsiliasi konflik scene [`StagingScene.unity`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scenes/StagingScene.unity) menggunakan strategi *Scene Reconstruction & Prefab Grafting*.
  - Integrasi batas pekarangan compound pada [`NightBrawlManager.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Combat/NightBrawlManager.cs) (`IsInsideCompound()` dan 4 sektor spawn) yang berpadu dengan mesin progresi `WaveProgressionEngine` dan `EnemyObjectPool`.
  - Integrasi panel Character Sheet 3 kolom pada UI Inventory.

### Diperbaiki (Fixed)

- **Material Hilang / Berwarna Pink pada `CobblestonePathways` `[Rafi]`**:
  - Mengalokasikan material Stylized URP Lit secara harmonis ke seluruh 106 batu pijakan:
    - [`Mat_Cobblestone_Warm`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Materials/Environment/Mat_Cobblestone_Warm.mat) (57 batu)
    - [`Mat_Campfire_Stone`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Materials/Environment/Mat_Campfire_Stone.mat) (27 batu)
    - [`Mat_Cobblestone_Dark`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Materials/Environment/Mat_Cobblestone_Dark.mat) (22 batu)
  - Mengupdate aset prefab [`CobblestonePathways.prefab`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Prefabs/Compound/CobblestonePathways.prefab). Audit 1.871 renderer scene mengonfirmasi 100% bebas dari material null / pink shader.

---

## - 2026-10-01

### Ditambahkan (Added)

#### 1. Sistem UI Character Sheet 3 Kolom (Tab UI) `[Roi]`
- Implementasi komponen [`PlayerStatsDisplayUI`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Player/UI/PlayerStatsDisplayUI.cs) untuk menampilkan atribut stat pemain secara real-time (Max Health, Max Stamina, Base Damage, Move Speed, Armor/Defense).
- Implementasi komponen [`EquipmentSlotUI`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Player/UI/EquipmentSlotUI.cs) untuk 11 slot perlengkapan karakter: Helmet, Chest, Pants, Boots, Weapon, Shield, Gloves, Belt, Ring, Necklace, Cape.
- Aset ikon perlengkapan di `Assets/Textures/Icons/Equipment/` (11 ikon slot + gambar preview statis karakter `Character_Static_Preview.png`).
- Integrasi 3 kolom pada [`InventoryManagerUI`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Inventory/UI/InventoryManagerUI.cs): Kolom kiri (Stats), Kolom tengah (Player Inventory), dan Kolom kanan (Equipment) saat menekan tombol `Tab`.

#### 2. Struktur Kompleks Pekarangan & Pagar Perimeter (Homestead Compound Yard) `[Roi]`
- Prefab [`HomesteadPerimeter.prefab`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Prefabs/Compound/HomesteadPerimeter.prefab): Pagar kayu pembatas pekarangan luar keliling dan gerbang utama di sektor utara.
- Prefab [`GardenEnclosure.prefab`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Prefabs/Compound/GardenEnclosure.prefab) dan `GardenEnclosure_East.prefab`: Pagar pembatas area perkebunan (Farmland) sisi barat dan timur.
- Prefab [`CobblestonePathways.prefab`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Prefabs/Compound/CobblestonePathways.prefab): Jalur setapak bebatuan penghubung teras depan, gerbang utara, sumur air, dan api unggun.
- Prefab [`WaterWell.prefab`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Prefabs/Compound/WaterWell.prefab): Model sumur air di pekarangan timur dengan atap sirap dan ember tali.
- Prefab [`RelaxationArea_Campfire.prefab`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Prefabs/Compound/RelaxationArea_Campfire.prefab): Area peristirahatan luar ruangan dengan susunan batu api unggun, abu, bara, dan bangku kayu.
- Komponen [`CampfireLightFlicker.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Environment/CampfireLightFlicker.cs): Efek kedip cahaya api unggun dinamis menggunakan Perlin noise.
- Prefab [`MerchantTable_Outdoor.prefab`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Prefabs/Compound/MerchantTable_Outdoor.prefab): Meja lapak pedagang luar ruangan.
- Prefab vegetasi dan semak modular: [`Tree_Stylized_Pine.prefab`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Prefabs/Environment/Tree_Stylized_Pine.prefab), [`Tree_Stylized_Oak.prefab`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Prefabs/Environment/Tree_Stylized_Oak.prefab), dan [`Bush_Stylized.prefab`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Prefabs/Environment/Bush_Stylized.prefab).
- Pengecatan tekstur terrain layer baru: `TL_DirtPath.terrainlayer` (jalur tanah setapak) dan `TL_WetMud.terrainlayer` (lumpur basah).

#### 3. Integrasi Batas Compound pada Night Brawl `[Roi]`
- Penambahan metode `IsInsideCompound(Vector3 pos)` pada [`NightBrawlManager.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Combat/NightBrawlManager.cs) untuk memvalidasi batas pagar pekarangan (`X: [4.0, 38.0], Z: [2.0, 48.0]`).
- Rekalibrasi titik tengah arena (`arenaCenter = (21f, 0.5f, 36f)`).
- Pembagian 4 sektor spawn luar baru:
  - Sektor 0: Penyerbu luar gerbang utama utara (`Z: 49-56, X: 17-25`).
  - Sektor 1: Pekarangan terbuka timur dekat sumur air (`X: 32-36.5, Z: 32-44`).
  - Sektor 2: Lorong pekarangan barat dekat pagar dan api unggun (`X: 5.5-7.5, Z: 32-42`).
  - Sektor 3: Plaza halaman depan teras rumah (`X: 18-24, Z: 29.5-33.5`).

#### 4. Mesin Sistem Pertanian Inti (Core Farming System POCO Backend) `[Rafi]`
- Implementasi kelas C# murni tanpa `MonoBehaviour` (`SoilState`, `SoilTile`, `FarmGrid`) dengan event `OnTileUpdated` untuk pemisahan logika yang independen dan testable.
- Asset ScriptableObject `CropData` untuk konfigurasi varietas tanaman, tahap pertumbuhan (*growth stages*), kebutuhan penyiraman, dan rentang hasil panen (*yield*).
- Komponen adapter pertanian `FarmGridManager` untuk memetakan koordinat grid tanah murni ke koordinat dunia 3D (*WorldToGrid* dan *GridToWorld*).
- Sistem visualisasi tanah dinamis (`SoilTileVisualizer`) yang merespons status petak tanah: kosong (*Empty*), dicangkul (*Tilled*), ditanami kering (*PlantedDry*), disiram basah (*PlantedWatered*), dan siap panen (*ReadyToHarvest*).
- Mekanik interaksi bertani: mencangkul dengan Hoe, menyiram dengan Watering Can, menanam benih, dan memanen dengan proteksi batas stack inventori (`maxStack = 20`).
- Pertumbuhan tanaman berbasis transisi fase waktu (`TimeManager.OnPhaseChanged`) tanpa polling per-frame.

#### 5. Mesin Pertarungan Jarak Dekat & Progresi Gelombang `[Rafi]`
- **Night Brawl Melee Combat Engine & FSM**: Implementasi `MeleeCombatStateMachine` dengan variasi serangan: kombo ringan 3-hit, tusukan saat berlari (*Dash Attack*), tebasan tahan tombol (*Charged Heavy Attack*), lompatan menerjang (*Leap Strike*), tendangan Spartan knockback, dan *Battlecry*.
- **Mesin Progresi Gelombang Monster (`WaveProgressionEngine`)**: Kalkulasi komposisi wave dinamis per malam, kuota bertahap, dan evolusi 6 varian musuh (Tuber Maw, Cyclops Tuber Maw, Taro Brute, Taro Colossus, Corn Musketeer, The Ranger).
- **Kemampuan Bos Modular**:
  - Boss Summon Ability untuk Cyclops Tuber Maw dan The Ranger dengan batas kuota dan validasi area outdoor.
  - Toolkit pertarungan Taro Colossus: Airborne Slam dengan bayangan penjejak (Tracking Shadow Decal), interupsi stagger 4 hit, Rock Projectiles, dan Grapple Slam dengan proteksi safety unlock.
  - Enrage Phase untuk The Ranger pada HP ≤ 50% yang melontarkan 20 proyektil melingkar (Radial 360 Burst).
- **Modular Composite Trophy**: Desain piala kejuaraan megah (pedestal kayu, pilar emas metalik, twin handles, brass plaque, dan mahkota batu permata bersudut) untuk 12 varian piala dekorasi.
- Komponen pembantu `EnemyLootDropHandler` untuk memisahkan kalkulasi drop ekonomi/item dari `EnemyBase`.

#### 6. Indikator Gelombang Tempur Malam (Wave Indicator HUD) `[Rafi]`
- Presenter visual stateless `NightBrawlWaveUI` yang sepenuhnya event-driven tanpa polling `Update()`.
- Algoritma pra-kalkulasi progres malam dan penanda gelombang (`WaveMilestoneData` & `NightScheduleSummary`) pada `WaveProgressionEngine` dengan presisi posisi penanda gelombang akhir di ujung bilah (`X = 1.0`).
- Komponen visual `WaveIndicator` dengan posisi jangkar ternormalisasi (*anchor-normalized positioning*) responsif di segala rasio layar.
- Efek animasi taktil (*juice*) Zero-GC: hentakan denyut (*scale punch*) pada ikon penjejak monster saat musuh mati, animasi lecutan bendera saat dilewati, dan spanduk pengumuman gelombang/bos di tengah layar (*Center Screen Announcement Overlay*).

#### 7. Kalibrasi Pencahayaan Siklus Siang/Malam & Lampu Taman Ganda `[Rafi]`
- Kalibrasi pencahayaan atmosferik siklus siang-malam (`Day_LightingTheme` dan `Night_LightingTheme`) dengan konfigurasi temperatur warna (5000K), ambient ground color, dan directional sun observer.
- Safe-Zone Light Layers pada interior rumah untuk memisahkan pencahayaan aman di dalam rumah dari atmosfer pertarungan malam luar ruangan.
- **Sistem Lampu Taman Ganda (Dual-Source Outdoor Illumination)**:
  - Integrasi tiang lentera modular `GardenLamp_Prefab` dengan perakitan fisik realistis: `BulbSocket`, `LanternBulb`, dan `LanternFilament`.
  - Kombinasi `Spot Downlight` (40 lux, jangkauan 9m, sudut 135°) untuk kolam cahaya terang terfokus dan `Point Light` (25 lux, jangkauan 15m) untuk ambient sekeliling. Total intensitas 65 lux bernuansa amber vintage (~2400K).
  - Komponen individual [`OutdoorGardenLamp`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Time/Atmosphere/OutdoorGardenLamp.cs) dengan pendaran Zero-GC berbasis `MaterialPropertyBlock` dan efek kelap-kelip halus (*organic micro-flicker*).
  - Pengontrol terpusat [`OutdoorLightingController`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Time/Atmosphere/OutdoorLightingController.cs) terhubung ke `TimeManager.OnPhaseChanged`.

### Diubah (Changed)

- **Optimasi Performa Rendering (GPU Instancing) `[Roi]`**:
  - Mengaktifkan varian GPU Instancing (`m_EnableInstancingVariants: 1`) pada 60+ material proyek (furnitur, lantai, dinding, tanaman, tanah, piala, dan TextMesh Pro).
  - Mengaktifkan rendering instanced pada terrain draw calls untuk memangkas overhead CPU.
- **Sinkronisasi Scene Lighting Utility `[Rafi]`**:
  - Mengintegrasikan [`OutdoorLightingController`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Time/Atmosphere/OutdoorLightingController.cs) ke dalam [`SceneLightingEditorUtility`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Editor/Rendering/SceneLightingEditorUtility.cs) untuk preview instan mode Day, Night, dan Work Light di Scene View.
- **Single Source of Truth Waktu `[Rafi]`**:
  - Menetapkan `TimeManager` sebagai otoritas tunggal siklus waktu dan memposisikan `DayNightTimeManager` sebagai visual presenter murni.
- **Refactoring Serangan Bos & Host Arsitektur `[Rafi]`**:
  - Logika serangan Taro Colossus dialihkan ke komponen modular `ColossusAirborneAbility` dan `ColossusGrappleAbility`.
  - Refactor `EnemyBase` menggunakan pola arsitektur Hybrid Host Strategy untuk mereduksi god class.
- **Migrasi Fisika Massal (BoxCollider Primitives) `[Rafi]`**:
  - Mengganti 104 komponen `MeshCollider` statis menjadi `BoxCollider` primitif di `StagingScene` dengan penegakan ketebalan minimum 0.05m (*Minimum Thickness Guard*) guna mencegah tunneling dan memangkas komputasi PhysX.

### Diperbaiki (Fixed)

- **Eliminasi Bug Pergerakan Terkunci ke Kiri pada Pemain `[Rafi]`**:
  - Mengatasi bug kritis pergerakan pemain yang terkunci tidak bisa ke kiri dan melenceng ke atas saat menekan tombol `A`.
  - Menghapus kebocoran state normal tembok (`activeWallColliders` HashSet leak) di `PlayerControl.cs` saat musuh dideaktivasi ke pool.
  - Mengimplementasikan sistem kontak tembok *self-healing* berbasis waktu (`lastWallContactTime`) yang otomatis mereset normal kontak dalam 1.5 frame fisika jika tidak ada kontak aktif.
  - Menyaring layer mask dan target prediktif `CapsuleCast` agar mengabaikan tubuh pemain sendiri dan objek dinamis.
  - Menghapus 7 komponen `BoxCollider` solid penghalang pada kusen pintu (`door_frame_*`) di `StagingScene`.
- **Collider Pekarangan & Farmland `[Roi]`**:
  - Mengatasi rintangan pergerakan pemain dengan memindahkan bollard batu ke tepian rumput dan menghapus collider non-esensial jalan setapak.
  - Memperbaiki ukuran collider petak tanah pada [`FarmlandTile.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Farming/FarmlandTile.cs) (`col.size = (1.2f, 0.18f, 1.2f)`).
- **Perbaikan Piala Melayang (Trophy SnapPoint Grounding) `[Rafi]`**:
  - Mengalibrasi offset vertikal pijakan piala sebesar -0.160m agar menempel presisi di atas papan rak.
- **Error Runtime Fatal Enemy Tag `[Rafi]`**:
  - Mengganti `CompareTag("Enemy")` dengan pengecekan berbasis komponen `GetComponent<EnemyBase>()` di `PlayerControl.ProcessWallCollision()`.
  - Mendaftarkan tag `"Enemy"` di Unity TagManager dan menetapkannya ke 6 prefab musuh di `Resources/Enemies/`.
- **Inisialisasi Wave Indicator HUD & Perbaikan Transparansi `[Rafi]`**:
  - Mengatasi race condition urutan inisialisasi dengan menetapkan Script Execution Order eksplisit (TimeManager: -200, NightBrawlManager: -100, NightBrawlWaveUI: 100) dan mekanisme *Deferred Subscription Coroutine*.
  - Mengatasi konflik hierarki ganda `CanvasGroup` di mana root memiliki `alpha = 0` statis sehingga bilah transparan permanen.
  - Memposisikan bilah di sebelah kiri widget Day/Night (`pos = -275, -20`) dan memindahkan spanduk pengumuman ke tengah layar (`pos = 0, 140`, ukuran `640 x 84`).
  - Mengganti seluruh emoji Unicode dengan tipografi ASCII bersih (`NIGHT`, `DAY`, `BOSS`, `W1`, `M`) untuk mengeliminasi kotak kosong `□`.
- **Shader Monster Pink pada Unity 6 URP Cluster Lighting `[Rafi]`**:
  - Menginisialisasi struct `InputData` lengkap pada pass `ForwardLit` shader `FarmBeware/Monster/MonsterFresnelLit`, mengatasi error kompilasi `undeclared identifier 'inputData'`.
  - Memigrasikan semua pemanggilan `meshRenderer.material.color` di `EnemyBase` ke `MaterialPropertyBlock` agar tidak merusak SRP Batcher.

---

## - 2026-09-30

### Ditambahkan (Added)

- **Sistem Daur Ulang Monster Tanpa GC (Enemy Object Pool) `[Rafi]`**:
  - Mengimplementasikan `EnemyObjectPool` untuk mendaur ulang 6 varian monster Night Brawl guna mencegah lag spike saat wave bergulir.
- **Navigasi NavMesh & Sensor Penghindar Rintangan `[Rafi]`**:
  - Navigasi dinamis dengan sensor penghindar rintangan (Whisker Avoidance) agar monster tidak bertumpuk atau macet di sudut pekarangan.
- **Mekanik Serangan Bertelegraf `[Rafi]`**:
  - Area telegraf serangan monster melee dengan teks "Miss!" saat pemain berhasil menghindar.
  - Efek visual garis laser bidikan (Aim Telegraph) dan jejak cahaya peluru (Trail Renderer) pada Corn Musketeer.
- **Deteksi Tabrakan Proyektil Kontinu `[Rafi]`**:
  - Sistem Continuous SphereCast Sweep agar peluru berkecepatan tinggi tidak menembus tubuh pemain.

### Diubah (Changed)

- **Balancing Pertempuran Jarak Jauh `[Rafi]`**:
  - Kecepatan peluru Corn Musketeer disesuaikan menjadi 13.5 m/s agar pemain memiliki jendela waktu reaksi yang adil.
  - Telegraf bidikan Corn Musketeer mengunci arah (*Aim Lock*) selama 0.15 detik sebelum menembak.
  - Pemanggilan monster Night Brawl dialihkan sepenuhnya menggunakan sistem Object Pool.

### Diperbaiki (Fixed)

- **Kalibrasi Proyektil Corn Musketeer `[Rafi]`**:
  - Mengatasi masalah peluru Corn Musketeer yang melayang terlalu tinggi di atas kepala pemain akibat perbedaan titik pivot 3D monster dan pemain.
  - Memperbaiki ketidakkonsistenan kecepatan peluru isometrik saat menembak ke arah atas dan bawah layar agar seimbang dan simetris.
- **Perbaikan Meteran Jarak Indikator Musuh `[Rafi]`**:
  - Memperbaiki bug meteran jarak indikator musuh di tepi layar yang bertambah saat didekati pemain.

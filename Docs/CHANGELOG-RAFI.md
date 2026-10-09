# Changelog (Rafi)

Semua perubahan penting pada proyek ini oleh Rafi akan dicatat di halaman ini.

## - 2026-10-09

### Ditambahkan (Added)
- **Pemisahan Sistem Air Mandiri (Separation of Water Bottle vs Plant Waterer)**:
  - **Bottle of Water (`food_bottle_water` | [`PlayerWaterBottle.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Kitchen/PlayerWaterBottle.cs))**:
    - Diformulasikan khusus untuk kebutuhan minum pemain dengan kapasitas **4 Charges (tegukan)**.
    - Setiap tegukan memulihkan +25 Thirst/Hydration.
    - **HANYA DAPAT DIISI ULANG DI KITCHEN SINK SAJA** via [`SinkManager.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Kitchen/SinkManager.cs).
  - **Plant Waterer (`tool_plant_waterer` | [`PlantWaterer.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Farming/PlantWaterer.cs) & [`Tool_PlantWaterer.asset`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Data/Items/Equipment/Tool_PlantWaterer.asset))**:
    - Komponen & aset perkakas kebun baru berkapasitas **100/100 L** (10L per siraman petak).
    - Terdaftar sebagai singleton service serta di katalog `ItemDatabase.asset` dan `ItemRegistrySO.asset`.
    - **HANYA DAPAT DIISI ULANG DI GARDEN WELL SAJA**.
    - **Ikon Baru Dedikasi ([`Icon_Tool_PlantWaterer.png`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Art/Textures/Icons/Equipment/Icon_Tool_PlantWaterer.png))**: Menggunakan visual kaleng penyiram tanaman metal rustic beresolusi tinggi dengan transparansi penuh, menggantikan ikon botol air minum sebelumnya.
  - **Garden Well Interactable ([`WaterWellInteractable.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Farming/WaterWellInteractable.cs))**:
    - Menyematkan `BoxCollider`, `Highlightable` (dengan `Mat_Highlight.mat`), dan `WaterWellInteractable` pada objek scene `WaterWell` dan prefab `WaterWell.prefab`, serta inisialisasi runtime otomatis (`[RuntimeInitializeOnLoadMethod]`).
    - Objek sumur kini otomatis berpendar/highlight saat pemain berada di dekatnya dan siap berinteraksi.
    - Memvalidasi kepemilikan alat `Plant Waterer` di inventori/hotbar dan mengisinya hingga 100L. Menolak pengisian botol minum dengan feedback floating text.
  - **Validasi Pertanian Ketat ([`FarmlandTile.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Farming/FarmlandTile.cs) & [`PlayerFarmInteraction.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Farming/Adapters/PlayerFarmInteraction.cs))**:
    - Menyiram tanaman kini **wajib memegang Plant Waterer di hotbar aktif**.
    - Memberikan feedback melayang jika pemain mencoba menyiram tanpa alat atau saat air di dalam Plant Waterer habis.
  - **UI Character Sheet Dual-Water Display ([`PlayerStatsDisplayUI.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Player/UI/PlayerStatsDisplayUI.cs))**:
    - Menampilkan informasi terpisah: `{cur}/{max} Sips | {cur}/{max}L Farm`.
  - **Persistensi Save & Load Terpisah ([`GameSaveData.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Data/GameSaveData.cs) & [`SaveSystemManager.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/SaveSystem/SaveSystemManager.cs))**:
    - Menyimpan `waterBottleAmount` (0-4 charges) dan `plantWatererAmount` (0-100L) dengan migrasi otomatis untuk save lama.

- **Toggle Inventory dengan Tombol TAB (Buka & Tutup) ([`InventoryManagerUI.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Inventory/UI/InventoryManagerUI.cs))**:
  - Menyatukan listener tombol TAB dan I pada method `Update()` dengan guard `frameInventoryOpened` untuk mencegah double-toggle pada frame yang sama.
  - Pemain kini dapat membuka tas dengan TAB dan menutupnya kembali secara instan dengan menekan tombol TAB.

- **Menu Kematian Pemain & Checkpoint Pagi Hari ([`DeathScreenUI.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Player/UI/DeathScreenUI.cs) & [`PlayerRespawnController.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Player/PlayerRespawnController.cs))**:
  - Menu layar kematian modular dengan opsi Checkpoint (kembali ke waktu pagi hari sebelum malam terpicu dengan status vital pagi utuh), Load Game, atau Main Menu.

### Diperbaiki (Fixed)
- **Pembersihan Bar Darah Monster saat Load Game ([`EnemyHealthBarManager.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Combat/UI/EnemyHealthBarManager.cs) & [`BossHealthBarManager.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Combat/UI/BossHealthBarManager.cs))**:
  - Mengekspos method `ReleaseAllBars()` dan `ClearAllBosses()` menjadi `public` serta memanggilnya saat `SaveSystemManager.LoadSave()` dan `NightBrawlManager.RestoreNightBrawlState()`.
  - Menambahkan hook `OnDestroy()` pada [`EnemyBase.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Combat/Enemy/EnemyBase.cs) untuk melepas bar darah secara otomatis saat objek musuh di-destroy.
- **Audit & Perbaikan Spawning Night Brawl ([`NightBrawlManager.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Combat/NightBrawlManager.cs))**:
  - Mengoreksi evaluasi `isEncounterDone`: kondisi `(!isBrawlActive && !hasSavedEnemies)` tidak lagi salah menandai malam sebagai selesai saat memuat permainan di fase malam.
  - Me-reset `isWaveInProgress = false` saat memuat sesi malam agar pemanggilan wave dan suspense delay tidak terblokir.
- **Console Assertion & Scene Teardown Leaks ([`SaveSystemUI.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/SaveSystem/SaveSystemUI.cs) & [`DeathScreenUI.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Player/UI/DeathScreenUI.cs))**:
  - Menambahkan guard `HasInstance => instance != null` dan lifecycle `isApplicationQuitting` untuk mencegah pembuatan GameObject baru saat scene dibongkar, menyelesaikan error `go.IsActive()` dan `SaveSystemUI scene cleanup leak`.

## - 2026-10-04

### Ditambahkan (Added)
- **Sistem UI Bar Darah Musuh Dual-Tier (Dual-Tier Enemy Health Bar System)**:
  - **Bar Darah Mengambang Monster Biasa ([`EnemyOverheadBarUI.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Combat/UI/EnemyOverheadBarUI.cs) & [`EnemyHealthBarManager.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Combat/UI/EnemyHealthBarManager.cs))**:
    - Bilah kesehatan melayang (*overhead floating health bar*) individual di atas setiap monster aktif dengan grafis *procedural sliced rounded bar*.
    - **Ghost Damage Bar (Amber Gold)**: Bilah tertinggal yang diam sesaat (0.22s) lalu meluncur turun (*lerp catch-up*), memberikan kepuasan feedback visual atas damage pemain.
    - **Bilah Darah Utama (Ruby Red)** yang berdenyut/menyala lebih terang saat HP kritis (≤ 25%).
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
    - Menambahkan `EnsureHealthBarManagers()` pada `Start()` dan `StartNightBrawl()` untuk menjamin kedua manager UI aktif otomatis di bawah `UI_Canvas` tanpa membutuhkan setup manual di scene.

- **Integrasi Dinamis Spawner Monster Berbasis `MonsterSpawnPoints` ([`NightBrawlManager.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Combat/NightBrawlManager.cs) & [`StagingScene.unity`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scenes/StagingScene.unity))**:
    - Menghubungkan pemanggilan monster tiap wave agar 100% menghormati titik lokasi yang dipindahkan pengguna di scene:
      - `MonsterSpawnPoints` (Parent): `(22.20, 0.00, 52.20)`
      - `SpawnPoint_FrontGate_Left`: `(37.40, 0.00, 64.20)`
      - `SpawnPoint_FrontGate_Center`: `(21.90, 0.00, 64.60)`
      - `SpawnPoint_FrontGate_Right`: `(9.60, 0.00, 64.50)`
    - Mengimplementasikan `GetAllActiveSpawnPoints()` yang secara dinamis mengumpulkan seluruh transform anak aktif di bawah `MonsterSpawnPoints` pada runtime.
    - Mengalibrasi `CalculateRandomSpawnPoint()`: Memilih titik spawn acak secara ketat dari titik aktif tersebut, menerapkan *scatter radius* ringan (1.0m), *surface raycasting*, dan *NavMesh sampling* dengan penjagaan jarak (maksimum 2.5m) agar monster berdiri menapak tanah tepat di titik yang ditentukan.
    - Memperbarui visualisasi `OnDrawGizmosSelected()` agar menampilkan bola gizmo di atas seluruh titik spawn anak.

### Diubah & Diintegrasikan (Changed & Integrated)
- **Integrasi & Penggabungan Fitur dari `Bas-branch` (Merge `Bas-branch` ke `Sprint-branch`)**:
  - Menggabungkan seluruh pembaruan dari `Bas-branch` ke `Sprint-branch` secara bersih tanpa kehilangan fitur maupun regresi struktur baru.
  - **Rekonsiliasi Konflik [`DayNightTimeManager.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Time/DayNightTimeManager.cs)**:
    - Melindungi arsitektur waktu berbasis antarmuka `ITimeService` dan injeksi dependensi via `ServiceLocator.Resolve<ITimeService>()`.
    - Menyelaraskan sistem interaksi tidur kasur (`BedInteractable`) yang sepenuhnya statis tanpa waktu berjalan otomatis (pemain berinteraksi dengan tempat tidur untuk beralih fase atau memajukan hari setelah gelombang malam tuntas).
    - Memposisikan `DayNightTimeManager` sebagai visual presenter murni tanpa benturan wewenang pergantian fase.
  - **Rekonsiliasi Konflik [`StagingScene.unity`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scenes/StagingScene.unity)**:
    - Mempertahankan integritas objek save station, UI canvas, layout pekarangan simetris, dan pencahayaan URP.
  - Melakukan commit merge (`386c69d`) dan push ke `origin/Sprint-branch`.

### Diperbaiki (Fixed)
- **Bug Teleportasi Ghost Monster ke Titik Kematian Lama saat Wave Baru Dimulai ([`EnemyObjectPool.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Combat/Enemy/EnemyObjectPool.cs) & [`EnemyBase.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Combat/Enemy/EnemyBase.cs))**:
  - Mengatasi insiden di mana monster yang tereliminasi di sektor barat pekarangan (dekat pohon cemara `X: -3.2, Z: 10.0`) tiba-tiba muncul/teleport seketika ke titik kematian lamanya saat wave berikutnya dimulai, alih-alih muncul dari gerbang depan.
  - **Penyebab**: Di Unity PhysX, mengubah `transform.position` pada objek non-aktif dengan `Rigidbody` tidak menyinkronkan posisi internal mesin fisika. Saat objek diaktifkan kembali (`SetActive(true)`), PhysX mengembalikan objek ke posisi terakhir saat dinonaktifkan.
  - **Solusi**:
    - Pada `EnemyObjectPool.Spawn()`: Eksplisit mengatur `rb.position = position`, `rb.rotation = Quaternion.identity`, mereset `linearVelocity` & `angularVelocity` ke `Vector3.zero`, dan memanggil `Physics.SyncTransforms()` baik sebelum maupun sesudah `enemy.gameObject.SetActive(true)`.
    - Pada `EnemyBase.ResetEnemyState()`: Memastikan pembersihan momentum fisika dan penataan ulang transform.
- **Penghapusan Hardcoded Clamp Spawner yang Menimpa Posisi Level Designer ([`NightBrawlManager.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Combat/NightBrawlManager.cs))**:
  - Menghapus batasan hardcoded lama `if (p.x >= 14f && p.x <= 28f && p.z >= 50f && p.z <= 60f)` dan `Mathf.Clamp(finalPos.x, 14f, 28f)` yang sebelumnya membuang koordinat baru yang dipindahkan pengguna di luar batas sempit tersebut.
- **Error Kompilasi CS1061 pada `NightBrawlManager`**:
  - Menambahkan deklarasi event publik `public event System.Action<EnemyBase> OnEnemyDied;` pada `NightBrawlManager` dan memanggilnya di dalam `HandleEnemyDied()`, menyelesaikan error kompilasi pada `BossHealthBarManager` dan `EnemyHealthBarManager`.
  - Mendaftarkan seluruh berkas `.meta` Unity yang dihasilkan ke Git tracking.

## - 2026-10-03

### Ditambahkan & Disesuaikan (Added & Adjusted)
- **Kurasi & Eliminasi Redundansi Lampu Taman Outdoor ([`OutdoorGardenLamps`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scenes/StagingScene.unity))**:
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

- **Koreksi Rotasi Terbalik Atap Pelana Sumur Air ([`WaterWell.prefab`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Prefabs/Compound/WaterWell.prefab) & [`StagingScene.unity`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scenes/StagingScene.unity))**:
  - Memperbaiki cacat orientasi rotasi pada komponen `GableRoof` di mana kemiringan atap dan kasau (*rafters*) sebelumnya terbalik membentuk corong V cekung (menampung air hujan ke dalam).
  - Mengoreksi tanda sudut rotasi sumbu X (+34° / -34°) pada 8 elemen atap:
    - `RoofSlope_North` dan `RoofSlope_South` (kemiringan atap sirap kini menukik ke bawah membentuk atap pelana segitiga sempurna).
    - `UnderRafter_Mid_N` dan `UnderRafter_Mid_S` (balok kasau penopang tengah).
    - `GableTruss_West` dan `GableTruss_East` (`Rafter_N` & `Rafter_S` pada rangka kuda-kuda sisi barat dan timur).
  - Menerapkan perbaikan secara permanen ke Prefab Asset [`WaterWell.prefab`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Prefabs/Compound/WaterWell.prefab) dan instance scene.

## - 2026-10-02

### Ditambahkan (Added)
- **Restorasi Penataan Objek Indoor & Pemulihan Posisi Lampu Gantung**:
  - **Pemulihan Posisi Lentera Gantung Workshop ([`ShelterLantern`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Prefabs/Compound/CraftingShelter_Stylized.prefab))**:
    - Memasang balok kayu penopang atap tengah (`Rafter_Center` pada `LocalPos: (0.00, 2.60, 0.00)`) yang miring 10° selaras dengan kanopi atap.
    - Menghubungkan rantai lentera gantung secara kokoh langsung ke bawah balok penopang (`LocalPos: (0.00, 2.25, 0.00)`), mengeliminasi celah melayang 25 cm di udara bebas.
  - **Koreksi & Keseimbangan Lampu Teras Depan ([`WallLamp_Porch`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Prefabs/Decorations/WallLamp_Rustic.prefab))**:
    - Memindahkan `WallLamp_Porch` dari tengah kusen/kaca pintu masuk (`X = 20.25`) ke permukaan dinding solid kanan pintu (`(21.90, 2.10, 25.96)`).
    - Menambahkan lentera dinding kembar simetris di sisi kiri pintu (`WallLamp_Porch_Left` pada `(19.04, 2.10, 25.96)`), menciptakan pencahayaan pintu masuk yang seimbang dan membebaskan akses jalan masuk 100%.
  - **Eliminasi Penembusan Dinding Kulkas Dapur (`Fridge`)**:
    - Menggeser kulkas dan rangkaian stasiun dapur (`Kitchen_Sink`, `food_prep_station`, `Kitchen_Stove`) ke arah timur (`+1.5m` pada sumbu X). Kulkas kini berada seutuhnya di sudut barat laut dalam dapur (`X = 12.08, Z = 11.75`), mengeliminasi cacat visual di mana badan kulkas menonjol 1,2 meter ke pekarangan luar barat.
  - **Pemulihan Posisi Nakas Kamar Tidur (`SmallDrawer`)**:
    - Memindahkan nakas kayu yang sebelumnya tercecer di lantai lorong luar kembali ke dalam kamar tidur (`BedroomZone`) tepat di samping kepala tempat tidur ([`Bed`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scenes/StagingScene.unity)) di `(31.75, 0.44, 19.49)`.
  - **Relokasi & Kalibrasi Kamera Trophy Cabinet (`TrophyCabinetSystem`)**:
    - Menyelaraskan rak piala 12 snap point ke dinding barat kamar tidur (`(22.85, 0.875, 23.50)`) menghadap ke dalam ruangan.
    - Menata ulang kamera First-Person `TrophyCamera` di `(25.45, 1.45, 23.50)` menghadap langsung ke rak piala tanpa terhalang dinding partisi.
  - **Penyelarasan Peti Logistik Tengah (`TestChest`)**:
    - Memindahkan peti penyimpanan lorong tengah dari tengah koridor pejalan kaki ke dinding selatan lorong (`(27.20, 0.04, 13.00)`), menjamin akses sirkulasi antara kamar, dapur, dan gudang tetap lapang dan bebas halangan.

- **Harmonisasi & Penyesuaian Zona Workshop Halaman Belakang (Backyard Artisan Courtyard)**:
  - Merespons penataan manual pengguna terhadap 6 objek kunci: [`CraftingShelter`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Prefabs/Compound/CraftingShelter_Stylized.prefab), [`MerchantTable_Outdoor`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Prefabs/Compound/MerchantTable_Outdoor.prefab), `StorageChest_Outdoor_1`, `GardenLamp_CraftingShelter`, `StepStone_094 (1)`, dan `HousePorch_Steps (1)`.
  - **Penyelarasan Tangga Pintu Belakang ([`HousePorch_Steps (1)`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Prefabs/Compound/CobblestonePathways.prefab))**:
    - Membersihkan duplikasi hierarki nested ganda dari hasil salinan manual.
    - Menyelaraskan posisi anak tangga batu dua tingkat (`Step_Top` dan `Step_Bottom`) tepat presisi di tengah pintu belakang dapur (`door_kitchen.001` pada `X = 13.27, Z = 5.26`).
    - Membersihkan duplikasi serupa pada tangga pintu depan utama.
  - **Jalur Setapak Bebatuan Baru Halaman Belakang (`Branch_Backyard_Workshop`)**:
    - Memisahkan 20+ batu pijakan yang tercecer di cabang lain ke cabang terdedikasi `CobblestonePathways/Branch_Backyard_Workshop`.
    - Merancang ulang jalur ganda organik yang mengalir mulus mulai dari kaki tangga pintu belakang (`StepStone_094 (1)` pada `(12.75, 0.03, 3.75)`) meliuk alami menuju teras depan [`CraftingShelter`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Prefabs/Compound/CraftingShelter_Stylized.prefab) di `(3.74, 0.0, -7.52)`.
    - Menambahkan cabang bebatuan penghubung khusus menuju lapak dagang [`MerchantTable_Outdoor`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Prefabs/Compound/MerchantTable_Outdoor.prefab).
    - Menghilangkan *collider* pada bebatuan jalur untuk mencegah tabrakan/tersangkutnya navigasi karakter.
  - **Restrukturisasi Transform & Material Meja Dagang ([`MerchantTable_Outdoor`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Prefabs/Compound/MerchantTable_Outdoor.prefab))**:
    - Memindahkan root transform [`MerchantTable_Outdoor`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Prefabs/Compound/MerchantTable_Outdoor.prefab) langsung ke koordinat visual `(6.42, 0.0, -10.98)` dan mereset seluruh *local offsets* kaki serta daun meja ke posisi nol yang bersih.
    - Memperbarui material visual dari *default grey Lit* menjadi tekstur kayu hangat [`Mat_Wood.mat`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Materials/Kitchen/Mat_Wood.mat) serta taplak kain mewah [`Mat_Decor_CarpetSquare.mat`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Materials/Furniture/Mat_Decor_CarpetSquare.mat).
    - Memasang komponen `BoxCollider` fisik berukuran proporsional.
  - **Penyelarasan Peti Logistik & Lampu Taman**:
    - Memindahkan parent `OutdoorWorkshopStorage` langsung ke koordinat `(2.89, 0.0, -3.16)` sehingga `StorageChest_Outdoor_1` memiliki *local transform* nol yang rapi, dan menambahkan `BoxCollider` interaksi.
    - Mengatur posisi `GardenLamp_CraftingShelter` di `(2.09, 0.0, -4.76)` sebagai penerang transisi antara peti dan gubuk workshop.
    - Menghapus lampu yatim piatu tak sengaja di koordinat `(0, 0, 0)` yang menembus pagar barat.
  - **Pengecatan Alami Permukaan Tanah (Terrain Splatmap Painting)**:
    - Mengecat lapisan tanah organik lembut ([`TL_DirtPath`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Terrain/New%20Terrain%201.asset)) di sepanjang jalur setapak belakang dari pintu dapur hingga ke teras workshop.
    - Membentuk area tanah lapang (*artisan clearing*) bergradasi halus di bawah [`CraftingShelter`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Prefabs/Compound/CraftingShelter_Stylized.prefab), lapak [`MerchantTable_Outdoor`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Prefabs/Compound/MerchantTable_Outdoor.prefab), dan sudut peti logistik.
  - **Regenerasi NavMesh & Baking**:
    - Melakukan re-bake NavMesh scene secara penuh untuk memastikan pergerakan AI dan pemain di area halaman belakang bebas hambatan.

  - Dokumen master plan level design lengkap disimpan pada [`Docs/outdoor_redesign_plan.md`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Docs/outdoor_redesign_plan.md) (disetujui oleh pengguna).
  - **Zona 2 (Social Hub - Focal Point Sentral)**:
    - Merelokasi [`RelaxationArea_Campfire`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Prefabs/Compound/RelaxationArea_Campfire.prefab) tepat di titik tengah poros pekarangan `(20.5, 0.0, 35.5)`.
    - Api unggun kini menjadi *Primary Visual Focal Point* yang menyambut pemain dari gerbang utama, sekaligus bertindak sebagai bundaran (*round-about*) sirkulasi ke seluruh sayap pekarangan.
  - **Zona 1 (Production Wing - Agrikultur Terpadu)**:
    - Memindahkan [`WaterWell`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Prefabs/Compound/WaterWell.prefab) ke bibir depan/barat kebun di `(24.0, 0.0, 31.5)` dengan bak air menghadap langsung ke arah bedengan kebun.
    - Mengintegrasikan stasiun perkakas dan kompos `GardenToolCorner` (`(26.8, 0.0, 31.5)`) berdampingan dengan sumur air, memangkas travel distance penyiraman dan pemeliharaan 16 petak sawah.
    - Menempatkan `Scarecrow` (`(23.5, 0.0, 39.5)`) dan `PoleLamp_Garden` tepat di gapura masuk kebun.
  - **Zona 3 (Relaxation Sanctuary - Sudut Tenang Kolam)**:
    - Merelokasi [`NaturalPond`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Prefabs/Compound/NaturalPond_Stylized.prefab) ke sudut kanan depan `(9.5, 0.01, 42.0)` dengan rotasi menghadap ke dalam pekarangan (dermaga kayu di `(11.7, 0.0, 42.0)` terhubung langsung dengan cabang jalan setapak).
    - Menambahkan bangku santai kayu tepi kolam `Pond_RelaxationBench` di `(12.2, 0.0, 44.2)` menghadap air dan teratai.
    - Menata ulang semak pelindung `Bush_Stylized` di sekeliling sudut pagar kolam.
  - **Zona 4 (Workshop & Logistics Outpost - Depo Pertukangan)**:
    - Menggeser [`CraftingShelter`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Prefabs/Compound/CraftingShelter_Stylized.prefab) ke arah barat `(8.5, 0.0, 27.5)` menghadap ke pekarangan timur, membebaskan ruang pandang fasad depan rumah.
    - Menambahkan meja dagang luar ruangan [`MerchantTable_Outdoor`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Prefabs/Compound/MerchantTable_Outdoor.prefab) di `(12.0, 0.0, 27.5)`.
    - Menambahkan tumpukan peti logistik `OutdoorWorkshopStorage` (`(6.8, 0.0, 26.5)`) untuk penyimpanan material crafting dan hasil jarahan.
  - **Rekonfigurasi Hierarkis Jalan Setapak ([`CobblestonePathways`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Prefabs/Compound/CobblestonePathways.prefab))**:
    - Menata ulang 106 batu pijakan menjadi jaringan jalan organik bertingkat:
      1. *Central Spine* (2.5m lebar): Dari Gerbang Utama `(20.5, 47.5)` lurus ke selatan, melingkari bundaran *Campfire Plaza*, lalu menyambung mulus ke tangga teras rumah `(20.5, 26.4)`.
      2. *East Branches*: Mengalir ke Gapura Kebun (9 batu) dan ke Sumur Air / Rak Perkakas (8 batu).
      3. *West Branches*: Mengalir ke Dermaga Kolam (12 batu) dan ke Workshop Crafting (11 batu).
    - Memperbarui prefab asset [`CobblestonePathways.prefab`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Prefabs/Compound/CobblestonePathways.prefab).
  - **Pengecatan Terrain Splatmap & Pembersihan Noda Tanah**:
    - Membersihkan bekas noda tanah lama di posisi awal api unggun kembali ke rumput hijau segar (`TL_Grass`).
    - Mengecat lapisan tanah organik (`TL_DirtPath`) persis di bawah seluruh 106 batu pijakan, lingkaran *Campfire Plaza*, area sumur, workshop, dan dermaga kolam.
  - **Distribusi Pencahayaan Dual-Source (`OutdoorGardenLamps`)**:
    - 11 unit lampu taman ditempatkan strategis di simpul-simpul fungsional: 2 di Gerbang Utama, 2 di Teras Rumah, 2 mengapit *Campfire Plaza*, 1 di Gapura Kebun, 1 di Sumur Air, 1 di Dermaga Kolam, 1 di Workshop, dan 1 di Koridor Selatan.
  - **NavMesh Regeneration**:
    - Membangun ulang (*rebake*) permukaan `NavMeshSurface` di seluruh pekarangan untuk menjamin navigasi pemain, companion, dan AI musuh 100% mulus tanpa hambatan.
- **Perluasan Pagar Pekarangan & Penataan Rumah di Tengah (Centered House & Expanded Compound)**:
  - Memperbesar dimensi batas pekarangan [`HomesteadPerimeter`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Prefabs/Compound/HomesteadPerimeter.prefab) dari semula `34.8m x 47.7m` menjadi `42.8m x 66.7m` (X: [0.0, 42.0], Z: [-17.0, 48.0]).
  - Rumah kini berada tepat di tengah-tengah compound (*perfect symmetry*): kedalaman pekarangan depan = 23.2m, kedalaman halaman belakang (*backyard*) = 22.4m, jarak samping barat = 9.5m, dan jarak samping timur = 10.5m.
  - Seluruh elemen depan (16 plot sawah, kolam alami, crafting shelter, sumur, api unggun, dan 106 batu pijakan) tetap aman dan utuh tanpa regresi.
  - Memindahkan seluruh pohon dari depan gerbang utama (area depan kini bersih dan lega) dan melipatgandakan jumlah pohon dari 13 menjadi 28 pohon (14 Oak & 14 Pine).
  - Distribusi pohon baru: 5 pohon di sayap barat, 5 pohon di sayap timur, dan 18 pohon membingkai halaman belakang serta benteng hutan selatan.
  - 100% pohon terintegrasi dengan komponen [`WallOccluder`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Camera/WallOccluder.cs) pada Layer 12 (`Wall`) untuk transparansi kamera otomatis.
  - Penyesuaian batas compound pada [`NightBrawlManager.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Combat/NightBrawlManager.cs) (`IsInsideCompound`) dan penambahan sektor serangan monster dari hutan belakang (Sektor 3 & 4).
- **Integrasi & Penggabungan Fitur dari `Bhaskoro-branch` (Merge `Bhaskoro-branch` ke `Sprint-branch`)**:
  - Dokumentasi terpisah dibuat pada [`Docs/CHANGELOG-BHASKORO.md`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Docs/CHANGELOG-BHASKORO.md).
  - Integrasi Multi-Slot Save/Load System modular (`SaveSystemManager`, `SaveSystemUI`, `SaveStationInteractable`, `GameSaveData`).
  - Integrasi Overhaul Stylized Fantasy Main Menu & Pause Menu (`MainMenuScene.unity`, `MainMenuUI.prefab`, `PauseMenuUI.prefab`, animators, dan tekstur).
  - Rekonsiliasi konflik cerdas: Melindungi scene pekarangan homestead, 16 petak lahan, dan tree occluder via scene grafting tanpa regresi; mencegah bug kritis `m_TimeScale: 0` pada `ProjectSettings/TimeManager.asset`; mengintegrasikan sistem save/load combat dengan arsitektur `WaveProgressionEngine`.
  - Pembersihan 38 file scratch/backup sementara dari root direktori.
- **Integrasi & Penggabungan Fitur dari `branch-roi-1` (Merge `branch-roi-1` ke `Sprint-branch`)**:
  - Rekonsiliasi konflik scene [`StagingScene.unity`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scenes/StagingScene.unity) menggunakan strategi *Scene Reconstruction & Prefab Grafting* di Unity Editor.
  - Integrasi struktur kompleks pekarangan (Homestead Compound Yard): Pagar luar perimeter [`HomesteadPerimeter`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Prefabs/Compound/HomesteadPerimeter.prefab) dengan gerbang utara, jalur setapak bebatuan [`CobblestonePathways`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Prefabs/Compound/CobblestonePathways.prefab), model sumur air pekarangan timur [`WaterWell`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Prefabs/Compound/WaterWell.prefab), area relaksasi api unggun luar ruangan [`RelaxationArea_Campfire`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Prefabs/Compound/RelaxationArea_Campfire.prefab) dengan skrip kelap-kelip cahaya [`CampfireLightFlicker.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Environment/CampfireLightFlicker.cs), serta pagar pembatas kebun [`GardenEnclosure`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Prefabs/Compound/GardenEnclosure.prefab) dan `GardenEnclosure_East`.
  - Integrasi vegetasi pekarangan luar (`_WORLD/Foliage`): 13 pohon modular (8 `Tree_Stylized_Oak` dan 5 `Tree_Stylized_Pine`) yang membingkai perimeter luar pagar.
  - Integrasi sistem Character Sheet 3 kolom pada UI: Menghubungkan [`Panel_PlayerStats`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Prefabs/UI/Panel_PlayerStats.prefab) (kolom kiri) dan [`Panel_Equipment`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Prefabs/UI/Panel_Equipment.prefab) (kolom tengah) dengan panel inventory pemain (kolom kanan) pada [`InventoryManagerUI.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Inventory/UI/InventoryManagerUI.cs) saat menekan tombol `Tab`.
  - Penggabungan batas pekarangan compound pada [`NightBrawlManager.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Combat/NightBrawlManager.cs) (`IsInsideCompound()` dan 4 sektor spawn) yang berpadu mulus dengan mesin progresi `WaveProgressionEngine` dan `EnemyObjectPool` Rafi.
  - Memastikan 100% fitur Rafi tetap beroperasi optimal tanpa regresi: 11 unit lampu taman Dual-Source (`OutdoorGardenLamp`), HUD indikator gelombang PvZ (`NightBrawlWaveUI`), 8 petak tanah interaktif POCO Farmland, serta pencahayaan aman Safe-Zone.
- **Relokasi & Tata Ulang Pekarangan Sesuai Versi Rekomendasi (Master Layout Redesign)**:
  - Konsolidasi Kebun Pertanian (Farmland): 16 petak lahan aktif disatukan ke sisi kiri layar dalam 4 baris bedengan terpadu (`Garden_Bed_Row1` s/d `Row4`) dengan pagar pembatas rendah [`GardenEnclosure`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Prefabs/Compound/GardenEnclosure.prefab) dan pintu masuk ke jalan setapak.
  - Relokasi Sumur Air ([`WaterWell`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Prefabs/Compound/WaterWell.prefab)) ke samping kiri pintu rumah dekat kebun untuk alur kerja penyiraman yang fungsional.
  - Relokasi Api Unggun ([`RelaxationArea_Campfire`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Prefabs/Compound/RelaxationArea_Campfire.prefab)) ke sisi kanan jalan setapak dengan formasi melingkar bangku balok kayu.
  - Pembangunan Pathway Alami: Mengganti ubin kotak kaku dengan 105 batu pijakan alam (*organic curved cobblestone & dirt path*) yang meliuk lembut dari Gerbang Utama menuju teras undakan batu pintu depan rumah, lengkap dengan 4 percabangan fungsional (ke Sumur, Kebun, Api Unggun, Kolam, dan Pondok Kerja).
  - Penambahan 3 Prefab Modular Baru:
    1. [`Scarecrow_Stylized.prefab`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Prefabs/Compound/Scarecrow_Stylized.prefab): Orang-orangan sawah pedesaan di sudut masuk kebun.
    2. [`NaturalPond_Stylized.prefab`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Prefabs/Compound/NaturalPond_Stylized.prefab): Kolam air alami berbibir batu kali dengan teratai, bunga lotus, dermaga kayu mini, dan pembatas tabrakan.
    3. [`CraftingShelter_Stylized.prefab`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Prefabs/Compound/CraftingShelter_Stylized.prefab): Pondok kanopi kayu terbuka dengan meja kerja, peti simpanan, dan lentera gantung hangat.
  - Reposisi 11 unit lampu taman outdoor (`OutdoorGardenLamp`) ke simpul-simpul fungsional dan penambahan lentera dinding teras kiri rumah (`WallLamp_Porch_Left`).
  - Penambahan 11 semak hias (`Bush_Stylized`) di kaki pagar keliling, fondasi rumah, dan bibir kolam.
- **Sistem Transparansi Pohon Penghalang Kamera (Dynamic Tree Occlusion Fading)**:
  - Integrasi komponen [`WallOccluder`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Camera/WallOccluder.cs) pada seluruh 13 pohon di [`StagingScene.unity`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scenes/StagingScene.unity) serta pada prefab dasar [`Tree_Stylized_Oak.prefab`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Prefabs/Environment/Tree_Stylized_Oak.prefab) dan [`Tree_Stylized_Pine.prefab`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Prefabs/Environment/Tree_Stylized_Pine.prefab).
  - Konfigurasi Layer 12 (`Wall`) pada batang dan kanopi pohon agar terdeteksi oleh sistem multi-height Line-of-Sight [`WallOcclusionManager`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Camera/WallOcclusionManager.cs).
  - Dukungan multi-renderer atomik: Seluruh bagian pohon (batang `Trunk` dan seluruh bola daun `Canopy`) memudar secara serempak dan halus (*fade alpha* ke 0.22) saat berada di antara kamera dan karakter pemain.
  - Pemulihan instan & bebas memory leak: Saat pemain keluar dari balik pohon, seluruh material pohon kembali ke material opaque asli (`Mat_Tree_Trunk`, `Mat_Tree_Leaf_Oak`, `Mat_Tree_Leaf_Pine`) dengan dukungan SRP Batcher penuh.

### Diperbaiki (Fixed)
- **Material Hilang / Berwarna Pink pada `CobblestonePathways`**:
  - Investigasi menemukan bahwa seluruh 106 MeshRenderer batu pijakan pekarangan (`CentralCurvedSpine`, `Branch_ScreenLeft_GardenWell`, `Branch_ScreenRight_CampfirePond`, dan `HousePorch_Steps`) memiliki slot material kosong (`null`), sehingga Unity merendernya dengan warna error magenta/pink bawaan.
  - Mengalokasikan material Stylized URP Lit secara harmonis ke seluruh 106 batu:
    - [`Mat_Cobblestone_Warm`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Materials/Environment/Mat_Cobblestone_Warm.mat) (57 batu): Nada batu kali hangat alami sebagai warna utama jalur.
    - [`Mat_Campfire_Stone`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Materials/Environment/Mat_Campfire_Stone.mat) (27 batu): Nada abu-abu sedang untuk variasi visual realistis.
    - [`Mat_Cobblestone_Dark`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Materials/Environment/Mat_Cobblestone_Dark.mat) (22 batu): Aksen bebatuan gelap dan undakan teras rumah (`Step_Bottom` dan `Step_Top`).
  - Mengupdate aset prefab [**`CobblestonePathways.prefab`**](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Prefabs/Compound/CobblestonePathways.prefab) dan menyimpan scene [`StagingScene.unity`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scenes/StagingScene.unity).
  - Melakukan audit menyeluruh ke 1.871 renderer di scene: 100% renderer kini bebas dari material null / pink shader.

## - 2026-10-01

### Ditambahkan (Added)
- Mesin Sistem Pertanian Inti (Core Farming System POCO Backend): Implementasi kelas C# murni tanpa `MonoBehaviour` (`SoilState`, `SoilTile`, `FarmGrid`) dengan event `OnTileUpdated` untuk pemisahan logika yang independen dan testable.
- Asset ScriptableObject `CropData` untuk konfigurasi varietas tanaman, tahap pertumbuhan (*growth stages*), kebutuhan penyiraman, dan rentang hasil panen (*yield*).
- Komponen adapter pertanian `FarmGridManager` untuk memetakan koordinat grid tanah murni ke koordinat dunia 3D (*WorldToGrid* dan *GridToWorld*).
- Sistem visualisasi tanah dinamis (`SoilTileVisualizer`) yang merespons perubahan status petak tanah: kosong (*Empty*), dicangkul (*Tilled*), ditanami kering (*PlantedDry*), disiram basah (*PlantedWatered*), dan siap panen (*ReadyToHarvest*).
- Mekanik interaksi bertani pada pemain: mencangkul tanah dengan Hoe, menyiram tanaman dengan Watering Can, menanam benih dari slot inventori aktif, dan memanen hasil dengan proteksi batas stack inventori (`maxStack = 20`).
- Pertumbuhan tanaman berbasis transisi fase waktu (`TimeManager.OnPhaseChanged`) yang memajukan pertumbuhan tanpa polling `Update()` per-frame.
- Mesin Pertarungan Jarak Dekat (Night Brawl Melee Combat Engine & FSM): Implementasi `MeleeCombatStateMachine` dengan variasi serangan lengkap: kombo ringan 3-hit, serangan tusukan saat berlari (*Dash Attack*), tebasan bertenaga berbiaya tahan tombol (*Charged Heavy Attack*), lompatan menerjang (*Leap Strike*), tendangan Spartan knockback, dan selebrasi *Battlecry*.
- Mesin Progresi Gelombang Monster (`WaveProgressionEngine`): Sistem kalkulasi komposisi wave dinamis per malam, kuota monster bertahap, dan evolusi varian musuh (Tuber Maw, Cyclops Tuber Maw, Taro Brute, Taro Colossus, Corn Musketeer, The Ranger).
- Kemampuan pemanggilan minion (Boss Summon Ability) modular untuk Cyclops Tuber Maw dan The Ranger dengan batasan kuota serta validasi area outdoor.
- Toolkit pertarungan lengkap untuk Taro Colossus: Airborne Slam dengan bayangan penjejak (Tracking Shadow Decal), mekanik interupsi stagger 4 hit, Rock Projectiles, dan Grapple Slam dengan proteksi safety unlock.
- Fase amarah (Enrage Phase) untuk The Ranger saat HP mencapai 50% atau lebih rendah yang melontarkan 20 proyektil melingkar (Radial 360 Burst).
- Visual piala Modular Composite Trophy (Pedestal kayu poles, pilar emas metalik, cawan piala, dan mahkota batu permata) untuk seluruh 12 varian piala dekorasi.
- Komponen pembantu EnemyLootDropHandler untuk memisahkan tanggung jawab kalkulasi drop ekonomi dan item dari EnemyBase.
- Indikator Gelombang Tempur Malam (Night Brawl Wave Indicator HUD): Implementasi presenter visual stateless `NightBrawlWaveUI` yang sepenuhnya event-driven tanpa polling `Update()`.
- Algoritma pra-kalkulasi progres malam dan penanda gelombang (`WaveMilestoneData` & `NightScheduleSummary`) pada `WaveProgressionEngine` dengan jaminan matematika 100% presisi posisi penanda gelombang akhir di ujung bilah (`X = 1.0`).
- Komponen visual `WaveIndicator` dengan posisi jangkar ternormalisasi (*anchor-normalized positioning*) yang responsif di segala rasio layar serta diferensiasi visual penanda bos.
- Efek animasi taktil (*juice*) Zero-GC: hentakan denyut (*scale punch*) pada ikon penjejak monster saat musuh mati, animasi lecutan bendera saat dilewati, dan spanduk pengumuman gelombang/bos di tengah layar (*Center Screen Announcement Overlay*).
- Kalibrasi pencahayaan atmosfer siklus siang dan malam (`Day_LightingTheme` dan `Night_LightingTheme`) dengan konfigurasi temperatur warna (5000K), ambient ground color, dan directional sun observer.
- Penerapan Safe-Zone Light Layers pada interior rumah untuk memisahkan pencahayaan aman di dalam rumah dari atmosfer pertarungan malam di luar rumah.
- Sistem Pencahayaan Luar Ruangan (Outdoor Garden Lighting System): Integrasi aset tiang lentera taman `garden_lamp.fbx` menjadi prefab modular `GardenLamp_Prefab` dengan perakitan fisik realistis: dudukan soket kuningan (`BulbSocket`), tabung kaca bohlam pijar Edison (`LanternBulb`), dan kawat filamen pijar (`LanternFilament`).
- Arsitektur Pencahayaan Ganda (Dual-Source Outdoor Illumination): Kombinasi `Spot Downlight` (40 lux, jangkauan 9m, sudut 135°) untuk proyeksi kolam cahaya terang terfokus ke permukaan tanah/kebun dan `Point Light` (25 lux, jangkauan 15m) untuk pencahayaan ambient sekeliling. Total intensitas mencapai 65 lux (naik 8x lipat dari kalibrasi awal 7.5 lux) dengan temperatur hangat amber vintage (~2400K).
- Komponen pengendali individual [`OutdoorGardenLamp`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Time/Atmosphere/OutdoorGardenLamp.cs) dengan dukungan pendaran Zero-GC berbasis `MaterialPropertyBlock` (kaca susu bersih saat siang, pijar emas HDR saat malam) dan efek kelap-kelip halus (*organic micro-flicker*) berbasis Perlin noise.
- Arsitektur pengontrol terpusat [`OutdoorLightingController`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Time/Atmosphere/OutdoorLightingController.cs) yang terhubung ke `TimeManager.OnPhaseChanged` untuk transisi halus intensitas lampu saat pergantian Day/Night.
- Penempatan strategis 11 unit lampu taman di `StagingScene`: koridor jalur masuk/teras depan (3 unit), area pekarangan belakang & lapak pedagang (2 unit), perimeter kebun pertanian / Farmland (4 unit), dan jalur samping barat & timur (2 unit).

### Diubah (Changed)
- Mengintegrasikan [`OutdoorLightingController`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Time/Atmosphere/OutdoorLightingController.cs) ke dalam [`SceneLightingEditorUtility`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Editor/Rendering/SceneLightingEditorUtility.cs) agar preview mode Day, Night, dan Work Light di Scene View langsung menyinkronkan seluruh lampu outdoor.
- Menetapkan TimeManager sebagai otoritas tunggal (Single Source of Truth) siklus waktu dan memposisikan DayNightTimeManager sebagai visual presenter murni tanpa wewenang memajukan hari.
- Logika serangan Taro Colossus kini dialihkan ke komponen modular terpisah ColossusAirborneAbility dan ColossusGrappleAbility.
- Refactor EnemyBase menggunakan pola arsitektur Hybrid Host Strategy untuk mereduksi kompleksitas god class.
- Migrasi Optimasi Fisika Massal: Mengganti 104 komponen `MeshCollider` statis menjadi `BoxCollider` primitif di `StagingScene` dengan penegakan batas ketebalan minimum 0.05m (*Minimum Thickness Guard*) untuk memangkas komputasi PhysX dan mencegah *tunneling*.
- Kalibrasi tonemapping, color grading, dan respons visual post-processing untuk transisi siang-malam yang lebih atmosferik.

### Diperbaiki (Fixed)
- Mengatasi bug kritis pergerakan pemain yang tiba-tiba terkunci tidak bisa bergerak ke kiri dan justru melenceng ke atas saat menekan tombol `A` atau *left arrow*.
- Menghapus kebocoran state normal tembok (`activeWallColliders` HashSet leak) di `PlayerControl.cs` yang tidak ter-reset saat musuh dideaktivasi/kembali ke Object Pool via `SetActive(false)`.
- Mengimplementasikan sistem kontak tembok *self-healing* berbasis waktu (`lastWallContactTime`) yang otomatis kedaluwarsa dan mereset normal kontak dalam 1.5 frame fisika jika tidak ada kontak aktif.
- Mengisolasi objek dinamis (musuh, proyektil, item loot) agar tidak memicu deteksi *wall sliding* pada pemain.
- Menyaring layer mask dan target prediktif `CapsuleCast` agar mengabaikan tubuh pemain sendiri, anak objek, dan objek dengan Rigidbody dinamis.
- Menghapus 7 komponen `BoxCollider` solid penghalang pada kusen pintu (`door_frame_*`) di `StagingScene` yang memblokir perlintasan antar-ruangan dan pekarangan.
- Mengatasi potensi tumpang tindih pemicu pergantian hari otomatis dari visual presenter saat jam mencapai senja.
- Memperbaiki bug status kontrol pemain yang berisiko terkunci permanen saat menerima efek status bos melalui penambahan coroutine timeout dan metode safety unlock.
- Mengatasi bug piala melayang di rak (Trophy Shelf) dengan mengalibrasi offset vertikal pijakan piala sebesar -0.160m agar menempel presisi di atas papan rak (SnapPoint grounding).
- Memperbarui desain visual piala menjadi piala kejuaraan megah (Grand Championship Cup) lengkap dengan gagang ganda lengkung (twin handles), pelat nama kuningan (brass plaque), pilar berulir, dan mahkota permata bersudut.
- Mengatasi error runtime fatal `Tag: Enemy is not defined` di `PlayerControl.ProcessWallCollision()` dengan menghapus `CompareTag("Enemy")` yang bergantung pada tag tidak terdaftar, diganti pengecekan berbasis komponen `GetComponent<EnemyBase>()` yang lebih robust.
- Mendaftarkan tag `"Enemy"` di Unity TagManager dan menetapkannya ke seluruh 6 prefab musuh di `Resources/Enemies/` agar `CompareTag` aman digunakan di bagian kode lain.
- Memperbaiki indikator gelombang (Wave Indicator HUD) yang tidak muncul saat malam akibat race condition urutan inisialisasi skrip: `NightBrawlWaveUI` berlangganan event sebelum `NightBrawlManager` selesai `Awake()`. Diperbaiki dengan menetapkan **Script Execution Order** eksplisit (TimeManager: -200, NightBrawlManager: -100, NightBrawlWaveUI: 100) dan menambahkan mekanisme **Deferred Subscription Coroutine** yang secara otomatis mencoba berlangganan ulang setiap 250ms hingga berhasil.
- Memperbaiki bug potensi *double-subscription* pada `TimeManager.OnPhaseChanged` di `NightBrawlWaveUI` yang menyebabkan event handler terpanggil ganda. Ditambahkan tracking terpisah `isTimeManagerSubscribed` dan pola defensif *unsubscribe-before-subscribe*.
- Mengatasi monster berwarna pink/magenta saat dimunculkan di mode Play dengan:
  - Memperbaiki shader `FarmBeware/Monster/MonsterFresnelLit`: Menginisialisasi struct `InputData` lengkap (`positionWS`, `positionCS`, `normalWS`, `viewDirectionWS`, `shadowCoord`, `normalizedScreenSpaceUV`, `shadowMask`) pada fragment pass `ForwardLit`, mengatasi error kompilasi `undeclared identifier 'inputData'` pada jalur *clustered lighting* (`_CLUSTER_LIGHT_LOOP`) Unity 6 URP.
  - Memigrasikan semua pemanggilan `meshRenderer.material.color` di `EnemyBase` ke `MaterialPropertyBlock` agar tidak merusak SRP Batcher dan tidak kehilangan properti `_BaseColor`.
- Memperbaiki bug kritis Indikator Gelombang (`HUD_NightBrawlWaveTracker`) yang tidak tampil sama sekali di layar:
  - Mengatasi konflik hierarki ganda `CanvasGroup`: GameObject root `HUD_NightBrawlWaveTracker` sebelumnya memiliki `CanvasGroup` statis dengan `alpha = 0`, sementara skrip `NightBrawlWaveUI` hanya memudarkan `CanvasGroup` anak (`WaveTracker_Panel`). Akibat multiplikasi alpha hierarkis Unity UI (`0 * 1 = 0`), seluruh bilah gelombang tetap transparan permanen. Kini `mainCanvasGroup` mengontrol langsung root `CanvasGroup` dan komponen ganda pada anak telah dibersihkan.
  - Memposisikan bilah indikator gelombang persis di **sebelah kiri** (bersebelahan) widget keterangan Day/Night (`HUD_CombatPhaseTracker`) di pojok kanan atas layar (`pos = -275, -20`, ukuran `320 x 56`), sesuai permintaan pengguna.
  - Memindahkan spanduk pengumuman gelombang dan bos (`Announcement_Overlay`) keluar dari widget sudut kanan atas menjadi anak langsung dari `UI_Canvas` dengan posisi tengah layar (`pos = 0, 140`, ukuran `640 x 84`), sehingga tampil megah dan tidak terpotong.
  - Mengganti seluruh karakter emoji Unicode yang tidak ada di font asset `LiberationSans SDF` (seperti `\u26A0` ⚠️, `\U0001F9DF` 🧟, `\U0001F6A9` 🚩, `\u2600` ☀️, `\U0001F319` 🌙) dengan tipografi ASCII bersih (`NIGHT`, `DAY`, `BOSS`, `W1`, `M`) untuk mengeliminasi kotak kosong `□` dan peringatan konsol.

## - 2026-09-30

### Ditambahkan (Added)
- Sistem Enemy Object Pool untuk mendaur ulang 6 varian monster Night Brawl guna mencegah lag spike saat wave bergulir.
- Navigasi NavMesh dan sensor penghindar rintangan (Whisker Avoidance) agar monster tidak bertumpuk atau macet di sudut pekarangan.
- Mekanik serangan bertelegraf pada monster melee dengan indikator area dan teks "Miss!" saat pemain berhasil menghindar.
- Efek visual garis laser bidikan (Aim Telegraph) dan jejak cahaya peluru (Trail Renderer) pada serangan Corn Musketeer.
- Sistem pendeteksi tabrakan proyektil kontinu (Continuous SphereCast Sweep) agar peluru tidak menembus tubuh pemain.

### Diubah (Changed)
- Kecepatan peluru Corn Musketeer disesuaikan menjadi 13.5 m/s agar pemain memiliki waktu reaksi yang adil untuk menghindar.
- Telegraf bidikan Corn Musketeer kini mengunci arah (Aim Lock) selama 0.15 detik sebelum menembak.
- Pemanggilan monster Night Brawl kini dialihkan sepenuhnya menggunakan sistem Object Pool.

### Diperbaiki (Fixed)
- Mengatasi masalah peluru Corn Musketeer yang melayang terlalu tinggi di atas kepala pemain akibat perbedaan titik pivot 3D monster dan pemain.
- Memperbaiki ketidakkonsistenan kecepatan peluru isometrik saat menembak ke arah atas dan bawah layar agar seimbang dan simetris.
- Memperbaiki bug meteran jarak indikator musuh di tepi layar yang bertambah saat didekati pemain.

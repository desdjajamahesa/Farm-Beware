# Changelog (Rafi)

Semua perubahan penting pada proyek ini oleh Rafi akan dicatat di halaman ini.

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

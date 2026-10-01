# Changelog

Semua perubahan penting pada proyek ini akan dicatat di halaman ini.

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
- Indikator Gelombang Bergaya "Plants vs. Zombies" (Night Brawl Wave Indicator HUD): Implementasi presenter visual stateless `NightBrawlWaveUI` yang sepenuhnya event-driven tanpa polling `Update()`.
- Algoritma pra-kalkulasi progres malam dan penanda gelombang (`WaveMilestoneData` & `NightScheduleSummary`) pada `WaveProgressionEngine` dengan jaminan matematika 100% presisi posisi bendera pamungkas (Boss Wave Flag) di ujung bilah (`X = 1.0`).
- Komponen visual `MilestoneFlagView` dengan posisi jangkar ternormalisasi (*anchor-normalized positioning*) yang responsif di segala rasio layar serta diferensiasi visual bendera bos (tengkorak merah).
- Efek animasi taktil (*juice*) Zero-GC: hentakan denyut (*scale punch*) pada ikon penjejak monster saat musuh mati, animasi lecutan bendera saat dilewati, dan spanduk pengumuman gelombang/bos di tengah layar (*Center Screen Announcement Overlay*).
- Kalibrasi pencahayaan atmosfer siklus siang dan malam (`Day_LightingTheme` dan `Night_LightingTheme`) dengan konfigurasi temperatur warna (5000K), ambient ground color, dan directional sun observer.
- Penerapan Safe-Zone Light Layers pada interior rumah untuk memisahkan pencahayaan aman di dalam rumah dari atmosfer pertarungan malam di luar rumah.

### Diubah (Changed)
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

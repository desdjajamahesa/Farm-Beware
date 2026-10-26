# CHANGELOG - TECHNICAL OVERVIEW (Farm-Beware)

Konsolidasi dokumentasi teknis dari seluruh kontributor tim pengembang (`Rafi`, `Bhaskoro`, `Roi`) yang diintegrasikan ke dalam `Sprint-branch`, diurutkan secara kronologis berdasarkan tanggal rilis.

---

### Tim Kontributor & Domain Fokus:
- **[Rafi]**: *Core Architecture, Night Brawl Combat Engine, Farming POCO Backend, Dynamic Lighting & Shaders, Level Design & Occlusion System.*
- **[Bhaskoro]**: *Multi-Slot Save & Load Persistence, Stylized Fantasy Main Menu & Pause Menu, Additive Gameplay Hooks.*
- **[Roi]**: *Character Sheet 3-Column UI (Stats & Equipment), Compound Perimeter Structures & Foliage, GPU Instancing Optimizations.*

---

## Daftar Isi Kronologis
- [2026-10-26](#--2026-10-26)
- [2026-10-25](#--2026-10-25)
- [2026-10-22](#--2026-10-22)
- [2026-10-20](#--2026-10-20)
- [2026-10-17](#--2026-10-17)
- [2026-10-16](#--2026-10-16)
- [2026-10-15](#--2026-10-15)
- [2026-10-14](#--2026-10-14)
- [2026-10-10](#--2026-10-10)
- [2026-10-09](#--2026-10-09)
- [2026-10-04](#--2026-10-04)
- [2026-10-03](#--2026-10-03)
- [2026-10-02](#--2026-10-02)
- [2026-10-01](#--2026-10-01)
- [2026-09-30](#--2026-09-30)

---

## - 2026-10-26

### Ditambahkan & Diintegrasikan (Added & Integrated)

#### 1. Overhaul Tipografi Sinematik & Realignment Trigger Pipa Bawah Tanah Day 1 `[Rafi]`
- **Tipografi & Estetika Visual Sinematik Cutscene ([`CutsceneLetterboxUI.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Storyline/Cutscenes/CutsceneLetterboxUI.cs), [`StorylineUI.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Storyline/UI/StorylineUI.cs))**:
  - Mendiagnosis keterbatasan visual subtitle dan prologue intro cards yang berukuran terlalu kecil (20f dan 24f) tanpa pelat kontras di atas dunia 3D.
  - Memperbesar ukuran teks subtitle menjadi **28f** (auto-sizing 22f – 32f, Bold, moonlight ivory `#FAF8F2`, kerning 1.2f, line spacing 6f).
  - Mengimplementasikan frosted obsidian glass backing plate (`SubtitlePlate`, color `#080A0F`, alpha 0.78) dengan garis aksen emas tipis 2px (`TopAccentLine`, color `#F2C752`) di bagian atas panel.
  - Memperbesar teks intro story cards menjadi **36f** (auto-sizing 26f – 42f, Bold, line spacing 18f, kerning 1.2f) dengan penyajian kutipan narasi sinematik murni `“...”` tanpa label header "PROLOGUE".
  - Memperbesar tinggi bilah letterbox sinematik menjadi **125f** (dari 110f) dan mempercantik tombol skip hold gauge dengan font 15f bold aksen amber.
  - Memperbesar dialog in-game pada `StorylineUI`: body 21f (dari 16f), speaker 24f (dari 22f bold gold), panel dialog diperluas menjadi 920x200f.
- **Koreksi Logika Pemicu Penemuan Pipa Logam Day 1 (`FirstTill` vs `FirstPlant`)**:
  - Mendiagnosis dan mengoreksi anomali naratif di mana beat pemotongan pipa logam ("CLANG! My hoe just hit something metal buried deep in the dirt...") sebelumnya keliru terpicu saat menanam bibit tanaman (`FirstPlant`).
  - Menambahkan trigger enum `StoryTriggerType.FirstTill` pada [`StoryEnums.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Storyline/Data/StoryEnums.cs).
  - Menambahkan event statis `FarmlandTile.OnAnySoilTilled` serta helper `InvokeSoilTilled` pada [`FarmlandTile.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Farming/FarmlandTile.cs), dipanggil saat tanah berhasil dicangkul di [`PlayerFarmInteraction.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Farming/Adapters/PlayerFarmInteraction.cs).
  - Mengintegrasikan listener `HandleSoilTilled` di [`StorylineManager.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Storyline/StorylineManager.cs) dan mengalihkan beat `D1_FIRST_TILL` di [`StoryDatabase.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Storyline/Data/StoryDatabase.cs) ke trigger `FirstTill`.
- **Ekspansi Automated Audit Test Suite ke 64 Tes Otomatis (64/64 Passed) ([`GameplayAndHUDTests.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Editor/GameplayAndHUDTests.cs))**:
  - Menambahkan **Test 64** yang memverifikasi tipografi cutscene (subtitle >= 28f, story cards >= 36f, backing plate frosted obsidian, gold accent line) serta memvalidasi secara deterministik bahwa pemicu penemuan pipa logam Day 1 hanya aktif saat pencangkulan tanah (`FirstTill`) dan tidak lagi terpicu saat penanaman (`FirstPlant`). Seluruh 64 tes audit lulus 100%.

#### 2. Arsitektur Karakter PlayerV2, Generic Rig & Normalisasi HandSocket `[Rafi, Roi]`
- **Transisi Model & Generic Rig**:
  - Menggantikan model karakter lama berbasis Mixamo Humanoid dengan aset resmi `Farmer_Asep_V2` ([`character_player_v2.prefab`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Prefabs/Player/character_player_v2.prefab)).
  - Mengonfigurasi `Animator.avatar = null` pada instance runtime untuk memastikan kecocokan 100% terhadap generic FBX animation curves tanpa distorsi retargeting tulang humanoid.
  - Membangun controller state machine baru [`AsepAnimator_V2.controller`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Resources/Player/AsepAnimator_V2.controller) yang mendukung:
    - Locomotion blend tree (Idle, Walk, Run, Sprint).
    - Farming animations: `farm_plantseed` dan `farm_watering`.
    - Athletic dodge roll: `roll_fit` dan `standing jump`.
    - Melee combo string: `standing melee attack horizontal`, `standing melee attack downward`, dan `standing melee attack 360 low`.
- **Normalisasi Skala HandSocket & Weapon Mount ([`PlayerEquipment.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Player/PlayerEquipment.cs))**:
  - Mendiagnosis isu di mana weapon/tool yang di-equip mengalami pembesaran atau pengecilan ekstrem karena scale inheritance dari bone rig.
  - Menetapkan normalisasi skala absolut `transform.localScale = Vector3.one` pada seluruh socket instansiasi tool/senjata (`HandSocket`, `WeaponSocket`).
  - Menghubungkan weapon visual prefab resmi untuk hoe, plant waterer, pedang, dan kapak tempur.
- **Ekspansi Buffer Deteksi Tempur Non-Alloc & Input Klik Kanan ([`PlayerEquipment.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Player/PlayerEquipment.cs), [`PlayerControl.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Player/PlayerControl.cs))**:
  - Mengembangkan buffer deteksi tabrakan melee non-alloc `_sphereHitResults` dan `_coneHitResults` dari kapasitas 16 menjadi **64 colliders**, mencegah dropped hits saat berhadapan dengan gerombolan musuh padat pada Night Brawl.
  - Menambahkan dukungan eksekusi combo attack melalui klik kanan mouse (`Mouse.current.rightButton.wasPressedThisFrame`) melengkapi klik kiri standar pada `PlayerControl.HandleAttackInput()`.

#### 2. Kalibrasi Geometri Duduk Cockpit & State Machine Berkendara Truk `[Rafi]`
- **Akar Masalah (Root Cause Diagnostic)**:
  - Karakter tampak berdiri kaku menembus atap kabin saat menaiki truk pickup pada Day 0 Cutscene dan free driving.
  - Investigasi mendalam menemukan bahwa `PlayerControl.Update()` memproses input baseline dan terus memanggil `animator.SetFloat("Speed", 0f)` serta state default, menimpa (*overriding*) pose duduk. Selain itu, posisi anchor kursi penumpang (`DriverSeatAnchor`) tidak sejajar dengan kurva animasi duduk generic.
- **Implementasi Solusi Arsitektural**:
  - **Pose Animasi Duduk Kustom ([`PlayerV2_DrivingPose.anim`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/PlayerV2/anim/PlayerV2_DrivingPose.anim))**:
    - Membuat pose duduk statis generic dengan tangan memegang setir dan kedua kaki tertekuk di pedal gas/rem pada elevasi $Y = 0.46\text{m}$.
    - Menghubungkan state `Driving` pada layer dasar `AsepAnimator_V2.controller` dengan transisi prioritas instan (`Driving == true`).
  - **Driving Heartbeat & Input Lock Guard ([`ArcadeVehicleController.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Vehicles/ArcadeVehicleController.cs), [`PlayerControl.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Player/PlayerControl.cs))**:
    - `ArcadeVehicleController.DriveVehicle()` kini secara konsisten menegakkan pose duduk: memanggil `anim.Play("Driving", 0, 0f)` dan mengeksekusi `anim.Update(0f)` setiap frame saat kendaraan aktif.
    - Menambahkan guard pada `PlayerControl.Update()` sehingga saat `playerCtx.HasInputLock("VehicleDriving")` atau `isDriving` aktif, seluruh evaluasi locomotion animator dilewati.
  - **Rekalibrasi Titik Anchor Kursi Pengemudi**:
    - Menata ulang koordinat lokal `DriverSeatAnchor` pada truk pickup ke $Y = 0.08\text{m}$ (sejajar permukaan busa jok $Y = 0.72\text{m}$ world space), mengeliminasi tembus pandang kaki ke bawah sasis truk.

#### 3. Konsolidasi Alur Prolog Day 0 / Day 1 & World Placement Truk `[Rafi]`
- **Transisi Kasur Tidur Day 0 ke Day 1 ([`BedInteractable.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Interaction/BedInteractable.cs), [`OpeningCutsceneController.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Storyline/Cutscenes/OpeningCutsceneController.cs))**:
  - Menghubungkan interaksi kasur kamar tidur saat Day 0: berinteraksi dengan kasur memicu konfirmasi tidur malam, memajukan nomor hari dari Day 0 ke Day 1, mengatur jam ke 06:00 pagi, dan merestorasi status stamina/kesehatan pemain.
- **Permanent World Placement Truk Pengiriman ([`StagingScene.unity`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scenes/StagingScene.unity))**:
  - Menetapkan posisi permanen truk pickup di depan halaman masuk kebun:
    - Posisi: `X = 30.5509, Y = 0.00035, Z = 54.1408`
    - Rotasi: `X = 0.0000, Y = -0.3903, Z = 0.0000, W = 0.9207`
    - Skala: `Vector3.one` (1, 1, 1)
  - Menyembunyikan spanduk pengujian usang (*test banners*) agar kanvas visual bebas dari gangguan visual sementara.

#### 4. Subsistem Side Quest 'First Steps' & Farmland Signal Bus `[Rafi]`
- **Arsitektur Pure C# POCO Quest Model ([`QuestProgressModel.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Quest/Core/QuestProgressModel.cs))**:
  - Mengembangkan model quest murni tanpa siklus hidup Unity (Zero-GC, deterministik) di namespace `FeaturesQuest.Core`.
  - Mengelola target side quest permulaan (*First Steps*):
    1. Mengumpulkan cangkul (`tool_hoe`).
    2. Mengumpulkan kaleng penyiram (`tool_plant_waterer`).
    3. Mengumpulkan botol air mineral (`food_bottle_water`).
    4. Mengumpulkan bibit ubi (`seed_sweet_potato`).
    5. Mencangkul tanah, menanam bibit, dan menyiram petak kebun.
- **QuestManager & Event Signal Bus ([`QuestManager.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Quest/QuestManager.cs), [`FarmlandTile.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Farming/FarmlandTile.cs))**:
  - Menambahkan sinyal event statis pada `FarmlandTile`: `OnAnyTileTilled`, `OnAnySeedPlanted`, dan `OnAnyTileWatered`.
  - `QuestManager` mendengarkan event inventori (`OnInventoryChanged`) dan sinyal pertanian untuk memperbarui status pencapaian secara instan.
- **Antarmuka Glassmorphic HUD Quest ([`QuestHUDUI.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Quest/UI/QuestHUDUI.cs))**:
  - Membangun kartu UI otomatis berlatar belakang glassmorphism semi-transparan yang berlabuh tepat di bawah jam digital HUD (`CombatPhaseTrackerUI`).
  - Menampilkan daftar checklist objektif interaktif dengan ikon status (centang hijau untuk selesai, lingkaran abu-abu untuk proses).

### Diperbaiki (Fixed)

#### 5. Pembersihan Entity HTML TextMeshPro & Koreksi Glyph Atlas `[Rafi]`
- **Eliminasi Artefak Render `□ &nbsp; P R O L O G U E &nbsp; □` ([`CutsceneLetterboxUI.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Storyline/Cutscenes/CutsceneLetterboxUI.cs), [`DayTransitionUI.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Time/UI/DayTransitionUI.cs), [`StoveUIManager.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Kitchen/UI/StoveUIManager.cs), [`InventoryManagerUI.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Inventory/UI/InventoryManagerUI.cs))**:
  - Mendiagnosis render artifact `□ &nbsp; P R O L O G U E &nbsp; □`:
    - TextMeshPro parser hanya mengevaluasi tag XML formatting bawaan (`<size>`, `<color>`, `<b>`, dll.) dan tidak memproses HTML character entities seperti `&nbsp;`, menyebabkan teks literal `&nbsp;` tercetak ke layar.
    - Karakter Unicode bintang `✦` (`\u2726`) tidak tercakup dalam tabel font atlas bawaan `LiberationSans SDF`, memicu fallback square `□`.
  - Mengganti format header prolog menjadi `<size=65%><color=#E5B044>—   P R O L O G U E   —</color></size>` menggunakan spasi standar ASCII dan glyph em-dash `—` (`\u2014`) yang diverifikasi ada pada atlas.
  - Mengonversi simbol dekoratif di `DayTransitionUI`, `StoveUIManager`, dan `InventoryManagerUI` ke bullet `•` (`\u2022`) dan em-dash `—` (`\u2014`) yang terbukti aman dari missing glyph.

#### 6. Keamanan Siklus Hidup Mesin & Eliminasi Warning Edit-Mode `[Rafi]`
- **SafeDestroy Lifecycle Abstraction ([`KitchenStationProgressOverlay.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Kitchen/UI/KitchenStationProgressOverlay.cs), [`WorldItemPickup.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Combat/WorldItemPickup.cs))**:
  - Mengeliminasi peringatan native engine `"Destroy may not be called from edit mode! Use DestroyImmediate instead."` saat menjalankan pengujian otomatis di Unity Editor.
  - Mengimplementasikan helper `SafeDestroy(Object target)`:
    - Memanggil `Object.DestroyImmediate(target)` jika `!Application.isPlaying`.
    - Memanggil `Object.Destroy(target)` jika `Application.isPlaying == true`.
- **Eliminasi Kebocoran Material Edit-Mode**:
  - Mengganti mutasi material langsung pada renderer dengan `MaterialPropertyBlock` di `KitchenStationProgressOverlay`, mencegah terbentuknya duplikat material yang tidak terlacak (*material leak assertion*).
- **Proteksi Singleton OnDestroy saat Game Berhenti**:
  - Mengeliminasi error `"GameObjects can not be made active when they are being destroyed"` pada `OpeningCutsceneController`, `StorylineUI`, dan `JournalUI` dengan memeriksa status penghancuran dan menghindari pengaktifan objek kembali saat teardown.

#### 6. Ekspansi Audit Test Suite ke 63 Tes Otomatis (63/63 Passed) `[Rafi]`
- Menambahkan **Test 50 - Test 63** pada [`GameplayAndHUDTests.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Editor/GameplayAndHUDTests.cs):
  - Validasi integrasi rig generic PlayerV2, controller locomotion, dan normalisasi HandSocket.
  - Validasi pose duduk kendaraan dan sinkronisasi heartbeat rotasi roda.
  - Validasi transisi Day 0/Day 1, status tidur kasur, dan registrasi signal quest.
  - Validasi ekspansi buffer serang 64 target dan penempelan hierarki QuestHUD.
  - Hasil: Seluruh 63 unit test lolos tanpa kegagalan (63/63 Passed, 0 Errors, 0 Warnings).

---

## - 2026-10-25

### Ditambahkan & Diintegrasikan (Added & Integrated)

#### 1. Overhaul Perspektif Kamera Sinematik & Cutscene Kedatangan Truk `[Rafi]`
- **Lensa Sinematik 50mm ([`OpeningCutsceneController.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Storyline/Cutscenes/OpeningCutsceneController.cs))**:
  - Menggantikan sudut pandang lebar yang terdistorsi dengan lensa sinematik proporsional 50mm (`fieldOfView = 50.0`, `nearClipPlane = 0.3`).
  - Menetapkan koordinat sudut pandang kamera dari arah utara bebas rintangan (`Z = 57.5 - 62.5`), mengeliminasi clipping visual pagar depan dan tiang gapura kebun.
- **Dinamika Fisika Kendaraan Zero-GC ([`VehicleDynamicsCore.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Vehicles/Core/VehicleDynamicsCore.cs))**:
  - Mengembangkan simulasi fisika akselerasi murni POCO Zero-GC yang menghitung kurva torsi mesin, batas kecepatan maksimum, transfer bobot pengereman, dan radius belok dinamis.
  - Mengintegrasikannya ke dalam `ArcadeVehicleController` sebagai adapter thin MonoBehaviour.
- **Letterbox & Subtitle Typewriter ([`CutsceneLetterboxUI.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Storyline/Cutscenes/CutsceneLetterboxUI.cs))**:
  - Mengembangkan panel bilah hitam sinematik atas-bawah dengan animasi sliding mulus.
  - Menampilkan narasi pengantar cerita bertahap (*typewriter subtitle*) yang mengiringi mobil saat melaju memasuki kebun.
  - Mendukung pembatalan instan (*Skip*) dengan tombol `Space` atau klik mouse tanpa menghasilkan glitch orientasi kamera.

---

## - 2026-10-22

### Ditambahkan & Diintegrasikan (Added & Integrated)

#### 1. Arsitektur Naratif Modular & Siklus Cerita 5 Hari `[Rafi]`
- **Decoupled Storyline Engine ([`StorylineEngine.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Storyline/Core/StorylineEngine.cs))**:
  - Mengembangkan mesin naratif deterministik POCO yang mengelola tahapan fakta cerita (`StoryState`), pencapaian objektif (`StoryObjective`), dan kueri dialog.
  - Mengintegrasikan kanon narasi fasilitas riset biokimia NexaPhyto yang terungkap secara bertahap dari Hari 1 hingga klimaks Hari 5.
- **Antarmuka Dialog & Jurnal Cerita ([`StorylineUI.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Storyline/UI/StorylineUI.cs), [`JournalUI.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Storyline/UI/JournalUI.cs))**:
  - Membangun antarmuka jurnal terpadu yang dapat dibuka menggunakan shortcut tombol **`J`**.
  - Mengimplementasikan kartu dialog glassmorphic dengan portret karakter, nama pembicara, dan typewriter text effect.
  - Mengembangkan sequence perkenalan bos sinematik ([`BossIntroSequence.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Storyline/UI/BossIntroSequence.cs)) saat boss gelombang malam pertama kali menampakkan diri.
- **Centralized Modal Input Stack ([`PlayerControl.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Player/PlayerControl.cs))**:
  - Memusatkan seluruh status gating input jendela modal ke `ModalStackManager`, menjamin kontrol pergerakan karakter dan aksi alat terkunci secara aman saat pemain membaca jurnal cerita atau berdialog.
- **Aset Data Jagung & Farm Grid Dual Lookup**:
  - Menambahkan ScriptableObject resmi [`CropData_Corn.asset`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Data/Items/Crops/CropData_Corn.asset) dan memperluas resolver string di `FarmGridManager` untuk mendukung rotasi panen jagung.

---

## - 2026-10-20

### Diperbaiki & Dioptimalkan (Fixed & Optimized)

#### 1. Monster Overhead & Boss Health Bar UI Canvas Resolution & Hierarchical Alpha Fix `[Rafi]`
- **Akar Masalah (Root Cause Diagnostic)**:
  - Pada rilis sebelumnya, pemanggilan `FindFirstObjectByType<Canvas>()` di dalam `NightBrawlManager.EnsureHealthBarManagers()`, `EnemyHealthBarManager.EnsureContainer()`, dan `BossHealthBarManager.EnsureContainer()` secara non-deterministik mengambil `DayTransition_Panel` (yang memiliki sub-canvas akibat auto-hierarchy di `DayTransitionUI.EnsureUIHierarchy()`).
  - Karena `DayTransition_Panel` memiliki `CanvasGroup` dengan nilai `alpha = 0f` (di luar transisi fajar/senja), hierarki uGUI melakukan multiplikasi transparansi bertingkat (`effective_alpha = 0.0 * child_alpha = 0.0`). Akibatnya, `EnemyHealthBarContainer`, `EnemyHealthBarManager`, `BossHealthBarPanel`, dan seluruh overhead health bar monster menjadi 100% transparan dan tidak terlihat di layar game.
  - Masalah serupa teridentifikasi pada `BuffTooltipUI.EnsureInstanceExists()`.
- **Implementasi Solusi Arsitektural**:
  - **`CanvasHelper.ResolveMainUICanvas()` ([`CanvasHelper.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Core/UI/CanvasHelper.cs))**: Membangun canonical utility resolver berkinerja tinggi yang memprioritaskan root GameObject `"UI_Canvas"`, menyaring sub-canvas dan panel transien, serta memvalidasi root canvas `ScreenSpaceOverlay`.
  - **Auto-Reparenting Resilience ([`EnemyHealthBarManager.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Combat/UI/EnemyHealthBarManager.cs) & [`BossHealthBarManager.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Combat/UI/BossHealthBarManager.cs))**: Memastikan manager dan container mendeteksi jika salah diparenting di bawah panel lain dan secara otomatis mere-parent dirinya kembali ke target root canvas.
  - **Sanitasi `DayTransitionUI` ([`DayTransitionUI.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Time/UI/DayTransitionUI.cs))**: Menghapus penambahan sub-canvas dan `CanvasScaler` redundan jika GameObject sudah berada di bawah root Canvas (`UI_Canvas`), serta memanggil `transform.SetAsLastSibling()` untuk mempertahankan urutan rendering teratas tanpa mengacaukan Canvas query engine.
  - **Konfigurasi Visibilitas Anti-Clutter ([`EnemyOverheadBarUI.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Combat/UI/EnemyOverheadBarUI.cs))**: Menambahkan parameter `hideAtFullHealth` (default `false`) pada `Bind()` dan `EvaluateVisibility()` sehingga monster aktif saat Night Brawl langsung menampilkan bar darahnya di atas kepala tanpa harus menunggu menerima damage terlebih dahulu.
  - **Automated Test Suite Expansion ([`GameplayAndHUDTests.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Editor/GameplayAndHUDTests.cs))**: Menambahkan **Test 23** (`Test_EnemyAndBossHealthBar_CanvasResolutionAndVisibilityAudit`) yang mengaudit Canvas resolution, auto-reparenting resilience dari panel ber-alpha 0, visibilitas bar darah, dan resolusi tooltip buff (23/23 audit tests lulus 100%).

#### 2. Night Brawl Wave Spawn Leak Prevention, Boss Minion Decoupling, and Climax Boss Order `[Rafi]`
- **Akar Masalah (Root Cause Diagnostic)**:
  - **Monster Kelewat Spawn Setelah Wave Selesai**: `NightBrawlManager.SpawnNextWave()` menjalankan `spawnCoroutine = StartCoroutine(RoutineSpawnWaveStaggered(toSpawn))` dengan jeda spawn 15–20 detik. Saat monster di layar dihabisi pemain sebelum seluruh antrean monster selesai di-spawn, `HandleWaveCompleted()` memulai hitung mundur jeda antar-wave (`waveIntermissionDelay = 30s`) tanpa menghentikan `spawnCoroutine`. Akibatnya, monster wave lama terus bermunculan di tengah masa jeda (intermission).
  - **Kematian Minion Merusak Progres Wave (Wave Meter Corruption)**: Saat bos memanggil minion (`BossSummonAbility`), minion didaftarkan via `RegisterDynamicEnemy()`, dan saat minion mati `HandleEnemyDied()` langsung memanggil `waveEngine.RecordEnemyDefeated()`. Hal ini mengurangi kuota `_activeEnemiesRemaining` dan menambah `_totalDefeatedEnemies` terhadap jadwal tetap wave, memicu penyelesaian wave prematur dan bar progres mencapai 100% saat bos atau monster utama masih hidup.
  - **Urutan Spawn Bos di Awal/Tengah Wave**: Di `WaveProgressionEngine.GetEnemiesScheduleForDayAndWave()`, bos di wave klimaks (Day 3 W3 CyclopsTuberMaw, Day 4 W4 TaroColossus, Day 5 W5 TheRanger) tercantum di awal daftar (`index 0`), sehingga bos langsung muncul pertama kali sebelum anak buahnya.
- **Implementasi Solusi Arsitektural**:
  - **Siklus Hidup Coroutine & Penghentian Instan ([`NightBrawlManager.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Combat/NightBrawlManager.cs))**:
    - Menambahkan pelacak status `isSpawningWave` dan helper `StopSpawnCoroutine()`.
    - Mengintegrasikan `StopSpawnCoroutine()` ke dalam `HandleWaveCompleted()`, `HandleAllWavesCleared()`, `EndNightBrawl()`, `RestoreNightBrawlState()`, dan `OnDisable()`.
    - Memastikan pembersihan coroutine dan penghentian spawn seketika saat wave berakhir atau malam diselesaikan.
  - **Pemisahan Minion dari Kuota Wave ([`EnemyBase.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Combat/Enemy/EnemyBase.cs), [`NightBrawlManager.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Combat/NightBrawlManager.cs), [`BossSummonAbility.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Combat/Enemy/BossAbilities/BossSummonAbility.cs), [`WaveProgressionEngine.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Combat/Wave/WaveProgressionEngine.cs))**:
    - Menambahkan flag `isSummonedMinion` pada `EnemyBase` (di-reset di `ResetEnemyState()` pada pool).
    - Menandai minion bos sebagai `isSummonedMinion = true` pada saat spawn dan pendaftaran dynamic enemy.
    - Mengisolasi penghitungan `waveEngine.RecordEnemyDefeated()` hanya untuk musuh reguler berjadwal (`!enemy.isSummonedMinion`), menjaga keakuratan absolut progress bar dan kuota wave.
    - Menambahkan penghentian pemanggilan minion saat bos mati atau malam usai, serta mengeliminasi minion yang tersisa saat bos dikalahkan (`DismissAllMinions()`).
  - **Urutan Klimaks Bos ([`WaveProgressionEngine.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Combat/Wave/WaveProgressionEngine.cs))**:
    - Menyusun ulang daftar jadwal monster di Day 3 W3, Day 4 W4, dan Day 5 W5 sehingga seluruh monster reguler muncul lebih dulu dan bos berada di urutan paling akhir.
    - Menerapkan programmatic sorting pada `PrepareNextWave()` yang menjamin tipe bos selalu diurutkan ke akhir daftar (`Sort((a,b) => IsBossType(a) ? 1 : -1)`).
  - **Automated Test Suite Expansion ([`GameplayAndHUDTests.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Editor/GameplayAndHUDTests.cs))**:
    - Menambahkan **Test 24** (`Test_NightBrawl_WaveSpawnLeakPrevention_MinionDecoupling_AndBossOrderAudit`) yang memvalidasi urutan bos di akhir wave, pemisahan kuota minion, pencegahan spawn leak pada wave completion, dan siklus hidup coroutine (24/24 audit tests lulus 100%).

#### 3. Multi-Stack Seed Consumption Priority & In-Hand Hotbar Allocation Fix `[Rafi]`
- **Akar Masalah (Root Cause Diagnostic)**:
  - Ketika pemain memiliki lebih dari satu stack bibit di inventori (satu stack di hotbar slot 0–3 dan satu stack lainnya di tas utama slot 4–15), penanaman bibit melalui `FarmlandTile` memanggil `InventoryComponent.RemoveItem(seed, 1)`.
  - Secara default, implementasi `RemoveItem()` mengiterasi slot dari belakang (`for int i = slots.Count - 1 down to 0`) agar susunan inventori depan tetap rapi. Akibatnya, stack yang pertama kali ditemukan dan dikurangi adalah stack di inventori utama (slot 4–15), sementara stack bibit yang sedang dipegang di tangan pemain pada hotbar (slot 0–3) tidak pernah berkurang.
  - Selain itu, `FarmlandTile.RoutinePlantSeedToTile()` sebelumnya tidak meneruskan indeks hotbar aktif ke coroutine dan mengabaikan nilai return boolean `RemoveItem()`, yang berpotensi memicu bibit tertanam tanpa konsumsi jika terjadi defisit inventori.
- **Implementasi Solusi Arsitektural**:
  - **Dukungan `preferredSlotIndex` & Auto-Active Hotbar Resolution ([`InventoryComponent.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Inventory/InventoryComponent.cs))**:
    - Memperbarui tanda tangan `public bool RemoveItem(ItemData item, int amount, int preferredSlotIndex = -1)`.
    - Menambahkan resolusi otomatis: jika `preferredSlotIndex < 0`, inventori bertipe player (`hasHotbar == true`), dan `selectedHotbarIndex` memegang item target yang sama, sistem otomatis menetapkan `resolvedPreferredIndex = selectedHotbarIndex` sehingga item di tangan pemain diprioritaskan untuk dikonsumsi terlebih dahulu hingga habis.
    - Sisa kebutuhan (`remaining > 0`) setelah slot prioritas habis tetap dikurangkan dari slot belakang inventori utama (`slots.Count - 1` ke 0, melewati `resolvedPreferredIndex`).
    - Menambahkan helper direct-slot `public bool RemoveItemFromSlot(int slotIndex, int amount)`.
  - **Penerusan Indeks Hotbar & Validasi Konsumsi Bibit ([`FarmlandTile.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Farming/FarmlandTile.cs) & [`PlayerFarmInteraction.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Farming/Adapters/PlayerFarmInteraction.cs))**:
    - `FarmlandTile.TryPlantSeed()` meneruskan `hotbarIdx` ke `RoutinePlantSeedToTile()`.
    - `RoutinePlantSeedToTile()` memanggil `inventory.RemoveItem(seed, 1, preferredSlotIndex: hotbarIdx)` dan memvalidasi `if (success)` sebelum mengubah status tanah menjadi `PlantedDry`.
    - `PlayerFarmInteraction.ExecutePlant()` meneruskan `preferredSlotIndex: PlayerInventory.selectedHotbarIndex`.
  - **Automated Test Suite Expansion ([`GameplayAndHUDTests.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Editor/GameplayAndHUDTests.cs))**:
    - Menambahkan **Test 25** (`Test_PlayerInventory_MultiStack_PlantSeedHotbarConsumptionPriorityAudit`) yang mengaudit auto-resolusi hotbar aktif, deduksi prioritas eksplisit, penipisan stack hotbar hingga kosong, fallback otomatis ke tas saat hotbar habis, dan deduksi direct-slot (25/25 audit tests lulus 100%).

#### 4. Monster Combat AI Attack Consistency, Distance Token Priority & Cooldown Decoupling `[Rafi]`
- **Akar Masalah (Root Cause Diagnostic)**:
  1. *Serangan Jarak Jauh Siluman*: `TheRanger` (Boss stationary turret) berstatus `isRanged = true`, namun pada `EnemyBase.TryPerformAttack()` dialihkan ke `else { StartCoroutine(RoutineMeleeTelegraphedAttack()); }` karena hanya `CornMusketeer` yang memiliki percabangan ranged. Dengan `attackRange = 6.0m`, telegraf dan hit check (`currentDist <= attackRange + 0.8f`) mengeksekusi telegraf melee raksasa seluas 6.8 meter yang mengenai pemain dari seberang arena. Selain itu, jangkauan serang melee boss/mob terinflasi (`CyclopsTuberMaw` 3.6m + 0.8m = 4.4m, `TaroColossus` 3.0m + 0.8m = 3.8m), dan skill khusus dieksekusi murni berdasarkan timer tanpa validasi jarak.
  2. *Monster Dekat Membeku/Berputar Tanpa Menyerang*: Pada `EnemyBase.UpdateAI()`, monster dalam jarak hingga **7.0 meter** mengambil token melee selama **3.5 detik**. Dengan kuota 3 token melee, 3 monster lambat di kejauhan memborong seluruh token. Saat monster lain tiba di jarak 0.8m–1.2m tepat di depan pemain, `TryAcquireToken()` mengembalikan `false` dan `MonsterTacticalEvaluator` memaksanya masuk ke `FlankOrbit`, berputar-putar tanpa menyerang selama 3.5 detik. Selain itu, `TheRanger` mengambil token ranged tetapi merelease token melee (`ReleaseToken(id, false)`), menyebabkan kebocoran token ranged.
  3. *Serangan Beruntun Tanpa Cooldown*: Timestamp `lastAttackTime = Time.time;` dicatat pada *awal* `TryPerformAttack()`. Animasi serang berdurasi 0.53s–0.8s menghabiskan sebagian besar interval cooldown saat animasi masih berjalan. Ditambah lagi, tidak ada sinkronisasi antara basic attack dan special skill; setelah skill berdurasi 2 detik selesai, `Time.time - lastAttackTime` sudah melampaui cooldown basic attack sehingga basic attack langsung ditembakkan di frame pertama (dan sebaliknya).
- **Implementasi Solusi Arsitektural**:
  - **Distance-Based Token Preemption ([`AttackTokenDispatcher.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Combat/Core/PureLogic/AttackTokenDispatcher.cs))**:
    - Menambahkan array primitif Zero-GC `_meleeDistances`, `_rangedDistances`, `_meleeCommitted`, `_rangedCommitted`.
    - `TryAcquireToken` kini mendukung parameter `float distanceToTarget` dan `bool isCommitted`.
    - Aturan Preemption Berbasis Jarak: Combatant yang berada dalam jangkauan serang langsung (`distanceToTarget <= 2.2f`) dapat mem-preempt token anticipatory yang dipegang oleh unit berjarak jauh (`distance >= 2.8f`) asalkan unit jauh tersebut belum committed dalam ayunan serang aktif (`!committed[i]`).
    - Menambahkan method query `IsCommitted(entityId, isRanged)`.
  - **Attack Routing & Sanitasi Jangkauan Serang ([`EnemyBase.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Combat/Enemy/EnemyBase.cs))**:
    - Menambahkan coroutine proyektil artileri berat khusus `RoutineTheRangerAttack()` dengan telegraf laser merah (0.45s) dan pelepasan token `ReleaseToken(id, isRanged: true)`.
    - Menyelaraskan jangkauan serang visual melee: `CyclopsTuberMaw` (2.4m), `TaroColossus` (2.4m), `TaroBrute` (1.8m), `TuberMaw` (1.4m), dan `TheRanger` (10m turret). Padding hitbox telegraf melee dikurangi dari `+0.8f` menjadi `+0.35f`, dan deteksi belakang disaring (`dot >= 0.20f || currentDist <= 1.0f`).
    - Menambahkan gating jarak efektif pada seluruh eksekusi skill khusus (`TuberMaw` Burrow: 2.5m–10m, `CyclopsTuberMaw` Beam: <=14m, `TaroBrute` RootGuard: <=5m, `TaroColossus` Slam: <=7m, `CornMusketeer` Kernel: 2.5m–13m, `TheRanger` HeavensFall: <=16m).
    - Mempersempit threshold akuisisi token anticipatory dari 7.0m ke `Mathf.Max(2.2f, attackRange + 1.2f)` dan durasi dari 3.5s ke 1.6s.
  - **Cooldown Stamping & Global Recovery Buffer ([`EnemyBase.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Combat/Enemy/EnemyBase.cs))**:
    - Timestamp `lastAttackTime = Time.time;` kini dicatat tepat saat coroutine serangan dasar *selesai* (fase recovery), menjamin monster menunggu interval cooldown penuh sebelum serangan berikutnya.
    - Setiap kali serangan dasar selesai, timer skill diberikan buffer jeda minimal 1.2 detik (`skillCooldownTimer = Mathf.Min(skillCooldownTimer, GetSkillCooldown() - 1.2f)`).
    - Setiap kali special skill selesai (`RoutineRetreatAndHeal`, `RoutineBurrowStrike`, `RoutineTrackingBeam`, `RoutineRootGuard`, `RoutineAirborneSlam`, `RoutineKernelShot`, `RoutineCornHeavensFall`), sistem menetapkan `lastAttackTime = Time.time + 0.5f ~ 0.8f;` untuk mengeliminasi serangan basic attack instan pada frame pertama.
  - **Automated Test Suite Expansion ([`GameplayAndHUDTests.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Editor/GameplayAndHUDTests.cs))**:
    - Menambahkan **Test 26** (`Test_MonsterAttackConsistency_DistanceTokenPriority_TheRangerAndCooldownDecouplingAudit`) yang mengaudit preemption token berbasis jarak, proteksi unit committed mid-swing, siklus token ranged The Ranger tanpa kebocoran, dan sanitasi nilai attackRange seluruh monster (26/26 audit tests lulus 100%).

#### 5. Standalone Windows Production Build & Dual-Layer Material/Shader Hardening `[Rafi]`
- **Akar Masalah (Root Cause Diagnostic)**:
  1. *Default Render Pipeline Kosong (`GraphicsSettings.defaultRenderPipeline == null`)*: Pada project settings sebelumnya, `QualitySettings` menunjuk ke `PC_RPAsset`, tetapi `GraphicsSettings.defaultRenderPipeline` berstatus `null`. Pada standalone player build, inisialisasi awal Unity sebelum `QualitySettings` aktif menyebabkan fallback ke Built-in Render Pipeline, merender seluruh material URP menjadi pink/magenta 100%.
  2. *Shader Stripping pada Runtime Dynamic Shaders*: Seluruh shader yang dipanggil melalui `Shader.Find(...)` (`Universal Render Pipeline/Lit`, `Universal Render Pipeline/Unlit`, `Universal Render Pipeline/Particles/Unlit`, `UI/Grayscale`, `FarmBeware/Combat/CombatTelegraph`, `FarmBeware/Monster/MonsterFresnelLit`, dll.) tidak terdaftar dalam `Always Included Shaders`. Saat proses player compilation, shader stripping menghapus varian shader tersebut sehingga mengembalikan `null` di runtime standalone.
  3. *Unbundled Soil Materials*: `FarmGridVisualizer.ResolveDefaultMaterials()` memanggil `Resources.Load<Material>("Materials/Farming/Mat_Soil_Untilled")`, namun folder `Assets/Resources/Materials/Farming/` belum ada (aset hanya berada di `Assets/Art/Materials/Farming/`).
  4. *Null Material Slots pada Prefab Enemies*: Pada `Assets/Prefabs/Enemies/`, terdapat 6 prefab duplikat dengan slot material 0 berstatus `null`.
- **Implementasi Solusi Arsitektural**:
  - **URP Pipeline Hardening (`ProjectSettings/GraphicsSettings.asset`)**: Menetapkan `GraphicsSettings.defaultRenderPipeline` secara eksplisit ke `PC_RPAsset` (`Assets/Settings/PC_RPAsset.asset`), mengeliminasi kemungkinan fallback ke Built-in pipeline.
  - **Always Included Shaders Whitelist**: Menambahkan 9 shader runtime penting ke dalam array `m_AlwaysIncludedShaders` (total 16 shader terproteksi) untuk menjamin Zero-Shader-Stripping pada kompilasi rilis.
  - **Resources Material Bundling (`Assets/Resources/Materials/Farming/`)**: Menyalin 8 material farming tanah dan tanaman (`Mat_Soil_Untilled`, `Mat_Soil_Tilled_Dry`, `Mat_Soil_Tilled_Wet`, dan leaf/stem/root materials) ke dalam Resources sehingga `Resources.Load<Material>` ter-resolve secara instan di runtime.
  - **Prefab Material Synchronization (`Assets/Prefabs/Enemies/`)**: Mensinkronisasi seluruh 6 enemy & boss prefab ke material resminya (`M_Enemy_TuberMaw`, `M_Boss_CyclopsTuberMaw`, `M_Enemy_TaroBrute`, `M_Boss_TaroColossus`, `M_Enemy_CornMusketeer`, `M_Boss_TheRanger`).
  - **Yield Fallback Sanitization ([`PlayerFarmInteraction.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Farming/Adapters/PlayerFarmInteraction.cs))**: Mengganti dead string fallback `Resources.Load<ItemData>("Data/Items/Crops/...")` dengan query kanonikal ke `ItemDatabase.Instance.GetItem("crop_sweetpotato_dirty")`.
  - **Build Execution & Smoke Test Validation**:
    - Build target: `StandaloneWindows64`, Subtarget: `Player`, Scripting Backend: `mono`, Kompresi: `compress_lz4`, Mode: `strict_mode`.
    - Executable terkompilasi: `Builds/Windows/Farm-Beware.exe` (667 KB), `UnityPlayer.dll` (36.5 MB), `Farm-Beware_Data/data.unity3d` (71.3 MB).
    - Menjalankan smoke test otomatis: Executable berhasil berjalan di background tanpa crash, Direct3D 11 rendering aktif, PhysX dan Input System terinisialisasi bersih (exit code 0).

#### 6. Plant Waterer Visual Mount & Melee AI Reachability Deadlock Elimination (27/27 Audit Tests Passing) `[Rafi]`
- **Akar Masalah (Root Cause Diagnostic)**:
  1. *Visual Model Kaleng Siram Hilang*: ScriptableObject `Tool_PlantWaterer.asset` memiliki serialized property `equipPrefab == null`, menyebabkan karakter memegang tangan kosong tanpa alat saat memilih watering can di hotbar.
  2. *Deadlock Agresi Melee (Circling Tanpa Serangan)*: Pada `MonsterTacticalEvaluator`, unit melee tanpa attack token dipaksa melakukan `FlankOrbit` di dalam radius `OrbitMaxRadius = 4.8m`. Namun pada `EnemyBase.UpdateAI()`, akuisisi token dibatasi oleh `distToPlayer <= approachThreshold` (2.6m–3.0m). Karena $4.8\text{m} > 3.0\text{m}$, monster yang mendekati pemain terjebak di orbit ring pada jarak 3.2m–4.8m dan *tidak pernah* bisa memenuhi syarat jarak untuk meminta token, memicu perputaran melingkar abadi tanpa serangan.
  3. *Serangan Meleset (Whiffing) Terhadap Target Diam*: Pada `RoutineMeleeTelegraphedAttack()`, monster menggunakan slerp rotasi lambat (`Time.deltaTime * 8f`) sehingga saat serangan diluncurkan, orientasi monster masih menyamping/tangensial dari posisi pemain. Dot product `Vector3.Dot(forward, dirToPlayer)` sering kali bernilai $< 0.20$, memicu deteksi meleset (*whiff*) bahkan saat pemain berdiri diam. Pada `RoutineBurrowStrike()`, erupsi bawah tanah meledak dari jarak 3 meter dengan radius sempit 1.6m.
- **Implementasi Solusi Arsitektural**:
  - **Pembuatan Prefab & Visual Mounting Plant Waterer ([`PlantWaterer.prefab`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Prefabs/Weapons/PlantWaterer.prefab) & [`Tool_PlantWaterer.asset`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Data/Items/Equipment/Tool_PlantWaterer.asset))**:
    - Membangun prefab 3D resmi `PlantWaterer.prefab` lengkap dengan komponen badan silinder (`CanBody`), corong (`CanSpout`), pegangan (`CanHandle`), material URP `Mat_Metal_Dark` ber-shader `Universal Render Pipeline/Lit` (Zero-Error/Pink-Shader), `BoxCollider` trigger, `WeaponHitbox` (range 1.5), kinematic `Rigidbody`, dan `WeaponTrailController`.
    - Menautkan GUID `f25e84560fcf3a644a476506d5820c3c` secara langsung ke properti `equipPrefab` di `Tool_PlantWaterer.asset`.
  - **Eliminasi Deadlock Flank Orbit pada Pure Logic ([`MonsterTacticalEvaluator.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Combat/Core/PureLogic/MonsterTacticalEvaluator.cs))**:
    - Menambahkan overload 9-parameter `EvaluateMovement(..., bool isAttackReady, out MonsterTacticalState calculatedState)`.
    - Jika `hasAttackToken || isAttackReady`: monster langsung keluar dari orbit dan bertransisi ke `MonsterTacticalState.EngageDirect` lurus menuju pemain untuk mengambil token dan melancarkan tebasan.
    - Menambahkan konfigurasi dinamis orbit radius pada `CalculateOrbitDirection(..., float minRadius, float maxRadius)`.
  - **Penataan Jarak & Presisi Serangan Musuh ([`EnemyBase.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Combat/Enemy/EnemyBase.cs))**:
    - Menghitung `isAttackReady` secara real-time berdasarkan sisa interval cooldown dan meneruskannya ke evaluator taktis.
    - Menyelaraskan `approachThreshold = Mathf.Max(2.4f, attackRange + 1.4f)` dan mengonfigurasi `tacticalEvaluator.OrbitMinRadius` serta `OrbitMaxRadius` secara dinamis sesuai `attackRange`.
    - Pada `RoutineMeleeTelegraphedAttack()`: melakukan direct rotation snap (`transform.rotation = Quaternion.LookRotation(toTarget)`) pada klimaks fase windup sebelum evaluasi damage, menyelaraskan radius telegraf tanah (`telegraphHitRange = attackRange + 0.55f`), dan memperluas kondisi hit menjadi `(dot >= 0.10f || currentDist <= 1.2f)`.
    - Pada `RoutineBurrowStrike()`: fase subterranean kini mendekati pemain hingga `diff.magnitude <= 1.2f` (maksimal 1.2s) dan memperluas radius erupsi tanah menjadi `2.4m`, mengeliminasi telegraf palsu yang meledak dari kejauhan.
  - **Automated Test Suite Expansion (27/27 Passing) ([`GameplayAndHUDTests.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Editor/GameplayAndHUDTests.cs))**:
    - Menambahkan **Test 27** (`Test_PlantWatererVisualMount_AndMeleeCombatReachabilityAudit`) yang mengaudit integritas prefab `Tool_PlantWaterer`, eliminasi deadlock orbit saat `isAttackReady = true`, pendekatan ke dynamic orbit ring, akuisisi token anticipatory, dan registrasi serangan presisi terhadap target stasioner.
    - Seluruh 27 tes audit otomatis lulus 100% (27/27 Passed, 0 Errors).

---

## - 2026-10-17

### Ditambahkan & Diintegrasikan (Added & Integrated)

#### 1. Standalone Player Build Hardening & Zero-Missing-Shader Architecture `[Rafi]`
- **Pre-Compiled Resources Material Bundling ([`Mat_CombatTelegraph.mat`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Resources/Materials/Combat/Mat_CombatTelegraph.mat) & [`CombatTelegraphDecal.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Combat/Enemy/CombatTelegraphDecal.cs))**:
  - Pada executable rilis Unity Standalone Player, shader prosedural yang dimuat via `Shader.Find()` berisiko di-strip oleh compiler jika tidak terdaftar pada *Always Included Shaders*. Hal ini memicu rendering kotak magenta (*magenta box*) pada proyektor telegraf serangan musuh.
  - Membangun aset material pre-compiled permanen di `Assets/Resources/Materials/Combat/Mat_CombatTelegraph.mat` (URP Decal/Unlit transparent).
  - Menyusun arsitektur fallback bertingkat pada `CombatTelegraphDecal.EnsureResourcesMaterial()` yang menjamin 100% ketersediaan material di semua target platform (`Resources.Load` -> `URP/Decal` -> `URP/Unlit` -> Standard Alpha Transparent).

#### 2. Precision Sub-Floor Collision & Teleport Elevation Safety Guard `[Rafi]`
- **Penataan Kolisi Lantai & Elevasi Teleportasi ([`DoorInteractable.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Kitchen/DoorInteractable.cs) & [`StagingScene.unity`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scenes/StagingScene.unity))**:
  - Mengatasi kendala fisika di mana pemain terperosok setengah badan ke lantai saat memasuki pintu rumah.
  - Memposisikan `House_UnifiedFloorCollider` ke $Y = -0.50$ dengan dimensi box $(X=9.8, Y=1.0, Z=9.8)$ sehingga permukaan atas lantai solid tepat bersinggungan di $Y = 0.00$ tanpa menciptakan tumpang tindih (*interpenetration*) dengan kapsul `CharacterController` / `Rigidbody`.
  - Menambahkan offset vertikal defensif $+0.04\text{m}$ pada target koordinat pendaratan pintu (`targetSpawnPoint.position + Vector3.up * 0.04f`), menjamin raycast grounder pemain langsung mendeteksi permukaan lantai tanpa tertanam di dalam collider.

#### 3. Dynamic Combat Telegraph & Burrow Strike Anticipation Window `[Rafi]`
- **Jendela Antisipasi Serangan Bawah Tanah ([`EnemyBase.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Combat/Enemy/EnemyBase.cs))**:
  - Memperbaiki mekanika musuh subterranean (TuberMaw): saat keluar dari tanah (`isBurrowed = false`), sistem kini memberikan jendela telegraf aktif berdurasi **0.55 detik**.
  - Menampilkan proyektor lingkaran telegraf merah di tanah dan teks indikator melayang `⚠️ Erupting!` sebelum damage ledakan area diaplikasikan, memberikan kesempatan taktis bagi pemain untuk melakukan aksi *dodge* atau *parry*.

#### 4. Main Menu Flow Streamlining & Persistent Data Sanitization `[Rafi]`
- **Alur Peluncuran New Game Langsung ([`MainMenuController.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/MainMenu/MainMenuController.cs))**:
  - Memisahkan logika tombol New Game dari SaveSystemUI: menekan New Game kini langsung mengaktifkan transisi awan organik ([`CloudTransitionManager.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Common/CloudTransitionManager.cs)) dan memuat `StagingScene` secara mulus tanpa membuka pop-up menu slot penyimpanan.
  - Membersihkan file simpanan pengujian (*testing save data*) dari `Application.persistentDataPath` untuk mencegah restorasi data debugging otomatis pada sesi permainan baru.

#### 5. Penyelarasan Nerf & Buff Musuh dengan MVP Guideline V0.1 `[Rafi]`
- **Penyelarasan Stat Roster Musuh ([`EnemyData.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Combat/Data/EnemyData.cs))**:
  - Menyelaraskan seluruh ScriptableObject data musuh sesuai dokumen MVP Version Guideline V0.1:
    - **The Ranger** ([`EnemyData_TheRanger.asset`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Data/Enemies/EnemyData_TheRanger.asset)): Max HP 250, Armor 25, Move Speed 0.0 (artillery siege turret), Attack Damage 20, Attack Interval 1.5s, Poise 60.
    - **Taro Colossus** ([`EnemyData_TaroColossus.asset`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Data/Enemies/EnemyData_TaroColossus.asset)): Max HP 900, Armor 55, Move Speed 2.2, Poise 180, Attack Damage 45, Attack Interval 3.0s.
    - **Corn Musketeer** ([`EnemyData_CornMusketeer.asset`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Data/Enemies/EnemyData_CornMusketeer.asset)): Max HP 85, Armor 10, Move Speed 3.8, Attack Damage 12, Attack Interval 2.2s.
    - **TuberMaw** ([`EnemyData_TuberMaw.asset`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Data/Enemies/EnemyData_TuberMaw.asset)): Max HP 120, Armor 15, Move Speed 3.2, Poise 40.
    - **PumpkinBrute** ([`EnemyData_PumpkinBrute.asset`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Data/Enemies/EnemyData_PumpkinBrute.asset)): Max HP 350, Armor 30, Move Speed 2.5, Attack Damage 35.
- **Konsolidasi Hirarki Resep Memasak ([`Assets/Data/Recipes/Cooking/`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Data/Recipes/Cooking/))**:
  - Memusatkan seluruh resep makanan ke folder kanonikal `Assets/Data/Recipes/Cooking/` (`Recipe_CornPorridge.asset` dan `Recipe_TaroCornStew.asset`) serta menyingkirkan duplikasi lama di `Assets/Data/Kitchen/Recipes/`.

#### 6. Animated Corner Leaves & Rice Decoration on Main Menu `[Bhaskoro]`
- **Dekorasi Sudut Tombol Menu Utama ([`PadiDaunAnimator.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/MainMenu/PadiDaunAnimator.cs) & [`UIAnimationHandler.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/MainMenu/UIAnimationHandler.cs))**:
  - Mengintegrasikan artwork tombol menu baru beresolusi tinggi beserta ornamen tanaman padi dan daun beranimasi prosedural di sudut tombol menu utama (`bas-branch`, commit `dec870e`).
  - Animasi menggunakan interpolasi sinusoidal dinamis pada sumbu rotasi Z dan skala pernapasan (*breathing scale*), memberikan responsivitas visual estetis saat disorot kursor mouse.

#### 7. Standalone Production Build Verification & 19/19 Automated Test Suite `[Rafi]`
- **Ekspansi Test Suite ([`GameplayAndHUDTests.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Editor/GameplayAndHUDTests.cs))**:
  - Menambahkan **Test 19**: `Test_BuildBugfixes_IntegrityAudit` yang memvalidasi integritas material Resources telegraph, elevasi pendaratan pintu rumah, dan jendela waktu erupsi burrow.
  - Seluruh 19 pengujian audit sistem lolos (19/19 Passed).
- **Hasil Rilis Standalone Build Windows 64-bit**:
  - Lokasi target: `Builds/StandaloneWindows64/Farm-Beware.exe`.
  - Durasi kompilasi: 655 detik, Ukuran total: 312.74 MB, 0 error kompilasi, 0 warning shader.

#### 8. Automated Food Buff Purge on Sleep & Day Transition (20/20 Audit Tests Passing) `[Rafi]`
- **Siklus Pembersihan Food Buff Otomatis ([`PlayerBuffManager.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Player/PlayerBuffManager.cs), [`BedInteractable.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Interaction/BedInteractable.cs), [`PlayerRespawnController.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Player/PlayerRespawnController.cs))**:
  - Menyelesaikan integrasi siklus tidur pemain: seluruh buff makanan (yang dikonsumsi sore hari untuk Night Brawl) secara otomatis dibersihkan saat pemain tidur di tempat tidur atau mengalami respawn kekalahan di pagi hari.
  - Mengimplementasikan `ClearAllBuffs()` pada `PlayerBuffManager` dengan reset modifier status pemain (`ApplyBuffModifiers(0, 0, 0)`) dan pemicuan event `OnBuffsChanged` untuk mengosongkan antarmuka HUD.
  - Mengintegrasikan penanganan decoupled via `interactor.SendMessage("ClearAllBuffs")` pada `BedInteractable` guna menghormati batasan isolasi assembly (`FarmBeware.Features.Interaction.asmdef`), serta integrasi langsung pada `PlayerRespawnController` dan `OnDayChanged`.
- **Ekspansi Test Suite (20/20 Passing) ([`GameplayAndHUDTests.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Editor/GameplayAndHUDTests.cs))**:
  - Menambahkan **Test 20**: `Test_PlayerBuff_ClearsOnSleepAndDayTransition` yang memvalidasi bahwa seluruh buff aktif terhapus saat tidur, status pemain kembali ke baseline, dan event HUD terpanggil. Seluruh 20 audit tests lolos (20/20 Passed).

#### 9. Stylized Day-to-Night & Night-to-Day Transition Banner System (21/21 Audit Tests Passing) `[Rafi]`
- **Arsitektur Banner Animasi Transisi Waktu ([`DayTransitionUI.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Time/UI/DayTransitionUI.cs))**:
  - Mengembangkan sistem transisi visual waktu procedural glassmorphic berkecepatan tinggi tanpa memblokir pergerakan atau kontrol pemain.
  - Membangun antarmuka secara mandiri (*auto-procedural UI hierarchy*):
    - `Canvas` ber-layer `ScreenSpaceOverlay` dengan `sortingOrder = 92` dan `CanvasScaler` adaptif (referensi $1920 \times 1080$, match height $1.0$).
    - `CanvasGroup` beranimasi transparansi alpha dan scale interpolator.
    - Glassmorphic banner horizontal container dengan aksen garis berpedar (*glowing accent line*), typography TextMeshProUGUI bergradasi, dan ikon status.
    - Soft vignette overlay tepi layar yang berdenyut lembut selama transisi berlangsung.
  - **Dua Tema Palet Visual & Tipografi**:
    - **Day ➔ Night**: Banner `🌙 NIGHTFALL` berlatar gelap pekat (`#14070A`), border merah menyala (`#E74C3C`), teks aksen crimson, dan subtitle `The shadows stir. Prepare your defenses.`
    - **Night ➔ Day**: Banner `☀️ DAY X` berlatar hangat keemasan (`#1C1608`), border emas berpedar (`#F1C40F`), teks aksen kuning fajar, dan subtitle `The morning sun rises. Tend to your crops.`
  - **Sintesis Audio Prosedural Mandiri (Zero External Asset Dependency)**:
    - Menghasilkan audio chime secara prosedural via `AudioClip.Create`:
      - *Nightfall*: Lonceng gong resonan 220Hz dengan peluruhan harmonik dan sub-osilasi misterius.
      - *Daybreak*: Melodi akor pagi tiga nada cerah (C5 $523.25\text{Hz}$, E5 $659.25\text{Hz}$, G5 $783.99\text{Hz}$) dengan amplop eksponensial halus.
  - **Koreografi Animasi Non-Blocking**:
    - Masuk dengan kurva pegas (*spring curve*: $0.85 \rightarrow 1.02 \rightarrow 1.0$) selama 0.35 detik, mengapung selama 1.8 detik, lalu memudar halus selama 0.5 detik berbasis `Time.unscaledDeltaTime`.
    - Terhubung secara aman ke event `TimeManager.Instance.OnPhaseChanged`, `OnDayChanged`, dan `DayNightTimeManager.Instance.OnTimePhaseChanged` dengan *duplicate-trigger guard* ($< 1.0\text{s}$).
- **Ekspansi Test Suite (21/21 Passing) ([`GameplayAndHUDTests.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Editor/GameplayAndHUDTests.cs))**:
  - Menambahkan **Test 21**: `Test_DayTransitionUI_DayAndNightBannerAnimation` yang memvalidasi pembentukan hierarki otomatis, palet warna dan teks Day/Night, serta pembatalan transisi seketika saat loading save file.
  - Seluruh 21 uji audit kelayakan sistem lolos 100% (21/21 Passed).

#### 10. Multi-Source Input Lock Architecture & Viewport Raycast Leak Prevention (22/22 Audit Tests Passing) `[Rafi]`
- **Arsitektur Multi-Source Input Lock ([`IPlayerContext.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Core/Contracts/IPlayerContext.cs) & [`PlayerControl.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Player/PlayerControl.cs))**:
  - Mengembangkan sistem pelacakan multi-source input lock berbasis koleksi `HashSet<string> _inputLockSources` di dalam `PlayerControl`.
  - Memperluas antarmuka `IPlayerContext` dengan method kontraktual:
    - `AddInputLock(string source)`: Menambahkan token pengunci unik (misal: `"DayTransition"`, `"CloudTransition"`, `"BedSleep"`, `"Respawn"`, `"Stun"`).
    - `RemoveInputLock(string source)`: Melepaskan token pengunci spesifik tanpa membocorkan atau membatalkan kunci dari subsistem lain.
    - `ClearAllInputLocks()`: Membersihkan seluruh token kunci seketika (digunakan untuk sanitasi saat pemuatan game/save atau reset scene).
    - `HasInputLock(string source)`: Memeriksa keberadaan token pengunci tertentu secara deterministik.
  - Menyelaraskan setter properti `IPlayerContext.IsInputLocked` dan `PlayerControl.isInputLocked`: panggilan generic legacy kini memetakan ke token `"LegacyGeneric"`, sehingga pemanggilan `IsInputLocked = false` dari dialog yang ditutup (misal `DailyReportModalUI`) tidak akan membatalkan kunci transisi fajar (`"DayTransition"`) yang sedang aktif.
  - Memperkuat `PlayerControl.SetControlLock(true)`: saat penguncian aktif, sistem otomatis mereset `StopMovement()`, membatalkan serangan terisi (`isChargingAttack = false`, `attackHoldDuration = 0f`), membersihkan input buffer (`hasBufferedAttack = false`), menginterupsi sequence combo combat (`InterruptCombatSequence()`), dan memaksa animator ke pose Idle tanpa lag.
- **Penyekatan Viewport Raycast & Eliminasi Kebocoran Mouse Click ([`DayTransitionUI.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Time/UI/DayTransitionUI.cs) & [`InventorySlotUI.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Inventory/UI/InventorySlotUI.cs))**:
  - Mengaktifkan raycast blocking 100% viewport pada `DayTransitionUI`: selama animasi banner transisi aktif, `vignetteOverlay.raycastTarget = true` dan `transitionCanvasGroup.blocksRaycasts = true` menjamin tidak ada klik mouse yang dapat tembus ke elemen UI di bawahnya maupun objek dunia (*world objects*).
  - Menambahkan guard clause pada `InventorySlotUI.OnPointerClick()`: klik mouse pada slot hotbar (0..3) atau double-click penggunaan item otomatis diabaikan jika `IPlayerContext.IsInputLocked == true`.
- **Integrasi Cloud Screen Transition Lock ([`CloudTransitionManager.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Common/CloudTransitionManager.cs))**:
  - Mengintegrasikan penguncian input otomatis via `AddInputLock("CloudTransition")` saat awan menutup layar dan melepaskannya (`RemoveInputLock("CloudTransition")`) saat transisi selesai terungkap, mengeliminasi kemampuan pemain bergerak di balik awan.
- **Ekspansi Test Suite (22/22 Passing) ([`GameplayAndHUDTests.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Editor/GameplayAndHUDTests.cs))**:
  - Menambahkan **Test 22**: `Test_MultiSourceInputLock_AndTransitionLeakPrevention` yang memvalidasi hirarki kunci multi-source, kekebalan terhadap generic premature unlock, penyerapan raycast `DayTransitionUI`, dan pencegahan kebocoran klik hotbar.
  - Seluruh 22 uji audit kelayakan sistem lolos 100% (22/22 Passed).

#### 11. Triple Clock Display Mode & 15s Quarter-Step Rhythm Integration `[Roi / Rafi]`
- **Arsitektur Tiga Tingkat Ritme Jam Digital ([`CombatPhaseTrackerUI.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Time/UI/CombatPhaseTrackerUI.cs))**:
  - Memperluas enum `ClockDisplayMode` dengan opsi ketiga `QuarterStep15s = 2`, melengkapi mode `FullStep60s` dan `HalfStep30s`.
  - Mode 15s memperbarui tampilan jam digital setiap 15 detik waktu nyata (setara penambahan +10 menit in-game time), memberikan presisi tinggi bagi pemain yang memantau jendela interval panen dan kompor secara ketat tanpa mengubah durasi total siklus siang 18 menit (1080 detik).
  - Mengonfigurasi notifikasi floating text di atas kepala pemain dengan warna Emerald (`#58F28C` / `new Color(0.35f, 0.95f, 0.55f)`) bertuliskan `⏱️ Clock Display: 15s Real = 10m Game (Quarter Step)`.
  - Mengintegrasikan siklus 3 arah berurutan pada shortcut keyboard `T` atau `9` / `Numpad 9`: `60s -> 30s -> 15s -> 60s`.
- **Ekspansi Test Suite Validasi Siklus Tiga Mode ([`GameplayAndHUDTests.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Editor/GameplayAndHUDTests.cs))**:
  - Memperbarui pengujian **Test 7**: `Test07_ProceduralHUDDayAndClock` untuk memverifikasi transisi siklis ketiga mode tampilan jam digital secara berurutan beserta validasi toleransi interval pembaruan jam.

---

## - 2026-10-16

### Ditambahkan & Diintegrasikan (Added & Integrated)

#### 1. Arsitektur AI Taktis Monster & POCO Evaluator Berbasis Peran (Tactical AI System) `[Rafi]`
- **Pure C# POCO Evaluator ([`MonsterTacticalEvaluator.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Combat/Enemy/MonsterTacticalEvaluator.cs))**:
  - Mengembangkan evaluator taktis berbasis pure C# tanpa dependensi lifecycle engine (Zero-GC) untuk menghitung posisi offset dinamis monster di sekitar target.
  - Mengimplementasikan 3 profil perilaku monster:
    - *Melee Harasser*: Mengelilingi target secara dinamis dengan formasi menyebar (flanking angle), menjaga jarak waspada (2.5m - 4.5m) dengan kecepatan orbit acak, lalu menerjang saat memperoleh token serangan.
    - *Ranged Skirmisher*: Menjaga jarak tembak optimal (6.0m - 9.0m), aktif melakukan repositioning (kiting), dan melangkah mundur menjauhi pemain bila didekati terlalu dekat (< 4.0m).
    - *Tank / Berserker*: Melakukan pengejaran langsung tanpa manuver flank untuk mendominasi ruang tempur.
- **Integrasi Adapter Musuh ([`EnemyBase.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Combat/Enemy/EnemyBase.cs))**:
  - Menghubungkan evaluator taktis ke siklus tick `UpdateMovement` pada `EnemyBase` dengan sanitasi `NavMesh.SamplePosition` pada radius 1.5m untuk memastikan titik navigasi valid di atas surface.
  - Memperbarui transisi state tempur: monster kini bergerak lincah dan berputar mengelilingi pemain alih-alih berdiam diri kaku.

#### 2. Resolusi Deadlock Agresi Melee & Dispatcher Token Antisipatif (Attack Token Dispatcher) `[Rafi]`
- **Anticipatory Token Reservation ([`AttackTokenDispatcher.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Combat/Enemy/AttackTokenDispatcher.cs))**:
  - Mengeliminasi *deadlock pasif* di mana monster jarak dekat hanya mengitari pemain tanpa menyerang bila pemain tidak mendekat duluan.
  - Token serangan kini dialokasikan saat monster berada dalam *engagement radius* (hingga 5.0m), memicu monster untuk langsung menutup jarak (*closing the gap*) dan melancarkan tebasan begitu tiba di jarak serang (`attackRange`).
  - Menetapkan batas maksimum token bersamaan (default 2 penyerang simultan) dan durasi kedaluwarsa token (3.5 detik) guna mencegah monster memonopoli slot serangan bila terhalang rintangan.
- **Logika Eksekusi Serangan Melee ([`EnemyBase.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Combat/Enemy/EnemyBase.cs))**:
  - Saat memegang token aktif, target navigasi langsung dikunci ke posisi pemain (`target.position`), menonaktifkan manuver flanking dan memaksa pendekatan agresif.
  - Eksekusi serangan langsung memanggil `animator.SetTrigger("Attack")` dan merilis token kembali ke dispatcher setelah animasi selesai atau saat cooldown dimulai.

#### 3. NavMesh Barrier Carving, Gate Routing & Raycast Obstruction Handling `[Rafi]`
- **Obstacle Carving & Dynamic Rebaking ([`StagingScene.unity`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scenes/StagingScene.unity))**:
  - Menambahkan komponen `NavMeshObstacle` dengan `Carve = true`, `Move Threshold = 0.1`, dan `Carving Time To Stationary = 0.5s` pada prefab pagar (`Fence_Medium_01`, `Fence_Single_01`, gerbang, dan dinding luar).
  - Melakukan rebake NavMesh Surface pekarangan untuk memotong rongga fisik pagar, memaksa `NavMeshAgent` mencari jalur terbuka (gerbang dan celah jalan) alih-alih menabrak dan tersangkut di dinding pagar saat mengejar pemain.
- **Raycast Line-of-Sight & Obstruction Pathing ([`EnemyBase.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Combat/Enemy/EnemyBase.cs))**:
  - Menambahkan verifikasi `Physics.Linecast` atau raycast visibilitas dengan layer mask `Default | Obstacles`.
  - Jika garis pandang terhalang oleh pagar atau objek statis, monster memprioritaskan kalkulasi pathing global agen NavMesh menuju gerbang terdekat daripada memaksakan direct approach point.

#### 4. Ekspansi Test Suite Audit Otomatis (18/18 Passing) `[Rafi]`
- **Penambahan Test Rig ([`GameplayAndHUDTests.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Editor/GameplayAndHUDTests.cs))**:
  - `Test16_MonsterTacticalEvaluator_MeleeFlankingCalculation`: Memvalidasi kalkulasi polar coordinate offset, batas jarak orbit, dan pergeseran sudut dinamis pada `MonsterTacticalEvaluator`.
  - `Test17_MonsterTacticalEvaluator_RangedKitingBehavior`: Memverifikasi reposisi mundur (*kiting retreat*) ranged monster ketika jarak pemain berada di bawah batas minimum.
  - `Test18_AttackTokenDispatcher_AcquireAndReleaseLifecycle`: Memverifikasi siklus reservasi token antisipatif, batas kapasitas simultan, dan rilis otomatis saat kedaluwarsa.
  - Seluruh rangkaian audit sistem lolos 18 dari 18 pengujian (18/18 Passed, 0 Errors, 0 Warnings).

---

## - 2026-10-15

### Ditambahkan & Diintegrasikan (Added & Integrated)

#### 1. Redesain Stasiun Sumur Air Pekarangan: Sistem Place & Collect (Water Well Refactor) `[Rafi]`
- **Mekanik Tempatkan & Ambil Kaleng Siram ([`WaterWellInteractable.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Farming/WaterWellInteractable.cs))**:
  - Mengubah sumur luar ruangan dari pengisian instan menjadi siklus kerja dua tahap interaktif:
    - *Tahap 1 (Place Can)*: Pemain meletakkan kaleng siram (`WateringCan`) di bibir sumur. Kaleng otomatis terisi selama durasi progres (default 3.0 detik, terakselerasi di mode 8x).
    - *Tahap 2 (Collect Can)*: Pemain mengambil kembali kaleng siram yang telah terisi penuh 100% (5/5 uses) ke dalam hotbar/inventory.
  - Menambahkan perlindungan *ownership guard*: kaleng tidak dapat diambil sebelum proses pengisian selesai secara menyeluruh.
- **Visualisasi & Animasi Posisi Ember**:
  - Memperbaiki penempatan ember dan posisi koordinat sumur di pekarangan ([`StagingScene.unity`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scenes/StagingScene.unity)) agar sejajar dengan ground mesh dan tidak melayang.

#### 2. Akselerasi Fast-Forward 8x Buff Makanan & Penguatan PlayerStats `[Rafi]`
- **Skalasi Waktu Konsumsi Makanan ([`PlayerBuffManager.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Cooking/PlayerBuffManager.cs))**:
  - Mengintegrasikan deteksi `TimeManager.Instance.IsFastForwarding` ke dalam loop pengurangan durasi buff makanan (`BuffDecayTick`), sehingga durasi buff menyusut 8x lebih cepat seirama dengan akselerasi waktu in-game.
- **Pencegahan Null-Reference & State Guard ([`PlayerStats.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Player/PlayerStats.cs))**:
  - Memperkuat null-safety saat menginisialisasi dan memperbarui buff stamina dan attack boost dari resep makanan, mencegah exception saat transisi pergantian scene atau pemuatan save data.

#### 3. Peningkatan Kapasitas Inventory 16-Slot & Navigasi Hotbar Mouse Click `[Rafi]`
- **Ekspansi Kapasitas Penyimpanan ([`InventoryManager.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Inventory/InventoryManager.cs))**:
  - Meningkatkan kapasitas penyimpanan default inventaris pemain dari 8 slot menjadi 16 slot (2 baris x 8 kolom) untuk menampung hasil panen, bahan masakan dapur, dan senjata tempur.
- **Interaksi Mouse Hotbar ([`HotbarUI.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Inventory/HotbarUI.cs))**:
  - Menambahkan dukungan klik mouse langsung pada slot hotbar di layar bagian bawah, melengkapi navigasi sebelumnya yang hanya mengandalkan keyboard numpad `1`–`8` dan scroll wheel mouse.

#### 4. Integrasi Layar Kematian (Death Screen) & Hardening Multi-Slot Save System `[Bhaskoro & Rafi]`
- **Sistem Respawn Pemain & Penalti Pagi Hari ([`DeathScreenUI.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Combat/UI/DeathScreenUI.cs), [`PlayerRespawnController.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Player/PlayerRespawnController.cs))**:
  - Mengintegrasikan branch fitur `bas-branch` ke dalam `Rafi-branch` (merge commit `224f365`).
  - Menghubungkan layar kematian saat HP pemain habis: menampilkan pesan kekalahan, memutar efek audio sedih, dan memicu respawn pemain di tempat tidur rumah pada pukul 06:00 pagi keesokan harinya dengan pemulihan HP & Stamina.
  - Mengkoordinasikan pemotongan biaya perawatan atau penalti denda harian melalui [`DailyEconomyManager.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Economy/DailyEconomyManager.cs).
- **Hardening Multi-Slot Save System ([`SaveSystemManager.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/SaveLoad/SaveSystemManager.cs))**:
  - Menambahkan validasi integritas slot penyimpanan (`slotIndex` 0–2), penanganan pembuatan direktori `Application.persistentDataPath` secara defensif, dan fallback graceful jika berkas JSON korup atau tidak ditemukan.

#### 5. Proteksi Pemetikan Tanaman & Night-Only Weapon Trail Emission `[Rafi]`
- **Pencegahan Panen Tanaman Belum Matang ([`CropPlot.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Farming/CropPlot.cs))**:
  - Memperbaiki interaksi panen agar pemain tidak dapat secara tidak sengaja memetik atau menghancurkan tanaman bibit yang belum mencapai tahap matang (*Mature*).
- **Kondisional Emisi Trail Senjata ([`PlayerCombatController.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Combat/PlayerCombatController.cs))**:
  - Mengatur efek jejak tebasan pedang (`TrailRenderer` / `MeleeTrailVFX`) agar hanya menyala aktif saat fase pertempuran malam (*Night Brawl Phase*), menjaga suasana tenang dan estetika visual saat siang hari.

---

## - 2026-10-14

### Ditambahkan & Diintegrasikan (Added & Integrated)

#### 1. Arsitektur Efek Impak Tempur & Eliminasi Delay Kematian Musuh (Combat Feel Engine) `[Rafi]`
- **Dual Hitstop Coordination ([`HitstopCoordinator.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Combat/Adapters/HitstopCoordinator.cs))**:
  - Mengembangkan sistem freeze mikro terkoordinasi yang membekukan animator penyerang (player) dan korban (monster) selama 0.06s hingga 0.10s tanpa memotong `Time.timeScale` global sistem game.
  - Mengintegrasikan impuls guncangan kamera dinamis melalui [`TraumaCameraShake.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Combat/Adapters/TraumaCameraShake.cs).
- **Procedural Blade Hit Audio & Spark VFX Engine ([`CombatHitVfxEngine.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Combat/Adapters/CombatHitVfxEngine.cs))**:
  - Menghasilkan audio sintetis dinamis benturan pedang logam dan tebasan musuh secara prosedural dengan modulasi pitch acak (0.95x - 1.15x) dan variasi harmonik zero-garbage heap allocation.
  - Sistem partikel percikan bunga api keemasan (*burst sparks*) pada titik benturan kontak ([`WeaponHitbox.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Combat/WeaponHitbox.cs)) dengan penanganan `StopEmittingAndClear` untuk mencegah runtime error durasi ParticleSystem.
- **Eliminasi Delay Kematian Musuh ([`EnemyBase.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Combat/Enemy/EnemyBase.cs))**:
  - Menghilangkan delay penundaan destruksi objek yang membuat monster membeku lama sebelum lenyap setelah HP mencapai 0; eksekusi pembersihan collider, ragdoll, dan navmesh kini instan dengan notifikasi langsung ke penyelesaian wave.

#### 2. Spawning Gelombang Bertahap (Staggered Waves) & Shortcut Persiapan `[Rafi]`
- **Interval Spawning Dinamis ([`NightBrawlManager.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Combat/NightBrawlManager.cs))**:
  - Menggantikan instant batch dump di gerbang depan dengan mekanisme pemunculan bertahap (*staggered spawn*): 1 monster dimunculkan setiap 15 hingga 20 detik secara berkala.
- **Jeda Persiapan Taktis 30 Detik**:
  - Memberikan jendela waktu 30 detik antar gelombang malam untuk memulihkan posisi dan strategi pemain, dilengkapi penampil hitungan mundur di layar.
  - Menyediakan shortcut instan `[Enter]` dan `[N]` untuk melewati hitungan mundur persiapan tanpa bentrok dengan kontrol navigasi atau inventory.

#### 3. Antarmuka Pengisian Air Radial Screen-Space (Screen-Space Water Refill UI) `[Rafi]`
- **Komponen Prosedural ([`WaterRefillProgressUI.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Interaction/WaterRefillProgressUI.cs))**:
  - Membangun UI overlay berbasis canvas Screen Space dengan visual meter melingkar radial (*circular filled gauge*), persentase numerik, dan label dinamis.
  - Menghapus ikon floating 3D di atas kepala pemain untuk menjaga kejernihan visual isometric.
  - Menghubungkan pengisian botol minum di wastafel ([`KitchenSinkInteractable.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Kitchen/KitchenSinkInteractable.cs)) dan kaleng siram di sumur pekarangan ([`WaterWellInteractable.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Farming/WaterWellInteractable.cs)).

#### 4. Sinkronisasi Waktu Akselerasi (Fast-Forward x8) & Perbaikan Stasiun Dapur `[Rafi]`
- **Skalasi Waktu Proporsional**:
  - Menyinkronkan seluruh penghitung waktu pada [`KitchenStation.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Kitchen/KitchenStation.cs), kompor 3-tungku ([`KitchenStove.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Kitchen/KitchenStove.cs)), tempat mencuci piring ([`SinkManager.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Kitchen/SinkManager.cs)), serta pengisian air di wastafel dan sumur agar berakselerasi mengikuti mode 8x (shortcut `0`).
- **Perbaikan Bar Progres Kompor ([`StoveUIManager.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Kitchen/UI/StoveUIManager.cs))**:
  - Memperbaiki uGUI Image fill type menjadi Horizontal dengan sprite solid putih dan sinkronisasi slider sehingga bar terisi bertahap 0-100% tanpa membeku atau langsung penuh.
- **Perbaikan Persentase & Freeze UI Mencuci ([`SinkManager.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Kitchen/SinkManager.cs))**:
  - Memperbaiki kalkulasi progres pada `PercentText` dan `TimeText` sehingga angka persentase bertambah dinamis secara real-time.
  - Menambahkan deteksi transisi edge `wasWashing` untuk mencegah UI membeku di 100% saat pencucian piring/sayur selesai.

#### 5. Kalibrasi Kurva Linimasa 18 Menit & Harmonisasi Sistem Waktu `[Rafi]`
- **Tabel Keyframe Linimasa 18 Menit ([`DayNightTimeManager.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Time/DayNightTimeManager.cs))**:
  - Mengkalibrasi ulang `TimelineRealSeconds` dan `TimelineInGameHours` dengan 13 titik keyframe presisi (06:00 s.d. 18:00) yang menyinkronkan jam digital in-game 100% dengan linimasa 18 menit ideal MVP.
  - Menjamin lonceng senja (Dusk Bell) berbunyi tepat di jam 16:00 in-game (menit 15:00 real) dan malam tiba di jam 18:00 in-game (menit 18:00 real).
  - Membatasi shortcut fast-forward `0` hanya di fase siang (`EnvironmentPhase.Day`), mencegah desinkronisasi kecepatan saat Night Brawl.
- **Penyelarasan Teks Linimasa Aktivitas ([`DayActivityTimelineManager.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Time/DayActivityTimelineManager.cs))**:
  - Memperbaiki label teks pada kartu aktivitas HUD (`08:00 - 10:20`, `11:00 - 14:20`, dan `16:00 - 16:20`).
- **Pemisahan Ketat Sistem Air Kebun ([`WaterWellInteractable.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Farming/WaterWellInteractable.cs))**:
  - Menghapus ketergantungan `IWaterService` pada sumur kebun sehingga botol minum pemain (`PlayerWaterBottle`) tidak dapat diisi di sumur luar, menjaga integritas peran wastafel dapur.
- **Proteksi Transisi Layar Bebas Freeze ([`DayTransitionUI.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Time/UI/DayTransitionUI.cs))**:
  - Mengonversi transisi layar ke `Time.unscaledDeltaTime` dan `WaitForSecondsRealtime(1.5f)` sehingga transisi tidak dapat terhenti akibat pause atau timescale 0.
- **Harmonisasi Vitals Drain ([`PlayerStats.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Player/PlayerStats.cs))**:
  - Menyesuaikan `thirstDrainRate = 0.25f` dan `hungerDrainRate = 0.20f` agar kapasitas botol air 4 sips bertahan proporsional melintasi siklus 18 menit.

#### 6. Integrasi Menu Jeda Fantasi & Visual Polish `[Bhaskoro]`
- **Menu Jeda Fantasi & Animasi Kematian**:
  - Mengintegrasikan antarmuka menu jeda bergaya kayu dan transisi animator pop-up dari `bas-branch` ke dalam `Rafi-branch`.
  - Integrasi sekuens animasi kematian pemain (*You Collapsed Screen*) dengan pilihan Checkpoint, Load Game, atau Main Menu.

---

## - 2026-10-10

### Ditambahkan & Diintegrasikan (Added & Integrated)

#### 1. Arsitektur Pencahayaan & Keterbacaan Tempur Bergaya Hades (Hades-Inspired Lighting & Shaders) `[Rafi]`
- **Fondasi Trilight & Directional Moon Shadow ([`Night_LightingTheme.asset`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Settings/LightingThemes/Night_LightingTheme.asset), [`DayNightLightingController.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Time/Atmosphere/DayNightLightingController.cs))**:
  - Palet pencahayaan trilight atmosfer malam (Langit `#5C668C`, Ekuator `#424C6B`, Tanah `#383D47`) untuk mengeliminasi pemadaman bayangan pekat (*black crush*).
  - Cahaya bulan spektrum dingin 11500K `#94C2FF` 1.3 lux dengan soft shadow 4-cascade.
- **Shader Siluet Karakter & Rim Fresnel Musuh ([`HeroStylizedLit.shader`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Art/Shaders/Character/HeroStylizedLit.shader), [`MonsterFresnelLit.shader`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Art/Shaders/Monster/MonsterFresnelLit.shader))**:
  - Rim lighting emas untuk karakter pemain dan identitas Fresnel HDR unik untuk setiap arketipe musuh (Emerald untuk TuberMaw, Amber untuk CornMusketeer, Bronze untuk TaroBrute, Crimson Menace untuk Boss).
- **Cincin Bahaya Telegraf Serangan ([`CombatTelegraphDecal.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Combat/Enemy/CombatTelegraphDecal.cs))**:
  - Cincin proyeksi geometris bahaya musuh dengan radial fill timer 0-100% dan impak flash tajam.
- **Kalibrasi Bloom & Performa Zero-GC ([`SampleSceneProfile.asset`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Settings/SampleSceneProfile.asset), [`CombatProjectilePool.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Combat/Projectiles/CombatProjectilePool.cs))**:
  - Kalibrasi threshold bloom 1.05 dan intensitas 0.32 untuk pendaran HDR terisolasi tanpa silau lingkungan SDR.

---

## - 2026-10-09

### Ditambahkan & Diintegrasikan (Added & Integrated)

#### 1. Separasi Sistem Air & Interaksi Pemulihan (Water Separation System) `[Rafi]`
- **Dua Sumber & Jenis Air Terpisah**:
  - **Drinking Water Bottle** (`ToolType.WaterBottle`): Khusus diminum oleh pemain untuk memulihkan stat dahaga (*Thirst*). Hanya dapat diisi ulang melalui *Kitchen Sink* di dalam rumah ([`KitchenSinkInteractable.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Environment/KitchenSinkInteractable.cs)).
  - **Plant Waterer** (`ToolType.WateringCan`): Peralatan berkebun khusus kapasitas 100 Liter untuk menyiram petak sawah (*Farmland*). Hanya dapat diisi ulang di sumur pekarangan luar (*Garden Well*) ([`WaterWellInteractable.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Environment/WaterWellInteractable.cs)).
- **Validasi & Proteksi Kesalahan Interaksi**:
  - Mencegah botol minum diisi di sumur luar rumah dan sebaliknya, dengan umpan balik *floating combat text* berbahasa Inggris standar:
    - `"⚠️ Drinking water bottle can only be refilled at Kitchen Sink!"`
    - `"Requires Plant Waterer to refill farm water!"`
    - `"💧 Plant Waterer is already full (100/100 L)!"`
    - `"💧 Plant Waterer Refilled (100/100 L)!"`
- **Aset Ikon Plant Waterer**: Penambahan ikon tekstur [`icon_plant_waterer.png`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Art/Sprites/icon_plant_waterer.png) dan integrasi ke ScriptableObject [`Tool_PlantWaterer.asset`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Data/Tools/Tool_PlantWaterer.asset).

#### 2. Perombakan Menu Layar Kematian & Siklus Respawn (Death Screen & Respawn Pipeline) `[Rafi]`
- **Sistem Layar Kematian Interaktif ([`DeathScreenUI.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Combat/UI/DeathScreenUI.cs))**:
  - Menggantikan alur auto-respawn instan dengan modal interaktif beranimasi canvas fade yang menyediakan dua pilihan bagi pemain saat tumbang:
    - **"Respawn at Homestead"**: Menjalankan sekuens respawn sinematik kembali ke pos pekarangan rumah.
    - **"Quit to Main Menu"**: Kembali ke layar judul utama secara bersih.
  - Implementasi kontrak modal terpadu [`IModalWindow`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Core/Contracts/IModalWindow.cs) dengan prioritas stack tertinggi (`ModalPriority.Critical`) terdaftar di [`ModalStackManager.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Core/UI/ModalStackManager.cs).
- **Pengontrol Respawn Pemain ([`PlayerRespawnController.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Player/PlayerRespawnController.cs))**:
  - Mengunci input kontroler dan physics pemain saat tumbang (`SetDead(true)`).
  - Melakukan transisi layar gelap (*Fade-to-Black*), mereposisi transform karakter ke titik aman pekarangan tanpa tabrakan fisika, dan memulihkan stat kesehatan & stamina saat bangkit.

#### 3. Sistem Transisi Layar Awan Sinematik (Organic Cloud Screen Transitions) `[Bhaskoro]`
- **Manajer Transisi Awan ([`CloudTransitionManager.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Common/CloudTransitionManager.cs))**:
  - Komponen singleton mandiri yang menangani perpindahan scene menggunakan formasi awan berlapis (*Organic Feathered Clouds*).
  - Mengatur interpolasi posisi lapisan formasi awan kiri dan kanan secara horizontal dengan timing sinematik (`coverDuration = 0.40f`, `revealDuration = 0.40f`, `revealDelayAfterSceneLoad = 0.05f`).
- **Prefab & Aset Visual**:
  - Prefab overlay [`CloudTransitionCanvas.prefab`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Prefabs/CloudTransitionCanvas.prefab) dan koleksi 44 sprite awan beresolusi tinggi di `Assets/Art/Textures/Clouds/`.
  - Integrasi dengan [`MainMenuController.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/MainMenu/MainMenuController.cs) dan [`FantasyMenuAnimator.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/MainMenu/FantasyMenuAnimator.cs).

#### 4. Pembaruan Dependensi & Integrasi Tooling `[Roi]`
- **Pembaruan Paket Unity MCP**:
  - Memperbarui lock dependensi [`com.coplaydev.unity-mcp`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Packages/packages-lock.json) ke `v10.3.0` untuk stabilitas RPC bridge editor.
  - Penyelarasan aset arsitektur pekarangan dan optimasi material GPU instancing.

### Diperbaiki (Fixed)

- **Audit & Remediasi Arsitektur Bug QA (BUG-001 s/d BUG-008) `[Rafi]`**:
  - **BUG-001**: Mengatasi memory pressure dan stabilitas startup Main Menu terkait model Sentis/DirectML serta transisi async fade.
  - **BUG-002**: Menghilangkan alokasi dan referensi objek Unity yang telah hancur pada `pulsingEnemies` di [`EnemyOffscreenIndicatorUI.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Combat/UI/EnemyOffscreenIndicatorUI.cs).
  - **BUG-003**: Memperbaiki siklus hidup kematian pemain pada [`PlayerStats.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Player/PlayerStats.cs) dan [`PlayerControl.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Player/PlayerControl.cs), mencegah input leak dan animasi tersangkut.
  - **BUG-004**: Memperbaiki desinkronisasi inventaris dan penanganan shortcut tombol `TAB` / `ESC` agar konsisten menutup modal teratas.
  - **BUG-005**: Migrasi Text legacy ke TextMeshPro pada [`WardrobeUI.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Player/UI/WardrobeUI.cs) dan eliminasi anonymous lambda listener pada `Start()`/`OnEnable()`.
  - **BUG-006 & BUG-007**: Lokalisasi teks UI mengambang ke bahasa Inggris baku serta sanitasi unsubscription event pada lifecycle `OnDestroy()`.
  - **BUG-008**: Rekonsiliasi save slot manifest (`Saves/saves_manifest.json`) menjaga integritas save metadata terbaru pasca-merge.

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

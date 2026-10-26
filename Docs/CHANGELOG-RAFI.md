# Changelog (Rafi)

Semua perubahan penting pada proyek ini oleh Rafi akan dicatat di halaman ini.

## - 2026-10-26

### Ditambahkan & Diintegrasikan (Added & Integrated)
- **Overhaul Tipografi & Estetika Visual Sinematik Cutscene ([`CutsceneLetterboxUI.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Storyline/Cutscenes/CutsceneLetterboxUI.cs), [`StorylineUI.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Storyline/UI/StorylineUI.cs))**:
  - Mengeliminasi teks kecil dan polos pada cutscene dengan merombak tipografi dan panel visual secara menyeluruh:
    - **Subtitle Sinematik**: Ukuran font ditingkatkan dari 20f menjadi **28f** (dengan TextMeshPro auto-sizing 22f – 32f, font style Bold, warna moonlight ivory `#FAF8F2`, character kerning 1.2f, dan line spacing 6f).
    - **Frosted Obsidian Backing Plate & Golden Accent**: Membangun pelat latar semi-transparan `SubtitlePlate` (warna obsidian charcoal `#080A0F`, alpha 0.78) yang dilengkapi garis aksen emas tipis 2px (`TopAccentLine` warna `#F2C752`) di bagian atas panel, memberikan kontras tinggi di atas latar belakang 3D dunia game.
    - **Teks Kartu Prologue Layar Penuh (Intro Story Cards)**: Ukuran font intro story sentences ditingkatkan dari 24f menjadi **36f** (dengan auto-sizing 26f – 42f, Bold, line spacing 18f, character spacing 1.2f).
    - **Penyajian Naratif Murni (Tanpa Header Tag 'PROLOGUE')**: Menghapus baris judul "PROLOGUE" yang kaku dan menyajikan kutipan narasi sinematik langsung di tengah layar dengan tanda kutip elegan `“...”`, menghasilkan tampilan layar hitam pembuka yang bersih, fokus, dan sinematik.
    - **Bilah Letterbox & Tombol Hold-to-Skip**: Tinggi bilah letterbox sinematik ditingkatkan menjadi **125f** (dari 110f), dan label tombol skip diperbesar menjadi 15f Bold dengan aksen warm amber `<color=#F5D166>[ESC]</color> / HOLD <color=#F5D166>[SPACE]</color> TO SKIP`.
    - **Dialog Dalam Game**: Ukuran font teks isi dialog (`dialogueBodyText`) dinaikkan menjadi **21f** (dari 16f), nama pembicara (`dialogueSpeakerText`) menjadi **24f** Bold emas, dan ukuran panel dialog diperluas menjadi 920x200f.
- **Koreksi Logika Pemicu Penemuan Pipa Bawah Tanah Day 1 (`FirstTill` vs `FirstPlant`) ([`StoryEnums.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Storyline/Data/StoryEnums.cs), [`FarmlandTile.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Farming/FarmlandTile.cs), [`PlayerFarmInteraction.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Farming/Adapters/PlayerFarmInteraction.cs), [`StorylineManager.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Storyline/StorylineManager.cs), [`StoryDatabase.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Storyline/Data/StoryDatabase.cs))**:
  - Mendiagnosis dan mengoreksi anomali narasi Day 1 di mana dialog pemotongan pipa logam ("CLANG! My hoe just hit something metal buried deep in the dirt...") sebelumnya keliru terpicu saat pemain menanam bibit tanaman (`FirstPlant`).
  - Menambahkan trigger enum baru `StoryTriggerType.FirstTill` pada [`StoryEnums.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Storyline/Data/StoryEnums.cs).
  - Menambahkan event statis `FarmlandTile.OnAnySoilTilled` dan helper `InvokeSoilTilled` pada [`FarmlandTile.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Farming/FarmlandTile.cs) serta memicu sinyal saat tanah berhasil dicangkul di [`PlayerFarmInteraction.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Farming/Adapters/PlayerFarmInteraction.cs).
  - Mengintegrasikan penanganan event `HandleSoilTilled` di [`StorylineManager.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Storyline/StorylineManager.cs) untuk mengevaluasi trigger `StoryTriggerType.FirstTill`.
  - Memperbarui beat cerita di [`StoryDatabase.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Storyline/Data/StoryDatabase.cs) menjadi `D1_FIRST_TILL` terikat pada `StoryTriggerType.FirstTill`, sehingga notifikasi penemuan pipa logam muncul tepat saat pemain mencangkul tanah dengan cangkul.
- **Ekspansi Automated Audit Test Suite ke 64 Tes Otomatis (64/64 Passed) ([`GameplayAndHUDTests.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Editor/GameplayAndHUDTests.cs))**:
  - Menambahkan **Test 64** yang memvalidasi ukuran font subtitle cutscene (>=28f), auto-sizing, keberadaan backing plate frosted obsidian dan gold line, ukuran font intro story cards (>=36f), badge prologue emas, serta memverifikasi secara deterministik bahwa pemicu penemuan pipa logam Day 1 hanya aktif saat pencangkulan tanah (`FirstTill`) dan tidak lagi terpicu saat penanaman (`FirstPlant`).
  - Seluruh 64 tes audit komprehensif lulus 100% tanpa error maupun warning.
- **Implementasi Model Karakter PlayerV2 & Generic Rig Integration ([`character_player_v2.prefab`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Prefabs/Player/character_player_v2.prefab))**:
  - Menggantikan model karakter lama dengan model petani stylized beresolusi tinggi `Farmer_Asep_V2`.
  - Mengonfigurasi `Animator.avatar = null` pada instance runtime agar engine membaca kurva animasi generic secara langsung dari FBX tanpa retargeting humanoid yang dapat menyebabkan pergeseran bone.
  - Mengonfigurasi state machine controller [`AsepAnimator_V2.controller`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Resources/Player/AsepAnimator_V2.controller):
    - BlendTree pergerakan 8 arah: Idle, Walk, Run, Sprint.
    - Animasi aksi pertanian: `farm_plantseed` dan `farm_watering`.
    - Animasi manuver gerak: `roll_fit` (dodge roll atletis) dan `standing jump`.
    - Animasi tempur kapak/pedang: `standing melee attack horizontal`, `standing melee attack downward`, dan `standing melee attack 360 low`.
- **Normalisasi Skala HandSocket & Weapon Mount ([`PlayerEquipment.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Player/PlayerEquipment.cs))**:
  - Mendiagnosis dan memperbaiki distorsi skala weapon socket. Menetapkan `transform.localScale = Vector3.one` pada seluruh socket instansiasi peralatan (`HandSocket`, `WeaponSocket`) untuk menjamin ukuran senjata dan alat kerja selalu presisi 1:1.
- **Ekspansi Buffer Deteksi Tempur Non-Alloc & Input Klik Kanan ([`PlayerEquipment.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Player/PlayerEquipment.cs), [`PlayerControl.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Player/PlayerControl.cs))**:
  - Mengembangkan kapasitas buffer non-alloc `_sphereHitResults` dan `_coneHitResults` dari 16 menjadi **64 colliders**, mencegah dropped hits saat menebas gerombolan musuh padat pada Night Brawl.
  - Mengaktifkan klik kanan mouse (`Mouse.current.rightButton.wasPressedThisFrame`) pada `PlayerControl.HandleAttackInput()` untuk memicu rangkaian combo serangan selain klik kiri standar.
- **Kalibrasi Geometri Duduk Cockpit & Animasi Berkendara Truk ([`PlayerV2_DrivingPose.anim`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/PlayerV2/anim/PlayerV2_DrivingPose.anim), [`ArcadeVehicleController.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Vehicles/ArcadeVehicleController.cs), [`PlayerControl.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Player/PlayerControl.cs))**:
  - Mengeliminasi karakter berdiri tegak menembus atap kabin saat menaiki truk pickup.
  - Membuat klip pose duduk kustom generic `PlayerV2_DrivingPose.anim` dengan tangan memegang setir dan kaki tertekuk pada pedal di $Y = 0.46\text{m}$.
  - Mengimplementasikan driving heartbeat di `ArcadeVehicleController.DriveVehicle()` yang menegakkan `anim.Play("Driving", 0, 0f)` dan `anim.Update(0f)` setiap frame saat kendaraan bergerak.
  - Menambahkan guard pada `PlayerControl.Update()` agar locomotion idle/walk tidak menimpa pose duduk saat kontrol kendaraan aktif.
  - Menata ulang koordinat lokal `DriverSeatAnchor` pada truk pickup ke $Y = 0.08\text{m}$ (sejajar busa jok $Y = 0.72\text{m}$ world space), mengeliminasi kaki menembus sasis bawah truk.
- **Konsolidasi Alur Prolog Day 0 / Day 1 & World Placement Truk ([`BedInteractable.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Interaction/BedInteractable.cs), [`StagingScene.unity`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scenes/StagingScene.unity))**:
  - Menghubungkan interaksi kasur kamar tidur saat Day 0 untuk menyelesaikan prologue, memajukan hari ke Day 1 jam 06:00 pagi, dan merestorasi status stamina/kesehatan.
  - Menempatkan truk pickup pengiriman secara permanen di depan halaman kebun:
    - Posisi: `(30.5509, 0.00035, 54.1408)`
    - Rotasi: `(-0.3903, 0.9207)`
  - Menyembunyikan spanduk pengujian editor (*test banners*) agar kanvas visual bebas dari gangguan visual sementara.
- **Subsistem Side Quest 'First Steps' & Farmland Signal Bus ([`QuestProgressModel.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Quest/Core/QuestProgressModel.cs), [`QuestManager.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Quest/QuestManager.cs), [`QuestHUDUI.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Quest/UI/QuestHUDUI.cs), [`FarmlandTile.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Farming/FarmlandTile.cs))**:
  - Mengembangkan model quest deterministik Zero-GC murni POCO `QuestProgressModel` yang melacak pengumpulan peralatan awal (`tool_hoe`, `tool_plant_waterer`, `food_bottle_water`, `seed_sweet_potato`) serta aksi pertanian (cangkul, tanam bibit, siram tanaman).
  - Menambahkan sinyal event statis pada `FarmlandTile`: `OnAnyTileTilled`, `OnAnySeedPlanted`, `OnAnyTileWatered`.
  - Membangun kartu antarmuka glassmorphic `QuestHUDUI` yang berlabuh otomatis di bawah jam digital HUD.
- **Ekspansi Automated Audit Test Suite ke 63 Tes Otomatis (63/63 Passed) ([`GameplayAndHUDTests.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Editor/GameplayAndHUDTests.cs))**:
  - Menambahkan **Test 50 - Test 63** yang memvalidasi integritas rig PlayerV2, pose cockpit truk, transisi Day 0/1 kasur, pendaftaran sinyal side quest, ekspansi buffer tempur 64 target, dan penempelan hierarki QuestHUD (63/63 audit tests lulus 100%).

### Diperbaiki (Fixed)
- **Koreksi Glyph SDF & Eliminasi HTML Entity `&nbsp;` pada Cutscene & UI ([`CutsceneLetterboxUI.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Storyline/Cutscenes/CutsceneLetterboxUI.cs), [`DayTransitionUI.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Time/UI/DayTransitionUI.cs), [`StoveUIManager.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Kitchen/UI/StoveUIManager.cs), [`InventoryManagerUI.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Inventory/UI/InventoryManagerUI.cs))**:
  - Mendiagnosis penyebab teks `□ &nbsp; P R O L O G U E &nbsp; □` pada kartu narasi awal:
    1. **HTML Entity `&nbsp;`**: TextMeshPro hanya mendukung subset tag styling XML dan tidak mendukung HTML entity seperti `&nbsp;` (non-breaking space), sehingga string literal `&nbsp;` dicetak langsung ke layar. Mengganti seluruh entity `&nbsp;` dengan spasi standar ASCII.
    2. **Missing Glyph Box `□`**: Karakter extended unicode `✦` (`\u2726`) tidak tercakup dalam font atlas bawaan `LiberationSans SDF`, sehingga Unity merendernya sebagai fallback missing glyph box `□`.
  - Mengganti seluruh simbol `✦` dengan tanda pisah em-dash `—` (`\u2014`) pada header prolog cutscene (`—   P R O L O G U E   —`) dan DayTransitionUI (`—   D A Y  {roman}  •  A  N E W  D A W N  A W A K E N S   —`), serta peluru bullet `•` (`\u2022`) pada daftar buff kompor dan inventory.
  - Memverifikasi secara empiris melalui font inspection bahwa seluruh glyph karakter tersebut didukung 100% oleh font atlas tanpa missing glyph.
- **SafeDestroy Abstraction & Eliminasi Warning Edit-Mode ([`KitchenStationProgressOverlay.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Kitchen/UI/KitchenStationProgressOverlay.cs), [`WorldItemPickup.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Combat/WorldItemPickup.cs))**:
  - Mengeliminasi peringatan native engine `"Destroy may not be called from edit mode! Use DestroyImmediate instead."` dengan abstraksi helper `SafeDestroy` yang secara otomatis memilih `DestroyImmediate` di edit mode dan `Destroy` di play mode.
- **Eliminasi Kebocoran Material Edit-Mode ([`KitchenStationProgressOverlay.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Kitchen/UI/KitchenStationProgressOverlay.cs))**:
  - Menggunakan `MaterialPropertyBlock` saat memperbarui visual progres stasiun dapur, mengeliminasi duplikasi material yang tidak terlacak (*material leak*).
- **Proteksi Singleton OnDestroy saat Game Berhenti ([`OpeningCutsceneController.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Storyline/Cutscenes/OpeningCutsceneController.cs), [`StorylineUI.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Storyline/UI/StorylineUI.cs), [`JournalUI.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Storyline/UI/JournalUI.cs))**:
  - Mengeliminasi error native `"GameObjects can not be made active when they are being destroyed"` saat stop run game dengan memvalidasi status teardown sebelum mengakses instance singleton.

---

## - 2026-10-25

### Ditambahkan & Diintegrasikan (Added & Integrated)
- **Overhaul Perspektif Kamera Sinematik Lensa 50mm ([`OpeningCutsceneController.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Storyline/Cutscenes/OpeningCutsceneController.cs))**:
  - Mengonfigurasi kamera cutscene dengan lensa sinematik 50mm (`fieldOfView = 50.0`, `nearClipPlane = 0.3`) pada sudut pandang utara bebas rintangan (`Z = 57.5 - 62.5`), mengeliminasi clipping visual pada pagar dan pilar gerbang.
- **Fisika Pergerakan Mobil Zero-GC ([`VehicleDynamicsCore.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Vehicles/Core/VehicleDynamicsCore.cs), [`ArcadeVehicleController.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Vehicles/ArcadeVehicleController.cs))**:
  - Mengembangkan simulasi fisika akselerasi POCO murni Zero-GC yang menghitung torsi mesin, batas kecepatan maksimum, transfer bobot pengereman, dan radius putar setir.
- **Panel Letterbox & Subtitle Naratif ([`CutsceneLetterboxUI.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Storyline/Cutscenes/CutsceneLetterboxUI.cs))**:
  - Mengembangkan bilah hitam sinematik atas-bawah dengan animasi sliding mulus dan teks subtitle naratif typewriter yang mengiringi kedatangan truk di kebun. Mendukung fitur skip instan tanpa glitch orientasi kamera.
- **Ekspansi Test Suite ke 49 Tes Otomatis ([`GameplayAndHUDTests.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Editor/GameplayAndHUDTests.cs))**:
  - Menambahkan **Test 31 - Test 49** yang memvalidasi dinamika kendaraan, timeline intro, lensa kamera sinematik, dan sinkronisasi proyeksi kamera (49/49 Passed).

---

## - 2026-10-22

### Ditambahkan & Diintegrasikan (Added & Integrated)
- **Mesin Naratif Modular & Siklus Cerita 5 Hari ([`StorylineEngine.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Storyline/Core/StorylineEngine.cs), [`StoryDatabase.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Storyline/Data/StoryDatabase.cs), [`StorylineManager.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Storyline/StorylineManager.cs))**:
  - Mengembangkan arsitektur backend cerita POCO murni Zero-GC yang mengelola status alur cerita (`StoryState`), sasaran misi naratif (`StoryObjective`), serta dialog bertahap fasilitas penelitian NexaPhyto dari Hari 1 hingga Hari 5.
- **Antarmuka Dialog & Jurnal Cerita ([`StorylineUI.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Storyline/UI/StorylineUI.cs), [`JournalUI.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Storyline/UI/JournalUI.cs), [`BossIntroSequence.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Storyline/UI/BossIntroSequence.cs))**:
  - Membangun UI jurnal terpadu yang dapat dibuka kapan saja melalui tombol **`J`**.
  - Mengimplementasikan pop-up dialog glassmorphic dengan portret karakter dan typewriter text effect.
  - Mengembangkan sequence perkenalan bos sinematik saat boss wave malam pertama kali hadir di arena.
- **Gating Input Modal Terpusat ([`PlayerControl.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Player/PlayerControl.cs))**:
  - Menghubungkan seluruh status jendela modal (Journal, Storyline, Settings) ke `ModalStackManager`, memastikan kontrol gerak dan aksi alat pemain terkunci secara aman saat antarmuka modal aktif.
- **Aset Data Jagung & Farm Grid Dual Lookup ([`CropData_Corn.asset`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Data/Items/Crops/CropData_Corn.asset), [`FarmGridManager.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Farming/FarmGridManager.cs))**:
  - Menambahkan ScriptableObject resmi untuk Jagung dan memperluas resolver string di `FarmGridManager` untuk mendukung kueri panen jagung.
- **Ekspansi Test Suite ke 30 Tes Otomatis ([`GameplayAndHUDTests.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Editor/GameplayAndHUDTests.cs))**:
  - Menambahkan **Test 28, 29, 30** untuk memvalidasi engine narasi, modal stack input gating, dan data jagung (30/30 Passed).

---

## - 2026-10-21

### Ditambahkan & Diintegrasikan (Added & Integrated)
- **Prefab Visual & Mounting Alat Siram 3D ([`PlantWaterer.prefab`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Prefabs/Weapons/PlantWaterer.prefab), [`Tool_PlantWaterer.asset`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Data/Items/Equipment/Tool_PlantWaterer.asset))**:
  - Membangun model 3D alat penyiram tanaman resmi lengkap dengan geometri silinder badan, corong, pegangan, material URP `Mat_Metal_Dark`, weapon hitbox, dan trail controller. Menautkannya ke ScriptableObject alat siram untuk mengeliminasi tangan kosong saat pemain memegang watering can.
- **Eliminasi Deadlock Agresi Melee AI ([`MonsterTacticalEvaluator.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Combat/Core/PureLogic/MonsterTacticalEvaluator.cs), [`EnemyBase.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Combat/Enemy/EnemyBase.cs))**:
  - Mengeliminasi kondisi deadlock di mana monster melee berputar-putar tanpa henti di orbit ring ($3.2\text{m} - 4.8\text{m}$) tanpa bisa meminta token serang. Musuh yang cooldown serangnya siap kini langsung keluar dari orbit menuju pemain (`EngageDirect`) untuk menyerang.
  - Memperbaiki presisi tebasan dengan melakukan rotation snap pada klimaks ayunan serang dan memperluas radius erupsi tanah Tuber Maw menjadi $2.4\text{m}$.
- **Harden Material & Shader Standalone Windows Build**:
  - Mengonfigurasi `GraphicsSettings.defaultRenderPipeline` secara eksplisit ke `PC_RPAsset` untuk mencegah rendering pink shader pada standalone player build.
  - Mendaftarkan 9 shader runtime ke dalam `Always Included Shaders` guna mencegah shader stripping oleh compiler.
  - Mem-bundle 8 material tanah dan tanaman ke dalam `Assets/Resources/Materials/Farming/`.
- **Konsumsi Bibit Prioritas Hotbar Tangan ([`InventoryComponent.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Inventory/InventoryComponent.cs), [`FarmlandTile.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Farming/FarmlandTile.cs))**:
  - Menambahkan parameter `preferredSlotIndex` pada `InventoryComponent.RemoveItem()`. Bibit yang sedang dipegang di tangan pemain pada hotbar aktif (slot 0-3) kini diprioritaskan untuk berkurang terlebih dahulu sebelum mengambil cadangan bibit dari tas utama.
- **Pencegahan Kebocoran Spawn Wave & Pemisahan Minion Bos ([`NightBrawlManager.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Combat/NightBrawlManager.cs), [`WaveProgressionEngine.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Combat/Wave/WaveProgressionEngine.cs))**:
  - Menghentikan coroutine spawn seketika saat seluruh musuh aktif dihabisi, mencegah monster muncul di tengah masa jeda 30 detik (*intermission*).
  - Memisahkan minion yang dipanggil oleh bos dari kuota wave, menjaga keakuratan progress bar dan mengeliminasi penyelesaian wave prematur.
  - Mengurutkan bos selalu di akhir gelombang klimaks (Day 3 W3, Day 4 W4, Day 5 W5).
- **Ekspansi Test Suite ke 27 Tes Otomatis ([`GameplayAndHUDTests.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Editor/GameplayAndHUDTests.cs))**:
  - Menambahkan **Test 23 - Test 27** yang memvalidasi resolusi canvas health bar, isolasi minion bos, konsumsi bibit hotbar, sanitasi jangkauan serang monster, dan prefab alat siram (27/27 Passed).

---

## - 2026-10-17

### Ditambahkan & Diintegrasikan (Added & Integrated)
- **Bundling Material Pre-compiled Attack Telegraph ([`CombatTelegraphDecal.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Combat/Enemy/CombatTelegraphDecal.cs) & [`Mat_CombatTelegraph.mat`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Resources/Materials/Combat/Mat_CombatTelegraph.mat))**:
  - Mengembangkan dan mem-bundle material pre-compiled di folder Resources (`Assets/Resources/Materials/Combat/Mat_CombatTelegraph.mat`) dengan shader `Universal Render Pipeline/Decal` atau `Universal Render Pipeline/Unlit` berkategori transparan merah.
  - Membangun mekanisme resolusi runtime bertingkat (*multi-tier fallback*) pada `CombatTelegraphDecal.EnsureResourcesMaterial()`:
    1. Mencoba memuat asset material pre-compiled via `Resources.Load<Material>("Materials/Combat/Mat_CombatTelegraph")`.
    2. Jika tidak ditemukan, mencari runtime shader `Universal Render Pipeline/Decal`.
    3. Jika tidak tersedia, beralih ke runtime shader `Universal Render Pipeline/Unlit`.
    4. Fallback darurat ke shader standar bertransparansi alpha, sepenuhnya mengeliminasi isu kotak magenta (*pink/magenta missing shader*) pada rilisan executable Standalone Player Build.
- **Visual Telegraph & Warning Window Serangan Burrow TuberMaw ([`EnemyBase.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Combat/Enemy/EnemyBase.cs))**:
  - Mengimplementasikan jendela antisipasi serangan berdurasi 0.55 detik saat TuberMaw keluar dari tanah (`isBurrowed = false`).
  - Menampilkan proyektor lingkaran telegraf merah di tanah dan teks indikator peringatan melayang `⚠️ Erupting!` sebelum damage ledakan area diaplikasikan, memberikan kesempatan taktis bagi pemain untuk menghindar (*dodge*) atau menangkis (*parry*).
- **Integrasi Main Menu Animasi Daun & Padi (`bas-branch`) ([`MainMenuUI.prefab`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Prefabs/UI/MainMenuUI.prefab))**:
  - Mengintegrasikan pembaruan UI tombol menu utama dari `origin/bas-branch` via commit `8408d2c`, mencakup dekorasi sudut daun-padi animasi ([`PadiDaunAnimator.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/MainMenu/PadiDaunAnimator.cs) dan [`UIAnimationHandler.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/MainMenu/UIAnimationHandler.cs)).
- **Animasi Banner & Audio Transisi Waktu Siang-Malam ([`DayTransitionUI.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Time/UI/DayTransitionUI.cs))**:
  - Mengembangkan sistem animasi transisi banner procedural glassmorphic non-blocking (`sortingOrder = 92`) yang mengindikasikan pergantian fase waktu:
    - **Day ➔ Night**: Banner `🌙 NIGHTFALL` berlatar gelap (`#14070A`), border merah berpedar (`#E74C3C`), teks merah tegas, dan subtitle atmosferik `The shadows stir. Prepare your defenses.`
    - **Night ➔ Day**: Banner `☀️ DAY X` berlatar hangat (`#1C1608`), border emas berpedar (`#F1C40F`), teks keemasan, dan subtitle `The morning sun rises. Tend to your crops.`
  - Dilengkapi efek *vignette pulse* transparan di tepi layar serta audio sintetis prosedural Zero-Asset (*resonant bell gong* untuk malam dan melodi akor pagi C5-E5-G5 untuk siang).
  - Animasi menggunakan skala pegas (*spring curve*: $0.85 \rightarrow 1.02 \rightarrow 1.0$) dengan durasi hold 1.8 detik dan fade-out 0.5 detik berbasis `Time.unscaledDeltaTime` sehingga gameplay tetap berjalan lancar tanpa membekukan pemain.
  - Mendukung auto-procedural hierarchy generation yang mandiri di scene mana pun dan terhubung ke event `TimeManager.Instance.OnPhaseChanged`, `OnDayChanged`, serta `DayNightTimeManager.Instance.OnTimePhaseChanged`.
- **Sistem Purge Food Buff Otomatis Saat Tidur & Pagi Hari ([`PlayerBuffManager.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Player/PlayerBuffManager.cs), [`BedInteractable.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Interaction/BedInteractable.cs), [`PlayerRespawnController.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Player/PlayerRespawnController.cs))**:
  - Mengimplementasikan method publik `ClearAllBuffs()` pada `PlayerBuffManager` yang mengosongkan seluruh buff aktif, mengembalikan modifier status pemain ke baseline (`ApplyBuffModifiers(0, 0, 0)`), dan memicu event `OnBuffsChanged` untuk membersihkan tampilan HUD.
  - Menghubungkan listener otomatis ke event `OnDayChanged` dari `TimeManager`, `DayNightTimeManager`, dan `ITimeService` sehingga setiap pergantian hari otomatis membersihkan sisa buff.
  - Memanggil `ClearAllBuffs()` saat pemain berinteraksi dengan kasur di malam hari (`BedInteractable.Interact`) dan saat respawn bangun tidur di pagi hari (`PlayerRespawnController.RoutineRespawnSequence`), menjamin di pagi hari pemain selalu terbangun dalam keadaan bersih dari buff.
- **Arsitektur Multi-Source Input Lock & Sistem Pencegahan Kebocoran Input ([`IPlayerContext.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Core/Contracts/IPlayerContext.cs), [`PlayerControl.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Player/PlayerControl.cs), [`DayTransitionUI.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Time/UI/DayTransitionUI.cs), [`CloudTransitionManager.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Common/CloudTransitionManager.cs), [`InventorySlotUI.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Inventory/UI/InventorySlotUI.cs))**:
  - Mengimplementasikan sistem pelacakan multi-source input lock berbasis `HashSet<string>` pada `PlayerControl` dengan kontrak `AddInputLock(source)`, `RemoveInputLock(source)`, `ClearAllInputLocks()`, dan `HasInputLock(source)` pada interface `IPlayerContext`.
  - Mengeliminasi *premature unlock overwrite* (kebocoran pembatalan kunci) saat berbagai dialog atau sistem menutup secara tumpang tindih (misal dialog report kasur menutup saat transisi fajar sedang berlangsung).
  - Mengunci input pemain secara penuh selama animasi banner transisi `DayTransitionUI` (`"DayTransition"`) dan transisi awan sinematik `CloudTransitionManager` (`"CloudTransition"`).
  - Mengaktifkan raycast blocking 100% viewport (`vignetteOverlay.raycastTarget = true` dan `transitionCanvasGroup.blocksRaycasts = true`) selama transisi visual sehingga kursor mouse tidak dapat mengklik objek di balik transisi.
  - Memblokir kebocoran klik seleksi slot hotbar dan konsumsi item pada `InventorySlotUI.OnPointerClick()` jika kontrol pemain sedang terkunci.
  - Membersihkan seluruh residu aksi serangan, charging, buffer serangan, dan sequence combo pada `PlayerControl.SetControlLock(true)`.
- **Ekspansi Automated Audit Test Suite ke Test 22 ([`GameplayAndHUDTests.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Editor/GameplayAndHUDTests.cs))**:
  - Menambahkan **Test 19**: `Test_BuildBugfixes_IntegrityAudit` (validasi material telegraph Resources, offset elevasi pintu rumah $\ge +0.02\text{m}$, dan window warning erupsi burrow $\ge 0.4\text{s}$).
  - Menambahkan **Test 20**: `Test_PlayerBuff_ClearsOnSleepAndDayTransition` (validasi penghapusan seluruh food buff aktif saat tidur/transisi hari, restorasi status Max HP/Speed ke baseline, dan notifikasi HUD).
  - Menambahkan **Test 21**: `Test_DayTransitionUI_DayAndNightBannerAnimation` (validasi pembentukan hierarki visual otomatis, styling banner Day/Night, konfigurasi ikon, dan pembatalan instan).
  - Menambahkan **Test 22**: `Test_MultiSourceInputLock_AndTransitionLeakPrevention` (validasi hirarki kunci multi-source, kekebalan terhadap generic premature unlock, penyerapan raycast `DayTransitionUI`, dan pencegahan kebocoran klik hotbar). Seluruh 22 audit tests lolos (22/22 Passed).

### Diubah & Dioptimasi (Changed & Optimized)
- **Penyelarasan Stat Musuh dengan MVP Guideline V0.1 ([`EnemyData.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Combat/Data/EnemyData.cs))**:
  - Menyelaraskan status data musuh sesuai dokumen MVP Version Guideline V0.1:
    - **The Ranger** ([`EnemyData_TheRanger.asset`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Data/Enemies/EnemyData_TheRanger.asset)): Max HP 250, Armor 25, Move Speed 0.0 (artillery siege turret stasioner), Attack Damage 20, Attack Interval 1.5s, Poise 60. Display name diselaraskan menjadi `The Ranger`.
    - **Taro Colossus** ([`EnemyData_TaroColossus.asset`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Data/Enemies/EnemyData_TaroColossus.asset)): Max HP 900, Armor 55, Move Speed 2.2, Poise 180, Attack Damage 45, Attack Interval 3.0s.
    - **Corn Musketeer** ([`EnemyData_CornMusketeer.asset`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Data/Enemies/EnemyData_CornMusketeer.asset)): Max HP 85, Armor 10, Move Speed 3.8, Attack Damage 12, Attack Interval 2.2s.
    - **TuberMaw** ([`EnemyData_TuberMaw.asset`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Data/Enemies/EnemyData_TuberMaw.asset)): Max HP 120, Armor 15, Move Speed 3.2, Poise 40.
    - **PumpkinBrute** ([`EnemyData_PumpkinBrute.asset`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Data/Enemies/EnemyData_PumpkinBrute.asset)): Max HP 350, Armor 30, Move Speed 2.5, Attack Damage 35.
- **Konsolidasi Hirarki Resep Memasak ([`Assets/Data/Recipes/Cooking/`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Data/Recipes/Cooking/))**:
  - Menyatukan lokasi penyimpanan seluruh ScriptableObject resep kuliner ke folder kanonikal `Assets/Data/Recipes/Cooking/` (`Recipe_CornPorridge.asset` dan `Recipe_TaroCornStew.asset`), serta menghapus salinan usang di `Assets/Data/Kitchen/Recipes/`.
- **Alur Peluncuran New Game Langsung dari Main Menu ([`MainMenuController.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/MainMenu/MainMenuController.cs))**:
  - Menyederhanakan tombol New Game agar langsung menginisiasi transisi sinematik awan dan memuat scene game (`StagingScene`) tanpa membuka pop-up menu SaveSystemUI terlebih dahulu.

### Diperbaiki (Fixed)
- **Penanggulangan Karakter Terperosok di Lantai Rumah ([`DoorInteractable.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Kitchen/DoorInteractable.cs) & [`StagingScene.unity`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scenes/StagingScene.unity))**:
  - Menata ulang posisi vertikal `House_UnifiedFloorCollider` ke $Y = -0.50$ (ketebalan 1.0m, permukaan atas tepat di $Y = 0.00$) guna mengeliminasi tumpang tindih collider dengan kapsul fisik pemain.
  - Menambahkan offset pengaman vertikal $+0.04\text{m}$ pada target teleportasi pintu masuk rumah di `DoorInteractable.cs` untuk mencegah capsule raycast snagging.
- **Pembersihan Residual Testing Save File ([`SaveSystemManager.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/SaveSystem/SaveSystemManager.cs))**:
  - Membersihkan save data pengujian dari `persistentDataPath` agar tidak ada file testing usang yang termuat secara otomatis saat pemain menekan New Game.
- **Eliminasi Magenta Box Telegraph pada Standalone Build ([`CombatTelegraphDecal.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Combat/Enemy/CombatTelegraphDecal.cs))**:
  - Mengatasi kegagalan kompilasi runtime shader URP pada executable standalone dengan memastikan asset material pre-compiled selalu tersedia di dalam player package Resources.

---

## - 2026-10-16

### Ditambahkan & Diintegrasikan (Added & Integrated)
- **Arsitektur Pure C# POCO Tactical AI ([`MonsterTacticalEvaluator.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Combat/Core/PureLogic/MonsterTacticalEvaluator.cs))**:
  - Mengembangkan evaluator taktis murni (`MonsterTacticalEvaluator`) dalam namespace `FeaturesCombat.Core.PureLogic` tanpa dependensi siklus hidup `MonoBehaviour` (Zero-GC, deterministik, thread-safe).
  - **Tactical Encirclement Ring**: Menghitung vektor kepungan tangensial pada radius ring tempur ($3.2\text{m} - 4.8\text{m}$) dengan distribusi arah rotasi berlawanan (*clockwise* vs *counter-clockwise*) menggunakan paritas ID entitas (`(entityId & 1) == 0 ? 1 : -1`).
  - **Tangential Evasive Kiting**: Menghitung vektor mundur bersudut $\pm 120^\circ$ dengan komponen offset lateral bagi musuh spesialis ranged ([`CornMusketeer`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Combat/Enemy/EnemyType.cs)) ketika pemain mendekat ke radius kritis ($< 3.8\text{m}$), mencegah musuh terjebak di dinding lurus.
  - **Ambient Patrol Wander**: Menghitung titik patroli santai lokal secara acak di sekitar anchor spawn awal ketika musuh belum aggro.
- **Mekanika Pack Morale Surge ([`EnemyBase.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Combat/Enemy/EnemyBase.cs))**:
  - Berlangganan event statis `EnemyBase.OnAnyEnemyDied`. Saat rekan kawanan gugur dalam radius $8.0\text{m}$, monster di sekitarnya menerima lonjakan moral berupa $+15\%$ kecepatan gerak dan repathing instan selama $2.5\text{ detik}$ disertai feedback visual terapung `⚡ Surge!`.
- **Ekspansi Automated Audit Test Suite ([`GameplayAndHUDTests.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Editor/GameplayAndHUDTests.cs))**:
  - Menambahkan **Test 16**: `Test_MonsterTacticalEvaluator_EncirclementAndKiting` (validasi formasi orbit tangensial, diferensiasi arah paritas ID, dan diagonal kiting).
  - Menambahkan **Test 17**: `Test_AttackTokenDispatcher_AnticipatoryAcquisitionAndEviction` (validasi siklus hidup token serang antisipatif, batas kapasitas, dan priority eviction Boss).
  - Menambahkan **Test 18**: `Test_NavMesh_ObstaclePathfinding_RoutesAroundFences` (validasi raycast rintangan pagar, celah gerbang, dan navigasi memutar melewati `MainEntranceGate`).

### Diubah & Dioptimasi (Changed & Optimized)
- **NavMesh Barrier Carving & Gate Routing ([`StagingScene.unity`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scenes/StagingScene.unity) & [`NavMesh-_WORLD 1.asset`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scenes/StagingScene/NavMesh-_WORLD%201.asset))**:
  - Memasang komponen `NavMeshModifier (overrideArea = true, area = 1 / Not Walkable)` pada seluruh 5 segmen pagar keliling (`Fence_North_WestWing`, `Fence_North_EastWing`, `Fence_South`, `Fence_West`, `Fence_East`), pilar gerbang, pilar sudut, serta pembatas kebun dan workshop.
  - Membakar (*bake*) ulang NavMesh secara resmi menggunakan `NavMeshAssetManager.StartBakingSurfaces`, memastikan seluruh pagar memotong NavMesh dengan batas agent radius yang presisi dan gerbang `MainEntranceGate` ($X=18.4 - 22.4, Z=48.0$) menjadi satu-satunya jalur masuk legal.
- **Anticipatory Attack Token Acquisition ([`EnemyBase.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Combat/Enemy/EnemyBase.cs))**:
  - Memperbarui `EnemyBase.Update()` agar monster yang cooldown serangnya siap secara proaktif mengklaim token serangan dari `TokenDispatcher` saat memasuki jarak pendekatan ($\le 7.0\text{m}$), bukan menunggu hingga jarak serang melee ($\le 1.5\text{m}$).
  - Monster pemegang token langsung beralih ke state `EngageDirect`, menerjang maju dengan kecepatan penuh untuk melancarkan serangan bertelegraf.
  - Membatasi kuota token serang simultan (3 Melee, 2 Ranged) dengan giliran rotasi alami: monster cadangan mengitari pemain (`FlankOrbit`) dan langsung menerobos masuk begitu token dilepaskan (`ReleaseToken`).

### Diperbaiki (Fixed)
- **Eliminasi Deadlock Agresi Musuh Jarak Dekat ([`EnemyBase.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Combat/Enemy/EnemyBase.cs))**:
  - Memecahkan siklus Catch-22 di mana monster tanpa token dipaksa mengitari pemain pada jarak 4m sehingga tidak pernah mencapai 1.5m untuk memicu klaim token.
- **Line-of-Walkability Raycast Obstruction ([`EnemyBase.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Combat/Enemy/EnemyBase.cs))**:
  - Menambahkan pengecekan `NavMesh.Raycast` sebelum mengevaluasi orbit dan serangan. Mencegah monster mengitari pagar dari luar atau mencoba menyerang menembus papan kayu saat pemain berdiri dekat di balik pagar.
- **Orientasi Hadap Cerdas (*Dynamic Head Rotation*) ([`EnemyBase.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Combat/Enemy/EnemyBase.cs))**:
  - Menonaktifkan `LookAtTarget` saat jalur langsung terhalang rintangan solid (`isDirectPathBlocked`), sehingga monster menatap ke arah gerak navigasi (`finalMoveDir`) saat membelok mencari gerbang alih-alih menatap kaku ke arah dinding pagar.
- **Position Snapping di `ChaseTarget` ([`EnemyBase.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Combat/Enemy/EnemyBase.cs))**:
  - Menyematkan `NavMesh.SamplePosition` pada titik awal dan titik tujuan dengan toleransi radius $2.0\text{m} - 3.0\text{m}$ untuk menjamin keandalan kalkulasi jalur pada medan berkontur.

---

## - 2026-10-15

### Ditambahkan & Diintegrasikan (Added & Integrated)
- **Perombakan Stasiun Sumur Pekarangan: Place & Collect ([`WaterWellInteractable.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Farming/WaterWellInteractable.cs))**:
  - Merombak total mekanika sumur kebun menjadi stasiun interaktif berdurasi (*Place & Collect*).
  - Pemain menaruh kaleng siram (`Plant Waterer`) ke dudukan timba sumur, memicu animasi rotasi engkol kayu (*crank*), timba turun-naik, serta audio prosedural gesekan batu dan percikan air (*stone grind & water splash SFX*).
  - Mendukung pengisian kuota proporsional berbasis sisa isi air saat ditaruh, serta serialisasi status penyimpanan ke save file (`WellStationSaveState`).
- **Ekspansi Kapasitas Tas Menjadi 16 Slot ([`InventoryComponent.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Inventory/InventoryComponent.cs))**:
  - Memperluas kapasitas tas pemain dari 12 slot menjadi **16 slot penuh** (4 slot hotbar + 12 slot tas utama).
  - Mengimplementasikan seleksi hotbar via klik mouse pada slot UI ([`InventorySlotUI.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Inventory/UI/InventorySlotUI.cs)) dengan penanganan tombol kiri mouse `PointerEventData.InputButton.Left`.
- **Integrasi Menu Layar Kematian & Sistem Respawn (`bas-branch`)**:
  - Mengintegrasikan sekuens layar tumbang (*You Collapsed Screen*) melalui [`DeathScreenUI.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Player/UI/DeathScreenUI.cs) dan controller respawn [`PlayerRespawnController.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Player/PlayerRespawnController.cs).
  - Menyediakan opsi kebangkitan Checkpoint (terbangun di ranjang pukul 06:00 dengan pemulihan status) dan integrasi laporan harian [`DailyEconomyManager.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Economy/DailyEconomyManager.cs).
- **Automated Comprehensive Audit Tests ([`GameplayAndHUDTests.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Editor/GameplayAndHUDTests.cs))**:
  - Membangun suite audit otomatis mandiri yang memvalidasi 15 pengujian sistem kunci (linimasa siang, perizinan ranjang, siklus 4 tegukan botol minum, retensi 100L save/load, UI kompor, ekspansi 16 slot tas, dan akselerasi x8 buff makanan).

### Diubah & Dioptimasi (Changed & Optimized)
- **Akselerasi Status Buff Makanan pada Mode Fast-Forward 8x ([`PlayerBuffManager.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Player/PlayerBuffManager.cs))**:
  - Menyinkronkan pengurangan durasi aktif buff makanan dan interval tick periodik pemulihan HP (`HealthRegenTick`) dengan faktor pengali `FastForwardMultiplier` (8x saat shortcut `0` aktif) agar buff tidak bertahan berlebihan saat eksplorasi siang dipercepat.
- **Kelanjutan Simulasi Waktu Melewati Dusk (16:00 $\rightarrow$ 18:00) ([`DayNightTimeManager.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Time/DayNightTimeManager.cs))**:
  - Memperbaiki penghentian jam di pukul 16:00 (Dusk) sehingga waktu terus berjalan mulus hingga pukul 18:00 (Nightfall).
  - Mengizinkan pemain tidur di ranjang ([`BedInteractable.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Interaction/BedInteractable.cs)) saat fase Dusk (16:00 - 18:00) tanpa peringatan palsu monster malam.
- **Redesain Kartu Panduan HUD & Jam Digital ([`CombatPhaseTrackerUI.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Time/UI/CombatPhaseTrackerUI.cs))**:
  - Menyematkan badge aktif `'DAY 1'` yang tebal, jam digital 18pt berpenanda jelas, serta panel slate berukuran 280x72 dengan tipografi kontras tinggi (12pt judul, 11.5pt deskripsi).
- **Refactoring Indikator Progres Stasiun Dapur**:
  - Menghapus bar progres global yang membingungkan pada [`StoveUIManager.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Kitchen/UI/StoveUIManager.cs) dan memusatkan progres pada masing-masing kartu tungku aktif.
  - Memodernisasi overlay wastafel ([`KitchenStationProgressOverlay.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Kitchen/UI/KitchenStationProgressOverlay.cs)) dari veil vertikal 0.7m menjadi bar progres horizontal ramping dengan cyan fill line.

### Diperbaiki (Fixed)
- **Proteksi Jatuhnya Hasil Panen Saat Tas Penuh ([`FarmlandTile.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Farming/FarmlandTile.cs) & [`WorldItemPickup.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Combat/WorldItemPickup.cs))**:
  - Memperbaiki hilangnya bibit saat memanen tanaman dengan sisa 1 slot kosong: bibit yang tidak muat di tas otomatis dimunculkan mengapung di tanah via `WorldItemPickup.Spawn` dengan proteksi penyerapan instan 1.5 detik.
- **Pelepasan Emisi Trail Serangan Khusus Malam Hari ([`WeaponTrailController.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Combat/Adapters/WeaponTrailController.cs))**:
  - Membatasi nyala efek visual trail tebasan pedang agar hanya aktif pada malam hari (`DayPhase.Night` atau `IsNightBrawlActive`), menjaga ayunan cangkul dan pedang di siang hari tetap bersih.
- **Reset Otomatis Scroll Bar Toko Pedagang ([`MerchantShopUI.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Economy/MerchantShopUI.cs))**:
  - Mengembalikan posisi scroll view ke paling atas (`verticalNormalizedPosition = 1f`) setiap kali jendela toko dibuka atau tab kategori diganti.
- **Keamanan I/O Direktori Multi-Slot Save System ([`SaveSystemManager.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/SaveSystem/SaveSystemManager.cs))**:
  - Menambahkan verifikasi `Directory.CreateDirectory` otomatis sebelum operasi penyimpanan dan penanganan fallback graceful jika metadata slot kosong.

---

## - 2026-10-14

### Ditambahkan & Diintegrasikan (Added & Integrated)
- **Antarmuka Pengisian Air Radial Screen-Space (`WaterRefillProgressUI.cs`)**:
  - Mengimplementasikan komponen prosedural [`WaterRefillProgressUI.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Interaction/WaterRefillProgressUI.cs) berbasis *Screen Space - Overlay Canvas*.
  - Menyajikan indikator melingkar radial (*circular filled ring gauge*) bersih di tengah layar lengkap dengan pembacaan persentase (`0%` - `100%`) dan label kontekstual (`Refilling Water Bottle...`, `Refilling Plant Waterer...`).
  - Mengeliminasi ikon mengambang 3D di atas kepala karakter sesuai arahan desain antarmuka.
  - Terintegrasi penuh dengan [`KitchenSinkInteractable.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Kitchen/KitchenSinkInteractable.cs) (botol minum) dan [`WaterWellInteractable.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Farming/WaterWellInteractable.cs) (kaleng siram tanaman).
- **Mesin Efek Impak Tempur & Audio Sintetis (`CombatHitVfxEngine.cs` & `HitstopCoordinator.cs`)**:
  - Mengembangkan [`CombatHitVfxEngine.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Combat/Adapters/CombatHitVfxEngine.cs) yang mengelola sintesis audio prosedural *metal-on-flesh / blade clash* tanpa beban file audio diskrit, dengan modulasi acak nada harmonik (0.95x - 1.15x) bebas alokasi GC.
  - Menghasilkan partikel percikan bunga api emas (*golden burst sparks*) pada titik kontak bilah pedang ([`WeaponHitbox.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Combat/WeaponHitbox.cs)) dengan penanganan `StopEmittingAndClear` untuk mencegah runtime warning durasi ParticleSystem.
  - Memperkuat koordinasi *Dual Hitstop* ([`HitstopCoordinator.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Combat/Adapters/HitstopCoordinator.cs)) yang membekukan animasi penyerang dan target selama 0.06s - 0.10s secara independen tanpa memanipulasi waktu global, serta mengintegrasikan impuls getaran kamera dinamis via [`TraumaCameraShake.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Combat/Adapters/TraumaCameraShake.cs).
- **Siklus Spawning Bertahap (*Staggered Waves*) & Periode Persiapan Gelombang (`NightBrawlManager.cs`)**:
  - Mengubah mekanika pemunculan musuh dari *simultaneous instant batch* menjadi pemunculan bertahap (*staggered spawn*) dengan interval 15 hingga 20 detik per monster.
  - Menambahkan jeda persiapan taktis 30 detik antar gelombang malam dengan penampil hitungan mundur di layar.
  - Menyediakan shortcut instan `[Enter]` dan `[N]` untuk melewati sisa waktu persiapan gelombang tanpa bentrok dengan tombol pergerakan atau menu.
- **Integrasi Visual & Pause Menu (`bas-branch`)**:
  - Mengintegrasikan aset menu jeda fantasi bergaya kayu, animator pop-up, serta animasi sekuens tumbang (*player death*) hasil sinkronisasi branch `bas-branch`.

### Diubah & Dioptimasi (Changed & Optimized)
- **Kalibrasi Kurva Linimasa 18 Menit Siang Hari ([`DayNightTimeManager.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Time/DayNightTimeManager.cs))**:
  - Mengkalibrasi ulang `TimelineRealSeconds` dan `TimelineInGameHours` dengan 13 titik keyframe presisi sesuai linimasa ideal MVP:
    - 01:00 real (60s) $\rightarrow$ 06:40 in-game.
    - 03:00 real (180s) $\rightarrow$ 08:00 in-game.
    - 06:00 real (360s) $\rightarrow$ 10:00 in-game.
    - 06:30 real (390s) $\rightarrow$ 10:20 in-game (Sweet Potato 1 matang).
    - 07:30 real (450s) $\rightarrow$ 11:00 in-game.
    - 12:00 real (720s) $\rightarrow$ 14:00 in-game.
    - 12:30 real (750s) $\rightarrow$ 14:20 in-game (Sweet Potato 2 matang).
    - 15:00 real (900s) $\rightarrow$ 16:00 in-game (Dusk Bell berbunyi tepat di menit 15:00 real / 3 menit sebelum malam).
    - 18:00 real (1080s) $\rightarrow$ 18:00 in-game (Auto-sleep / Night Brawl).
- **Pembatasan Mode Fast-Forward (Key '0') Hanya di Siang Hari ([`DayNightTimeManager.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Time/DayNightTimeManager.cs))**:
  - Menonaktifkan shortcut `0` saat malam hari (`EnvironmentPhase.Night`) dan mereset kecepatan ke 1.0x untuk menjaga autentisitas ritme tempur Night Brawl dan mencegah tag HUD desinkronisasi.
- **Pemisahan Ketat Fungsi Sumur Kebun ([`WaterWellInteractable.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Farming/WaterWellInteractable.cs))**:
  - Menghapus resolusi `IWaterService` dari sumur kebun agar botol air minum (`PlayerWaterBottle`) tidak dapat diisi di sumur, menegakkan aturan desain bahwa sumur kebun 100% khusus untuk `PlantWaterer` (100L).
- **Skalasi Percepatan Waktu (Fast-Forward x8) Stasiun Dapur & Sumur**:
  - Menyinkronkan seluruh penghitung waktu stasiun memasak ([`KitchenStation.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Kitchen/KitchenStation.cs)), kompor 3-tungku ([`KitchenStove.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Kitchen/KitchenStove.cs)), tempat mencuci piring ([`SinkManager.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Kitchen/SinkManager.cs)), serta pengisian air di wastafel ([`KitchenSinkInteractable.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Kitchen/KitchenSinkInteractable.cs)) dan sumur pekarangan ([`WaterWellInteractable.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Farming/WaterWellInteractable.cs)) dengan faktor pengali waktu `FastForwardMultiplier` (8x saat shortcut `0` aktif).
- **Penyelarasan Durasi Buff Makanan Tempur MVP V0.1**:
  - Menstandardisasi seluruh durasi status buff makanan siap saji tempur (*Combat Food Buffs*) menjadi 3 menit (180 detik) pada dokumentasi acuan MVP dan dataset CSV.
- **Harmonisasi Laju Dehidrasi & Kelaparan ([`PlayerStats.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Player/PlayerStats.cs))**:
  - Menyesuaikan `thirstDrainRate` menjadi `0.25f` (~6.6 menit per 100 thirst) dan `hungerDrainRate` menjadi `0.20f` (~8.3 menit per 100 hunger) agar seimbang dengan siklus 18 menit siang.

### Diperbaiki (Fixed)
- **Eliminasi Freeze UI Selesai Mencuci Wastafel ([`SinkManager.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Kitchen/SinkManager.cs))**:
  - Menambahkan deteksi transisi edge `wasWashing` sehingga saat item terakhir selesai dicuci, UI otomatis memanggil `SyncProgressUI()` dan `RefreshSlotVisuals()`, mengembalikan bar progres ke 0%, timer ke `—`, dan status ke `"Done! Collect clean items"`.
- **Koreksi Rentang Waktu Teks HUD Linimasa Aktivitas ([`DayActivityTimelineManager.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Time/DayActivityTimelineManager.cs))**:
  - Memperbaiki label teks Workbench menjadi `"08:00 - 10:20"`, memasak siang menjadi `"11:00 - 14:20"`, dan Dusk Bell menjadi `"16:00 - 16:20"`.
- **Dukungan Multi-Sistem Input pada Skip Intermisi Gelombang ([`NightBrawlManager.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Combat/NightBrawlManager.cs))**:
  - Mendukung `UnityEngine.InputSystem.Keyboard.current` untuk tombol `Enter`, `KeypadEnter`, dan `N` serta pembungkus try/catch pada legacy `Input.GetKeyDown`.
  - Memperbarui label teks countdown pada [`CombatPhaseTrackerUI.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Time/UI/CombatPhaseTrackerUI.cs) menjadi `Prep Time ([Enter] / [N] to Skip)`.
- **Proteksi Unscaled Time Transisi Hari ([`DayTransitionUI.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Time/UI/DayTransitionUI.cs))**:
  - Mengubah transisi layar hitam pagi hari menggunakan `Time.unscaledDeltaTime` dan `WaitForSecondsRealtime(1.5f)` sehingga transisi tidak akan membeku saat pause game.
- **Standarisasi Kontrak Modal Dialog Tidur ([`BedSleepConfirmationUI.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Interaction/BedSleepConfirmationUI.cs))**:
  - Mengimplementasikan `IModalWindow` dan registrasi push/pop pada `ModalStackManager`.
- **Sinkronisasi Bar Progres Kompor Memasak ([`StoveUIManager.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Kitchen/UI/StoveUIManager.cs))**:
  - Memperbaiki uGUI Image fill type menjadi Horizontal dengan sprite solid putih serta sinkronisasi nilai slider 0-1 dan event listener dari `KitchenStation` sehingga bar terisi mulus dari 0% hingga matang dan tidak membeku atau langsung penuh.
- **Indikator Persentase & Timer Mencuci Sink ([`SinkManager.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Kitchen/SinkManager.cs))**:
  - Memperbaiki binding teks `PercentText` dan `TimeText` agar menghitung rasio progres secara dinamis dan tidak lagi tertahan di nilai `0%`.
- **Eliminasi Delay Kematian Musuh ([`EnemyBase.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Combat/Enemy/EnemyBase.cs))**:
  - Mengeliminasi delay penundaan destruksi objek yang menyebabkan musuh tampak membeku lama sebelum lenyap setelah HP mencapai 0.
- **Asosiasi Monster Bibit Jagung ([`Seed_Corn.asset`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Data/Items/Seeds/Seed_Corn.asset))**:
  - Memperbaiki relasi bibit jagung agar memicu pemunculan *Corn Musketeer* sesuai spesifikasi desain monster malam.

---

## - 2026-10-10

### Ditambahkan & Dioptimasi (Added & Optimized)
- **Arsitektur Pencahayaan & Keterbacaan Tempur Bergaya Hades (Hades-Inspired Stylized Isometric Lighting & Combat Readability - Milestones M1 - M7)**:
  - **M1 - Audit Baseline & Validasi**:
    - Memetakan baseline URP, hierarki pencahayaan, profil post-processing, dan status kompilasi proyek.
  - **M2 - Fondasi Trilight & Bayangan Lembut ([`Night_LightingTheme.asset`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Settings/LightingThemes/Night_LightingTheme.asset), [`DayNightLightingController.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Time/Atmosphere/DayNightLightingController.cs), [`DaySunLightingObserver.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Rendering/Lighting/DaySunLightingObserver.cs))**:
    - Mengonfigurasi palet trilight atmosferik malam: Langit `#5C668C` (0.36, 0.40, 0.55), Ekuator `#424C6B` (0.26, 0.30, 0.42), dan Tanah `#383D47` (0.22, 0.24, 0.28) untuk mengeliminasi pemadaman bayangan pekat (*black crush*).
    - Cahaya bulan spektrum dingin 11500K `#94C2FF` pada Directional Light dengan intensitas 1.3 lux, soft shadows 4 cascades, dan `shadowNearPlaneOffset = 0.15f` untuk mencegah *Peter-panning*.
  - **M3 - Pemisahan Siluet Karakter & Arketipe Musuh ([`HeroStylizedLit.shader`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Art/Shaders/Character/HeroStylizedLit.shader), [`MonsterFresnelLit.shader`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Art/Shaders/Monster/MonsterFresnelLit.shader), 6x `M_*.mat`)**:
    - Siluet pemain dilengkapi warm golden rim lighting untuk kontras tajam terhadap latar malam.
    - Setiap arketipe musuh memiliki identitas rim Fresnel HDR unik:
      - TuberMaw: *Emerald Rim* (`RGBA(0.3, 1.8, 0.6)`)
      - CornMusketeer: *Amber Rim* (`RGBA(1.8, 1.2, 0.2)`)
      - TaroBrute: *Bronze Rim* (`RGBA(1.6, 0.8, 0.3)`)
      - Boss (Cyclops, Taro Colossus, The Ranger): *Crimson Menace Rim* (`RGBA(2.2, 0.2, 0.4)`)
    - 100% kompatibel dengan SRP Batcher dan GPU Resident Drawer via CBuffer `UnityPerMaterial`.
  - **M4 - Umpan Balik Tempur, Kombo, & Jejak Tebasan ([`WeaponTrailController.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Combat/Adapters/WeaponTrailController.cs), [`HitFeedbackRenderer.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Combat/Adapters/HitFeedbackRenderer.cs), [`PlayerControl.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Player/PlayerControl.cs), [`PlayerEquipment.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Player/PlayerEquipment.cs))**:
    - Mengembalikan dan mensinkronisasi rantai 3-hit combo serangan ringan (Hit 1 -> Hit 2 -> Hit 3 bertahap tanpa lompatan).
    - Mensinkronisasi serangan berat klik-kanan (*Leaping Ground Slam*): pita kobaran api merah membentang sejak lompatan mengudara (0.25s) hingga benturan tanah (1.14s) dengan follow-through linger 0.20s.
    - Cincin proyeksi bahaya musuh geometris ala Hades ([`CombatTelegraphDecal.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Combat/Enemy/CombatTelegraphDecal.cs) & [`CombatTelegraph.shader`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Art/Shaders/Combat/CombatTelegraph.shader)) dengan lingkaran pengisian 0-100% dan flash impak tajam.
  - **M5 - Penegakan Performa & Pengujian Alokasi Zero-GC ([`CombatProjectilePool.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Combat/Projectiles/CombatProjectilePool.cs))**:
    - Pengujian burst 50 proyektil, 30 hit flash, dan 30 pergantian trail menghasilkan **tepat 0 bytes alokasi heap GC**.
    - Penegakan `shadows = LightShadows.None` dan `shadowRenderingLayers = 0` pada seluruh point light proyektil dan benturan.
    - Batches terkendali $\le 30$, SetPass calls $\le 18$.
  - **M6 - Harmoni Bioma Siang-Malam & Kalibrasi Bloom Terkendali ([`SampleSceneProfile.asset`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Settings/SampleSceneProfile.asset), [`DayNightVolumeController.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Time/Atmosphere/DayNightVolumeController.cs))**:
    - Menetapkan threshold bloom ke **1.05**, intensitas **0.32**, dan scatter **0.65** pada volume global dan factory preset.
    - Geometri SDR standar (tanah, tanaman, pagar, batu, lantai) tidak mengalami silau/kabur, sementara elemen HDR $>1.05$ (trail tebasan, flash benturan, proyektil, rim musuh) berpendar filmik.
    - Verifikasi visual komparatif tertangkap pada `m6_farmland_day.png`, `m6_farmland_dusk.png`, `m6_farmland_night.png`, `m6_combat_readability_day.png`, dan `m6_combat_readability_night.png`.
  - **M7 - Validasi Build, Uji Regresi & Hardening Audit**:
    - Kompilasi build skrip Unity Player (`BuildPipeline.BuildPlayer` - `BuildScriptsOnly`) berhasil dengan 0 error kompilasi.
    - Konsol Unity bersih dengan 0 runtime exceptions dan 0 error.
    - Resolusi temuan audit kualitas repo:
      - Memperbaiki kebocoran delegate input lompat pada [`PlayerControl.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Player/PlayerControl.cs) dengan mengganti lambda anonim menjadi method group bernama (`OnJumpPerformed`).
      - Menyematkan proteksi atomic write (`.tmp` + swap) dan penanganan error try-catch pada penyimpanan data lemari pakaian di [`PlayerOutfit.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Wardrobe/PlayerOutfit.cs).
      - Menambahkan auto-recovery file `.tmp` pada pemulihan slot penyimpanan di [`SaveSystemManager.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/SaveSystem/SaveSystemManager.cs).
      - Mendaftarkan pelepasan referensi `ServiceLocator.Unregister<IPlayerDamageNotifier>()` pada `OnDestroy()` di [`PlayerStats.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Player/PlayerStats.cs) untuk mencegah dangling pointer lintas scene reload.
    - Pohon kerja Git terisolasi murni pada `Rafi-branch` tanpa menyentuh `Sprint-branch`.

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
- **Architectural QA Remediation (BUG-001 through BUG-008)**:
  - **Compliance & UI Localization**:
    - Localized all floating text notifications in [`WaterWellInteractable.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Farming/WaterWellInteractable.cs) to exact standard English strings.
    - Updated hat toggle button in [`WardrobeUI.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Wardrobe/UI/WardrobeUI.cs) to English ("Toggle Hat"), upgraded text rendering to `TMPro.TextMeshProUGUI`, and eliminated heap closures in `Start()`/`OnEnable()` by routing to explicit private click handlers (`OnSaveClicked()`, `OnCancelClicked()`).
  - **Memory & Indicator Tracking**:
    - Refactored `pulsingEnemies` in [`EnemyOffscreenIndicatorUI.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Combat/UI/EnemyOffscreenIndicatorUI.cs) from holding `EnemyBase` references to `HashSet<int>` using `enemy.GetInstanceID()`. Guaranteed immediate purging when indicators are returned to pool, become on-screen, or upon enemy despawn/death.
    - Upgraded deprecated `FindObjectOfType<MainMenuController>()` in [`SettingsUIController.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/MainMenu/SettingsUIController.cs) to Unity 6 `UnityEngine.Object.FindFirstObjectByType<MainMenuController>(FindObjectsInactive.Exclude)`.
  - **Modal & Cursor Unification**:
    - Added `CanDismissWithEscape` contract to [`IModalWindow.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Core/Contracts/IModalWindow.cs) and updated [`ModalStackManager.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Core/UI/ModalStackManager.cs) to respect modal dismissal permissions on ESC key.
    - Implemented `IModalWindow` on [`KitchenSinkInteractable.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Kitchen/KitchenSinkInteractable.cs) and [`DailyReportModalUI.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Economy/DailyReportModalUI.cs) with automatic registration into `ModalStackManager`.
    - Enforced non-dismissible modal contract (`CanDismissWithEscape = false`) on [`DeathScreenUI.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Player/UI/DeathScreenUI.cs), requiring explicit selection between Checkpoint, Load Game, or Main Menu.
  - **Physics Locomotion & Grounding**:
    - Implemented `groundedGraceTimer` with ~0.1s decay and `isGroundedRecently` in [`PlayerControl.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Player/PlayerControl.cs) to eliminate sprint micro-stutter when navigating downward slopes or stair steps.
    - Enforced stamina gating on sprinting (`hasStamina = playerStats == null || (!playerStats.IsExhausted && playerStats.currentStamina > 0.5f)`).
  - **Player Defeat & Night Brawl Cleanup**:
    - Integrated explicit `NightBrawlManager.Instance.AbortBrawl()` in [`PlayerRespawnController.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Player/PlayerRespawnController.cs) before time advances, ensuring all active night enemies and waves are purged cleanly before the morning cycle.
    - Routed respawn positioning strictly through `playerControl.Teleport(respawnPos, respawnRot)` with fallback elevation sanitized to `y = 0.5f` to prevent ground clipping, followed by `Physics.SyncTransforms()`.

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

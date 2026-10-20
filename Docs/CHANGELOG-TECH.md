# CHANGELOG - TECHNICAL OVERVIEW (Farm-Beware)

Konsolidasi dokumentasi teknis dari seluruh kontributor tim pengembang (`Rafi`, `Bhaskoro`, `Roi`) yang diintegrasikan ke dalam `Sprint-branch`, diurutkan secara kronologis berdasarkan tanggal rilis.

---

### Tim Kontributor & Domain Fokus:
- **[Rafi]**: *Core Architecture, Night Brawl Combat Engine, Farming POCO Backend, Dynamic Lighting & Shaders, Level Design & Occlusion System.*
- **[Bhaskoro]**: *Multi-Slot Save & Load Persistence, Stylized Fantasy Main Menu & Pause Menu, Additive Gameplay Hooks.*
- **[Roi]**: *Character Sheet 3-Column UI (Stats & Equipment), Compound Perimeter Structures & Foliage, GPU Instancing Optimizations.*

---

## Daftar Isi Kronologis
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

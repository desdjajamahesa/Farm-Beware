# Phase 3: Night Lighting & GPU Profiling Guide (Unity 6 URP Deferred+)

Panduan komprehensif untuk pengujian pencahayaan malam hari (*Phase 3: Night Lighting*), keterbacaan pertarungan (*combat readability*), integrasi mode malam legacy ('N'), shader siluet monster Fresnel, dan panduan validasi **GPU Profiler** untuk mengukur efisiensi 200+ Point Lights pada jalur **URP Deferred+**.

---

## 1. Integrasi Mode Malam Legacy & Directional Light Rona Biru-Keunguan

### Alur Kerja Sistem:
1. **Pintasan Keyboard 'N'**:
   - Menekan tombol `N` saat permainan berjalan akan ditangkap oleh [`DayNightTimeManager.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Time/DayNightTimeManager.cs).
   - Waktu langsung melompat ke jam malam (`19.5f` / 19:30), memicu event:
     `OnTimePhaseChanged(EnvironmentPhase.Night)` dan menyinkronkan `TimeManager.Instance.StartNightPhase()`.
2. **Respon Observer Pencahayaan (`DaySunLightingObserver.cs`)**:
   - Directional Light berotasi ke sudut malam (Pitch $55^\circ$, Yaw $35^\circ$).
   - Intensitas meredup ke **`0.15 lux`** (rentang aman `0.1 – 0.2 lux`).
   - Spektrum warna diatur ke **biru-keunguan lembut** (`Color(0.20f, 0.26f, 0.48f)`).
   - **Tujuan Estetika**: Memberikan orientasi spasial minimal agar pemain tetap dapat melihat batas pagar, rintangan batu, dan siluet rumah pertanian tanpa merusak suasana malam Action Survival yang mencekam (mencegah layar hitam kelam).
3. **Penyelarasan APV (Adaptive Probe Volumes)**:
   - Warna ambient langit (`RenderSettings.ambientSkyColor`) dan kabut (`fogColor`) otomatis diselaraskan ke rona biru malam, sehingga reflektansi probe APV Sky Occlusion memantulkan cahaya difus malam yang kohesif pada permukaan model.

---

## 2. Keterbacaan Pertarungan: Monster Fresnel Rim Light

### Mengapa Fresnel Rim Light Sangat Krusial?
Pada malam hari, perbedaan nilai luminansi antara tubuh musuh dan permukaan tanah gelap menjadi sangat tipis. Untuk mencegah musuh "menghilang" di kegelapan, material monster dilengkapi shader khusus:
- **File Shader**: [`Assets/Shaders/Monster/MonsterFresnelLit.shader`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Shaders/Monster/MonsterFresnelLit.shader)
- **Rancangan Node Graph**: [`Docs/Monster_Fresnel_ShaderGraph_Design.md`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Docs/Monster_Fresnel_ShaderGraph_Design.md)
- **Implementasi Matematis**:
  $$\text{fresnel} = \text{pow}(\text{saturate}(1.0 - \text{dot}(\mathbf{N}, \mathbf{V})), \text{\_FresnelPower}) \times \text{\_FresnelIntensity}$$
  $$\text{FinalEmission} = \text{\_EmissionColor} + (\text{\_FresnelColor} \times \text{fresnel})$$
- **Pewarnaan Otomatis ([`EnemyPrefabFactory.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Combat/Enemy/EnemyPrefabFactory.cs))**:
  - *Tuber Maw*: Pendaran Neon Magenta `(1.8, 0.2, 1.3)`
  - *Cyclops Tuber Maw (Boss)*: Merah Darah Menyala `(2.5, 0.1, 0.2)`
  - *Taro Brute*: Earthen Amber `(1.6, 0.9, 0.2)`
  - *Taro Colossus (Boss)*: Molten Core Gold `(2.2, 1.2, 0.3)`
  - *Corn Musketeer*: Electric Lime Cyan `(0.3, 1.8, 1.2)`
  - *The Ranger (Boss)*: Radiant Solar Flare `(2.5, 1.5, 0.1)`

---

## 3. Optimalisasi Proyektil (Deferred+ Point Lights)

### Aturan Kritis: `light.shadows = LightShadows.None`
Telah dibuat komponen [`CombatProjectile.cs`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Features/Combat/Projectiles/CombatProjectile.cs):
- Menggunakan Point Light beradius pendek ($3.5\text{m} - 4.5\text{m}$) dengan intensitas $1.5 - 2.5\text{ lux}$.
- **Alasan Teknis**: Pada arsitektur **URP Deferred+**, sumber cahaya tanpa bayangan tidak memerlukan kalkulasi geometri tambahan; posisinya dikelompokkan ke dalam frustum ubin 3D (*spatial light clustering*). Fragment shader hanya mengevaluasi daftar cahaya yang relevan untuk setiap piksel.
- Sebaliknya, jika bayangan diaktifkan pada lampu proyektil dinamis, GPU akan dipaksa merender ratusan *shadow pass* terpisah yang akan menjatuhkan framerate secara katastropik.

---

## 4. Prosedur Pengujian GPU Profiler (200+ Point Lights)

### Langkah-langkah Validasi di Unity Editor:
1. Buka scene pengujian `Assets/Scenes/StagingScene.unity`.
2. Buat GameObject kosong baru di hierarchy: `[STRESS_TEST_LIGHTS]`.
3. Tambahkan komponen **[`DeferredPointLightStressTest`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Rendering/Testing/DeferredPointLightStressTest.cs)**.
4. Masuk ke **Play Mode**.
5. Tekan tombol **"N"** pada keyboard untuk memastikan mode malam aktif (Directional Light meredup ke 0.15 lux biru-keunguan).
6. Pada panel overlay OnGUI di pojok kanan atas, klik **"Spawn 200 Proyektil Point Lights"**. Sebanyak 200 lampu dinamis berwarna-warni akan mengorbit di sekitar arena.
7. Buka jendela **Unity Profiler**:
   - Menu: **`Window > Analysis > Profiler`** (Pintasan: `Ctrl + 7`).
   - Pada dropdown kiri atas jendela Profiler (**Profiler Modules**), pastikan modul **GPU Usage** dicentang aktif.
   - Klik satu frame rekaman di grafik GPU Profiler untuk menjeda inspeksi.
8. Di panel bawah Profiler (hirarki pemanggilan GPU):
   - Telusuri pass:  
     `UniversalRenderer.Render` $\rightarrow$ `DrawOpaqueObjects` $\rightarrow$ `RenderGraph: DeferredLights` (atau `LightClustering`).
   - Periksa kolom **Time (ms)**.

### Kriteria Kelulusan (Pass Criteria):
- **Batas Toleransi ALU GPU**: Konsumsi GPU time untuk pass Deferred Lights pada 200 Point Lights aktif **TIDAK BOLEH melampaui 3.0 milidetik** ($\le 3.0\text{ ms}$).
- **Framerate Target**: Tetap stabil di **60+ FPS** pada GPU PC berspesifikasi menengah-atas (NVIDIA GTX 1660 / RTX 2060 / AMD RX 5600 ke atas).

### Uji Komparasi (Membuktikan Alasan Shadow Off):
- Pada overlay OnGUI, centang opsi: **`Bandingkan: Paksa Aktifkan Shadows (Uji Lonjakan)`**.
- Amati grafik GPU Profiler:
  - GPU time akan melonjak seketika dari $\le 3\text{ ms}$ ke $> 20 - 30\text{ ms}$ karena GPU merender 200 *shadow depth maps*.
  - Hilangkan centang untuk mengembalikan lampu ke `shadows = None` dan saksikan performa GPU langsung kembali enteng dan mulus.

# Phase 2: Day Lighting Testing Checklist & Visual Validation Guide

Checklist pengujian dan panduan verifikasi visual langkah demi langkah untuk memvalidasi **Phase 2: Day Lighting** pada proyek **Farm-Beware** di Unity 6000.3.20f1 URP.

---

## 1. Persiapan Komponen di Unity Editor

1. Buka scene pengujian utama: `Assets/Scenes/StagingScene.unity`.
2. Temukan GameObject **`Directional Light`** di hierarchy:
   - Pastikan komponen **[`DaySunLightingObserver`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Rendering/Lighting/DaySunLightingObserver.cs)** terpasang pada Directional Light tersebut.
   - Pada Inspector `DaySunLightingObserver`:
     - *Sunrise Hour*: `6.0`
     - *Sunset Hour*: `18.5`
     - *Sunrise Azimuth*: `135°`
     - *Sunset Azimuth*: `225°`
     - *Max Elevation*: `55°`
     - *Noon Kelvin*: `5500K`
     - *Dusk Kelvin*: `3200K`
     - *Shadow Near Plane Offset*: `0.15`
3. Pasang Komponen Uji Coba:
   - Tambahkan komponen **[`DayLightingValidationHarness`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scripts/Rendering/Testing/DayLightingValidationHarness.cs)** pada GameObject `Directional Light` (atau buat objek kosong baru `[TESTING_HARNESS]`).

---

## 2. Prosedur Pengujian Sapuan Azimuth 135° ke 225°

Jalankan Play Mode (atau gunakan slider di Inspector pada Edit Mode):

### Posisi 1: Azimuth 135° (Pagi Hari / Fajar Awal)
- Geser slider Azimuth ke **`135.0°`** (atau klik tombol **`135° (Pagi)`** di overlay OnGUI).
- **Ciri Fisik Terkalkulasi**:
  - *Elevasi (Pitch)*: Sekitar `12.0°` (matahari rendah di ufuk).
  - *Suhu Kelvin*: Sekitar `5500K` (sinar pagi cerah).
  - *Intensitas*: ~`0.3 lux` (lembut, belum menyilaukan).
  - *Orientasi Bayangan*: Bayangan memanjang ke arah Barat-Laut.

### Posisi 2: Azimuth 180° (Siang Hari Puncak / Zenith)
- Geser slider Azimuth ke **`180.0°`** (atau klik tombol **`180° (Siang)`**).
- **Ciri Fisik Terkalkulasi**:
  - *Elevasi (Pitch)*: Maksimum `55.0°` (matahari tegak dari arah Selatan).
  - *Suhu Kelvin*: `5500K` (daylight netral seimbang, warna dedaunan tanaman tampil alami).
  - *Intensitas*: Maksimum `1.15 lux`.
  - *Orientasi Bayangan*: Bayangan pendek dan terkumpul tepat di bawah kanopi tanaman dan kaki karakter.

### Posisi 3: Azimuth 225° (Sore / Senja Keemasan)
- Geser slider Azimuth ke **`225.0°`** (atau klik tombol **`225° (Senja)`**).
- **Ciri Fisik Terkalkulasi**:
  - *Elevasi (Pitch)*: Kembali turun ke `12.0°`.
  - *Suhu Kelvin*: Menurun drastis secara eksponensial ke **`3200K`** (rona jingga-oranye hangat/golden hour).
  - *Intensitas*: Meredup ke ~`0.3 lux`.
  - *Orientasi Bayangan*: Bayangan memanjang ke arah Timur-Laut.

---

## 3. Checklist Validasi Visual Kritis

Lakukan inspeksi visual pada Scene View / Game View saat melakukan sapuan matahari:

| No | Parameter Kritis | Kriteria Lolos (PASS) | Tanda Kegagalan (FAIL) |
|---|---|---|---|
| 1 | **Shadow Terminator Softness** | Garis batas terang/gelap pada kontur melengkung (seperti tanah bergelombang atau dinding silinder) bertransisi secara halus tanpa tangga piksel tajam. | Garis batas bayangan pecah menjadi kotak-kotak piksel kasar bergerigi. |
| 2 | **Shadow Acne Elimination** | Permukaan tanah isometrik dengan sudut kemiringan $45^\circ$ dan dinding vertikal mulus bersih dari bintik-bintik atau garis belang zebra frekuensi tinggi. | Terlihat moiré pattern / garis hitam belang-belang berkedip pada permukaan datar (acne). |
| 3 | **Peter-Panning Guard (Contact Shadows)** | Pangkal batang tanaman wortel/gandum yang tertancap di tanah dan sol sepatu karakter menempel rapat dengan bayangannya. Tidak ada celah udara (*gap*). | Bayangan terlihat melayang beberapa sentimeter di bawah model (disebabkan oleh `shadowNearPlane` > 0.3). |
| 4 | **Camera-Relative Culling Stability** | Saat menggerakkan kamera isometrik mengelilingi lahan pertanian, tepi bayangan kaskade tidak bergetar atau berkedip (*zero edge shimmering/crawling*). | Tepi bayangan melompat-lompat atau berganti resolusi secara kasar saat kamera digeser. |
| 5 | **Observer Purity (Zero Update Overhead)** | Skrip `DaySunLightingObserver` tidak mengonsumsi frame time CPU ketika jam in-game dijeda (paused). Modifikasi transform hanya terjadi saat event menit/jam dipancarkan. | Terdapat Profiler spike di fungsi `Update()` dari Directional Light. |

---

## 4. Opsi Pengujian Otomatis (Auto-Sweep)

Pada jendela overlay OnGUI di pojok kiri bawah saat Play Mode:
1. Centang opsi **`Auto-Sweep Sapuan Matahari Bolak-Balik`**.
2. Kamera akan tetap diam sementara arah matahari menyapu lembut bolak-balik antara 135° dan 225°.
3. Amati perubahan rona warna lingkungan dari daylight 5500K ke sunset 3200K yang terjadi secara mulus dan organik.

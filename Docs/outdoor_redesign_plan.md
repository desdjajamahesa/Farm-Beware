# Master Plan: Redesain Tata Letak Area Luar Rumah (Homestead Compound)

## 1. Analisis Kritis Tata Letak Saat Ini (Current Layout Audit)

Berdasarkan inspeksi tangkapan layar dan data spasial di [`StagingScene.unity`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scenes/StagingScene.unity):

1. **Ketidakseimbangan Massa Visual (Visual Weight Imbalance)**:
   - **Sayap Kiri (Screen-Left / Dunia Timur, X: 25–38)**: Didominasi secara masif oleh blok kebun 16 petak (`Farmland`) dan sumur di belakangnya. Blok ini terlihat kaku dan terisolasi.
   - **Sayap Kanan (Screen-Right / Dunia Barat, X: 8–18)**: Mengalami *spatial crowding* (penumpukan fungsi). Kolam alami (`NaturalPond`), api unggun (`Campfire`), dan gubuk kerja (`CraftingShelter`) berjejer terlalu rapat dalam satu garis, saling berebut ruang dan atensi visual.
   - **Koridor Tengah**: Jalan setapak batu (`CobblestonePathways`) berbelok tajam tanpa alasan topografi, diapit tiang lampu taman yang terlalu rapat sehingga terasa seperti penghalang pergerakan (*movement obstruction*).

2. **Hierarki Visual & Focal Point yang Lemah**:
   - Saat pemain masuk dari Gerbang Utama (Utara), tidak ada **Focal Point Sentral** yang menyambut. Pandangan mata terbelah secara canggung antara kebun di kiri dan gubuk/kolam di kanan.
   - Api unggun yang seharusnya menjadi simbol kehangatan dan tempat istirahat roguelike justru tersembunyi miring di sayap kanan.

3. **Inkoherensi Alur Fungsional (Environmental Storytelling)**:
   - **Sumur Air (`WaterWell`)** terletak jauh di belakang kebun dekat dinding rumah (`X=26.5, Z=31.0`), menyulitkan loop penyiraman tanaman.
   - **Crafting Shelter** menempel di samping teras depan rumah, memblokir fasad rumah dan tidak memiliki area logistik (penyimpanan peti kayu, tumpukan kayu gelondongan, dan meja dagang).

---

## 2. Layout Diagram ASCII (Proposed Master Layout)

```text
======================= PAGAR BELAKANG (Z = -17.0) =======================
|                                                                        |
|      [Hutan Pinus & Oak Peneduh Belakang Rumah - 18 Pohon]             |
|                                                                        |
|----------------------- DINDING RUMAH (Z = 5.0) -------------------------|
|                     ========================                           |
|                     |     RUMAH PEMAIN     |                           |
|  [Halaman Samping]  |    (Pusat Hunian)    |    [Halaman Samping]      |
|  (West Wing X:0-9)  |  X: 9.5 s/d 31.5     |    (East Wing X:32-42)    |
|                     ========================                           |
|----------------------- TERAS DEPAN (Z = 26.2) --------------------------|
|       [Lamp]                  [Door]                  [Lamp]           |
|                                 ||                                     |
|   === ZONA WORKSHOP ===         ||         === ZONA PRODUKSI ===       |
|   [Crafting Shelter]            ||             [Water Well]            |
|   (X: 8.5, Z: 27.5)             ||          (X: 24.0, Z: 31.5)         |
|   [Resource Crates & Table]     ||                  ||                 |
|                                 ||          ====================       |
|                                 ||          | 16 FARMLAND PLOTS|       |
|                                 ||          |   (4x4 Petak)    |       |
|                     === ZONA SOSIAL UTAMA ===  |   + Scarecrow    |    |
|                     |   CENTRAL CAMPFIRE    |  ====================    |
|                     |  (Plaza Api Unggun)   |  (X: 25.2 - 31.2)        |
|                     |   (X: 20.5, Z: 35.5)  |  (Z: 35.0 - 44.0)        |
|                     =========================           ||             |
|   === ZONA RELAKSASI ===        ||                      ||             |
|   [Natural Pond & Pier]         ||                      ||             |
|   (X: 9.5, Z: 42.0)             ||                      ||             |
|   [Lili Air, Batu & Bench]      ||                      ||             |
|          \\                     ||                     //              |
|           \\==================  ||  ==================//               |
|                                 ||                                     |
|                               [Gate]                                   |
|       [Lamp]            (X: 20.5, Z: 48.0)            [Lamp]           |
======================= PAGAR DEPAN (Z = 48.0) ===========================
```

---

## 3. Pembagian dan Penjelasan 4 Zona Fungsional

### Zona 1: Production Wing (40% Luas Halaman Depan)
- **Cakupan Spasial**: Sisi Kiri Halaman (`X: 24.0 s/d 40.0`, `Z: 30.0 s/d 46.0`).
- **Komponen**: 16 Petak `FarmlandPlot`, Pagar Pembatas `GardenEnclosure`, `Scarecrow`, dan `WaterWell`.
- **Konsep Desain**: 
  - Sumur air digeser ke bibir barat kebun (`X=24.0, Z=31.5`) dengan bak air menghadap langsung ke jalan setapak kebun.
  - Membentuk siklus agrikultur terpadu: Pemain menimba air dari sumur dan langsung melangkah 2 meter masuk ke petak kebun tanpa membuang waktu.
  - Patung orang-orangan sawah berdiri di gerbang masuk kebun sebagai penanda visual yang ikonik.

### Zona 2: Social Hub & Central Plaza (30% Luas Halaman Depan)
- **Cakupan Spasial**: Pusat Poros Pekarangan (`X: 16.0 s/d 25.0`, `Z: 31.0 s/d 40.0`).
- **Komponen**: `RelaxationArea_Campfire` (Tungku api unggun batu, 4 bangku kayu melingkar, tunggul pemotong kayu/axe, tumpukan kayu bakar, dan peti perbekalan).
- **Konsep Desain**:
  - Diletakkan tepat di tengah garis simetri antara Gerbang Utama dan Pintu Rumah (`X=20.5, Z=35.5`).
  - Menjadi **Primary Focal Point** begitu pemain masuk ke homestead. Cahaya api hangat, pendar partikel asap, dan siluet melingkar menyambut kedatangan pemain.
  - Berfungsi ganda sebagai *round-about* sirkulasi: persimpangan alami untuk membelokkan langkah ke kebun (kiri), kolam (kanan depan), atau workshop (kanan belakang).

### Zona 3: Relaxation & Fishing Sanctuary (20% Luas Halaman Depan)
- **Cakupan Spasial**: Sayap Kanan Depan (`X: 5.0 s/d 16.0`, `Z: 38.0 s/d 46.0`).
- **Komponen**: `NaturalPond` (Kolam air alami, tanaman teratai/lotus, tepian batu kali, dermaga kayu mini), dipadukan dengan bangku santai kayu dan semak berbunga (`Bush_Stylized`).
- **Konsep Desain**:
  - Kolam digeser ke sudut kanan depan (`X=9.5, Z=42.0`) dengan rotasi 45°, sehingga dermaga kayu mengarah ke jalan setapak utama.
  - Menciptakan sudut santai (zen corner) yang damai dan asri, terpisah dari hiruk-pikuk area kerja. Air memantulkan pendar lampu taman di malam hari.

### Zona 4: Workshop & Logistics Outpost (10% Luas Halaman Depan)
- **Cakupan Spasial**: Sayap Kanan Belakang (`X: 5.0 s/d 14.0`, `Z: 24.0 s/d 32.0`).
- **Komponen**: `CraftingShelter`, Meja Dagang Luar (`MerchantTable_Outdoor`), Tumpukan Peti Kayu (`Crates/Chests`), dan Tumpukan Kayu Gelondongan (`Firewood Logs`).
- **Konsep Desain**:
  - Digeser ke arah barat menjauhi pintu teras (`X=8.5, Z=27.5`) dengan orientasi menghadap ke arah pekarangan timur.
  - Fasad depan rumah kini bersih dan megah. Area ini berfungsi sebagai bengkel pertukangan dan depo logistik untuk menyimpan hasil jarahan dan material tempaan.

---

## 4. Tabel Relokasi Objek (Koordinat Lama → Koordinat Baru)

| Nama GameObject / Prefab | Posisi Lama (X, Y, Z) | Posisi Baru (X, Y, Z) | Rotasi Baru (Y) | Alasan Level Design |
| :--- | :--- | :--- | :--- | :--- |
| **`RelaxationArea_Campfire`** | `(14.5, 0.0, 36.5)` | `(20.5, 0.0, 35.5)` | `0°` | Dijadikan Focal Point Sentral di sumbu utama pekarangan. |
| **`NaturalPond`** | `(12.5, 0.01, 43.5)` | `(9.5, 0.01, 42.0)` | `45°` | Mengisi sudut kanan depan; dermaga menghadap ke arah jalan setapak. |
| **`CraftingShelter`** | `(11.5, 0.0, 29.5)` | `(8.5, 0.0, 27.5)` | `90°` | Membuka pandangan fasad rumah; membentuk kluster workshop terdedikasi. |
| **`WaterWell`** | `(26.5, 0.0, 31.0)` | `(24.0, 0.0, 31.5)` | `-90°` | Terintegrasi langsung di bibir kebun untuk loop penyiraman efisien. |
| **`Scarecrow`** | `(23.5, 0.0, 39.5)` | `(24.5, 0.0, 39.5)` | `-90°` | Menjadi ikon penyambut di gapura masuk kebun. |
| **`GardenEnclosure`** | `(24.1, 0.0, 35.8)` | `(25.0, 0.0, 35.8)` | `0°` | Penyesuaian margin pagar kebun terhadap jalan setapak. |
| **`Farmland Plots (1 s/d 16)`** | `X: 25.2 - 31.2, Z: 35.0 - 44.0` | Tetap stabil | `0°` | Struktur grid 4x4 optimal; diselaraskan dengan batas pagar baru. |
| **`CobblestonePathways`** | Jalur zig-zag acak | Jalur bercabang simetris-organik | - | Mengalir mulus: Gate → Campfire Plaza → Teras Rumah + Cabang ke Kebun/Kolam/Workshop. |
| **`OutdoorGardenLamps (11x)`** | Berjejer rapat di tengah jalan | Menyebar di setiap simpul zona | - | Menyinari simpul interaktif & memandu arah di malam hari (*wayfinding*). |

---

## 5. Rencana Injeksi Objek Baru (Environmental Storytelling Props)

Untuk membuat pekarangan terasa "hidup, terawat, dan nyata" (*lived-in believable homestead*), elemen modular berikut akan ditambahkan:

1. **Depo Logistik & Perdagangan (Dekat Crafting Shelter)**:
   - `MerchantTable_Outdoor`: Meja kayu beratap/kain runner (`X=12.5, Z=27.5`) untuk display barang dagangan.
   - `Supply Crates Stack`: 3 peti kayu bertumpuk di sisi shelter (`X=7.0, Z=26.5`) menandakan stok material crafting.
2. **Area Relaksasi Kolam (Pond Oasis)**:
   - `Wooden Rest Bench`: 1 bangku santai kayu di tepi kolam (`X=13.0, Z=44.0`) menghadap air dan teratai.
   - `Riverstone & Shrub Cluster`: Semak peneduh (`Bush_Stylized`) di sisi pagar untuk melembutkan sudut pekarangan.
3. **Penyimpanan Kayu Bakar (Dekat Campfire & Shelter)**:
   - `Firewood Stack`: Tumpukan kayu gelondongan rapi untuk cadangan perapian.

---

## 6. Analisis Visual Hierarchy & Komposisi Kamera

1. **First Impression (Saat Masuk Gerbang Utama Z=48)**:
   - **Foreground**: Gapura kayu dengan sepasang lampu pilar bercahaya hangat.
   - **Midground (Primary Focal Point)**: `Central Campfire Plaza` di tengah lapangan hijau dengan pendar api oranye, dikelilingi jalan batu melingkar.
   - **Background**: Fasad rumah pemain yang kokoh dengan pintu berlampu lentera, dibingkai pepohonan rimbun di latar belakang.
2. **Keseimbangan Sayap Kiri dan Kanan**:
   - Sayap Kiri (Kebun & Sumur) memiliki karakter fungsional-geometris (kotak petak coklat & atap sumur).
   - Sayap Kanan (Kolam & Workshop) memiliki karakter organik-natural (lingkaran air biru & gubuk kayu berkanopi).
   - Kedua sayap memiliki bobot visual (*visual weight*) yang seimbang tanpa ada sisi yang tampak kosong atau terlalu padat.
3. **Camera Occlusion**:
   - Tidak ada pohon tinggi di depan rumah (semua pohon berada di belakang dan samping).
   - Atap gubuk crafting dan sumur tidak menghalangi pintu masuk rumah dari sudut pandang kamera top-down/isometrik (Elevasi 45°-60°).

---

## 7. Analisis Gameplay Flow (Daily Loop Pemain)

Alur loop harian pemain dirancang tanpa langkah mubazir (*zero wasted steps*):

```text
[PAGI HARI / DAWN]
Keluar Rumah (Z=26.2) 
      ↓ (5 meter)
Ambil Air di Sumur (X=24.0, Z=31.5)
      ↓ (2 meter)
Siram & Panen 16 Petak Kebun (X:25-31, Z:35-44)
      ↓ (Melintas Campfire Plaza ke arah Barat)
Olah Hasil Panen & Crafting Senjata di Workshop (X=8.5, Z=27.5)
      ↓ (Lurus ke Utara)
Keluar Lewat Gerbang Utama (Z=48.0) untuk Bertualang / Menghadapi Wave

[MALAM HARI / DUSK]
Masuk Lewat Gerbang Utama (Z=48.0)
      ↓ (Lurus ke Selatan)
Singgah di Central Campfire (X=20.5, Z=35.5) -> Masak & Pulihkan Stamina
      ↓
Memancing / Bersantai di Kolam (X=9.5, Z=42.0)
      ↓
Simpan Loot di Peti Workshop & Masuk ke Rumah untuk Tidur
```

---

## 8. Langkah Eksekusi Teknis (Siap Dijalankan Setelah Persetujuan)

1. **Step 1 - Relokasi Campfire**: Pindahkan `RelaxationArea_Campfire` ke `(20.5, 0.0, 35.5)`.
2. **Step 2 - Relokasi Sumur & Kebun**: Geser `WaterWell` ke `(24.0, 0.0, 31.5)` dan luruskan pagar `GardenEnclosure`.
3. **Step 3 - Relokasi Kolam**: Pindahkan `NaturalPond` ke `(9.5, 0.01, 42.0)` dengan rotasi `45°`.
4. **Step 4 - Relokasi Workshop**: Pindahkan `CraftingShelter` ke `(8.5, 0.0, 27.5)` dengan rotasi `90°` dan pasang meja display/peti logistik.
5. **Step 5 - Rekonfigurasi CobblestonePathways**: Rangkai ulang batu pijakan membentuk koridor arteri utama dan cabang-cabang fungsional yang mulus.
6. **Step 6 - Penataan Lighting**: Sebarkan 11 unit `GardenLamp` di titik-titik simpul wayfinding.
7. **Step 7 - Verifikasi Visual & Validasi**: Cek di scene view, pastikan 0 error, dan simpan.

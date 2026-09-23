# Adaptive Probe Volumes (APV) & Sky Occlusion Baking Guide (Unity 6 URP)

Panduan teknis konfigurasi, pemanggangan (*baking*), dan optimasi memori VRAM untuk **Adaptive Probe Volumes (APV)** dengan **Sky Occlusion** pada game hibrida Farming Simulation dan Action Survival **Farm-Beware** di Unity 6000.3.20f1 URP.

---

## 1. Konsep & Arsitektur APV di Unity 6

### Mengapa Menggunakan APV (Bukan Light Probes Legacy)?
1. **Penyimpanan Berbasis Bricks & 3D Texture**: APV mengelompokkan light probes ke dalam struktur *Bricks* ($4 \times 4 \times 4$ probes) yang dialokasikan dalam 3D Texture Atlas di GPU VRAM. Hal ini memungkinkan sampling *hardware trilinear filtering* secepat kilat tanpa interpolasi tetangga manual di CPU.
2. **Streaming & Memory Budget Terkontrol**: Berbeda dengan Light Probes biasa yang memuat seluruh probe ke RAM, APV hanya mengunggah volume di sekitar kamera ke GPU VRAM sesuai batas *Memory Budget* yang dialokasikan di URP Asset.
3. **Sky Occlusion Terintegrasi**: Memungkinkan probe menyimpan faktor oklusi langit secara terpisah. Ketika warna ambient atau matahari berubah dari siang (5500K) ke senja (3200K), pantulan cahaya difus langit outdoor langsung bereaksi dinamis secara real-time tanpa perlu membakar ulang (*re-bake*) pantulan statis.

---

## 2. Prasyarat Konfigurasi URP Asset

Pastikan `PC_RPAsset.asset` telah mengaktifkan sistem APV:
1. Buka file asset `Assets/Settings/PC_RPAsset.asset` di Inspector.
2. Pada bagian **Lighting** $\rightarrow$ **Light Probe System**:
   - Pilih: **Adaptive Probe Volumes** (Value `1`).
3. Pada parameter APV Memory:
   - **Probe Volume Memory Budget**: Atur ke **1024 MB** (untuk platform target PC menengah-atas).
   - **Probe Volume Blending Memory Budget**: **256 MB**.
   - **Support Probe Volume GPU Streaming**: Centang aktif (`True`).
   - **Probe Volume SH Bands**: **Spherical Harmonics L1** (cepat & hemat) atau **L2** (kualitas difus kaya untuk tanaman).

---

## 3. Alokasi Hierarki Kepadatan Proksi (Hierarchical Density Allocation)

> [!WARNING]
> **Peringatan VRAM**: Jangan pernah membiarkan subdivisi densitas APV dipanggang secara seragam padat di seluruh pulau/level. Memanggang area perifer luar dengan resolusi padat akan menyia-nyiakan VRAM GPU untuk ruang kosong yang tidak terlihat dekat oleh kamera.

Struktur hierarki probe terbagi menjadi 2 tier:

```
┌──────────────────────────────────────────────────────────────┐
│  Tier 1: Global Coarse Volume (Perifer / Batas Luar Peta)    │
│  - Jarak Antar Probe: 3.0m – 5.0m (Subdivision Level 0 - 1)  │
│                                                              │
│        ┌──────────────────────────────────────────────┐      │
│        │  Tier 2: High-Density Farming Core Volume    │      │
│        │  - Jarak Probe: 0.5m – 1.0m (Level 2 - 3)    │      │
│        │  - Kontak bayangan tajam pada bedengan tanah, │      │
│        │    tanaman bertumbuh, pagar, & interior.     │      │
│        └──────────────────────────────────────────────┘      │
└──────────────────────────────────────────────────────────────┘
```

### Langkah Menata Komponen Volume di Scene:
1. **Tier 1 (Global Outer Bounds)**:
   - Buat GameObject di scene: `[LIGHTING]/APV_Global_Coarse`.
   - Tambahkan komponen **`Probe Volume`**.
   - Atur **Mode**: `Global Volume` (atau `Local` melingkupi seluruh pulau).
   - Atur **Min Subdivision**: `Level 0` (probe spacing ~4.0 meter).
   - Atur **Max Subdivision**: `Level 1` (probe spacing ~2.0 meter).
2. **Tier 2 (Lahan Pertanian & Area Rumah)**:
   - Buat GameObject anak: `[LIGHTING]/APV_Farming_Dense`.
   - Tambahkan komponen **`Probe Volume`**.
   - Atur **Mode**: `Local`.
   - Sesuaikan *Box Bounds* tepat membatasi petak kebun aktif, rumah pemain, dan area kerja (workbench/stove).
   - Atur **Min Subdivision**: `Level 2` (probe spacing ~1.0 meter).
   - Atur **Max Subdivision**: `Level 3` (probe spacing ~0.5 meter).
   - Ini memastikan batil dan daun tanaman wortel/gandum memiliki oklusi difus yang kontras dan kontak visual yang kokoh dengan tanah.

---

## 4. Konfigurasi Pemanggangan Sky Occlusion

### Prosedur di Unity Editor (`Window > Rendering > Lighting`):
1. Buka jendela **Lighting** (`Window > Rendering > Lighting`).
2. Masuk ke tab **Scene**:
   - **Lightmapping Settings**:
     - *Lightmapper*: `Progressive GPU (Preview)` atau `Progressive CPU`.
     - *Direct Samples*: `32`.
     - *Indirect Samples*: `256` - `512`.
     - *Environment Samples*: `128`.
3. Masuk ke tab **Adaptive Probe Volumes**:
   - Centang opsi **Bake Sky Occlusion**.
   - Opsi ini menginstruksikan lightmapper untuk menghitung *visibility cone* ke kubah langit (*sky dome*) dari setiap titik probe.
   - Hasilnya, tanaman di bawah atap teras atau di balik dinding gudang akan memiliki nilai *Sky Occlusion* rendah (tidak terpengaruh cahaya langit langsung), sedangkan tanaman di tengah lahan terbuka menerima 100% reflektansi atmosfer langit.
4. Klik tombol **Generate Lighting**.

---

## 5. Menangani Artefak APV (Invalidated Probes / Leak Prevention)

1. **Probes di Dalam Objek Solid (Mesh Tanpa Interior)**:
   - Jika ada batu besar atau pohon tebal di mana probe berada di dalam geometri tertutup, probe dapat menjadi gelap pekat dan bocor ke luar.
   - Solusi: Pasang komponen **`Probe Adjustment Volume`** pada area tersebut, lalu pilih mode:
     - `Invalidate Probes`: Mematikan probe di dalam batu agar probe terdekat di luar yang digunakan.
     - `Virtual Offset`: Mendorong posisi sampling probe ke luar permukaan collider.
2. **Dilation Range**:
   - Pada pengaturan APV di Lighting Window, atur *Dilation Distance* ke `1.0m`. Dilation otomatis memperluas data probe valid ke probe yang sedikit terjebak di dalam geometri.

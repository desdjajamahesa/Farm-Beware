# Phase 4: Interiors — Testing Checklist

> Checklist pengujian komprehensif untuk validasi sistem interior bangunan: Dithered Alpha Clipping, HouseInteriorTrigger, dan Light Layers Segregation.

---

## Prasyarat Pengujian

- [ ] Unity Editor dalam mode **Play Mode** di scene staging (StagingScene atau scene rumah)
- [ ] Karakter pemain memiliki tag `"Player"` dan Collider aktif
- [ ] Directional Light utama telah ditetapkan ke **Light Layer 0** (Exterior)
- [ ] Lampu interior telah ditetapkan ke **Light Layer 1** (Interior)
- [ ] `Use Light Layers` telah diaktifkan di `PC_Renderer.asset`
- [ ] Material atap/dinding bangunan menggunakan shader `FarmBeware/Building/DitheredBuildingLit`
- [ ] `HouseInteriorTrigger` terpasang pada GameObject trigger di ambang pintu
- [ ] BoxCollider trigger berukuran tepat menutupi area ambang pintu

---

## 1. Dithered Alpha Clipping — Shader Validation

### 1.1 Material Inspector Test

| # | Langkah | Hasil yang Diharapkan | Status |
|---|---|---|---|
| 1 | Pilih material atap bangunan di Inspector | Shader menampilkan `FarmBeware/Building/DitheredBuildingLit` | ☐ |
| 2 | Atur `_DitherFade = 0.0` | Atap tampak **solid penuh** — tidak ada piksel yang dibuang | ☐ |
| 3 | Atur `_DitherFade = 0.5` | Atap tampak **setengah transparan** — pola checkerboard terlihat | ☐ |
| 4 | Atur `_DitherFade = 0.85` | Atap **sangat transparan** — interior terlihat jelas dari kamera | ☐ |
| 5 | Atur `_DitherFade = 1.0` | Atap **tembus pandang penuh** — seluruh piksel dibuang | ☐ |
| 6 | Kembalikan `_DitherFade = 0.0` | Atap kembali solid — tidak ada artefak visual | ☐ |

### 1.2 Shadow Consistency Test

| # | Langkah | Hasil yang Diharapkan | Status |
|---|---|---|---|
| 7 | Atur `_DitherFade = 0.0` | Bayangan atap solid penuh di pekarangan | ☐ |
| 8 | Atur `_DitherFade = 0.85` | Bayangan atap tetap terlihat (dithered) di pekarangan luar — bayangan **TIDAK** menghilang sepenuhnya | ☐ |
| 9 | Periksa bahwa tidak ada **Z-fighting** atau kedipan (flickering) pada batas dither | ☐ |

### 1.3 Depth Buffer Integrity

| # | Langkah | Hasil yang Diharapkan | Status |
|---|---|---|---|
| 10 | Tempatkan objek furnitur di bawah atap yang di-dither | Furnitur tampak di belakang atap yang tersisa — **tidak ada** sorting error | ☐ |
| 11 | Gerakkan karakter di bawah atap dither | Karakter **tidak** z-fight dengan piksel atap yang tersisa | ☐ |

---

## 2. HouseInteriorTrigger — Avatar Walkthrough

### 2.1 Transisi Masuk (OnTriggerEnter)

| # | Langkah | Hasil yang Diharapkan | Status |
|---|---|---|---|
| 12 | Gerakkan karakter mendekati ambang pintu rumah | Tidak ada perubahan visual — atap/dinding masih solid | ☐ |
| 13 | Gerakkan karakter **menembus** BoxCollider trigger | Transisi dither **halus** dimulai — atap/dinding memudar secara gradual | ☐ |
| 14 | Amati Console log | Muncul: `[HouseInteriorTrigger] Pemain masuk interior '...'` | ☐ |
| 15 | Tunggu transisi selesai (~0.4 detik) | Atap/dinding mencapai transparansi target (0.85) — interior terlihat jelas | ☐ |
| 16 | Verifikasi bahwa transisi **tidak** tersendat atau melompat tiba-tiba | Interpolasi SmoothStep menghasilkan kurva akselerasi-deselerasi yang mulus | ☐ |

### 2.2 Transisi Keluar (OnTriggerExit)

| # | Langkah | Hasil yang Diharapkan | Status |
|---|---|---|---|
| 17 | Gerakkan karakter **keluar** dari BoxCollider trigger | Transisi balik dimulai — atap/dinding kembali solid secara gradual | ☐ |
| 18 | Amati Console log | Muncul: `[HouseInteriorTrigger] Pemain keluar interior '...'` | ☐ |
| 19 | Tunggu transisi selesai | Atap/dinding kembali **solid penuh** (`_DitherFade = 0.0`) | ☐ |

### 2.3 Edge Cases

| # | Skenario | Hasil yang Diharapkan | Status |
|---|---|---|---|
| 20 | Masuk-keluar cepat berulang (spam) | Transisi di-interrupt dan restart — tidak crash, tidak stuck | ☐ |
| 21 | Masuk rumah saat transisi malam sedang berlangsung | Dither tetap berjalan independen dari transisi pencahayaan | ☐ |
| 22 | Keluar aplikasi saat di dalam rumah | Tidak ada error — `OnDestroy` mengembalikan dither ke 0 | ☐ |

---

## 3. Light Layers Segregation — Isolasi Cahaya

### 3.1 Siang Hari

| # | Skenario | Hasil yang Diharapkan | Status |
|---|---|---|---|
| 23 | Berdiri di **luar** rumah | Directional Light (Layer 0) menyinari tanah, tanaman, dinding luar dengan baik | ☐ |
| 24 | Masuk ke **dalam** rumah (dither aktif) | Interior gelap jika lampu interior padam — Directional Light Layer 0 **TIDAK** menyinari lantai dalam | ☐ |

### 3.2 Malam Hari

| # | Skenario | Hasil yang Diharapkan | Status |
|---|---|---|---|
| 25 | Tekan `N` untuk beralih ke malam | Directional Light redup ke moonlight biru (Layer 0) | ☐ |
| 26 | Berdiri di **luar** rumah, lihat pekarangan | Cahaya bulan biru dingin menerangi pekarangan — suasana malam terasa | ☐ |
| 27 | Masuk ke **dalam** rumah | Lantai interior diterangi lampu amber hangat (Layer 1) | ☐ |
| 28 | **KUNCI**: Verifikasi lantai interior **TIDAK** berwarna biru | Cahaya bulan (Layer 0) tidak menodai lantai (Layer 1) — segregasi berhasil | ☐ |
| 29 | Keluar rumah, lihat pekarangan | Cahaya lampu interior (Layer 1) **TIDAK** bocor ke tanah pekarangan | ☐ |

### 3.3 Audit via Tool

| # | Langkah | Hasil yang Diharapkan | Status |
|---|---|---|---|
| 30 | Buka `Tools → Farm-Beware → Rendering → Light Layer Assignment Utility` | Window terbuka tanpa error | ☐ |
| 31 | Klik "Audit Seluruh Light & Renderer di Scene" | Console menampilkan daftar lengkap Light dan Renderer beserta `renderingLayerMask` masing-masing | ☐ |
| 32 | Verifikasi: Directional Light → mask=1 | Hanya Layer 0 (Exterior) | ☐ |
| 33 | Verifikasi: Lampu interior → mask=2 | Hanya Layer 1 (Interior) | ☐ |

---

## 4. Performa — Frame Profiler

| # | Metrik | Threshold Aman | Pengukuran | Status |
|---|---|---|---|---|
| 34 | Frame Time (di luar rumah) | < 16.6ms (60 FPS) | ___ms | ☐ |
| 35 | Frame Time (di dalam rumah, dither aktif) | < 16.6ms (60 FPS) | ___ms | ☐ |
| 36 | Draw Call delta (masuk rumah vs. luar) | < +5 draw calls | ___dc | ☐ |
| 37 | GPU Time: Shadow pass | Tidak ada peningkatan signifikan dengan dither | ___ms | ☐ |

---

## 5. Hasil Pengujian

| Kategori | Jumlah Test | Lulus | Gagal | Catatan |
|---|---|---|---|---|
| Shader Validation | 11 | ___ | ___ | |
| Avatar Walkthrough | 11 | ___ | ___ | |
| Light Layers | 11 | ___ | ___ | |
| Performa | 4 | ___ | ___ | |
| **Total** | **37** | ___ | ___ | |

---

> [!NOTE]
> Setelah seluruh pengujian lulus, Phase 4: Interiors dinyatakan **tervalidasi**.
> Sistem interior siap untuk integrasi konten (furniture placement, interior decoration, NPC pathfinding dalam rumah).

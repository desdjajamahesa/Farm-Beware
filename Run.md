# Run.md

Panduan menjalankan, mengoperasikan, dan mengonfigurasi proyek **Farm-Beware** di lingkungan lokal dan Unity Editor.

---

## 1. Persyaratan Sistem & Lingkungan (Environment Prerequisites)

- **Unity Engine**: Unity 6000.3.20f1 (Unity 6 / 2023 LTS)
- **Render Pipeline**: Universal Render Pipeline (URP)
- **Target OS**: Windows 10 / 11 (64-bit)
- **Input Framework**: Unity New Input System (`com.unity.inputsystem`)
- **Text Rendering**: TextMeshPro (`com.unity.textmeshpro`)
- **Scripting Backend**: Mono / .NET Standard 2.1

---

## 2. Cara Menjalankan Game (Execution Workflow)

### 2.1 Membuka Proyek
1. Buka **Unity Hub**.
2. Klik **Add project from disk** dan arahkan ke folder:
   `C:\Users\HP\Rafi\MyProject\Farm-Beware`
3. Pastikan versi editor yang digunakan adalah **6000.3.20f1**.

### 2.2 Membuka Scene Utama
1. Di jendela *Project* Unity, buka folder:
   `Assets/Scenes/`
2. Dobel-klik file scene aktif:
   [`StagingScene.unity`](file:///c:/Users/HP/Rafi/MyProject/Farm-Beware/Assets/Scenes/StagingScene.unity)
3. Scene ini adalah lingkungan integrasi staging utama yang mencakup seluruh area:
   - Kamar Tidur (`Bedroom`)
   - Dapur (`Kitchen`)
   - Area Lemari & Piala (`Wardrobe & Trophy Cabinet`)
   - Area Pengujian (`Environment/Testing`)
   - Entitas Pemain & Kamera Utama (`Player & Main Camera`)
   - Canvas Antarmuka Pengguna (`Canvas_UI`)

### 2.3 Memulai Play Mode
- Tekan tombol **Play** di bagian atas Editor (atau pintasan keyboard `Ctrl + P`).

---

## 3. Kontrol Pemain & Pintasan Keyboard (Controls & Shortcuts)

| Tombol | Aksi | Konteks |
|---|---|---|
| `W, A, S, D` | Gerak Pemain | Gameplay |
| `Space` | Lompat (*Jump*) | Gameplay |
| `Left Shift` | Melesat (*Dash*) | Gameplay |
| `1, 2, 3, 4` | Pilih Slot Hotbar | Gameplay |
| `Scroll Wheel Mouse` | Ganti Slot Hotbar / Zoom Kamera | Gameplay |
| `Klik Kanan (Tahan + Drag)` | Putar Orbit Kamera Isometrik | Gameplay |
| `E` | Interaksi dengan Objek (Kasur, Kompor, Meja Cuci, Lemari, Peti) | Di dekat objek interaktif |
| `Tab` atau `I` | Buka / Tutup Panel Inventory Pemain | Gameplay |
| `ESC` | Tutup Panel Aktif (Prioritas 1) / Buka Pause Menu (Prioritas 2) | Kapan saja |
| `N` *(Debug)* | Lompat Langsung ke Fase Malam (*Skip to Night*) | Pengujian siklus waktu kasur |

---

## 4. Integrasi Unity MCP Bridge (Autonomous Tools)

Pengembangan proyek ini didukung oleh server Unity MCP yang terhubung langsung ke Editor:
- **Kompilasi & Domain Reload**:
  Trigger via MCP tool `refresh_unity` dengan parameter `compile="request"`.
- **Eksekusi Roslyn C# In-Memory**:
  Gunakan tool `execute_code` untuk menjalankan pengujian runtime atau memvalidasi scene tanpa perlu membuat skrip sementara di aset.
- **Monitoring Console Log**:
  Gunakan tool `read_console` (filter: `error`, `warning`) untuk memastikan stabilitas bebas bug setiap kali terjadi modifikasi.

---

## 5. Lokasi Objek Penting di Hierarchy (`StagingScene`)

- **Player**: `Player` (memiliki `PlayerControl`, `PlayerStats`, `InventoryComponent`, `PlayerEquipment`, `PlayerOutfit`).
- **Camera Controller**: `Main Camera` (memiliki `IsometricCameraController`, `CameraManager`, `WallOcclusionManager`).
- **Peti Pengujian Item**: `Environment/Testing/TestChest` (berisi 21 item pengujian lengkap dari seluruh kategori MVP).
- **Stasiun Memasak**: `Environment/Kitchen/stove` (memiliki `GenshinStove` dan pemicu UI memasak).
- **Bak Cuci**: `Environment/Kitchen/kitchen_sink` (memiliki `KitchenSinkInteractable` untuk mencuci bahan makanan).
- **Kulkas**: `Environment/Kitchen/refrigerator` (memiliki `RefrigeratorInteractable` untuk penyimpanan dingin).
- **Cermin Kamar Tidur**: `Environment/Bedroom/Mirror` (memiliki komponen `MirrorCamera`).

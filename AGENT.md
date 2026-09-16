# AGENT.md

Panduan utama bagi Autonomous AI Agent dan developer dalam mengoperasikan, mengembangkan, dan memodifikasi codebase proyek **Farm-Beware**.

---

## 1. Identitas & Konteks Proyek
- **Nama Proyek**: Farm-Beware
- **Engine**: Unity 6000.3.20f1 (Unity 6 / 2023 LTS equivalent)
- **Genre**: 3D Isometric Farming, Cooking, and Night Brawl Game
- **Bridge Tooling**: Unity MCP (Model Context Protocol). Agent mampu dan wajib memantau hierarki scene, mengeksekusi C# Roslyn in-memory (`execute_code`), dan menarik log Unity console (`read_console`) secara mandiri.
- **Standar Kode**: Ditujukan untuk standar Senior Engineer dengan prinsip OOP yang ketat, arsitektur data-driven, dan pemisahan murni antara logic dan rendering.

---

## 2. Aturan Baku Operasional Agent (Operational Laws)

### 2.1 Kedisiplinan Eksekusi & Token
- Berikan output yang terstruktur, padat, dan teknis tanpa basa-basi (*no preamble/postamble unnecessary apologies*).
- Gunakan format link file standar `path/File.cs:LINE` untuk referensi kode.
- Terapkan *single-concern edits*: jangan lakukan refactor liar (*drive-by refactors*) di luar cakupan tugas.

### 2.2 Larangan Script Setup Editor Otomatis (Non-Negotiable)
- ❌ **DILARANG KERAS** membuat script otomatisasi sementara seperti `Assets/Editor/*Setup*.cs` atau `[MenuItem("Farm Beware/...")]` untuk memodifikasi scene secara buta.
- Skrip semacam ini terbukti menyebabkan duplikasi komponen fatal (misalnya menduplikasi controller kamera atau menimpa referensi inspector).
- ✅ **Gunakan MCP Langsung**: Lakukan inspeksi dan modifikasi scene via MCP tools (`execute_code`, `manage_scene`, `manage_gameobject`, `manage_components`).

### 2.3 Protokol Self-Healing (MCP-First)
- Jangan meminta pengguna menempelkan error console atau screenshot jika bisa diakses melalui tool:
  1. Pantau error/warning secara real-time via `read_console` (filter: `error`).
  2. Identifikasi akar masalah (*root cause*) hingga ke baris spesifik.
  3. Lakukan patch minimalis dan tepat sasaran.
  4. Trigger refresh Unity via `refresh_unity` (compile: `request`).
  5. Verifikasi ulang console log hingga 0 error.

### 2.4 Arsitektur Pemisahan Logika Murni (Pure Logic Law)
- Pisahkan logika perhitungan murni dari `MonoBehaviour`.
- Logika murni (matematika grid isometrik, formula buff, kalkulasi inventory, validasi resep, sorting order) wajib berupa plain C# classes / POCO tanpa dependensi Unity lifecycle.
- `MonoBehaviour` hanya bertindak sebagai **Thin Adapter**: menangani event input, Update/LateUpdate pumping, dan pemanggilan API Unity engine.

### 2.5 Higienitas File & Cache Engine
- ❌ **JANGAN PERNAH** membaca, mencari, atau meng-indeks folder cache engine:
  `Library/`, `Temp/`, `Obj/`, `Logs/`, `UserSettings/`, `Builds/`, `.vs/`.
- ❌ Jangan pernah merusak file `.meta` tanpa sinkronisasi GUID. Ketika memindahkan aset Unity, gunakan `AssetDatabase.MoveAsset`.

---

## 3. Protokol Git & Aturan Commit (SOP Baku)

Berdasarkan arsitektur Git 3-Layer yang ditetapkan di repositori ini:

### 3.1 Struktur Lapisan Branch
1. **Layer 1 (Tech Lead)**: `main` (Production) & `staging` (Testing)
2. **Layer 2 (Team Lead)**: `development` / `dev` (Integrasi Utama)
3. **Layer 3 (Programmer/Agent)**: `<nama_programmer>` (misal: `rafi-branch`). **Area kerja eksklusif Agent.**

### 3.2 Aturan Keras Branching
- Agent **DILARANG KERAS** membuat branch baru (`git checkout -b feature/...` dilarang).
- Agent **DILARANG KERAS** melakukan push langsung ke `main`, `staging`, atau `development`.
- Target operasi dan push **HANYA** branch Layer 3 milik programmer (`origin/rafi-branch`).

### 3.3 Konvensi Commit Atomik
Setiap commit harus atomik dan mengikuti format:
`<Tipe_Commit> : <Deskripsi Singkat dan Jelas>`

| Tipe Commit | Penggunaan | Contoh |
|---|---|---|
| `feat (100%)` | Fitur baru selesai penuh dan telah terverifikasi | `feat (100%) : implement genshin cooking UI panel` |
| `progress` | Snapshot pekerjaan berjalan (WIP) untuk backup | `progress : 50% wardrobe item reorganization` |
| `fix` | Perbaikan bug atau null reference | `fix : resolve ESC key pause conflict on UI panels` |
| `refactor` | Restrukturisasi kode tanpa mengubah output fungsional | `refactor : move wardrobe scripts to feature directory` |
| `chore` | Pemeliharaan sistem, folder, atau dokumentasi | `chore : update architecture documentation and clean obsolete assets` |
| `assets` | Penambahan/penghapusan resource visual, audio, 3D | `assets : import food icons into project resources` |

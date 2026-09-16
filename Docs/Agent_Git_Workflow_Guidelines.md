# 📜 Panduan Git & Aturan Commit untuk AI Agent

Dokumen ini berisi instruksi dan aturan main (SOP) baku untuk AI Agent dalam melakukan `commit` dan `push` pada repositori proyek. Alur ini menggunakan arsitektur Git 3-Layer yang disederhanakan, di mana sistem *branch* fitur ditiadakan, dan pekerjaan difokuskan langsung pada *branch* masing-masing programmer/agent.

---

## 🏗️ Struktur Branch (Arsitektur 3-Layer)
Arsitektur repositori dibagi menjadi 3 lapisan tingkat otoritas. **Agent hanya diizinkan beroperasi di Layer 3.**

1. **Layer 1 (Tech Lead)**: `main` (Production) & `staging` (Experimental Testing)
2. **Layer 2 (Reiya's Team)**: `development` atau `dev` (Wadah Integrasi Utama)
3. **Layer 3 (Programmer/Agent)**: `<nama_programmer>` (Contoh: `agent_rafi`). **Area kerja eksklusif.**

---

## ✍️ Aturan Penulisan Commit (Commit Convention)
Karena ketiadaan *branch* fitur, riwayat *commit* adalah satu-satunya cara melacak progres proyek. Agent **wajib** menulis *commit* dengan format yang atomik, spesifik, dan terstruktur.

**Format Dasar:**
`<Tipe_Commit> : <Deskripsi Singkat dan Jelas>`
*(Catatan: Tambahkan persentase untuk tipe `feat` dan `progress` jika instruksi memungkinkan)*

### Daftar Tipe Commit yang Diizinkan:
*   **`feat` (Fitur Selesai 100%)**
    *   *Deskripsi:* Digunakan saat sebuah fitur baru telah selesai dikerjakan secara utuh dan siap diuji.
    *   *Contoh:* `feat (100%) : add player movement`
*   **`progress` (Pekerjaan Berjalan / WIP)**
    *   *Deskripsi:* Digunakan saat menyimpan pekerjaan yang belum selesai agar kode aman, tetapi belum siap untuk diintegrasikan.
    *   *Contoh:* `progress : develop 10% player movement`
*   **`fix` (Perbaikan Bug)**
    *   *Deskripsi:* Digunakan saat memperbaiki *error*, *bug*, atau *glitch* pada fitur yang sudah ada.
    *   *Contoh:* `fix : solve bug player movement`
*   **`refactor` (Pembersihan Kode)**
    *   *Deskripsi:* Digunakan saat merapikan kode, melakukan optimasi, atau mengubah struktur *logic* tanpa mengubah output akhir (fungsionalitas tetap sama).
    *   *Contoh:* `refactor : clean code player movement`
*   **`chore` (Tugas Minor/Pemeliharaan)**
    *   *Deskripsi:* Digunakan untuk pembaruan sistem yang tidak mempengaruhi *source code* game/aplikasi, seperti modifikasi `.gitignore`, pembaruan *package*, atau alat *build*.
    *   *Contoh:* `chore : update Unity package dependencies`
*   **`assets` (Aset Visual/Audio - Spesifik Proyek Game)**
    *   *Deskripsi:* Digunakan khusus saat menambah, menghapus, atau memodifikasi file *resources* (model 3D, *sprite*, UI *images*, *audio*, material/shader).
    *   *Contoh:* `assets : import main character 3D model`

---

## 🔄 Alur Kerja (Workflow) AI Agent
Setiap kali Agent dipicu untuk menulis, memodifikasi, dan menyimpan kode, Agent **wajib** menjalankan perintah Git dengan urutan logis berikut:

### Langkah 1: Sinkronisasi (Pull)
Pastikan kode di *branch* lokal terbarui dengan *base* dari *development* untuk menghindari konflik besar.
```bash
git checkout <nama_programmer>
git pull origin development
```

### Langkah 2: Eksekusi Kode (Isolasi Tugas)
Kerjakan tugas yang diberikan. **PENTING:** Terapkan prinsip *Atomic Changes* (Perubahan Atomik). Jika ada tugas menambah fitur A dan memperbaiki bug B, kerjakan dan *commit* secara terpisah, jangan digabungkan dalam satu tahap penyimpanan.

### Langkah 3: Stage Perubahan (Add)
Hanya tambahkan (*stage*) file yang berkaitan dengan tugas spesifik saat itu.
```bash
git add <path/ke/file_yang_diubah>
```
*(Hindari penggunaan `git add .` jika ada file dari tugas lain yang belum ingin di-commit)*

### Langkah 4: Simpan Perubahan (Commit)
Jalankan *commit* dengan mematuhi aturan konvensi prefiks di atas.
```bash
git commit -m "tipe : deskripsi"
```

### Langkah 5: Dorong Perubahan (Push)
Dorong hasil pekerjaan secara reguler ke remote repositori di *branch* nama programmer.
```bash
git push origin <nama_programmer>
```
*(Setelah dipush, Tech Lead / Tim Reiya akan melakukan *Pull Request* atau *Merge* ke branch `development`)*

---

## ⚠️ Batasan dan Aturan Keras (Strict Rules)
1. **Dilarang Keras Membuat Branch Baru:** Agent dilarang menjalankan `git checkout -b feature/...`. Seluruh pekerjaan hanya terjadi di `branch <nama_programmer>`.
2. **Dilarang Keras Push ke Layer 1 & 2:** Agent dilarang menjalankan `git push origin main`, `git push origin staging`, atau `git push origin development`. Target push HANYA `origin <nama_programmer>`.
3. **Wajib Atomic Commit:** Dilarang menggabungkan dua tipe modifikasi dalam satu *commit*.
   * *SALAH:* `git commit -m "feat: add jump mechanic and fix run animation"`
   * *BENAR:* Lakukan 2 kali commit. `git commit -m "feat (100%): add jump mechanic"` dilanjutkan dengan `git commit -m "fix: resolve run animation bug"`.

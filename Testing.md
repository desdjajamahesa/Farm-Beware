# Testing.md

Strategi pengujian, skrip verifikasi otomatis, skenario Play Mode manual, dan diagnosis masalah pada proyek **Farm-Beware**.

---

## 1. Strategi & Filosofi Pengujian (Testing Philosophy)

Pengujian dalam Farm-Beware berfokus pada **Zero-Regression & Data Integrity**:
1. **MCP Automated Verification (Editor Mode)**: Menggunakan tool MCP `execute_code` untuk menguji integritas database, relasi komponen, dan struktur scene tanpa memerlukan Play Mode penuh.
2. **Console Log Sanity Check**: Setiap perubahan kode wajib menghasilkan **0 Error** dan **0 Warning** pada console Unity via tool `read_console`.
3. **Play Mode Manual Verification**: Pengujian fisika Rigidbody, pergerakan karakter, transisi kamera, dan interaksi UI (drag & drop serta tombol ESC).

---

## 2. Skrip Pengujian Otomatis (MCP C# Verification)

Skrip-skrip berikut dapat langsung dieksekusi melalui tool MCP `execute_code` (compiler: `roslyn`, `safety_checks: false`):

### 2.1 Verifikasi Integritas ItemDatabase
Memastikan seluruh 33 item terdaftar dengan benar tanpa ada referensi aset yang hilang/null:

```csharp
var db = ItemDatabase.Instance;
if (db == null) return "FAIL: ItemDatabase tidak dapat dimuat!";

var items = db.GetAllItems();
int count = items != null ? items.Count : 0;
int nullCount = 0;
var categories = new System.Collections.Generic.Dictionary<string, int>();

foreach (var item in items)
{
    if (item == null) { nullCount++; continue; }
    string cat = item.category.ToString();
    if (!categories.ContainsKey(cat)) categories[cat] = 0;
    categories[cat]++;
}

var result = $"Total Item: {count} (Null: {nullCount})\n";
foreach (var kvp in categories) result += $" - {kvp.Key}: {kvp.Value} items\n";
return result;
```

### 2.2 Verifikasi Peti Pengujian (`TestChest`)
Memastikan `TestChest` di scene memiliki komponen penyimpanan yang valid dan berisi 21 item testing lengkap:

```csharp
var chest = GameObject.Find("TestChest");
if (chest == null) return "FAIL: TestChest tidak ditemukan di scene!";

var inv = chest.GetComponent<InventoryComponent>();
int filledSlots = 0;
for (int i = 0; i < inv.slots.Count; i++)
{
    if (inv.slots[i] != null && inv.slots[i].item != null) filledSlots++;
}

var storage = chest.GetComponent<FeaturesInteraction.StorageInteractable>();
int nullDrops = storage.lootTable.FindAll(d => d.item == null).Count;

return $"PASS: TestChest terisi {filledSlots}/24 slot item testing. Null drops: {nullDrops}.";
```

### 2.3 Verifikasi Komponen Kamera & Player
Memastikan `CameraManager`, `IsometricCameraController`, dan komponen pemain terpasang tanpa *missing script*:

```csharp
var camMgr = UnityEngine.Object.FindAnyObjectByType<FeaturesCamera.CameraManager>();
var isoCam = UnityEngine.Object.FindAnyObjectByType<FeaturesCamera.IsometricCameraController>();
var player = GameObject.FindWithTag("Player");

return $"CameraManager: {(camMgr != null ? "OK" : "MISSING")}\n" +
       $"IsometricCameraController: {(isoCam != null ? "OK" : "MISSING")}\n" +
       $"Player: {(player != null ? "OK" : "MISSING")}";
```

---

## 3. Skenario Pengujian Manual Play Mode (Manual Test Checklist)

### 3.1 Transisi Kamera & Prioritas Tombol ESC
1. Dekati Lemari Pakaian (`Wardrobe`), tekan `E`.
   - Kamera harus beralih ke sudut pandang Wardrobe, input gerak terkunci, dan kursor mouse muncul.
2. Tekan tombol `ESC`.
   - Panel Wardrobe harus tertutup, kamera kembali ke `Gameplay`, input gerak aktif kembali.
   - **PENTING**: Menu Pause **TIDAK BOLEH** terbuka saat menutup panel dengan ESC.
3. Saat tidak ada panel yang terbuka, tekan `ESC`.
   - Menu Pause (`PauseMenuUI`) harus terbuka dengan benar.

### 3.2 Alur Memasak Genshin-Style (`GenshinStove`)
1. Ambil bahan masakan dari `TestChest` (misal: `Crop_SweetPotato`, `Mat_CookingOil`, dll.).
2. Dekati kompor (`stove`), tekan `E`.
   - Panel memasak harus terbuka dengan tata letak simetris Single Central Axis.
3. Pilih resep yang bahannya mencukupi di inventori.
   - Tombol "MASAK!" harus aktif (berwarna hijau).
4. Klik tombol "MASAK!".
   - Bahan harus langsung terpotong dari inventory, dan hasil masakan masuk ke tas pemain.

### 3.3 Pencucian Bahan Makanan (`KitchenSink`)
1. Letakkan item kotor (misal: sayur yang memiliki `isDirty = true`) ke dalam bak cuci.
2. Proses pencucian harus berjalan otomatis.
3. Setelah selesai, item di slot harus berubah menjadi varian bersihnya (`cleanVariant`).

### 3.4 Skala & Proporsi UI
1. Buka seluruh panel interaktif (`Inventory`, `Stove`, `Sink`, `Chest`).
2. Pastikan teks label dan ikon terbaca dengan jelas pada resolusi 1920×1080 maupun resolusi lainnya tanpa ada teks yang terpotong (*overlap*) atau terlalu kecil.

---

## 4. Matriks Diagnosis Masalah Umum (Troubleshooting Matrix)

| Gejala Masalah | Kemungkinan Penyebab | Tindakan Perbaikan |
|---|---|---|
| Teks TextMeshPro tampak turun ke bawah / terpotong | Nilai `m_VerticalAlignment` pada file YAML prefab tertimpa angka 4608 | Edit YAML prefab secara direct: ubah `m_VerticalAlignment: 4608` menjadi `m_VerticalAlignment: 512` (Midline) |
| Kamera terkunci atau pemain tidak bisa bergerak setelah keluar UI | `CameraManager.CurrentMode` belum kembali ke `Gameplay` | Pastikan event penutupan UI memanggil `CameraManager.Instance.SetMode(CameraMode.Gameplay)` |
| Muncul peringatan "There are 2 audio listeners in the scene" | Kamera cermin (`MirrorCamera`) memiliki AudioListener aktif | Matikan komponen AudioListener pada kamera cermin (`audioListener.enabled = false`) |
| Tombol ESC langsung memunculkan Pause Menu saat panel terbuka | Skrip UI mendengarkan tombol ESC bersamaan dengan PauseMenuUI | Terapkan pengecekan modal: PauseMenuUI hanya bereaksi jika tidak ada panel aktif |
| Item hilang saat dipindahkan antar inventory | Pengecekan `CanAcceptItem` menolak tipe item (misal: Trophy di Player) | Verifikasi flag `blockTrophyItems` dan `allowedFoodCategories` pada komponen target |

# Architecture.md

Desain sistem dan arsitektur teknis menyeluruh dari proyek **Farm-Beware**.

---

## 1. Ikhtisar Arsitektur Inti (High-Level Overview)

Farm-Beware dibangun di atas **Unity 6000.3.20f1** menggunakan **Universal Render Pipeline (URP)**, **New Input System (`UnityEngine.InputSystem`)**, dan **TextMeshPro (TMP)**.

Arsitektur proyek mengusung prinsip **Feature-Based Modular Architecture** di mana setiap domain permainan memiliki direktori mandiri di bawah `Assets/Scripts/Features/` yang memisahkan logika backend murni dari adapter presentasi (`MonoBehaviour`).

```
                              ┌─────────────────────────────┐
                              │     GameInitializer         │
                              └──────────────┬──────────────┘
                                             │
               ┌─────────────────────────────┼─────────────────────────────┐
               ▼                             ▼                             ▼
    ┌──────────────────────┐      ┌──────────────────────┐      ┌──────────────────────┐
    │    CameraManager     │      │     TimeManager      │      │     ItemDatabase     │
    │ (State Machine Cam)  │      │ (Day/Night Cycles)   │      │ (Central Registry)   │
    └──────────┬───────────┘      └──────────┬───────────┘      └──────────┬───────────┘
               │                             │                             │
       ┌───────┴───────┐             ┌───────┴───────┐             ┌───────┴───────┐
       ▼               ▼             ▼               ▼             ▼               ▼
   Gameplay Cam   Mode Cam       Day Phase      Night Phase     Player/Chest     Kitchen/
   (Follow/Zoom) (Trophy/Ward)  (Farm/Cook)    (Brawl Waves)     Inventory       Crafting
```

---

## 2. Singleton Managers & Mesin State (Core State Machines)

### 2.1 CameraManager (`Features/Camera/CameraManager.cs`)
Pusat kendali tunggal (*single source of truth*) untuk seluruh kamera dalam game:
- **Mode Kamera (`CameraMode`)**:
  - `Gameplay`: Mengaktifkan `IsometricCameraController` untuk navigasi pemain bebas di dunia 3D.
  - `TrophyMode`: Mengunci pemain dan memposisikan kamera FP di depan `TrophyCabinetSystem`.
  - `WardrobeMode`: Memposisikan kamera menghadap lemari pakaian dan mengaktifkan render cermin.
- **Tanggung Jawab**:
  - Transisi kamera yang tervalidasi (mencegah lompatan langsung antar mode interaktif tanpa kembali ke `Gameplay`).
  - Mengunci input pemain via `PlayerControl.isInputLocked`.
  - Mengontrol visibilitas dan penguncian kursor (`Cursor.lockState` & `Cursor.visible`).

### 2.2 TimeManager (`Features/Time/TimeManager.cs`)
Singleton pengatur siklus waktu diskret (berbasis fase, bukan real-time clock):
- **Day Phase**: Eksplorasi, bercocok tanam (*farming*), memasak (*cooking*), upgrade perlengkapan, dan persiapan.
- **Night Phase**: Pertarungan malam (*night brawl*) melawan gelombang monster.
- **Transisi**: Interaksi dengan kasur tidur (`BedInteractable`) bertindak sebagai gerbang kemajuan hari (`AdvanceToNextDay()`).

### 2.3 ItemDatabase (`Features/Inventory/Data/ItemDatabase.cs`)
Registry pusat runtime yang dimuat secara otomatis dari `Resources/Database/ItemDatabase`:
- Menyimpan dan memetakan 33 item aktif ke dalam kamus `itemLookup` berbasis ID string yang cepat.
- Mengkategorikan item ke dalam: `Food`, `Crop`, `Seed`, `Material`, `MonsterDrop`, `Equipment`, dan `Trophy`.

---

## 3. Sistem Inventory & Penyimpanan (`Features/Inventory/`)

### 3.1 InventoryComponent (`InventoryComponent.cs`)
Komponen backend yang disematkan pada Player, Peti (`TestChest`), Kulkas (`Refrigerator`), dan Meja Cuci (`KitchenSink`):
- **Struktur Data**: `List<InventorySlot>` (pasangan `ItemData` dan integer `quantity`).
- **Operasi Backend**: `AddItem()`, `RemoveItem()`, `SwapSlots()`, `MoveItemToSlot()`, `TransferItemTo()`.
- **Enforcement Rules (`CanAcceptItem()`)**:
  - `blockTrophyItems`: Mencegah item piala masuk ke tas pemain (khusus ditaruh di rak/lemari trophy).
  - `allowedFoodCategories`: Membatasi tipe bahan makanan yang dapat masuk (digunakan oleh kulkas untuk sayur/buah).
- **Event-Driven**: Memancarkan event `OnInventoryChanged` dan `OnHotbarSelected` untuk rendering UI yang reaktif.

### 3.2 Hotbar System
- 4 slot pertama dari `InventoryComponent` pemain dialokasikan sebagai hotbar aktif.
- Pilihan slot dikendalikan oleh tombol angka `1-4` atau scroll wheel mouse.
- `PlayerEquipment` mendengarkan event ini dan memunculkan model 3D perlengkapan secara real-time di tangan pemain.

---

## 4. Sistem Dapur & Memasak (`Features/Kitchen/`)

### 4.1 Genshin-Style Cooking (`GenshinStove.cs` & `StoveUIManager.cs`)
- Menggantikan sistem stasiun masak lambat dengan antarmuka instan bergaya Genshin Impact.
- Membaca daftar resep `KitchenRecipe` yang mendukung multi-ingredient (`RecipeIngredient`).
- Validasi instan: mengecek ketersediaan bahan di inventory via `CountItem()`, mengonsumsi bahan via `RemoveItem()`, dan memberikan hasil masakan via `AddItem()`.
- UI menggunakan arsitektur **Single Central Axis** berbasis TextMeshPro untuk tampilan yang simetris dan tajam.

### 4.2 Sistem Cuci Bahan Dinamis (`KitchenSinkInteractable.cs`)
- Tidak lagi memerlukan asset `KitchenRecipe` terpisah untuk setiap bahan yang dicuci.
- **Item-Level Data**: `FoodItemData` dan `MaterialItemData` memiliki boolean `isDirty` dan referensi `cleanVariant`.
- Bak cuci secara otomatis membuat *virtual recipe* saat runtime untuk mengubah item kotor menjadi varian bersihnya.

### 4.3 Kulkas (`RefrigeratorInteractable.cs`)
- Komponen penyimpanan dengan filter kategori ketat: hanya menerima sayur, buah, daging, dan hidangan jadi.

---

## 5. Sistem Wardrobe & Kustomisasi (`Features/Wardrobe/`)

- **PlayerOutfit**: Komponen pada GameObject pemain yang menerapkan varian pakaian ke `SkinnedMeshRenderer` karakter melalui konfigurasi `OutfitData`.
- **OutfitPartResolver**: Modul logika murni untuk memetakan nama mesh/kategori pakaian ke varian renderer tanpa dependensi Unity lifecycle.
- **MirrorCamera**: Kamera off-screen khusus yang merender cermin kamar tidur secara live ke `RenderTexture` tanpa mengganggu viewport utama.

---

## 6. Sistem Kamera & Oklusi (`Features/Camera/`)

- **IsometricCameraController**:
  - Mengatur kamera isometrik berproyeksi ortografis/perspektif tinggi.
  - Fitur: Smooth follow ke pemain, orbit rotasi (tahan klik kanan mouse), dan zoom (scroll wheel).
  - Dilengkapi *Mode Guard*: hanya beroperasi saat `CameraManager.CurrentMode == Gameplay`.
- **WallOcclusionManager & WallOccluder**:
  - Menembakkan raycast dari kamera ke posisi pemain.
  - Tembok yang menghalangi pandangan kamera otomatis ditransparansikan via material fading (`transparentAlpha = 0.15`).

---

## 7. Sistem UI & Hirarki Navigasi Input

### 7.1 Canvas Scaler
- Resolusi Referensi: **1920 × 1080**
- Mode Skala: **Scale With Screen Size**
- Screen Match Mode: **Match Width Or Height (0.5)** untuk stabilitas tampilan di layar ultrawide maupun 16:10.

### 7.2 Hirarki Prioritas Tombol ESC (Modal Priority Stack)
Untuk mencegah bentrokan antara tombol keluar modal dan menu pause:
1. **Prioritas 1 (Modal Panel)**: Jika ada panel interaktif yang terbuka (`Panel_Stove`, `Panel_Sink`, `WardrobeUI`, `InventoryUI`, `ChestUI`), tombol `ESC` pertama HANYA menutup panel tersebut dan mengembalikan kursor/gameplay.
2. **Prioritas 2 (Pause Menu)**: Menu Pause (`PauseMenuUI`) HANYA akan muncul jika tombol `ESC` ditekan saat seluruh modal panel dalam keadaan tertutup.

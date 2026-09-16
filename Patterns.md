# Patterns.md

Standar penulisan kode, pola desain berulang (*design patterns*), dan pedoman anti-pattern dalam pengembangan proyek **Farm-Beware**.

---

## 1. Pola Arsitektur Berulang (Recurring Architectural Patterns)

### 1.1 Singleton dengan Fallback Resolver (Awake-Safe Singleton)
Digunakan pada manajer utama (`TimeManager`, `CameraManager`, `TrophySystemManager`) untuk memastikan akses instance tidak menghasilkan null bahkan saat dipanggil di luar urutan inisialisasi normal:

```csharp
private static T _instance;
public static T Instance
{
    get
    {
        if (_instance == null)
        {
            T[] found = FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (found != null && found.Length > 0)
                _instance = found[0];
        }
        return _instance;
    }
    private set { _instance = value; }
}
```

### 1.2 Pola Delegasi Sentral Kamera (Camera Delegation Pattern)
Semua modul fitur (Trophy, Wardrobe, Interaction) **dilarang** mengaktifkan atau mematikan kamera sendiri. Semua kontrol kamera wajib didelegasikan ke `CameraManager`:

```csharp
// Mengaktifkan mode interaktif
CameraManager.Instance.SetMode(CameraManager.CameraMode.WardrobeMode, wardrobeRootTransform);

// Keluar kembali ke mode penjelajahan
CameraManager.Instance.SetMode(CameraManager.CameraMode.Gameplay);
```

`CameraManager` menangani secara otomatis:
- Penonaktifan kamera lama & pengaktifan kamera baru.
- Penataan posisi kamera lokal terhadap root konteks.
- Penguncian input pemain (`PlayerControl.isInputLocked`).
- Pengaturan state kursor mouse (`Cursor.lockState` dan `Cursor.visible`).

### 1.3 Mode Guard pada Kamera Gameplay
Komponen kamera bebas wajib memiliki pengecekan guard di awal siklus frame agar tidak saling berebut kendali saat pemain berada dalam mode interaksi:

```csharp
void LateUpdate()
{
    if (CameraManager.Instance != null && 
        CameraManager.Instance.CurrentMode != CameraManager.CameraMode.Gameplay)
    {
        return;
    }

    // Eksekusi logic follow, orbit, dan zoom
}
```

### 1.4 Komunikasi UI Berbasis Event (Event-Driven UI)
Backend data (`InventoryComponent`, `KitchenStation`) memiliki data dan memancarkan event. Komponen UI murni bertindak sebagai penampil tanpa state ganda (*stateless renderer*):

```csharp
private void OnEnable()
{
    if (targetInventory != null)
        targetInventory.OnInventoryChanged += RefreshUI;
}

private void OnDisable()
{
    if (targetInventory != null)
        targetInventory.OnInventoryChanged -= RefreshUI;
}
```

### 1.5 Virtual Recipe Pattern (Item-Level Transformation)
Untuk mencegah ledakan file ScriptableObject resep satu-lawan-satu (seperti mencuci apel, mencuci kentang, dll.), data transformasi disimpan langsung pada item sumber:

```csharp
// Pada FoodItemData / MaterialItemData
public bool isDirty;
public ItemData cleanVariant;

// Pada stasiun pembersih (KitchenSinkInteractable)
if (foodItem.isDirty && foodItem.cleanVariant != null)
{
    // Buat virtual recipe secara dinamis saat runtime tanpa asset file terpisah
    var virtualRecipe = ScriptableObject.CreateInstance<KitchenRecipe>();
    virtualRecipe.SetProcess(foodItem, foodItem.cleanVariant, processTime: 2.0f);
    StartProcessing(virtualRecipe);
}
```

### 1.6 Dual-Inventory Pattern (Trophy System)
Pemisahan penyimpanan logis dari representasi fisik dunia 3D:
- **`CabinetInventory`**: Tempat penyimpanan data item piala yang belum dipajang.
- **`RackInventory`**: Sumber kebenaran (*source of truth*) visual untuk piala yang sedang dipajang pada `SnapPoint` 3D.
- Interaksi drag/drop atau klik raycast memicu transfer antar dua inventory ini tanpa menduplikasi objek.

---

## 2. Standar & Konvensi Penulisan Kode

### 2.1 Penamaan & Struktur Namespace
- Setiap modul fitur wajib berada dalam namespace spesifik di bawah `Features<NamaFitur>`:
  - `FeaturesCamera`
  - `FeaturesInteraction`
  - `FeaturesInventory`
  - `FeaturesKitchen`
  - `FeaturesTrophy`
  - `FeaturesWardrobe`
- Public Class / Struct / Enum / Method / Property: **`PascalCase`**
- Private Field: **`camelCase`** atau **`_camelCase`**
- Local Variable & Method Parameter: **`camelCase`**

### 2.2 Komponen UI Wajib Menggunakan TextMeshPro
- ❌ Dilarang menggunakan `UnityEngine.UI.Text` (legacy).
- ✅ Wajib menggunakan `TMPro.TextMeshProUGUI`.
- Gunakan perataan teks eksplisit (*Alignment*) dan bungkus teks panjang dengan mode *Ellipsis* atau *Truncate*.

### 2.3 Standar Input System Baru
- ❌ Hindari `Input.GetKeyDown(KeyCode.Escape)` atau API legacy lainnya.
- ✅ Gunakan New Input System API:
  ```csharp
  if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
  {
      ClosePanel();
  }
  ```

### 2.4 Resolusi Dependensi Defensif di `Awake()`
Jangan mengandalkan referensi Inspector saja untuk komponen yang berada pada GameObject yang sama:
```csharp
private void Awake()
{
    if (rb == null) rb = GetComponent<Rigidbody>();
    if (col == null) col = GetComponent<Collider>();
}
```

---

## 3. Daftar Larangan / Anti-Patterns (STRICT PROHIBITION)

1. ❌ **Mengubah State Kamera Secara Langsung** di luar `CameraManager.Instance.SetMode()`.
2. ❌ **Memanipulasi `PlayerControl.isInputLocked` Secara Manual** dari skrip interaksi (hanya boleh dikontrol oleh `CameraManager`).
3. ❌ **Polling Status Backend di Loop `Update()` UI** — Gunakan event callback (`OnInventoryChanged`, `OnProcessCompleted`).
4. ❌ **Melakukan Subskripsi Event Tanpa Unsubskripsi di `OnDisable()`** — Menyebabkan memory leak dan bug pemanggilan ganda.
5. ❌ **Menggunakan String/Angka Index Layer Hardcoded** — Selalu gunakan `LayerMask.NameToLayer("LayerName")` atau `LayerMask.GetMask("LayerName")`.
6. ❌ **Mematikan Kamera Menggunakan `gameObject.SetActive(false)`** — Matikan komponen kameranya saja (`camera.enabled = false`) agar tidak memicu konflik listener audio dan hierarchy overhead.
7. ❌ **Membuat Script Automation Sementara di Editor** — Dilarang membuat script penataan scene di `Assets/Editor/*Setup*.cs` yang dapat merusak struktur hierarki secara otomatis. Gunakan MCP tools terukur.

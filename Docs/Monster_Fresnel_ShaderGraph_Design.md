# Monster Fresnel Rim Light - Shader Graph Design & Architecture

Dokumentasi rancangan visual, diagram node, dan formula matematis untuk mengimplementasikan atau mereplikasi efek **Fresnel Rim Light** pada monster menggunakan **Unity Shader Graph** (Universal Target - Lit).

---

## 1. Tujuan Visual & Logika Matematika

Pada gameplay Action Survival malam hari, latar belakang (tanah, rumput, bayangan malam) memiliki nilai luminansi yang rendah. Tanpa rim lighting, siluet monster akan membaur dengan kegelapan (*loss of combat readability*).

Lapisan **Fresnel Rim Light** menghasilkan pendaran intensitas tinggi (*grazing-angle emission*) hanya pada poligon di tepian siluet monster:

$$\text{NdotV} = \text{saturate}(\text{dot}(\mathbf{N}_{WS}, \mathbf{V}_{WS}))$$
$$\text{Rim} = \text{pow}(\text{saturate}(1.0 - \text{NdotV}), \text{FresnelPower}) \times \text{FresnelIntensity}$$
$$\text{Emission}_{Final} = \text{BaseEmission} + (\text{FresnelColor} \times \text{Rim})$$

- Di tengah model: $\mathbf{N} \cdot \mathbf{V} \approx 1.0 \rightarrow 1.0 - 1.0 = 0.0$ (tidak ada pendaran rim, warna tubuh asli tetap jernih).
- Di tepian model: $\mathbf{N} \cdot \mathbf{V} \approx 0.0 \rightarrow 1.0 - 0.0 = 1.0$ (pendaran rim penuh, menghasilkan garis siluet tajam).

---

## 2. Diagram Alur Node Shader Graph (Node Graph Map)

```
[Normal Vector (World)] ─────┐
                             ├──> [Dot Product] ──> [Saturate] ──> [One Minus] ──┐
[View Direction (World)] ────┘                                                   │
                                                                                 ▼
[_FresnelPower (Float)] ───────────────────────────────────────────────────> [Power]
                                                                                 │
                                                                                 ▼
[_FresnelColor (HDR Color)] ──────────────────────────────────────────────> [Multiply]
                                                                                 │
                                                                                 ▼
[_EmissionColor (Color)] ─────────────────────────────────────────────────> [Add]
                                                                                 │
                                                                                 ▼
                                                          [Universal Target: Emission Slot]
```

---

## 3. Langkah Rekonstruksi di Unity Shader Graph Editor

Bagi developer yang ingin membuat file `.shadergraph` secara visual di Editor:

1. **Buat Asset**:
   - Klik kanan di Project View $\rightarrow$ **Create > Shader Graph > URP > Lit Shader Graph**.
   - Beri nama `SG_MonsterFresnelLit`.
2. **Buka Blackboard** dan buat parameter berikut:
   - `Base Color` (`Color`, default: Dark Magenta/Yam `(0.55, 0.15, 0.35, 1.0)`)
   - `Base Map` (`Texture2D`, default: White)
   - `Smoothness` (`Float`, Slider `0.0 - 1.0`, default: `0.35`)
   - `Metallic` (`Float`, Slider `0.0 - 1.0`, default: `0.0`)
   - `Fresnel Color` (`Color`, Mode: **HDR**, default: Bright Pink/Magenta `(1.8, 0.3, 1.2, 1.0)`)
   - `Fresnel Power` (`Float`, Slider `0.5 - 10.0`, default: `3.5`)
   - `Fresnel Intensity` (`Float`, Slider `0.0 - 5.0`, default: `2.0`)
   - `Emission Color` (`Color`, Mode: **HDR**, default: Black)
3. **Konfigurasi Graph Inspector**:
   - Target: **Universal**
   - Material: **Lit**
   - Workflow: **Metallic**
   - Surface Type: **Opaque**
   - Blend Mode: **Alpha**
   - Two Sided: Nonaktif
4. **Sambungkan Node Sesuai Diagram**:
   - Buat node **Normal Vector** (Space: World).
   - Buat node **View Direction** (Space: World).
   - Sambungkan keduanya ke node **Dot Product**.
   - Sambungkan output Dot Product ke node **Saturate**.
   - Sambungkan ke node **One Minus**.
   - Buat node **Power**, masukkan output *One Minus* ke slot `A`, dan parameter `_FresnelPower` ke slot `B`.
   - Buat node **Multiply**, kalikan output *Power* dengan parameter `_FresnelColor` dan `_FresnelIntensity`.
   - Tambahkan dengan `_EmissionColor` menggunakan node **Add**.
   - Sambungkan output akhir ke port **Emission** pada Master Stack.
5. **Simpan Asset**: Klik **Save Asset**.

---

## 4. Palet Warna Rim per Varian Musuh (Enemy Visual Presets)

| Varian Musuh | Body Color | Fresnel Rim Color (HDR) | Fresnel Power | Karakter Siluet |
|---|---|---|---|---|
| **Tuber Maw** | Dark Yam Purple `(0.55, 0.15, 0.35)` | Neon Magenta `(1.8, 0.2, 1.3)` | `3.5` | Gesit, tajam, mudah terlihat di rumput |
| **Cyclops Tuber Maw (Boss)** | Deep Blood Purple `(0.40, 0.05, 0.20)` | Menacing Blood Crimson `(2.5, 0.1, 0.2)` | `2.8` | Siluet masif intimidatif |
| **Taro Brute** | Muddy Taro Earth `(0.45, 0.35, 0.30)` | Hardened Earthen Amber `(1.6, 0.9, 0.2)` | `4.0` | Rim tebal seperti batu berlapis |
| **Taro Colossus (Boss)** | Granite Brown `(0.30, 0.25, 0.25)` | Molten Core Gold `(2.2, 1.2, 0.3)` | `3.0` | Siluet raksasa tahan banting |
| **Corn Musketeer** | Golden Corn Yellow `(0.95, 0.80, 0.15)` | Electric Lime Cyan `(0.3, 1.8, 1.2)` | `4.2` | Tembakan jarak jauh mudah diantisipasi |
| **The Ranger (Boss)** | Vibrant Amber Tower `(0.90, 0.65, 0.10)` | Radiant Solar Flare `(2.5, 1.5, 0.1)` | `2.5` | Menara statis yang mendominasi horizon |

---

## 5. Integrasi URP Deferred+ & Penanganan Pass `UniversalForwardOnly`

> [!IMPORTANT]
> **Peringatan Kritis Arsitektur Deferred+**:
> Pada rendering path **Deferred+** (`RenderingMode.DeferredPlus`), shader HLSL maupun Shader Graph yang merender objek Opaque dengan kalkulasi forward custom **WAJIB** menggunakan pass dengan tag:
> ```hlsl
> Tags { "LightMode" = "UniversalForwardOnly" }
> ```
> 
> ### Mengapa BUKAN `UniversalForward`?
> - Jika menggunakan `LightMode = "UniversalForward"` pada material `RenderType = "Opaque"`:
>   1. GBuffer Pass melewatkannya karena bukan `UniversalGBuffer`.
>   2. Forward-Only Opaque Pass (`m_RenderOpaqueForwardOnlyPass`) di Deferred+ **hanya** menerima tag `UniversalForwardOnly` (dan `SRPDEFAULTUNLIT`), sehingga pass `UniversalForward` **dibuang dari render loop**.
>   3. Akibatnya, piksel tubuh monster **tidak pernah digambar ke layar** (tampak gaib / tembus pandang).
>   4. Namun pass `ShadowCaster` tetap dieksekusi oleh shadow mapper, menyebabkan gejala: **"Monster tembus pandang, hanya bayangannya yang terlihat"**.
> - Dengan menetapkan `LightMode = "UniversalForwardOnly"`, URP Deferred+ secara otomatis mengeksekusi pass forward objek ini di atas buffer GBuffer, sehingga tubuh monster tampil solid dengan pendaran rim HDR yang tajam sekaligus tetap memproyeksikan bayangan fisik ke tanah.

